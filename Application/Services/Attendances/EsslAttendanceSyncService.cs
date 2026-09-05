using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs.Attendances;
using Application.DTOs.Employee;
using Application.Interfaces.Attendances;
using Application.Interfaces.ErrorLog;
using Domain.Entities;
using Domain.Helper;
using Infrastructure;
using Infrastructure.Data;
using Infrastructure.EsslIntegration;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using static Domain.Enums.EnumExtensions;

// Lets EsslIntegration.Tests exercise ResolvePunchType (internal static)
// directly - the only piece of this service worth pure unit-testing
// without a database; everything else is covered by the InMemory
// integration tests in that project instead.
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("EsslIntegration.Tests")]

namespace Application.Services.Attendances
{
    /// <summary>
    /// Core sync engine for the eSSL eTimeTrackLite1 direct-SQL attendance
    /// integration. Reads raw punches via IEsslAttendanceDataSource (SELECT
    /// only, separate connection), maps them to HRMS employees via the
    /// existing EmployeeBiometricMapping table, and inserts them into the
    /// existing BiometricAttendanceLog staging table - the exact same table
    /// BiometricSyncService.IngestPunchesAsync (the agent-push path) already
    /// writes to. AttendanceProcessorService.ProcessAttendanceAsync (the
    /// existing, unmodified attendance engine) is what turns those rows into
    /// real Attendance records - this class deliberately contains NO
    /// shift/late/overtime/night-shift logic of its own (requirement #29:
    /// "do not create a second competing attendance engine").
    ///
    /// Idempotency: relies on the two unique indexes already on
    /// BiometricAttendanceLog (see ApplicationDbContext.OnModelCreating) -
    /// IX_..._Device_TransactionId on (DeviceId, DeviceTransactionId), where
    /// DeviceTransactionId here is "ESSL-{eTimeTrackLite DeviceLogId}" (that
    /// source column is a table-wide unique IDENTITY, so this is a safe,
    /// simple, globally-unique key - see EsslDeviceLogRaw's remarks). A
    /// pre-check query also skips rows already present before even
    /// attempting an insert, but the unique index is the real guarantee -
    /// a race between two concurrent runs (which the IsSyncRunning lock
    /// below should already prevent) would still be caught there.
    /// </summary>
    public class EsslAttendanceSyncService : IEsslAttendanceSyncService
    {
        private readonly ApplicationDbContext _db;
        private readonly IEsslAttendanceDataSource _esslDataSource;
        private readonly IAttendanceProcessorService _attendanceProcessor;
        private readonly IErrorLogService _errorLogService;
        private readonly IConfiguration _configuration;
        private readonly IDataProtector _protector;
        private readonly ILogger<EsslAttendanceSyncService> _logger;

        // How far back an automatic (incremental) run re-scans past its last
        // watermark, to safely pick up late-arriving/corrected rows
        // (requirement #8: "do not implement a fragile synchronization
        // mechanism that only assumes WHERE DeviceLogId > LastProcessedId").
        // Safe to re-scan this window every run because duplicates are
        // caught by the DB unique index, not by this overlap being "exact".
        private const int DefaultOverlapMinutes = 30;

        // A lock held longer than this is treated as abandoned (the process
        // that took it crashed/was killed mid-run) rather than blocking the
        // feature forever.
        private const int DefaultStaleLockMinutes = 30;

        public EsslAttendanceSyncService(
            ApplicationDbContext db,
            IEsslAttendanceDataSource esslDataSource,
            IAttendanceProcessorService attendanceProcessor,
            IErrorLogService errorLogService,
            IConfiguration configuration,
            IDataProtectionProvider dataProtectionProvider,
            ILogger<EsslAttendanceSyncService> logger)
        {
            _db = db;
            _esslDataSource = esslDataSource;
            _attendanceProcessor = attendanceProcessor;
            _errorLogService = errorLogService;
            _configuration = configuration;
            // Same purpose string as EsslAttendanceDataSource's protector -
            // both must match exactly for Protect/Unprotect to round-trip.
            _protector = dataProtectionProvider.CreateProtector("EsslIntegration.DatabasePassword.v1");
            _logger = logger;
        }

