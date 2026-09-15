using System;
using System.Collections.Generic;
using System.Data;
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

        // SQL Server stored-procedure-based bulk staging path (see
        // EsslBulkAttendanceLogWriter.cs / EsslBulkStaging.sql) - the
        // preferred, faster way to write a batch into BiometricAttendanceLogs.
        // The existing EF AddRange/SaveChangesAsync logic below is kept
        // completely unchanged as the automatic fallback whenever this
        // throws (bulk-copy failure, the SQL objects not yet deployed to
        // this environment, a genuine whole-batch DB error, etc.) -
        // correctness first, then performance: a bulk-staging failure never
        // loses a batch, it only makes that one batch slower.
        private readonly IEsslBulkAttendanceLogWriter _bulkWriter;

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
            IEsslBulkAttendanceLogWriter bulkWriter,
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
            _bulkWriter = bulkWriter;
            _logger = logger;
        }

        public async Task<EsslSyncResultDto> SyncAsync(
            EsslSyncRequestDto request,
            string tenantId,
            string triggeredBy,
            CancellationToken ct = default,
            bool lockAlreadyClaimed = false)
        {
            var startedAt = DateTime.Now;
            var result = new EsslSyncResultDto { Success = false };

            if (!await _esslDataSource.IsEnabledAsync(tenantId, ct))
            {
                result.Message = "eSSL integration is disabled for this tenant.";
                return result;
            }

            var state = await GetOrCreateStateAsync(tenantId);

            // lockAlreadyClaimed=true means the caller (SyncNow's
            // controller action, via ClaimSyncLockAsync) has already done
            // everything in this block, synchronously, before this job
            // was even enqueued - claiming it again here would make
            // SyncAsync see its own just-claimed lock and incorrectly
            // refuse to run (see IEsslAttendanceSyncService's remarks).
            // The automatic background cycle never pre-claims, so it still
            // takes this branch exactly as before.
            if (!lockAlreadyClaimed)
            {
                // Requirement #12: two sync processes must never process
                // the same batch simultaneously. A stale (abandoned) lock
                // is taken over rather than blocking forever.
                if (state.IsSyncRunning &&
                    state.LastSyncStartedAt.HasValue &&
                    (DateTime.Now - state.LastSyncStartedAt.Value).TotalMinutes < DefaultStaleLockMinutes)
                {
                    result.Message = "A sync is already in progress for this tenant. Please wait for it to complete.";
                    return result;
                }

                // ROOT-CAUSE FIX (confirmed hang: IsSyncRunning stuck true
                // forever, RecordsRead=0, LastError=null, LastSyncCompletedAt=
                // null, no exception ever recorded anywhere). Everything from
                // this point on used to run AFTER the lock was already claimed
                // and persisted, but BEFORE the try/catch/finally below - the
                // batch-size lookup, the from/to date computation, and the
                // syncLog construction. Any exception (or a genuinely slow/
                // blocked query - e.g. lock contention on this same HRMS
                // database) in that unprotected window left IsSyncRunning=true
                // committed to the database with absolutely nothing left to
                // ever set it back to false: the finally block that does that
                // was never reached, because it wasn't in scope yet. This is
                // exactly what "stuck Running with no error, forever" looks
                // like. Fix: the try/catch/finally now starts IMMEDIATELY
                // after the lock is claimed, so literally nothing can run
                // between "lock taken" and "lock guaranteed to be released"
                // without going through the same cleanup path.
                state.IsSyncRunning = true;
                state.LastSyncStartedAt = startedAt;
                await _db.SaveChangesAsync(ct);
            }

            BiometricSyncLog? syncLog = null;

            try
            {
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

                syncLog = new BiometricSyncLog
                {
                    Id = IDManager.GetNewId(new BiometricSyncLog()),
                    TenantId = tenantId,
                    SyncType = "EsslDbPull",
                    StartTime = startedAt,
                    FromDate = fromDateInclusive,
                    ToDate = toDateExclusive,
                    TriggeredBy = triggeredBy
                };

                // Phase 12 diagnostic: sync-start line - every field an
                // engineer reproducing a reported gap (e.g. "2671 source
                // rows, only 303 ended up in Attendance") needs before
                // looking at a single batch.
                _logger.LogInformation(
                    "eSSL sync START: tenant={TenantId}, mode={Mode}, fromDate={FromDate:o}, toDateExclusive={ToDate:o}, batchSize={BatchSize}, triggeredBy={TriggeredBy}.",
                    tenantId, isManualWindow ? "Manual/Historical" : "Automatic/Incremental",
                    fromDateInclusive, toDateExclusive, batchSize, triggeredBy);

                // Cache active employee mappings once for the whole run -
                // cheap, and avoids one query per punch (requirement #26).
                //
                // ROOT-CAUSE FIX: keyed by BiometricEmployeeCodeNormalizer.Normalize
                // (trim + invariant uppercase), not the raw string - see that
                // class's remarks. Previously a plain Dictionary<string,...>
                // with C#'s default ordinal comparer meant "260123" (typed
                // into the mapping form) and "260123 " / "essl260123" /
                // "Essl260123" (as the device actually sends it) were
                // treated as different keys and NEVER matched, silently
                // stranding those punches as permanently unmapped.
                // BiometricEmployeeCodeNormalizer.Normalize is plain C# (not
                // a SQL-translatable expression), so the GroupBy on it has to
                // run in memory - fetch the (small) active-mappings table
                // first via ToListAsync, then group/normalize client-side.
                // Same pattern EmployeeBiometricMappingService.CreateAsync/
                // UpdateAsync already use for their own normalized-duplicate
                // checks.
                var activeMappings = await _db.EmployeeBiometricMappings
                    .AsNoTracking()
                    .Where(x => x.IsActive)
                    .ToListAsync(ct);

                var mappingByCode = activeMappings
                    .GroupBy(x => BiometricEmployeeCodeNormalizer.Normalize(x.BiometricEmployeeCode))
                    .ToDictionary(g => g.Key, g => g.First());

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

                // Advances the watermark for one successfully-inserted raw
                // punch - same "furthest point we can safely resume from"
                // rule as before (only a genuinely-inserted row moves it;
                // a skipped duplicate or a failed row never does), just
                // pulled out into one place now that it's called from two
                // spots below (the bulk-insert fast path, and the row-by-
                // row fallback).
                void AdvanceWatermark(EsslDeviceLogRaw raw)
                {
                    if (maxProcessedLogDate == null || raw.LogDate >= maxProcessedLogDate)
                    {
                        maxProcessedLogDate = raw.LogDate;
                        maxProcessedDeviceLogId = raw.DeviceLogId;
                        maxProcessedSourceTable = raw.SourceTable;
                    }
                }

                int batchNumber = 0;

                while (sourceTables.Count > 0)
                {
                    ct.ThrowIfCancellationRequested();

                    batchNumber++;
                    var batchStopwatch = System.Diagnostics.Stopwatch.StartNew();

                    var batch = await _esslDataSource.GetDeviceLogsAsync(
                        tenantId, sourceTables, fromDateInclusive, toDateExclusive, cursor, batchSize, ct);

                        if (batch.Count == 0)
                        break;

                    result.RecordsFound += batch.Count;
                    syncLog.RecordsFetched += batch.Count;

                    // Phase 12 diagnostic: per-batch reconciliation counters
                    // are computed as deltas against the running totals
                    // below (result.RecordsImported etc.), captured before
                    // and after this batch's work, so the log line always
                    // reconciles exactly (BatchCount = Imported + Duplicate
                    // + Unmapped-but-imported-anyway is NOT double-counted -
                    // Unmapped rows ARE part of Imported, since they still
                    // get inserted; only Duplicate and Failed are mutually
                    // exclusive with Imported).
                    var batchImportedBefore = result.RecordsImported;
                    var batchDuplicateBefore = result.DuplicateCount;
                    var batchUnmappedBefore = result.UnknownEmployeeCount;
                    var batchFailedBefore = result.ErrorCount;

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

                    // Perf fix: this used to call _db.SaveChangesAsync()
                    // once PER ROW here - for a 10,000+/100,000+ record
                    // historical sync that is one SQL round-trip per punch,
                    // which is exactly the N+1 write pattern that both
                    // slows the run down (network round-trip cost times the
                    // record count) and is the real cause of "execution
                    // timeout" on large windows, not the CommandTimeout
                    // value itself. Build every row for this batch first,
                    // with no DB writes yet...
                    var toInsert = new List<(EsslDeviceLogRaw Raw, BiometricAttendanceLog Entity)>();

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
                                syncLog.DuplicateCount++;
                                continue;
                            }

                            bool isMapped = mappingByCode.ContainsKey(BiometricEmployeeCodeNormalizer.Normalize(raw.UserId));

                            if (!isMapped)
                            {
                                result.UnknownEmployeeCount++;
                                syncLog.UnmappedCount++;

                                // Phase 8 requirement: an unmapped record must
                                // NEVER be silently ignored - log the full
                                // identifying detail (UserId, SourceTable,
                                // DeviceLogId, LogDate, DeviceId) every time,
                                // not just a running count. The row is still
                                // imported below (staging never depends on
                                // mapping existing yet), so this is
                                // discoverable later via the Unmapped
                                // Employees screen too - this line is for
                                // the per-run diagnostic trail Phase 12 asks
                                // for.
                                _logger.LogWarning(
                                    "eSSL sync: UNMAPPED employee code - UserId={UserId}, SourceTable={SourceTable}, DeviceLogId={DeviceLogId}, LogDate={LogDate:o}, DeviceId={DeviceId}. Record will still be imported into BiometricAttendanceLogs but AttendanceProcessorService will leave it unprocessed until a matching EmployeeBiometricMapping exists.",
                                    raw.UserId, raw.SourceTable, raw.DeviceLogId, raw.LogDate, raw.DeviceId);
                            }

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

                            toInsert.Add((raw, entity));
                        }
                        catch (Exception rowEx)
                        {
                            // A row that fails even to build (never touched
                            // the DB) - requirement #22 still applies: it
                            // must never stop the rest of the batch.
                            result.ErrorCount++;
                            syncLog.RecordsFailed++;

                            _logger.LogError(rowEx,
                                "eSSL sync: failed to build punch DeviceLogId {DeviceLogId}, SourceTable {SourceTable}, UserId {UserId}, DeviceId {DeviceId}, LogDate {LogDate:o}.",
                                raw.DeviceLogId, raw.SourceTable, raw.UserId, raw.DeviceId, raw.LogDate);

                            await _errorLogService.LogAsync(
                                rowEx,
                                module: "Biometric Device Integration",
                                feature: "eSSL SQL Sync",
                                controller: "EsslAttendanceSync",
                                action: "SyncAsync",
                                tenantId: tenantId);
                        }
                    }

                    // ...then write the whole batch. Preferred path: SQL
                    // Server stored-procedure-based bulk staging (one TDS
                    // bulk-copy into dbo.EsslDeviceLogStaging + one set-based
                    // merge via dbo.usp_EsslStaging_MergeAttendanceLogs - see
                    // EsslBulkAttendanceLogWriter.cs / EsslBulkStaging.sql).
                    // On ANY failure here (SQL objects not deployed yet to
                    // this environment, a genuine whole-batch DB error, bulk
                    // copy failure) this falls straight through to the
                    // existing EF AddRange/SaveChangesAsync logic below,
                    // completely unchanged - correctness first, performance
                    // second.
                    bool bulkStagingSucceeded = false;

                    if (toInsert.Count > 0)
                    {
                        try
                        {
                            var connectionString = _db.Database.GetConnectionString();

                            var stageRows = toInsert
                                .Select(x => new EsslBulkStageRow
                                {
                                    Entity = x.Entity,
                                    SourceDeviceLogId = x.Raw.DeviceLogId,
                                    IsUnmapped = !mappingByCode.ContainsKey(
                                        BiometricEmployeeCodeNormalizer.Normalize(x.Raw.UserId))
                                })
                                .ToList();

                            var stageResult = await _bulkWriter.StageAndMergeAsync(
                                connectionString, tenantId, stageRows, ct);

                            // Reconciliation: StagedCount == InsertedCount +
                            // DuplicateCount is guaranteed by the stored
                            // procedure itself (DB-verified, not a C#
                            // running count) - see EsslBulkStaging.sql.
                            // DuplicateCount here catches a genuinely
                            // concurrent writer that inserted the same
                            // DeviceTransactionId between this batch's
                            // alreadyImported pre-check and the merge -
                            // extremely rare given the IsSyncRunning lock,
                            // but still counted correctly rather than
                            // silently dropped.
                            result.RecordsImported += stageResult.InsertedCount;
                            result.DuplicateCount += stageResult.DuplicateCount;
                            result.RecordsSkipped += stageResult.DuplicateCount;
                            result.UnknownEmployeeCount += stageResult.UnmappedInsertedCount;

                            syncLog.RecordsInserted += stageResult.InsertedCount;
                            syncLog.DuplicateCount += stageResult.DuplicateCount;
                            syncLog.RecordsSkipped += stageResult.DuplicateCount;

                            foreach (var (raw, _) in toInsert)
                                AdvanceWatermark(raw);

                            bulkStagingSucceeded = true;
                        }
                        catch (Exception bulkStageEx)
                        {
                            _logger.LogWarning(bulkStageEx,
                                "eSSL sync: bulk staging failed for a batch of {Count} rows - falling back to the existing EF insert path for this batch only.",
                                toInsert.Count);
                        }
                    }

                    // Fallback path - identical to the logic that ran
                    // unconditionally before bulk staging was added above.
                    // Untouched: still the safety net for a bulk-staging
                    // failure, exactly as it already was the safety net for
                    // an EF bulk-insert failure (see its own inner
                    // try/catch below).
                    if (!bulkStagingSucceeded && toInsert.Count > 0)
                    {
                        _db.BiometricAttendanceLogs.AddRange(toInsert.Select(x => x.Entity));

                        try
                        {
                            await _db.SaveChangesAsync(ct);

                            foreach (var (raw, _) in toInsert)
                            {
                                result.RecordsImported++;
                                syncLog.RecordsInserted++;
                                AdvanceWatermark(raw);
                            }

                            // ROOT-CAUSE FIX (ChangeTracker bloat across a
                            // long-running multi-batch sync): every
                            // exception branch below already called
                            // ChangeTracker.Clear() after its SaveChangesAsync,
                            // but the HAPPY path never did. Since _db is one
                            // long-lived DbContext instance reused across
                            // every batch of the whole run, the entities
                            // AddRange'd into this batch stayed tracked
                            // (Unchanged, but still tracked) for the rest of
                            // the run, so tracked-entity count grew batch
                            // over batch instead of resetting. On a large
                            // historical window (thousands of rows across
                            // many batches) this measurably slows every
                            // later SaveChangesAsync/query on _db (including
                            // the mapping/alreadyImported lookups) and, in
                            // the worst case, can push a batch past
                            // CommandTimeoutSeconds and abort the run
                            // mid-window - which looks exactly like "some
                            // records just silently never got synced."
                            _db.ChangeTracker.Clear();
                        }
                        catch (Exception bulkEx)
                        {
                            // Rare path: something in this batch's single
                            // SaveChangesAsync call failed as a whole (a
                            // unique-index race with another process, a
                            // genuine constraint violation on one row, a
                            // transient DB error). Fall back to inserting
                            // THIS batch row-by-row so one bad/duplicate row
                            // can never sink the rest of an otherwise-good
                            // batch - the exact same per-row isolation the
                            // old code always had, now only paid for on the
                            // rare occasions it's actually needed.
                            _db.ChangeTracker.Clear();

                            _logger.LogWarning(bulkEx,
                                "eSSL sync: bulk insert failed for a batch of {Count} rows - falling back to row-by-row for this batch only.",
                                toInsert.Count);

                            foreach (var (raw, entity) in toInsert)
                            {
                                try
                                {
                                    _db.BiometricAttendanceLogs.Add(entity);
                                    await _db.SaveChangesAsync(ct);

                                    result.RecordsImported++;
                                    syncLog.RecordsInserted++;
                                    AdvanceWatermark(raw);
                                }
                                catch (DbUpdateException dbEx)
                                {
                                    // Final backstop for the unique-index race - two
                                    // overlapping runs both saw this row as "new".
                                    _db.ChangeTracker.Clear();

                                    result.RecordsSkipped++;
                                    result.DuplicateCount++;
                                    syncLog.RecordsSkipped++;
                                    syncLog.DuplicateCount++;

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
                            }
                        }
                    }

                    // Live progress (requirement: "Progress Indicator" /
                    // "Total: ... Synced: ... Failed: ..." on the UI while a
                    // large historical run is still going). One extra small
                    // single-row write per BATCH (not per record - batches
                    // are hundreds of rows, so this is negligible next to
                    // the per-row round-trips already removed above) so a
                    // client polling GetSettingsAsync mid-run sees these
                    // counters climb instead of staying at the previous
                    // run's final numbers until this one finishes.
                    state.RecordsRead = result.RecordsFound;
                    state.RecordsImported = result.RecordsImported;
                    state.RecordsSkipped = result.RecordsSkipped;
                    state.RecordsFailed = result.ErrorCount;

                    await _db.SaveChangesAsync(ct);

                    // Phase 12 diagnostic: one structured line per batch so
                    // a long historical run's console/file log shows every
                    // batch's own reconciliation, not just a final summary -
                    // BatchCount always equals (this batch's Imported +
                    // Duplicate + Failed), independent of the running
                    // totals, so a support engineer can spot the exact batch
                    // where something stopped reconciling without needing
                    // live DB access.
                    batchStopwatch.Stop();
                    _logger.LogInformation(
                        "eSSL sync BATCH {BatchNumber}: fetched={BatchCount}, imported={Imported}, duplicate={Duplicate}, unmapped={Unmapped}, failed={Failed}, durationMs={DurationMs}, first=({FirstLogDate:o},{FirstSourceTable},{FirstDeviceLogId}), last=({LastLogDate:o},{LastSourceTable},{LastDeviceLogId}), nextCursor=({CursorLogDate:o},{CursorSourceTable},{CursorDeviceLogId}).",
                        batchNumber,
                        batch.Count,
                        result.RecordsImported - batchImportedBefore,
                        result.DuplicateCount - batchDuplicateBefore,
                        result.UnknownEmployeeCount - batchUnmappedBefore,
                        result.ErrorCount - batchFailedBefore,
                        batchStopwatch.ElapsedMilliseconds,
                        batch[0].LogDate, batch[0].SourceTable, batch[0].DeviceLogId,
                        batch[batch.Count - 1].LogDate, batch[batch.Count - 1].SourceTable, batch[batch.Count - 1].DeviceLogId,
                        batch[batch.Count - 1].LogDate, batch[batch.Count - 1].SourceTable, batch[batch.Count - 1].DeviceLogId);

                    // Advance the intra-run keyset cursor past every row in
                    // this batch regardless of outcome (success/duplicate/
                    // failure) - once a row has been seen (in any outcome),
                    // the next batch in THIS run must never fetch it again.
                    // batch is already ordered ascending (LogDate,
                    // SourceTable, DeviceLogId) by GetDeviceLogsAsync's
                    // contract, so its last row is always the furthest
                    // point reached this batch. Cross-run duplicate safety
                    // still comes from the DB unique index, not from this
                    // cursor.
                    cursor = EsslDeviceLogCursor.FromRow(batch[batch.Count - 1]);

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
                    // Uses the richer "WithResult" call instead of the
                    // plain bool ProcessAttendanceAsync() so the full
                    // reconciliation (found/mapped/unmapped/applied/
                    // rejected/failed, and the exact reason for every row
                    // that did NOT become an AttendanceLog) is visible here
                    // - previously this call's outcome was entirely opaque
                    // (a raw-row-count vs AttendanceLog-count gap with no
                    // way to see why short of manual SQL). Still
                    // best-effort exactly as before: a processing problem
                    // must not make this SYNC run look failed - the raw
                    // punches are already safely persisted and IsProcessed
                    // stays false for retry on the next pass, by this
                    // service or the existing manual "Process Attendance"
                    // action, whichever runs next.
                    var processResult = await _attendanceProcessor.ProcessAttendanceWithResultAsync();

                    _logger.LogInformation(
                        "eSSL sync: attendance processing after import - TotalRawRecords={TotalRawRecords}, PendingBeforeRun={PendingRecords}, MappedRecords={MappedRecords}, UnmappedRecords={UnmappedRecords}, AlreadyProcessed={AlreadyProcessed}, AttendanceCreated={AttendanceCreated}, AttendanceUpdated={AttendanceUpdated}, AttendanceLogsCreated={AttendanceLogsCreated}, Skipped={Skipped}, Failed={Failed}, SuccessfullyProcessed={SuccessfullyProcessed}.",
                        processResult.TotalRawRecords, processResult.PendingRecords, processResult.MappedRecords,
                        processResult.UnmappedRecords, processResult.AlreadyProcessed, processResult.AttendanceCreated,
                        processResult.AttendanceUpdated, processResult.AttendanceLogsCreated, processResult.Skipped,
                        processResult.Failed, processResult.SuccessfullyProcessed);

                    // Requirement: a processing failure must be visible in
                    // Sync Logs/ErrorLog, not just the console/file
                    // ILogger. The run itself never throws upward from
                    // here (still best-effort per the remark above), but
                    // a hard failure (processResult.Success == false, the
                    // outer try/catch in ProcessAttendanceWithResultAsync
                    // caught something) or a meaningfully high per-row
                    // failure count both get their own ErrorLog entry so
                    // they surface on the Error Log admin screen instead
                    // of only in a log file nobody is tailing.
                    if (!processResult.Success)
                    {
                        await _errorLogService.LogAsync(
                            new Exception($"AttendanceProcessorService run reported failure: {processResult.ErrorMessage}"),
                            module: "Biometric Device Integration",
                            feature: "eSSL Attendance Processing",
                            controller: "EsslAttendanceSyncService",
                            action: "SyncAsync",
                            tenantId: tenantId);
                    }
                    else if (processResult.Failed > 0)
                    {
                        await _errorLogService.LogAsync(
                            new Exception(
                                $"AttendanceProcessorService left {processResult.Failed} row(s) unprocessed due to unexpected per-row exceptions " +
                                $"out of {processResult.TotalRawRecords} found this run (Unmapped={processResult.UnmappedRecords}, " +
                                $"RejectedOutOfSequence={processResult.Skipped}). Raw BiometricAttendanceLogs rows remain IsProcessed=false and will be retried on the next sync/Process Attendance run."),
                            module: "Biometric Device Integration",
                            feature: "eSSL Attendance Processing",
                            controller: "EsslAttendanceSyncService",
                            action: "SyncAsync",
                            tenantId: tenantId);
                    }
                }
                catch (Exception procEx)
                {
                    _logger.LogError(procEx, "eSSL sync: AttendanceProcessorService.ProcessAttendanceWithResultAsync failed after import.");

                    await _errorLogService.LogAsync(
                        procEx,
                        module: "Biometric Device Integration",
                        feature: "eSSL Attendance Processing",
                        controller: "EsslAttendanceSyncService",
                        action: "SyncAsync",
                        tenantId: tenantId);
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

                // Phase 12 diagnostic: FINAL RESULT line - must always
                // reconcile exactly: Source(Fetched) = AlreadyImported/Duplicate
                // + Failed + Imported (Unmapped is a SUBSET of Imported, not
                // a separate bucket, since an unmapped row is still staged -
                // only AttendanceProcessorService, logged separately just
                // above this call, decides whether it becomes a real
                // Attendance record). This is the line to check first
                // against a manual reconciliation query like
                // "SELECT COUNT(*) FROM DeviceLogs... WHERE LogDate BETWEEN
                // @From AND @To" - RecordsFound here MUST equal that count
                // for the same window (if it doesn't, the gap is in
                // DiscoverDeviceLogTablesAsync/GetDeviceLogsAsync's SQL, not
                // in mapping/processing).
                var reconciles = result.RecordsFound == (result.DuplicateCount + result.ErrorCount + result.RecordsImported);
                _logger.LogInformation(
                    "eSSL sync FINAL RESULT: tenant={TenantId}, durationSec={DurationSec}, sourceTables=[{Tables}], sourceFetched={Fetched}, imported={Imported} (of which unmapped={Unmapped}), duplicate={Duplicate}, failed={Failed}, reconciles={Reconciles} (Fetched should == Duplicate+Failed+Imported).",
                    tenantId, Math.Round((DateTime.Now - startedAt).TotalSeconds, 1),
                    sourceTables.Count == 0 ? "(none)" : string.Join(", ", sourceTables),
                    result.RecordsFound, result.RecordsImported, result.UnknownEmployeeCount,
                    result.DuplicateCount, result.ErrorCount, reconciles);

                if (!reconciles)
                {
                    _logger.LogWarning(
                        "eSSL sync FINAL RESULT did NOT reconcile for tenant {TenantId}: Fetched={Fetched} but Duplicate+Failed+Imported={Sum}. Investigate before trusting this run's counts.",
                        tenantId, result.RecordsFound, result.DuplicateCount + result.ErrorCount + result.RecordsImported);
                }
            }
            // Requirement: the lifecycle must reach Completed, Failed, OR
            // Cancelled - never stay Running. Cancellation (app shutdown,
            // or a future explicit "Cancel Sync" action) is deliberately
            // its own outcome, distinct from a genuine failure.
            catch (OperationCanceledException)
            {
                _logger.LogWarning(
                    "eSSL sync run cancelled for tenant {TenantId} after {Found} found / {Imported} imported.",
                    tenantId, result.RecordsFound, result.RecordsImported);

                state.LastSyncStatus = "Cancelled";
                state.LastError = "Sync was cancelled before it finished.";

                if (syncLog != null)
                {
                    syncLog.Status = "Cancelled";
                    syncLog.ErrorMessage = "Sync was cancelled before it finished.";
                }

                result.Success = false;
                result.Message = "Sync was cancelled.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "eSSL sync run failed.");

                state.LastSyncStatus = "Failed";
                state.LastError = Truncate(ex.Message, 1000);

                if (syncLog != null)
                {
                    syncLog.Status = "Failed";
                    syncLog.ErrorMessage = Truncate(ex.Message, 500);
                }

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

                if (syncLog != null)
                    syncLog.EndTime = DateTime.Now;

                result.DurationSeconds = Math.Round((DateTime.Now - startedAt).TotalSeconds, 1);

                // Logging/state-write failures must never mask the real
                // sync outcome above, and must never throw out of here.
                // Deliberately CancellationToken.None (NOT ct) - this write
                // is what releases the per-tenant lock, and it MUST
                // persist even when the reason we're in this finally block
                // is that ct itself was already cancelled; passing the
                // cancelled ct here would make this very save throw
                // immediately and leave IsSyncRunning=true stuck in the
                // database despite the in-memory object already being set
                // back to false - the exact permanent-hang symptom this
                // whole fix targets, just moved one line later.
                try
                {
                    if (syncLog != null)
                        _db.BiometricSyncLogs.Add(syncLog);

                    await _db.SaveChangesAsync(CancellationToken.None);
                }
                catch (Exception logEx)
                {
                    _logger.LogError(logEx, "eSSL sync: failed to write BiometricSyncLog / EsslAttendanceSyncState.");
                }
            }

            return result;
        }

        /// <summary>
        /// Atomically claims this tenant's sync lock without running a
        /// sync - see IEsslAttendanceSyncService's remarks for why this
        /// exists (closes the "poll sees a stale/absent state row before
        /// the background job has actually started" race). Same
        /// stale-lock take-over rule as SyncAsync's own internal claim.
        /// </summary>
        public async Task<(bool Success, string Message)> ClaimSyncLockAsync(string tenantId)
        {
            var state = await GetOrCreateStateAsync(tenantId);

            if (state.IsSyncRunning &&
                state.LastSyncStartedAt.HasValue &&
                (DateTime.Now - state.LastSyncStartedAt.Value).TotalMinutes < DefaultStaleLockMinutes)
            {
                return (false, "A sync is already in progress for this tenant. Please wait for it to complete.");
            }

            state.IsSyncRunning = true;
            state.LastSyncStartedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            return (true, "Lock claimed.");
        }

        /// <summary>
        /// Read-only status poll - this is what the UI hits every ~2s
        /// while a sync may be running, and it must never be slow or
        /// blocked. Uses AsNoTracking (no change-tracker overhead for a
        /// pure read) inside a ReadUncommitted transaction, so this query
        /// can NEVER be made to wait behind a long-running sync write
        /// transaction on EsslAttendanceSyncState/BiometricAttendanceLogs
        /// under SQL Server's default READ COMMITTED locking - a dirty
        /// read of in-flight counters is exactly what "live progress"
        /// means here, so the tiny staleness risk is the correct
        /// trade-off. Does NOT create a state row if one doesn't exist yet
        /// (unlike GetOrCreateStateAsync, which only runs from the actual
        /// write path in SyncAsync) - a brand-new tenant simply gets all
        /// default/never-synced values back.
        /// </summary>
        public async Task<EsslSyncSettingsDto> GetSettingsAsync(string tenantId)
        {
            var enabled = await _esslDataSource.IsEnabledAsync(tenantId);

            var strategy = _db.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadUncommitted);

                var state = await _db.EsslAttendanceSyncStates
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.TenantId == tenantId);

                if (state == null)
                    return new EsslSyncSettingsDto { Enabled = enabled };

                var isStale = state.IsSyncRunning &&
                    state.LastSyncStartedAt.HasValue &&
                    (DateTime.Now - state.LastSyncStartedAt.Value).TotalMinutes >= DefaultStaleLockMinutes;

                return new EsslSyncSettingsDto
                {
                    Enabled = enabled,
                    IsSyncRunning = state.IsSyncRunning,
                    LastSyncStartedAt = state.LastSyncStartedAt,
                    LastSyncCompletedAt = state.LastSyncCompletedAt,
                    LastSyncStatus = state.LastSyncStatus,
                    LastError = state.LastError,
                    LastProcessedDeviceLogId = state.LastProcessedDeviceLogId,
                    LastProcessedLogDate = state.LastProcessedLogDate,
                    RecordsRead = state.RecordsRead,
                    RecordsImported = state.RecordsImported,
                    RecordsSkipped = state.RecordsSkipped,
                    RecordsFailed = state.RecordsFailed,
                    CanForceReset = isStale
                };
            });
        }

        /// <summary>
        /// Safe manual recovery (requirement: "Add a safe Reset/Cancel
        /// Stuck Sync mechanism... Do not reset a genuinely active sync
        /// blindly."). Only resets when the lock has been held for at
        /// least DefaultStaleLockMinutes - the exact same staleness rule
        /// SyncAsync's own automatic take-over already uses - so this can
        /// never interrupt a sync that is still genuinely running.
        /// </summary>
        public async Task<(bool Success, string Message)> ResetStuckSyncAsync(string tenantId)
        {
            var state = await GetOrCreateStateAsync(tenantId);

            if (!state.IsSyncRunning)
                return (false, "This tenant's sync is not currently running - there is nothing to reset.");

            if (!state.LastSyncStartedAt.HasValue ||
                (DateTime.Now - state.LastSyncStartedAt.Value).TotalMinutes < DefaultStaleLockMinutes)
            {
                return (false,
                    $"This sync has been running for less than {DefaultStaleLockMinutes} minutes and may still be genuinely active. " +
                    "Please wait before forcing a reset.");
            }

            state.IsSyncRunning = false;
            state.LastSyncCompletedAt = DateTime.Now;
            state.LastSyncStatus = "Cancelled";
            state.LastError = "Manually reset from the UI after being stuck in the Running state for over " +
                DefaultStaleLockMinutes + " minutes.";

            await _db.SaveChangesAsync();

            _logger.LogWarning(
                "eSSL sync: tenant {TenantId}'s stuck sync lock was manually reset (was running since {StartedAt:o}).",
                tenantId, state.LastSyncStartedAt);

            return (true, "The stuck sync has been reset. You can now start a new sync.");
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
            var strategy = _db.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(() => GetSyncHistoryCoreAsync(filter, tenantId));
        }

        // Read-only (requirement #4: SyncLogs must stay responsive while a
        // sync is running) - same ReadUncommitted-isolation reasoning as
        // GetSettingsAsync above, so a page of sync history can never be
        // blocked behind the sync's own writes to this same table.
        private async Task<PagedResult<EsslSyncHistoryDto>> GetSyncHistoryCoreAsync(EsslSyncHistoryFilterDto filter, string tenantId)
        {
            await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadUncommitted);

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
            var strategy = _db.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(() => GetUnmappedEmployeesCoreAsync(tenantId));
        }

        // Read-only (requirement #4: UnmappedEmployees must stay
        // responsive while a sync is running) - same ReadUncommitted
        // reasoning as GetSettingsAsync/GetSyncHistoryAsync above. This is
        // also the query most likely to actually be blocked in practice,
        // since it scans BiometricAttendanceLogs - the exact table a
        // large sync batch-inserts into.
        private async Task<List<EsslUnmappedEmployeeDto>> GetUnmappedEmployeesCoreAsync(string tenantId)
        {
            await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadUncommitted);

            var activeCodes = await _db.EmployeeBiometricMappings
                .AsNoTracking()
                .Where(x => x.IsActive)
                .Select(x => x.BiometricEmployeeCode)
                .ToListAsync();

            // ROOT-CAUSE FIX: normalized the same way as SyncAsync's
            // mappingByCode and AttendanceProcessorService's lookup - see
            // BiometricEmployeeCodeNormalizer's remarks. Before this fix,
            // this screen (and the mapping-based skip logic everywhere
            // else) used a case/whitespace-sensitive HashSet, so a code
            // that actually WAS mapped (just typed/stored with different
            // casing or trailing whitespace) still showed up here as
            // "unmapped" and, more importantly, was still silently skipped
            // by AttendanceProcessorService.
            var activeCodeSet = activeCodes
                .Select(BiometricEmployeeCodeNormalizer.Normalize)
                .ToHashSet();

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
                .Where(g => !activeCodeSet.Contains(BiometricEmployeeCodeNormalizer.Normalize(g.Code)))
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
            // IsBaseTableName (not a bare "== BaseTableName" comparison) -
            // an install whose real base table is spelled "Device_Logs"
            // must still get the short/legacy id format here, exactly like
            // "DeviceLogs" always has, since that IS its base table.
            return EsslDeviceLogTableName.IsBaseTableName(raw.SourceTable)
                ? $"ESSL-{raw.DeviceLogId}"
                : $"ESSL-{raw.SourceTable}-{raw.DeviceLogId}";
        }
    }
}
