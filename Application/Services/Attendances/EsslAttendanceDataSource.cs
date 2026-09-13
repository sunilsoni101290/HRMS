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
        // remarks). C1-C7, UserId, Direction, AttDirection and WorkCode are
        // ALL read via CONVERT(nvarchar(...), ...) rather than selected raw.
        //
        // ROOT-CAUSE FIX: this used to select UserId/Direction/AttDirection/
        // WorkCode with their native type. Several monthly partition tables
        // are UNION ALL'd together in GetDeviceLogsAsync - if even ONE of
        // those tables happened to store, say, UserId as int while the
        // others store it as varchar (observed to drift across
        // eTimeTrackLite1 installs/months - the vendor tool recreates these
        // monthly tables and does not always do so identically), SQL Server
        // resolves the UNION ALL's column type once for the whole query via
        // data type precedence, then implicitly converts every branch's
        // value to that one type. A value that doesn't convert cleanly
        // (e.g. a non-numeric UserId being implicitly converted to int
        // because another table's column is int) throws a SQL-side
        // conversion error for the WHOLE batch - not a single bad row - and
        // that failure previously escaped as a batch-level exception,
        // aborting the entire sync run after whatever had already committed
        // (exactly the "found 303, then nothing" shape). Converting every
        // one of these columns to nvarchar in the SQL itself, per branch,
        // means the UNION ALL is always type-safe regardless of how any one
        // installation's monthly table happens to be typed. DeviceLogId/
        // DeviceId/LogDate are left as their native types deliberately (they
        // drive ordering/pagination and duplicate-detection and must sort
        // correctly), and are read defensively in C# instead - see
        // GetInt32Flexible. Never "SELECT *" (requirement #26).
        private const string DeviceLogColumns =
            "[DeviceLogId], [DeviceId], " +
            "CONVERT(nvarchar(50), [UserId]) AS [UserId], [LogDate], [DownloadDate], " +
            "CONVERT(nvarchar(50), [Direction]) AS [Direction], CONVERT(nvarchar(50), [AttDirection]) AS [AttDirection], " +
            "CONVERT(nvarchar(50), [WorkCode]) AS [WorkCode], " +
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
            string? connectionString;

            try
            {
                connectionString = await BuildConnectionStringAsync(tenantId, ct);
            }
            catch (InvalidOperationException ex)
            {
                // The one expected failure mode of BuildConnectionStringAsync
                // (an undecryptable saved password) - return it as a normal
                // (false, message) result like every other Test Connection
                // failure, instead of letting it surface as an unhandled 500.
                return (false, ex.Message);
            }

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

                // ROOT-CAUSE FIX: this used to build every row with hard
                // reader.GetString calls on UserId/Direction/AttDirection/
                // WorkCode. Several monthly partition tables are UNION ALL'd
                // together - if even one of those tables happened to store
                // one of those columns with a slightly different underlying
                // type (observed to drift across eTimeTrackLite1 installs/
                // months), a hard GetString could throw InvalidCastException
                // for just that one row. That exception used to escape this
                // whole while loop, propagate out of GetDeviceLogsAsync into
                // SyncAsync's outer catch, and end the ENTIRE sync run as
                // "Failed" - but every batch already committed via
                // SaveChangesAsync earlier in the run stays imported. That
                // is exactly a "303 imported out of 2671 eligible, then the
                // run just stops" shape: everything AFTER the first bad
                // value never even gets fetched, let alone imported.
                //
                // Fix, in two parts:
                //  1. DeviceLogColumns (above) now CONVERTs UserId/Direction/
                //     AttDirection/WorkCode/C1-C7 to nvarchar in the SQL
                //     itself, so a type mismatch across the UNION ALL'd
                //     tables can no longer happen for these columns at all.
                //  2. As defense in depth, every non-identity field below is
                //     still read through a try/catch that logs and falls
                //     back to null/"" instead of throwing. Identity/cursor
                //     columns (DeviceLogId, DeviceId, LogDate, SourceTable)
                //     are deliberately NOT swallowed this way: they drive
                //     keyset pagination and duplicate detection, so a row
                //     with a genuinely unreadable identity is a real,
                //     visible failure (caught by the outer catch below,
                //     which is what MUST happen so it is never silently
                //     miscounted as "no more data" and does not shift the
                //     batch size the caller uses for loop-termination and
                //     cursor advancement).
                while (await reader.ReadAsync(ct))
                {
                    results.Add(new EsslDeviceLogRaw
                    {
                        DeviceLogId = GetInt32Flexible(reader, "DeviceLogId"),
                        DeviceId = GetInt32Flexible(reader, "DeviceId"),
                        LogDate = reader.GetDateTime(reader.GetOrdinal("LogDate")),
                        SourceTable = reader.GetString(reader.GetOrdinal("SourceTable")),
                        UserId = SafeGetNullableString(reader, "UserId", tenantId, _logger) ?? "",
                        DownloadDate = SafeGetNullableDateTime(reader, "DownloadDate", tenantId, _logger),
                        Direction = SafeGetNullableString(reader, "Direction", tenantId, _logger),
                        AttDirection = SafeGetNullableString(reader, "AttDirection", tenantId, _logger),
                        WorkCode = SafeGetNullableString(reader, "WorkCode", tenantId, _logger),
                        C1 = SafeGetNullableString(reader, "C1", tenantId, _logger),
                        C2 = SafeGetNullableString(reader, "C2", tenantId, _logger),
                        C3 = SafeGetNullableString(reader, "C3", tenantId, _logger),
                        C4 = SafeGetNullableString(reader, "C4", tenantId, _logger),
                        C5 = SafeGetNullableString(reader, "C5", tenantId, _logger),
                        C6 = SafeGetNullableString(reader, "C6", tenantId, _logger),
                        C7 = SafeGetNullableString(reader, "C7", tenantId, _logger)
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

        /// <summary>
        /// Reads an integer column defensively: eTimeTrackLite1 installs have
        /// been observed to store DeviceLogId/DeviceId as int, smallint, or
        /// bigint depending on the install/table, and occasionally as a
        /// numeric/decimal column. A hard reader.GetInt32 throws
        /// InvalidCastException the moment it hits a differently-typed
        /// column - which, before this fix, killed the entire batch (see the
        /// ROOT-CAUSE FIX comment above). This reads the raw value and
        /// converts it, so a schema difference on ONE of the several
        /// UNION-ALL'd tables can't take down the whole run.
        /// </summary>
        private static int GetInt32Flexible(SqlDataReader reader, string columnName)
        {
            var ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal))
                throw new InvalidOperationException($"Column '{columnName}' was NULL - cannot be used as a required identifier.");

            var raw = reader.GetValue(ordinal);
            return Convert.ToInt32(raw);
        }

        /// <summary>
        /// Reads a non-identity, non-critical string column defensively: logs
        /// and falls back to null instead of throwing, so a single
        /// unexpected value in an enrichment column (UserId/Direction/
        /// AttDirection/WorkCode/C1-C7) can never abort the whole batch. Only
        /// used for columns that are already CONVERT(nvarchar, ...)'d in the
        /// SQL itself (see DeviceLogColumns), so this should essentially
        /// never actually hit its catch in practice - it is defense in
        /// depth, not the primary fix.
        /// </summary>
        private static string? SafeGetNullableString(SqlDataReader reader, string columnName, string tenantId, ILogger logger)
        {
            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "eSSL GetDeviceLogsAsync: could not read column '{ColumnName}' for tenant {TenantId} on an otherwise-valid row - defaulting it to null so the row still imports.",
                    columnName, tenantId);
                return null;
            }
        }

        /// <summary>Same defensive fallback as <see cref="SafeGetNullableString"/>, for the one nullable DateTime enrichment column (DownloadDate).</summary>
        private static DateTime? SafeGetNullableDateTime(SqlDataReader reader, string columnName, string tenantId, ILogger logger)
        {
            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? (DateTime?)null : reader.GetDateTime(ordinal);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "eSSL GetDeviceLogsAsync: could not read column '{ColumnName}' for tenant {TenantId} on an otherwise-valid row - defaulting it to null so the row still imports.",
                    columnName, tenantId);
                return null;
            }
        }

        public async Task<Dictionary<string, EsslEmployeeRaw>> GetEmployeeNamesAsync(
            string tenantId,
            IEnumerable<string> deviceUserIds,
            CancellationToken ct = default)
        {
            var codes = deviceUserIds?.Distinct().ToList() ?? new List<string>();

            if (codes.Count == 0)
                return new Dictionary<string, EsslEmployeeRaw>();

            string? connectionString;

            try
            {
                connectionString = await BuildConnectionStringAsync(tenantId, ct);
            }
            catch (InvalidOperationException ex)
            {
                // This report is a best-effort enrichment (falls back to
                // showing raw device codes) - never let a broken password
                // take down the whole Unmapped Employees page. The Sync
                // Now / Historical Import path is where this same failure
                // needs to be loud (see DiscoverDeviceLogTablesAsync,
                // which deliberately does NOT catch this).
                _logger.LogWarning(ex, "eSSL GetEmployeeNamesAsync: {Message}", ex.Message);
                return new Dictionary<string, EsslEmployeeRaw>();
            }

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

                    // ROOT-CAUSE FIX: this used to "return null" here, which
                    // every caller (TestConnectionAsync/
                    // DiscoverDeviceLogTablesAsync/GetDeviceLogsAsync/
                    // GetEmployeeNamesAsync) treats IDENTICALLY to "not
                    // configured yet" - silently doing nothing and letting
                    // the sync report success with zero records found,
                    // instead of failing loudly. That is exactly what made
                    // a sync "just stop working" with no error anywhere -
                    // most commonly after the app pool's Data Protection key
                    // ring changes (a fresh IIS deployment, or before this
                    // app was configured with persistent key storage - see
                    // Program.cs), which makes every previously-saved eSSL
                    // password permanently undecryptable. Throwing here
                    // instead lets it propagate up to SyncAsync's existing
                    // catch block, which already turns any exception into a
                    // proper Failed sync-log row and a visible error
                    // message - that plumbing already existed, this was the
                    // only piece silently swallowing the real failure
                    // before it ever got there.
                    throw new InvalidOperationException(
                        "The saved eSSL database password could not be decrypted (this can happen after a server restart or redeploy). Please re-open eSSL Settings, re-enter the database password, click Save, then try the sync again.");
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
                //
                // ROOT-CAUSE FIX: this used to only look for tables
                // overlapping [now-1month, now+1day] - so on 2026-09-12 it
                // would only ever report DeviceLogs_8_2026/DeviceLogs_9_2026
                // as "found", even when DeviceLogs_1_2026 through
                // DeviceLogs_7_2026 also exist on the server. That made Test
                // Connection look like only the last ~2 months of partition
                // tables existed/were reachable, which is misleading (an
                // admin reading "Tables found: DeviceLogs, DeviceLogs_8_2026,
                // DeviceLogs_9_2026" has no way to tell whether that's
                // because only 2 months exist, or because 7 more months were
                // simply excluded from this preview by its date window) and
                // has nothing to do with what the actual sync/historical
                // import will use - those pass their own real requested
                // date range to table discovery, not "now ± 1 month". Widen
                // this diagnostic to a wide, effectively-unbounded window so
                // it reports every monthly table that actually exists on
                // the server, regardless of today's date - it is a cheap
                // sys.tables metadata query, not a data scan, so scanning a
                // wide window costs nothing extra.
                string tableSummary;
                try
                {
                    var tables = await DiscoverDeviceLogTablesByConnectionAsync(
                        connectionString, new DateTime(2000, 1, 1), new DateTime(2100, 1, 1), ct);
                    tableSummary = tables.Count == 0
                        ? " No DeviceLogs tables were found."
                        : $" Tables found ({tables.Count}): {string.Join(", ", tables)}.";
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