        public async Task<EsslSyncResultDto> SyncAsync(
            EsslSyncRequestDto request,
            string tenantId,
            string triggeredBy,
            CancellationToken ct = default)
        {
            var startedAt = DateTime.Now;
            var result = new EsslSyncResultDto { Success = false };

            if (!await _esslDataSource.IsEnabledAsync(tenantId, ct))
            {
                result.Message = "eSSL integration is disabled for this tenant.";
                return result;
            }

            var state = await GetOrCreateStateAsync(tenantId);

            // Requirement #12: two sync processes must never process the
            // same batch simultaneously. A stale (abandoned) lock is taken
            // over rather than blocking forever.
            if (state.IsSyncRunning &&
                state.LastSyncStartedAt.HasValue &&
                (DateTime.Now - state.LastSyncStartedAt.Value).TotalMinutes < DefaultStaleLockMinutes)
            {
                result.Message = "A sync is already in progress for this tenant. Please wait for it to complete.";
                return result;
            }

            state.IsSyncRunning = true;
            state.LastSyncStartedAt = startedAt;
            await _db.SaveChangesAsync(ct);

            // Batch size now comes from this tenant's persisted setting
            // (editable via the Settings form) - appsettings.json's
            // EsslDatabase:BatchSize is only the fallback default for a
            // tenant that has never saved settings yet.
            var savedSetting = await _db.EsslIntegrationSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId, ct);

            var batchSize = savedSetting?.BatchSize ?? _configuration.GetValue<int?>("EsslDatabase:BatchSize") ?? 500;
            if (batchSize < 1) batchSize = 500;

            bool isManualWindow = request.FromDate.HasValue || request.ToDate.HasValue;

            DateTime fromDateInclusive;
            DateTime toDateExclusive;

            if (isManualWindow)
            {
                // Manual/historical mode - the admin's explicit window
                // (requirement #13/#14). ToDate is treated as inclusive of
                // that whole calendar day.
                fromDateInclusive = (request.FromDate ?? DateTime.Today).Date;
                toDateExclusive = (request.ToDate ?? DateTime.Today).Date.AddDays(1);
            }
            else
            {
                // Automatic incremental mode - resume from the last
                // watermark MINUS an overlap window, never a bare "> last
                // id" (requirement #8). First-ever run falls back to a
                // 1-day lookback so it doesn't try to import the device's
                // entire history unattended.
                var overlapMinutes = _configuration.GetValue<int?>("EsslDatabase:OverlapMinutes") ?? DefaultOverlapMinutes;

                fromDateInclusive = (state.LastProcessedLogDate ?? DateTime.Now.AddDays(-1))
                    .AddMinutes(-overlapMinutes);

                toDateExclusive = DateTime.Now.AddMinutes(1);
            }

            var syncLog = new BiometricSyncLog
            {
                Id = IDManager.GetNewId(new BiometricSyncLog()),
                TenantId = tenantId,
                SyncType = "EsslDbPull",
                StartTime = startedAt,
                FromDate = fromDateInclusive,
                ToDate = toDateExclusive,
                TriggeredBy = triggeredBy
            };

