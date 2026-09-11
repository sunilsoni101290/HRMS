using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    /// No method here ever calls SaveChangesAsync/INSERT/UPDATE/DELETE
    /// against the eSSL side. GetDeviceLogsAsync/DiscoverDeviceLogTablesAsync
    /// use raw ADO.NET (Microsoft.Data.SqlClient) rather than EF Core,
    /// because the set of physical tables to read from is only known at
    /// runtime (DeviceLogs plus zero or more discovered DeviceLogs_M_YYYY
    /// partitions) - EF Core requires each DbSet to map onto one fixed table
    /// name, so it cannot express "UNION ALL across a dynamically-discovered
    /// table list" the way a hand-built parameterized query can. Every
    /// table name that reaches that dynamic SQL is validated against
    /// EsslDeviceLogTableName's whitelist first (defense in depth: also
    /// re-validated here, not just trusted from the caller) - only ever
    /// "DeviceLogs" or "DeviceLogs_&lt;1-12&gt;_&lt;yyyy&gt;" is accepted,
    /// and every value (dates, batch size, cursor) is passed as a SQL
    /// parameter, never concatenated.
    /// </summary>
    public class EsslAttendanceDataSource : IEsslAttendanceDataSource
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _configuration;
        private readonly IDataProtector _protector;
        private readonly ILogger<EsslAttendanceDataSource> _logger;

        // Columns selected from DeviceLogs (and, by assumption, every
        // identically-shaped monthly partition - see EsslDeviceLogRaw's
        // remarks). C1-C7 read via CONVERT(nvarchar(50), ...) so an unknown
        // underlying column type on a given installation can never break the
        // query. Never "SELECT *" (requirement #26).
        private const string DeviceLogColumns =
            "[DeviceLogId], [DeviceId], [UserId], [LogDate], [DownloadDate], [Direction], [AttDirection], [WorkCode], " +
            "CONVERT(nvarchar(50), [C1]) AS [C1], CONVERT(nvarchar(50), [C2]) AS [C2], CONVERT(nvarchar(50), [C3]) AS [C3], " +
            "CONVERT(nvarchar(50), [C4]) AS [C4], CONVERT(nvarchar(50), [C5]) AS [C5], CONVERT(nvarchar(50), [C6]) AS [C6], " +
            "CONVERT(nvarchar(50), [C7]) AS [C7]";

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

        public async Task<List<string>> DiscoverDeviceLogTablesAsync(
            string tenantId,
            DateTime fromDateInclusive,
            DateTime toDateExclusive,
            CancellationToken ct = default)
        {
            var connectionString = await BuildConnectionStringAsync(tenantId, ct);

            if (connectionString == null)
                return new List<string>();

            try
            {
                return await DiscoverDeviceLogTablesByConnectionAsync(connectionString, fromDateInclusive, toDateExclusive, ct);
            }
            catch (Exception ex)
            {
                // Requirement: a single bad/unreachable step must never take
                // down the whole sync - discovery failing degrades to "no
                // tables this run" (the caller logs/reports that), not a
                // thrown exception.
                _logger.LogError(ex, "eSSL DiscoverDeviceLogTablesAsync failed for tenant {TenantId}.", tenantId);
                return new List<string>();
            }
        }

        /// <summary>
        /// Shared metadata-only discovery (sys.tables/sys.schemas) used by
        /// both the public per-tenant DiscoverDeviceLogTablesAsync and
        /// TestRawConnectionAsync (which already has a resolved connection
        /// string in hand and would otherwise need a second
        /// EsslIntegrationSettings lookup). Never reads a single row of
        /// DeviceLogs data, never creates/alters anything. Callers are
        /// responsible for catching/logging - this method lets exceptions
        /// propagate so each caller can decide its own failure behavior.
        /// </summary>
        private static async Task<List<string>> DiscoverDeviceLogTablesByConnectionAsync(
            string connectionString,
            DateTime fromDateInclusive,
            DateTime toDateExclusive,
            CancellationToken ct)
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(ct);

            using var command = new SqlCommand(
                "SELECT t.name FROM sys.tables t " +
                "INNER JOIN sys.schemas s ON t.schema_id = s.schema_id " +
                "WHERE s.name = 'dbo' AND (" +
                "t.name = 'DeviceLogs' OR t.name = 'Device_Logs' OR " +
                "t.name LIKE 'DeviceLogs[_]%' OR t.name LIKE 'Device[_]Logs[_]%')",
                connection);
            command.CommandTimeout = 30;

            var discovered = new List<string>();

            using (var reader = await command.ExecuteReaderAsync(ct))
            {
                while (await reader.ReadAsync(ct))
                {
                    var name = reader.GetString(0);

                    // Re-validated here (not just trusted from the LIKE
                    // filter above, which is only a cheap pre-filter) - this
                    // is the actual whitelist gate before a name can ever
                    // reach dynamic SQL.
                    if (EsslDeviceLogTableName.IsValidTableName(name))
                        discovered.Add(name);
                }
            }

            var result = new List<string>();

            // The bare device-log table, if present (whichever real-world
            // spelling this installation uses - "DeviceLogs" or
            // "Device_Logs"), is always included regardless of the
            // requested window - installations that never adopted monthly
            // partitioning keep all history there, and even partitioned
            // installations may still land the current, not-yet-archived
            // month's rows there.
            foreach (var name in discovered)
            {
                if (EsslDeviceLogTableName.IsBaseTableName(name))
                {
                    result.Add(name);
                    continue;
                }

                if (!EsslDeviceLogTableName.TryParseMonthlyTable(name, out var year, out var month))
                    continue;

                if (EsslDeviceLogTableName.MonthOverlapsRange(year, month, fromDateInclusive, toDateExclusive))
                    result.Add(name);
            }

            return result;
        }

        public async Task<List<EsslDeviceLogRaw>> GetDeviceLogsAsync(
            string tenantId,
            IReadOnlyList<string> sourceTables,
            DateTime fromDateInclusive,
            DateTime toDateExclusive,
            EsslDeviceLogCursor? afterCursor,
            int batchSize,
            CancellationToken ct = default)
        {
            var results = new List<EsslDeviceLogRaw>();

            // Only ever validated, whitelisted identifiers reach the SQL
            // text below - anything else is dropped rather than trusted.
            var validTables = (sourceTables ?? Array.Empty<string>())
                .Where(EsslDeviceLogTableName.IsValidTableName)
                .Distinct()
                .ToList();

            if (validTables.Count == 0)
                return results;

            var connectionString = await BuildConnectionStringAsync(tenantId, ct);

            if (connectionString == null)
                return results;

            // UNION ALL across every validated table, each branch filtered
            // by the same LogDate range, then re-sorted and re-paged as one
            // combined result set (SQL Server does this for us via the
            // outer ORDER BY/OFFSET-FETCH over the UNION ALL). SourceTable is
            // a literal per branch so the caller can tell which physical
            // table each row came from - required for idempotency (see
            // EsslDeviceLogRaw/EsslDeviceLogCursor remarks).
            var unionParts = new List<string>();
            for (int i = 0; i < validTables.Count; i++)
            {
                unionParts.Add(
                    $"SELECT {DeviceLogColumns}, '{validTables[i].Replace("'", "''")}' AS [SourceTable] " +
                    $"FROM {EsslDeviceLogTableName.ToBracketedIdentifier(validTables[i])} " +
                    "WHERE [LogDate] >= @FromDate AND [LogDate] < @ToDate");
            }

            var sql = new StringBuilder();
            sql.Append("SELECT * FROM (");
            sql.Append(string.Join(" UNION ALL ", unionParts));
            sql.Append(") AS Combined ");

            if (afterCursor != null)
            {
                sql.Append(
                    "WHERE ([LogDate] > @CursorLogDate) " +
                    "OR ([LogDate] = @CursorLogDate AND [SourceTable] > @CursorSourceTable) " +
                    "OR ([LogDate] = @CursorLogDate AND [SourceTable] = @CursorSourceTable AND [DeviceLogId] > @CursorDeviceLogId) ");
            }

            sql.Append("ORDER BY [LogDate] ASC, [SourceTable] ASC, [DeviceLogId] ASC ");
            sql.Append("OFFSET 0 ROWS FETCH NEXT @BatchSize ROWS ONLY;");

            try
            {
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync(ct);

                using var command = new SqlCommand(sql.ToString(), connection);
                command.CommandTimeout = 60;

                command.Parameters.AddWithValue("@FromDate", fromDateInclusive);
                command.Parameters.AddWithValue("@ToDate", toDateExclusive);
                command.Parameters.AddWithValue("@BatchSize", batchSize);

                if (afterCursor != null)
                {
                    command.Parameters.AddWithValue("@CursorLogDate", afterCursor.LogDate);
                    command.Parameters.AddWithValue("@CursorSourceTable", afterCursor.SourceTable);
                    command.Parameters.AddWithValue("@CursorDeviceLogId", afterCursor.DeviceLogId);
                }

                using var reader = await command.ExecuteReaderAsync(ct);

                while (await reader.ReadAsync(ct))
                {
                    results.Add(new EsslDeviceLogRaw
                    {
                        DeviceLogId = reader.GetInt32(reader.GetOrdinal("DeviceLogId")),
                        DeviceId = reader.GetInt32(reader.GetOrdinal("DeviceId")),
                        UserId = reader.IsDBNull(reader.GetOrdinal("UserId")) ? "" : reader.GetString(reader.GetOrdinal("UserId")),
                        LogDate = reader.GetDateTime(reader.GetOrdinal("LogDate")),
                        DownloadDate = reader.IsDBNull(reader.GetOrdinal("DownloadDate")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("DownloadDate")),
                        Direction = reader.IsDBNull(reader.GetOrdinal("Direction")) ? null : reader.GetString(reader.GetOrdinal("Direction")),
                        AttDirection = reader.IsDBNull(reader.GetOrdinal("AttDirection")) ? null : reader.GetString(reader.GetOrdinal("AttDirection")),
                        WorkCode = reader.IsDBNull(reader.GetOrdinal("WorkCode")) ? null : reader.GetString(reader.GetOrdinal("WorkCode")),
                        C1 = GetNullableString(reader, "C1"),
                        C2 = GetNullableString(reader, "C2"),
                        C3 = GetNullableString(reader, "C3"),
                        C4 = GetNullableString(reader, "C4"),
                        C5 = GetNullableString(reader, "C5"),
                        C6 = GetNullableString(reader, "C6"),
                        C7 = GetNullableString(reader, "C7"),
                        SourceTable = reader.GetString(reader.GetOrdinal("SourceTable"))
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "eSSL GetDeviceLogsAsync failed for tenant {TenantId} against tables [{Tables}].",
                    tenantId, string.Join(", ", validTables));
                throw;
            }

            return results;
        }

        private static string? GetNullableString(SqlDataReader reader, string columnName)
        {
            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
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
        // CONNECTION RESOLUTION (single place all public methods above
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

                // Bonus diagnostic (best-effort, never fails the test) -
                // lets an admin immediately see whether monthly partition
                // tables are present without leaving the Test Connection
                // button.
                string tableSummary;
                try
                {
                    var now = DateTime.Now;
                    var tables = await DiscoverDeviceLogTablesByConnectionAsync(connectionString, now.AddMonths(-1), now.AddDays(1), ct);
                    tableSummary = tables.Count == 0
                        ? " No DeviceLogs tables were found."
                        : $" Tables found: {string.Join(", ", tables)}.";
                }
                catch
                {
                    tableSummary = "";
                }

                return sample == null
                    ? (true, "Database connection successful. DeviceLogs table is reachable but currently empty." + tableSummary)
                    : (true, $"Database connection successful. Latest DeviceLogId = {sample.DeviceLogId}, LogDate = {sample.LogDate:yyyy-MM-dd HH:mm:ss}." + tableSummary);
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

        private static string? ExtractServerName(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return null;

            try
            {
                var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
                return builder.DataSource;
            }
            catch
            {
                // Never let a malformed connection string leak into an
                // exception message shown anywhere near the UI.
                return "(configured)";
            }
        }

        private static string Truncate(string? value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value ?? "";
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }
}
