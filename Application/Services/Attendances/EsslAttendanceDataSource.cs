using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Interfaces.Attendances;
using Domain.Entities;
using Domain.Helper;
using Infrastructure;
using Infrastructure.EsslIntegration;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Application.Services.Attendances
{
    /// <summary>
    /// SELECT-only implementation of IEsslAttendanceDataSource against the
    /// eTimeTrackLite1 database. The connection is now built PER CALL from
    /// that tenant's persisted EsslIntegrationSetting row (see
    /// BuildConnectionStringAsync) rather than a single fixed connection
    /// string resolved once at app startup - this is what makes the new
    /// editable Settings form actually take effect without an app restart.
    /// Falls back to the legacy "EsslDatabase:*" appsettings.json section
    /// only when no row has been saved yet for a tenant, so an existing
    /// deployment that never opens the new Settings form keeps working
    /// exactly as before.
    ///
    /// No method here ever calls SaveChangesAsync against the eSSL side -
    /// EsslDbContext instances built in this class are always short-lived,
    /// read-only, AsNoTracking, and disposed at the end of each call.
    /// </summary>
    public class EsslAttendanceDataSource : IEsslAttendanceDataSource
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _configuration;
        private readonly IDataProtector _protector;
        private readonly ILogger<EsslAttendanceDataSource> _logger;

        public EsslAttendanceDataSource(
            ApplicationDbContext db,
            IConfiguration configuration,
            IDataProtectionProvider dataProtectionProvider,
            ILogger<EsslAttendanceDataSource> logger)
        {
            _db = db;
            _configuration = configuration;
            // A dedicated purpose string scopes this protector to exactly
            // this use - the same pattern ASP.NET Core's own docs recommend
            // for any single kind of secret (never reuse a purpose string
            // across unrelated data).
            _protector = dataProtectionProvider.CreateProtector("EsslIntegration.DatabasePassword.v1");
            _logger = logger;
        }

        public async Task<bool> IsEnabledAsync(string tenantId, CancellationToken ct = default)
        {
            var setting = await _db.EsslIntegrationSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId, ct);

            if (setting != null)
                return setting.IntegrationEnabled;

            // Legacy fallback - no row saved yet for this tenant.
            return _configuration.GetValue<bool?>("EsslDatabase:Enabled") ?? false;
        }

        public async Task<(bool Success, string Message)> TestConnectionAsync(string tenantId, CancellationToken ct = default)
        {
            var connectionString = await BuildConnectionStringAsync(tenantId, ct);

            if (connectionString == null)
                return (false, "eSSL database connection is not configured yet.");

            return await TestRawConnectionAsync(connectionString, ct);
        }

        public async Task<(bool Success, string Message)> TestConnectionAsync(EsslConnectionParameters parameters, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(parameters.DatabaseServer))
                return (false, "Database server is required.");

            if (string.IsNullOrWhiteSpace(parameters.DatabaseName))
                return (false, "Database name is required.");

            if (parameters.AuthenticationType == EsslAuthenticationTypes.Sql &&
                string.IsNullOrWhiteSpace(parameters.Username))
                return (false, "Username is required for SQL Server Authentication.");

            var connectionString = BuildConnectionString(
                parameters.DatabaseServer,
                parameters.DatabaseName,
                parameters.AuthenticationType,
                parameters.Username,
                parameters.Password,
                parameters.ConnectionTimeout);

            return await TestRawConnectionAsync(connectionString, ct);
        }

        public async Task<List<EsslDeviceLogRaw>> GetDeviceLogsAsync(
            string tenantId,
            DateTime fromDateInclusive,
            DateTime toDateExclusive,
            int afterDeviceLogId,
            int batchSize,
            CancellationToken ct = default)
        {
            var connectionString = await BuildConnectionStringAsync(tenantId, ct);

            if (connectionString == null)
                return new List<EsslDeviceLogRaw>();

            using var esslDb = CreateContext(connectionString);

            // LogDate range (indexed, requirement #26) + DeviceLogId keyset
            // (requirement #27 - "ORDER BY on an indexed column", plus this
            // is what makes repeated batches within one run advance instead
            // of re-fetching the same rows). NOT "SELECT *" - only the
            // columns EsslDeviceLogRaw declares are ever read (no image blob,
            // no lat/long).
            return await esslDb.DeviceLogs
                .AsNoTracking()
                .Where(x =>
                    x.LogDate >= fromDateInclusive &&
                    x.LogDate < toDateExclusive &&
                    x.DeviceLogId > afterDeviceLogId)
                .OrderBy(x => x.DeviceLogId)
                .Take(batchSize)
                .ToListAsync(ct);
        }

        public async Task<Dictionary<string, EsslEmployeeRaw>> GetEmployeeNamesAsync(
            string tenantId,
            IEnumerable<string> deviceUserIds,
            CancellationToken ct = default)
        {
            var codes = deviceUserIds?.Distinct().ToList() ?? new List<string>();

            if (codes.Count == 0)
                return new Dictionary<string, EsslEmployeeRaw>();

            var connectionString = await BuildConnectionStringAsync(tenantId, ct);

            if (connectionString == null)
                return new Dictionary<string, EsslEmployeeRaw>();

            try
            {
                using var esslDb = CreateContext(connectionString);

                var rows = await esslDb.Employees
                    .AsNoTracking()
                    .Where(e => codes.Contains(e.EmployeeCodeInDevice))
                    .ToListAsync(ct);

                // A device UserId should map to exactly one eTimeTrackLite
                // employee, but this is someone else's database - be
                // defensive about duplicates rather than letting
                // ToDictionary throw and take down the whole report.
                return rows
                    .GroupBy(r => r.EmployeeCodeInDevice)
                    .ToDictionary(g => g.Key, g => g.First());
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "eSSL GetEmployeeNamesAsync failed - Unmapped Employees report will show raw codes only.");
                return new Dictionary<string, EsslEmployeeRaw>();
            }
        }

        // ==================================================================
        // CONNECTION RESOLUTION (single place all four public methods above
        // funnel through - requirement #10: "Do not duplicate SQL
        // connection-building logic")
        // ==================================================================

        /// <summary>Resolves this tenant's connection string from its persisted EsslIntegrationSetting row, decrypting the password if SQL auth. Returns null only when neither a saved row nor a legacy appsettings connection string exists at all.</summary>
        private async Task<string?> BuildConnectionStringAsync(string tenantId, CancellationToken ct)
        {
            var setting = await _db.EsslIntegrationSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId, ct);

            if (setting == null)
            {
                // Legacy fallback - the pre-Settings-form deployment path.
                var legacy = _configuration.GetValue<string>("EsslDatabase:ConnectionString");
                return string.IsNullOrWhiteSpace(legacy) ? null : legacy;
            }

            string? password = null;

            if (setting.AuthenticationType == EsslAuthenticationTypes.Sql && !string.IsNullOrEmpty(setting.EncryptedPassword))
            {
                try
                {
                    password = _protector.Unprotect(setting.EncryptedPassword);
                }
                catch (Exception ex)
                {
                    // Never let a corrupted/undecryptable secret surface as
                    // a raw exception (and never log the ciphertext or any
                    // derived value that could help guess the plaintext).
                    _logger.LogError(ex, "eSSL: failed to decrypt stored database password for tenant {TenantId}.", tenantId);
                    return null;
                }
            }

            return BuildConnectionString(
                setting.DatabaseServer,
                setting.DatabaseName,
                setting.AuthenticationType,
                setting.Username,
                password,
                setting.ConnectionTimeout);
        }

        /// <summary>The ONE place a SqlConnectionStringBuilder is ever constructed for this integration.</summary>
        private static string BuildConnectionString(
            string server,
            string database,
            string authenticationType,
            string? username,
            string? password,
            int connectionTimeoutSeconds)
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = server,
                InitialCatalog = database,
                TrustServerCertificate = true,
                ConnectTimeout = connectionTimeoutSeconds > 0 ? connectionTimeoutSeconds : 15
            };

            if (authenticationType == EsslAuthenticationTypes.Windows)
            {
                builder.IntegratedSecurity = true;
            }
            else
            {
                builder.IntegratedSecurity = false;
                builder.UserID = username ?? "";
                builder.Password = password ?? "";
            }

            return builder.ConnectionString;
        }

        private static EsslDbContext CreateContext(string connectionString)
        {
            var options = new DbContextOptionsBuilder<EsslDbContext>()
                .UseSqlServer(connectionString, sqlOptions => sqlOptions.CommandTimeout(60))
                .Options;

            return new EsslDbContext(options);
        }

        /// <summary>
        /// A trivial SELECT (TOP 1 from DeviceLogs) - proves connection
        /// string, network reachability, and SQL login/permissions all work
        /// without touching any real row data. Never throws; maps common
        /// SqlException error numbers to a friendly, non-sensitive message
        /// (requirement #6/#19 - never expose the connection string or
        /// credentials in the response).
        /// </summary>
        private async Task<(bool Success, string Message)> TestRawConnectionAsync(string connectionString, CancellationToken ct)
        {
            try
            {
                using var esslDb = CreateContext(connectionString);

                var canConnect = await esslDb.Database.CanConnectAsync(ct);

                if (!canConnect)
                    return (false, "Unable to connect to the database. Please verify the server, database, authentication, and credentials.");

                var sample = await esslDb.DeviceLogs
                    .AsNoTracking()
                    .OrderByDescending(x => x.DeviceLogId)
                    .Select(x => new { x.DeviceLogId, x.LogDate })
                    .FirstOrDefaultAsync(ct);

                return sample == null
                    ? (true, "Database connection successful. DeviceLogs table is reachable but currently empty.")
                    : (true, $"Database connection successful. Latest DeviceLogId = {sample.DeviceLogId}, LogDate = {sample.LogDate:yyyy-MM-dd HH:mm:ss}.");
            }
            catch (SqlException sqlEx)
            {
                // Never include ex.Message verbatim here - it can echo back
                // the server name/login in some driver versions. Map the
                // handful of common, genuinely useful cases only.
                _logger.LogError(sqlEx, "eSSL TestConnectionAsync failed (SqlException {Number}).", sqlEx.Number);

                var message = sqlEx.Number switch
                {
                    18456 => "Login failed. Please verify the username and password.",
                    4060 => "Database does not exist or is not accessible with the supplied credentials.",
                    -2 => "The connection attempt timed out. Please verify the server name and network path.",
                    53 or -1 or 11001 => "SQL Server could not be reached. Please verify the server name and that it is network-reachable.",
                    18452 => "Login failed - the supplied account is not a valid SQL Server login (check Authentication Type).",
                    _ => "Unable to connect to the database. Please verify the server, database, authentication, and credentials."
                };

                return (false, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "eSSL TestConnectionAsync failed (non-SQL exception).");
                return (false, "Unable to connect to the database. Please verify the server, database, authentication, and credentials.");
            }
        }
    }
}