            try
            {
                // Cache active employee mappings once for the whole run -
                // cheap, and avoids one query per punch (requirement #26).
                var mappingByCode = await _db.EmployeeBiometricMappings
                    .AsNoTracking()
                    .Where(x => x.IsActive)
                    .GroupBy(x => x.BiometricEmployeeCode)
                    .ToDictionaryAsync(g => g.Key, g => g.First(), ct);

                // Tracks the last resolved direction per (EmployeeCode,
                // calendar date) purely as a last-resort fallback for rows
                // where neither Direction nor AttDirection is a recognizable
                // token (requirement #16's "implement configurable
                // punch-pairing logic" when direction isn't reliably stored).
                var lastPunchTypeByEmployeeDay = new Dictionary<(string Code, DateTime Day), PunchType>();

                // Requirement #3/#4 - discover which physical tables
                // (DeviceLogs plus any DeviceLogs_M_YYYY monthly partitions
                // whose calendar month overlaps this run's window) actually
                // exist right now, ONCE per run - not once per batch. An
                // empty result is not an error (e.g. a historical import for
                // a month that was never partitioned, or eSSL temporarily
                // unreachable) - the run simply finds zero records and
                // completes normally, exactly like an empty DeviceLogs table
                // would today.
                var sourceTables = await _esslDataSource.DiscoverDeviceLogTablesAsync(
                    tenantId, fromDateInclusive, toDateExclusive, ct);

                result.TablesScanned = sourceTables;

                _logger.LogInformation(
                    "eSSL sync: table discovery for tenant {TenantId}, window {FromDate:yyyy-MM-dd HH:mm} to {ToDate:yyyy-MM-dd HH:mm} -> [{Tables}].",
                    tenantId, fromDateInclusive, toDateExclusive,
                    sourceTables.Count == 0 ? "(none found)" : string.Join(", ", sourceTables));

                EsslDeviceLogCursor? cursor = null;
                int maxProcessedDeviceLogId = state.LastProcessedDeviceLogId ?? 0;
                DateTime? maxProcessedLogDate = state.LastProcessedLogDate;
                string? maxProcessedSourceTable = state.LastProcessedSourceTable;

                while (sourceTables.Count > 0)
                {
                    ct.ThrowIfCancellationRequested();

                    var batch = await _esslDataSource.GetDeviceLogsAsync(
                        tenantId, sourceTables, fromDateInclusive, toDateExclusive, cursor, batchSize, ct);

                        if (batch.Count == 0)
                        break;

                    result.RecordsFound += batch.Count;
                    syncLog.RecordsFetched += batch.Count;

                    // Pre-check which of this batch's derived
                    // DeviceTransactionIds already exist, to skip an insert
                    // attempt (and the resulting DbUpdateException) for the
                    // common "already imported" case - the unique index
                    // remains the actual correctness guarantee.
                    var candidateTxnIds = batch.Select(BuildDeviceTransactionId).ToList();

                    var alreadyImported = (await _db.BiometricAttendanceLogs
                            .AsNoTracking()
                            .Where(x => x.DeviceTransactionId != null && candidateTxnIds.Contains(x.DeviceTransactionId))
                            .Select(x => x.DeviceTransactionId)
                            .ToListAsync(ct))
                        .ToHashSet();

                    foreach (var raw in batch)
                    {
                        try
                        {
                            var transactionId = BuildDeviceTransactionId(raw);

                            if (alreadyImported.Contains(transactionId))
                            {
                                result.RecordsSkipped++;
                                result.DuplicateCount++;
                                syncLog.RecordsSkipped++;
                                continue;
                            }

                            bool isMapped = mappingByCode.ContainsKey(raw.UserId);

                            if (!isMapped)
                                result.UnknownEmployeeCount++;

                            var day = raw.LogDate.Date;
                            var punchType = ResolvePunchType(raw, lastPunchTypeByEmployeeDay, day);
                            lastPunchTypeByEmployeeDay[(raw.UserId, day)] = punchType;

                            var entity = new BiometricAttendanceLog
                            {
                                Id = IDManager.GetNewId(new BiometricAttendanceLog()),
                                TenantId = tenantId,
                                EmployeeCode = raw.UserId,
                                PunchTime = raw.LogDate,
                                PunchType = punchType,
                                DeviceId = $"ESSL-{raw.DeviceId}",
                                DeviceTransactionId = transactionId,
                                VerifyMode = null,
                                IsDuplicate = false,
                                IsProcessed = false,
                                SourceTable = raw.SourceTable,
                                DownloadDate = raw.DownloadDate,
                                CreatedBy = triggeredBy,
                                CreatedOn = DateTime.UtcNow
                            };

                            _db.BiometricAttendanceLogs.Add(entity);

                            await _db.SaveChangesAsync(ct);

                            result.RecordsImported++;
                            syncLog.RecordsInserted++;

                            // Chronological watermark - LogDate is the
                            // primary ordering key now that more than one
                            // physical table can be involved (see
                            // EsslDeviceLogCursor's remarks: DeviceLogId
                            // alone is only unique within one table).
                            if (maxProcessedLogDate == null || raw.LogDate >= maxProcessedLogDate)
                            {
                                maxProcessedLogDate = raw.LogDate;
                                maxProcessedDeviceLogId = raw.DeviceLogId;
                                maxProcessedSourceTable = raw.SourceTable;
                            }
                        }
                        catch (DbUpdateException dbEx)
                        {
                            // Final backstop for the unique-index race - two
                            // overlapping runs both saw this row as "new".
                            _db.ChangeTracker.Clear();

                            result.RecordsSkipped++;
                            result.DuplicateCount++;
                            syncLog.RecordsSkipped++;

                            _logger.LogInformation(dbEx,
                                "eSSL sync: duplicate punch skipped via unique-index race (DeviceLogId {DeviceLogId}).",
                                raw.DeviceLogId);
                        }
                        catch (Exception rowEx)
                        {
                            // Requirement #22: one bad record must never stop
                            // the batch or roll back the ones already saved.
                            _db.ChangeTracker.Clear();

                            result.ErrorCount++;
                            syncLog.RecordsFailed++;

                            _logger.LogError(rowEx,
                                "eSSL sync: failed to import punch DeviceLogId {DeviceLogId}, SourceTable {SourceTable}, UserId {UserId}, DeviceId {DeviceId}, LogDate {LogDate:o}.",
                                raw.DeviceLogId, raw.SourceTable, raw.UserId, raw.DeviceId, raw.LogDate);

                            await _errorLogService.LogAsync(
                                rowEx,
                                module: "Biometric Device Integration",
                                feature: "eSSL SQL Sync",
                                controller: "EsslAttendanceSync",
                                action: "SyncAsync",
                                tenantId: tenantId);
                        }

                        // Advance the intra-run keyset cursor regardless of
                        // success/duplicate/failure - once this row has been
                        // seen (in any outcome), the next batch in THIS run
                        // must never fetch it again. Cross-run duplicate
                        // safety still comes from the DB unique index, not
                        // from this cursor.
                        cursor = EsslDeviceLogCursor.FromRow(raw);
                    }

                    if (batch.Count < batchSize)
                        break;
                }

                // Hand the newly-staged raw punches to the EXISTING,
                // unmodified attendance engine - no duplicate business logic
                // here (requirement #29). Best-effort: a processing failure
                // must not make this sync run look failed - the raw punches
                // are already safely persisted and IsProcessed stays false
                // for retry on the next pass (by this service or the
                // existing manual "Process Attendance" action, whichever
                // runs next).
                try
                {
                    await _attendanceProcessor.ProcessAttendanceAsync();
                }
                catch (Exception procEx)
                {
                    _logger.LogError(procEx, "eSSL sync: AttendanceProcessorService.ProcessAttendanceAsync failed after import.");
                }

                state.LastProcessedDeviceLogId = maxProcessedDeviceLogId;
                state.LastProcessedLogDate = maxProcessedLogDate;
                state.LastProcessedSourceTable = maxProcessedSourceTable;
                state.RecordsRead = result.RecordsFound;
                state.RecordsImported = result.RecordsImported;
                state.RecordsSkipped = result.RecordsSkipped;
                state.RecordsFailed = result.ErrorCount;
                state.LastSyncStatus = result.ErrorCount > 0 ? "PartialFailure" : "Success";
                state.LastError = null;

                syncLog.Status = result.ErrorCount > 0 ? "PartialFailure" : "Success";
                syncLog.SourceTables = sourceTables.Count == 0 ? null : string.Join(", ", sourceTables);

                result.Success = true;
                result.Message = sourceTables.Count == 0
                    ? "Sync completed. No DeviceLogs tables were found for the requested window."
                    : $"Sync completed. Found {result.RecordsFound}, imported {result.RecordsImported}, skipped {result.RecordsSkipped}, failed {result.ErrorCount}. Tables: {string.Join(", ", sourceTables)}.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "eSSL sync run failed.");

                state.LastSyncStatus = "Failed";
                state.LastError = Truncate(ex.Message, 1000);

                syncLog.Status = "Failed";
                syncLog.ErrorMessage = Truncate(ex.Message, 500);

                result.Success = false;
                result.Message = "Sync failed: " + ex.Message;

                await _errorLogService.LogAsync(
                    ex,
                    module: "Biometric Device Integration",
                    feature: "eSSL SQL Sync",
                    controller: "EsslAttendanceSync",
                    action: "SyncAsync",
                    tenantId: tenantId);
            }
            finally
            {
                state.IsSyncRunning = false;
                state.LastSyncCompletedAt = DateTime.Now;

                syncLog.EndTime = DateTime.Now;

                result.DurationSeconds = Math.Round((DateTime.Now - startedAt).TotalSeconds, 1);

                // Logging/state-write failures must never mask the real
                // sync outcome above, and must never throw out of here.
                try
                {
                    _db.BiometricSyncLogs.Add(syncLog);
                    await _db.SaveChangesAsync(ct);
                }
                catch (Exception logEx)
                {
                    _logger.LogError(logEx, "eSSL sync: failed to write BiometricSyncLog / EsslAttendanceSyncState.");
                }
            }

            return result;
        }

        public async Task<EsslSyncSettingsDto> GetSettingsAsync(string tenantId)
        {
            var state = await GetOrCreateStateAsync(tenantId);

            return new EsslSyncSettingsDto
            {
                Enabled = await _esslDataSource.IsEnabledAsync(tenantId),
                IsSyncRunning = state.IsSyncRunning,
                LastSyncStartedAt = state.LastSyncStartedAt,
                LastSyncCompletedAt = state.LastSyncCompletedAt,
                LastSyncStatus = state.LastSyncStatus,
                LastError = state.LastError,
                LastProcessedDeviceLogId = state.LastProcessedDeviceLogId,
                LastProcessedLogDate = state.LastProcessedLogDate
            };
        }

        public async Task<EsslDatabaseConfigViewDto> GetConfigurationAsync(string tenantId)
        {
            var setting = await _db.EsslIntegrationSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId);

            if (setting != null)
            {
                return new EsslDatabaseConfigViewDto
                {
                    IntegrationEnabled = setting.IntegrationEnabled,
                    DatabaseServer = setting.DatabaseServer,
                    DatabaseName = setting.DatabaseName,
                    AuthenticationType = setting.AuthenticationType,
                    Username = setting.Username,
                    HasPasswordConfigured = !string.IsNullOrEmpty(setting.EncryptedPassword),
                    ConnectionTimeout = setting.ConnectionTimeout,
                    SyncIntervalMinutes = setting.SyncIntervalMinutes,
                    BatchSize = setting.BatchSize
                };
            }

            // Legacy fallback - no row saved yet for this tenant. Surface the
            // appsettings.json values as the initial form contents so the
            // first Save doesn't silently reset anything the ops team
            // already had configured there.
            var legacyConnectionString = _configuration.GetValue<string>("EsslDatabase:ConnectionString") ?? "";

            return new EsslDatabaseConfigViewDto
            {
                IntegrationEnabled = _configuration.GetValue<bool?>("EsslDatabase:Enabled") ?? false,
                DatabaseServer = ExtractServerName(legacyConnectionString) ?? "",
                DatabaseName = _configuration.GetValue<string>("EsslDatabase:DatabaseName") ?? "etimetracklite1",
                AuthenticationType = EsslAuthenticationTypes.Sql,
                Username = null,
                HasPasswordConfigured = !string.IsNullOrEmpty(legacyConnectionString),
                ConnectionTimeout = 15,
                SyncIntervalMinutes = _configuration.GetValue<int?>("EsslDatabase:SyncIntervalMinutes") ?? 5,
                BatchSize = _configuration.GetValue<int?>("EsslDatabase:BatchSize") ?? 500
            };
        }

        public async Task<(bool Success, string Message)> SaveConfigurationAsync(EsslDatabaseConfigDto dto, string tenantId, string modifiedBy)
        {
            // Server-side validation (requirement #7 - "server-side
            // validation is mandatory", never rely on the client alone).
            // DataAnnotations on the DTO cover Required/Range and are
            // enforced via ModelState at the controller; this method adds
            // the cross-field rule DataAnnotations can't express on its own.
            if (dto.AuthenticationType == EsslAuthenticationTypes.Sql && string.IsNullOrWhiteSpace(dto.Username))
                return (false, "Username is required for SQL Server Authentication.");

            if (dto.AuthenticationType != EsslAuthenticationTypes.Sql && dto.AuthenticationType != EsslAuthenticationTypes.Windows)
                return (false, "Authentication type must be either SQL Server Authentication or Windows Authentication.");

            var setting = await _db.EsslIntegrationSettings
                .FirstOrDefaultAsync(x => x.TenantId == tenantId);

            if (setting == null)
            {
                setting = new EsslIntegrationSetting
                {
                    Id = IDManager.GetNewId(new EsslIntegrationSetting()),
                    TenantId = tenantId,
                    CreatedBy = modifiedBy,
                    CreatedOn = DateTime.UtcNow
                };
                _db.EsslIntegrationSettings.Add(setting);
            }

            setting.IntegrationEnabled = dto.IntegrationEnabled;
            setting.DatabaseServer = dto.DatabaseServer.Trim();
            setting.DatabaseName = dto.DatabaseName.Trim();
            setting.AuthenticationType = dto.AuthenticationType;
            setting.ConnectionTimeout = dto.ConnectionTimeout;
            setting.SyncIntervalMinutes = dto.SyncIntervalMinutes;
            setting.BatchSize = dto.BatchSize;
            setting.ModifiedBy = modifiedBy;
            setting.ModifiedOn = DateTime.UtcNow;

            if (dto.AuthenticationType == EsslAuthenticationTypes.Windows)
            {
                // Requirement #5 - "do not send unnecessary credential
                // values to the backend" is a client-side courtesy; this is
                // the server-side guarantee that Windows Authentication
                // never persists a stale username/password either, even if
                // the browser sent something anyway.
                setting.Username = null;
                setting.EncryptedPassword = null;
            }
            else
            {
                setting.Username = dto.Username?.Trim();

                // Blank/null Password = keep the existing saved password
                // unchanged (requirement #4) - never overwrite it with an
                // empty value just because the field was left blank.
                if (!string.IsNullOrEmpty(dto.Password))
                    setting.EncryptedPassword = _protector.Protect(dto.Password);
            }

            await _db.SaveChangesAsync();

            return (true, "Settings saved successfully.");
        }

        public async Task<(bool Success, string Message)> TestConnectionAsync(EsslDatabaseConfigDto dto, string tenantId)
        {
            if (string.IsNullOrWhiteSpace(dto.DatabaseServer))
                return (false, "Database server is required.");

            if (string.IsNullOrWhiteSpace(dto.DatabaseName))
                return (false, "Database name is required.");

            if (dto.AuthenticationType == EsslAuthenticationTypes.Sql && string.IsNullOrWhiteSpace(dto.Username))
                return (false, "Username is required for SQL Server Authentication.");

            var password = dto.Password;

            // Blank password while testing SQL auth - fall back to the
            // already-saved password (if any) rather than attempting to
            // connect with an empty one, so re-testing after a page reload
            // (without retyping a password already saved) still works.
            if (dto.AuthenticationType == EsslAuthenticationTypes.Sql && string.IsNullOrEmpty(password))
            {
                var existing = await _db.EsslIntegrationSettings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.TenantId == tenantId);

                if (!string.IsNullOrEmpty(existing?.EncryptedPassword))
                {
                    try
                    {
                        password = _protector.Unprotect(existing.EncryptedPassword);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "eSSL: failed to decrypt stored database password for tenant {TenantId} during Test Connection.", tenantId);
                        return (false, "A previously saved password could not be read. Please re-enter the password and try again.");
                    }
                }
            }

            return await _esslDataSource.TestConnectionAsync(new EsslConnectionParameters
            {
                DatabaseServer = dto.DatabaseServer,
                DatabaseName = dto.DatabaseName,
                AuthenticationType = dto.AuthenticationType,
                Username = dto.Username,
                Password = password,
                ConnectionTimeout = dto.ConnectionTimeout
            });
        }

        public async Task<PagedResult<EsslSyncHistoryDto>> GetSyncHistoryAsync(EsslSyncHistoryFilterDto filter, string tenantId)
        {
            var query = _db.BiometricSyncLogs
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.SyncType == "EsslDbPull");

            if (filter.DateFrom.HasValue)
                query = query.Where(x => x.StartTime >= filter.DateFrom.Value.Date);

            if (filter.DateTo.HasValue)
            {
                var to = filter.DateTo.Value.Date.AddDays(1);
                query = query.Where(x => x.StartTime < to);
            }

            var total = await query.CountAsync();

            var pageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
            var pageSize = filter.PageSize is < 1 or > 200 ? 20 : filter.PageSize;

            // NOTE: BiometricSyncLogs was originally created by an ad-hoc SQL
            // script before this feature had a real EF migration (see the
            // remarks on PendingMigrationEsslAttendanceSyncStates) - its
            // actual physical columns don't necessarily match the NOT NULL
            // assumptions in the entity/migration DDL. Some legacy rows have
            // NULL in StartTime/RecordsFetched/RecordsInserted/
            // RecordsSkipped/RecordsFailed, and reading a NULL SQL value
            // into a non-nullable DateTime/int throws
            // System.Data.SqlTypes.SqlNullValueException at materialization
            // time. Projecting straight to the DTO with a nullable cast +
            // coalesce (translated by EF Core into SQL COALESCE/ISNULL)
            // avoids ever reading those columns as a non-nullable type,
            // without requiring any database/schema change.
            var items = await query
                .OrderByDescending(x => x.StartTime)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new EsslSyncHistoryDto
                {
                    Id = x.Id,
                    StartTime = (DateTime?)x.StartTime ?? DateTime.MinValue,
                    EndTime = x.EndTime,
                    FromDate = x.FromDate,
                    ToDate = x.ToDate,
                    RecordsFetched = (int?)x.RecordsFetched ?? 0,
                    RecordsInserted = (int?)x.RecordsInserted ?? 0,
                    RecordsSkipped = (int?)x.RecordsSkipped ?? 0,
                    RecordsFailed = (int?)x.RecordsFailed ?? 0,
                    Status = x.Status ?? "Unknown",
                    ErrorMessage = x.ErrorMessage,
                    TriggeredBy = x.TriggeredBy
                })
                .ToListAsync();

            return new PagedResult<EsslSyncHistoryDto>
            {
                TotalRecords = total,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Data = items
            };
        }

        public async Task<List<EsslUnmappedEmployeeDto>> GetUnmappedEmployeesAsync(string tenantId)
        {
            var activeCodes = await _db.EmployeeBiometricMappings
                .AsNoTracking()
                .Where(x => x.IsActive)
                .Select(x => x.BiometricEmployeeCode)
                .ToListAsync();

            var activeCodeSet = activeCodes.ToHashSet();

            // Only ever looks at eSSL-sourced raw punches (DeviceId starts
            // with "ESSL-") - agent-pushed punches from other biometric
            // devices have their own, already-existing mapping workflow via
            // EmployeeBiometricMappingController.
            var grouped = await _db.BiometricAttendanceLogs
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.DeviceId != null && x.DeviceId.StartsWith("ESSL-"))
                .GroupBy(x => x.EmployeeCode)
                .Select(g => new
                {
                    Code = g.Key,
                    Count = g.Count(),
                    First = g.Min(x => x.PunchTime),
                    Last = g.Max(x => x.PunchTime)
                })
                .ToListAsync();

            var unmapped = grouped
                .Where(g => !activeCodeSet.Contains(g.Code))
                .ToList();

            if (unmapped.Count == 0)
                return new List<EsslUnmappedEmployeeDto>();

            var names = await _esslDataSource.GetEmployeeNamesAsync(tenantId, unmapped.Select(u => u.Code));

            return unmapped
                .Select(u => new EsslUnmappedEmployeeDto
                {
                    BiometricEmployeeCode = u.Code,
                    EsslEmployeeName = names.TryGetValue(u.Code, out var emp) ? emp.EmployeeName : null,
                    PunchCount = u.Count,
                    FirstSeen = u.First,
                    LastSeen = u.Last
                })
                .OrderByDescending(u => u.LastSeen)
                .ToList();
        }

        // ==================================================================
        // PUNCH DIRECTION RESOLUTION (requirement #16)
        // ==================================================================

        /// <summary>
        /// Normalizes eTimeTrackLite1's free-text Direction/AttDirection
        /// value to the HRMS PunchType enum. Real eSSL deployments have been
        /// seen using "IN"/"OUT", "I"/"O", and "Check-In"/"Check-Out" -
        /// this deliberately does not assume any single vocabulary. If
        /// neither field yields a recognizable token, falls back to
        /// alternating In/Out per (EmployeeCode, calendar date) - the
        /// "configurable punch-pairing logic" the requirement calls for
        /// when direction isn't reliably stored. BreakIn/BreakOut are never
        /// inferred here (eTimeTrackLite's DeviceLogs has no such concept in
        /// the supplied schema) - only plain In/Out.
        /// </summary>
        internal static PunchType ResolvePunchType(
            EsslDeviceLogRaw raw,
            Dictionary<(string Code, DateTime Day), PunchType> lastPunchTypeByEmployeeDay,
            DateTime day)
        {
            var token = (raw.Direction ?? raw.AttDirection ?? "").Trim().ToUpperInvariant();

            bool looksIn = token is "IN" or "I" or "CHECK-IN" or "CHECKIN" or "C/IN";
            bool looksOut = token is "OUT" or "O" or "CHECK-OUT" or "CHECKOUT" or "C/OUT";

            if (looksIn) return PunchType.In;
            if (looksOut) return PunchType.Out;

            // Fallback pairing: alternate based on the last resolved
            // direction for this same employee/day within this run.
            var key = (raw.UserId, day);

            if (lastPunchTypeByEmployeeDay.TryGetValue(key, out var last))
                return last == PunchType.In ? PunchType.Out : PunchType.In;

            return PunchType.In;
        }

        // ==================================================================
        // HELPERS
        // ==================================================================

        private async Task<EsslAttendanceSyncState> GetOrCreateStateAsync(string tenantId)
        {
            var state = await _db.EsslAttendanceSyncStates
                .FirstOrDefaultAsync(x => x.TenantId == tenantId);

            if (state != null)
                return state;

            state = new EsslAttendanceSyncState
            {
                Id = IDManager.GetNewId(new EsslAttendanceSyncState()),
                TenantId = tenantId,
                CreatedBy = "System",
                CreatedOn = DateTime.UtcNow
            };

            _db.EsslAttendanceSyncStates.Add(state);
            await _db.SaveChangesAsync();

            return state;
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

        // ==================================================================
        // IDEMPOTENCY KEY (requirement #7/#23 - stable per-record source identity)
        // ==================================================================

        /// <summary>
        /// The bare "DeviceLogs" table keeps its ORIGINAL, unqualified format
        /// ("ESSL-{DeviceLogId}") so already-imported production rows from
        /// before monthly-table support was added keep matching on the next
        /// run (backward compatible - never re-imported as if new). Any
        /// DISCOVERED monthly table ("DeviceLogs_M_YYYY") - which never had
        /// legacy rows under any format, since this integration could not
        /// read from them before - gets a table-qualified format instead
        /// ("ESSL-{SourceTable}-{DeviceLogId}"), which is required for
        /// correctness: DeviceLogId is only unique WITHIN one physical table,
        /// so two different monthly tables can genuinely contain the same
        /// DeviceLogId for two different punches (see EsslDeviceLogRaw's
        /// remarks) - the unqualified format alone would silently collapse
        /// those into one BiometricAttendanceLog row via the unique index.
        /// Max observed length is well under BiometricAttendanceLog.
        /// DeviceTransactionId's 100-char limit ("ESSL-" + up to ~20 chars of
        /// table name + "-" + up to 10 digits).
        /// </summary>
        internal static string BuildDeviceTransactionId(EsslDeviceLogRaw raw)
        {
            return string.Equals(raw.SourceTable, EsslDeviceLogTableName.BaseTableName, StringComparison.OrdinalIgnoreCase)
                ? $"ESSL-{raw.DeviceLogId}"
                : $"ESSL-{raw.SourceTable}-{raw.DeviceLogId}";
        }
    }
}
