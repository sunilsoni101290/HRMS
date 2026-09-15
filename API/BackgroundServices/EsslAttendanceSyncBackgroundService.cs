using Application.DTOs.Attendances;
using Application.Interfaces.Attendances;
using Application.Interfaces.ErrorLog;
using Domain.Entities;
using Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace API.BackgroundServices
{
    /// <summary>
    /// Automatic, periodic eSSL eTimeTrackLite1 direct-SQL sync (requirement
    /// #12). Same skeleton as CompOffDetectionService/LeaveAccrualService -
    /// singleton BackgroundService, scoped dependencies resolved fresh each
    /// cycle via IServiceScopeFactory. All the real logic (window
    /// calculation, idempotency, locking, employee mapping, error isolation)
    /// lives in EsslAttendanceSyncService.SyncAsync.
    ///
    /// Enable/disable and Sync Interval are now per-tenant, editable via the
    /// Settings form (EsslIntegrationSetting) rather than a single global
    /// appsettings.json flag - so this loop always runs every cycle (no
    /// outer "is the WHOLE integration enabled" gate); SyncAsync itself
    /// checks each tenant's own IntegrationEnabled and simply returns early
    /// for a disabled tenant. Since this is still one shared timer (not one
    /// per tenant - no need for that added complexity), the wait between
    /// cycles is the SHORTEST SyncIntervalMinutes among tenants that
    /// currently have the integration enabled, so no tenant's configured
    /// interval is ever exceeded, at the cost of possibly polling other,
    /// less time-sensitive tenants slightly more often than they asked for.
    /// EsslDatabase:SyncIntervalMinutes in appsettings.json remains the
    /// fallback default for a tenant that has never saved settings yet.
    ///
    /// Multi-tenant: every active Tenant is synced once per cycle,
    /// sequentially (not in parallel) - each call to SyncAsync already
    /// serializes itself per-tenant via EsslAttendanceSyncState.IsSyncRunning,
    /// so a manual "Sync Now" for one tenant and this background cycle can
    /// never race for that same tenant's batch.
    /// </summary>
    public class EsslAttendanceSyncBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly IEsslSyncJobQueue _jobQueue;

        // Second, independent queue: "just drain the biometric attendance
        // backlog" requests from BiometricSyncController (the LAN-agent
        // path), consumed by this same BackgroundService instead of that
        // controller calling IAttendanceProcessorService inline and
        // blocking the HTTP response - see IAttendanceProcessingJobQueue's
        // remarks.
        private readonly IAttendanceProcessingJobQueue _processingQueue;
        private readonly ILogger<EsslAttendanceSyncBackgroundService> _logger;

        public EsslAttendanceSyncBackgroundService(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            IEsslSyncJobQueue jobQueue,
            IAttendanceProcessingJobQueue processingQueue,
            ILogger<EsslAttendanceSyncBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _jobQueue = jobQueue;
            _processingQueue = processingQueue;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Recover any tenant left stuck "Running" by an app restart
            // (requirement: "Recover stale Running jobs after application
            // restart") BEFORE either loop below starts touching the
            // table. This is a fresh process just starting up, so ANY
            // row still showing IsSyncRunning=true at this exact moment is
            // provably left over from a process that no longer exists -
            // nothing legitimate could have set it since this instant -
            // so it is always safe to clear, with no staleness/age check
            // needed (unlike ResetStuckSyncAsync's manual UI reset, which
            // DOES need one, since it can be clicked while a same-process
            // sync is genuinely still running).
            await ReconcileStaleLocksOnStartupAsync(stoppingToken);

            // Two independent loops share this one host-managed service:
            //   - PeriodicAutoSyncLoopAsync: the original automatic,
            //     timer-driven incremental sync across every active tenant
            //     (unchanged behavior).
            //   - ConsumeManualJobsAsync: NEW - drains IEsslSyncJobQueue,
            //     so a manual "Sync Now" / "Historical Import" / "Retry
            //     Failed Sync" click also runs through this same
            //     framework-managed BackgroundService rather than a
            //     detached Task.Run started directly from the API
            //     controller. Both loops call the exact same
            //     IEsslAttendanceSyncService.SyncAsync core engine - the
            //     only thing this changes is where a manual request is
            //     actually executed, never the sync logic itself.
            // Running them via Task.WhenAll means a fault in one does not
            // silently stop the other (each already has its own top-level
            // try/catch per iteration/item), and the host still shuts
            // this whole service down cleanly on stoppingToken.
            await Task.WhenAll(
                PeriodicAutoSyncLoopAsync(stoppingToken),
                ConsumeManualJobsAsync(stoppingToken),
                ConsumeAttendanceProcessingJobsAsync(stoppingToken));
        }

        /// <summary>
        /// Drains IAttendanceProcessingJobQueue - the LAN-agent path's
        /// "please run the biometric attendance processor now" requests
        /// (BiometricSyncController.Ingest/Sync/SyncAll), so those API
        /// actions can enqueue and return immediately instead of blocking
        /// on AttendanceProcessorService draining the whole backlog inline.
        /// Sequential and a fresh DI scope per job, same shape as
        /// ConsumeManualJobsAsync - AttendanceProcessorService has no
        /// per-tenant locking of its own (unlike SyncAsync), but the
        /// queue's own coalescing (see AttendanceProcessingJobQueue) already
        /// keeps this from running more than one drain at a time in
        /// practice, and running two drains back-to-back is harmless anyway
        /// (the second simply finds nothing left to do).
        /// </summary>
        private async Task ConsumeAttendanceProcessingJobsAsync(CancellationToken stoppingToken)
        {
            try
            {
                await foreach (var job in _processingQueue.ReadAllAsync(stoppingToken))
                {
                    try
                    {
                        // IAttendanceProcessorService.ProcessAttendanceWithResultAsync
                        // takes no CancellationToken - unlike SyncAsync, its own
                        // MaxBatchesPerRun * batchSize ceiling (see
                        // AttendanceProcessorService) is what bounds one run, so
                        // there is no per-job timeout to apply here.
                        using var scope = _scopeFactory.CreateScope();
                        var processor = scope.ServiceProvider.GetRequiredService<IAttendanceProcessorService>();

                        var result = await processor.ProcessAttendanceWithResultAsync();

                        _logger.LogInformation(
                            "Attendance processing job {JobId} (triggered by {TriggeredBy}) finished: " +
                            "found={Found}, applied={Applied}, unmapped={Unmapped}, skipped={Skipped}, failed={Failed}.",
                            job.JobId, job.TriggeredBy, result.TotalRawRecords, result.SuccessfullyProcessed,
                            result.UnmappedRecords, result.Skipped, result.Failed);
                    }
                    catch (Exception ex)
                    {
                        // ProcessAttendanceWithResultAsync already catches
                        // everything internally and always returns a result
                        // rather than throwing - this is only a backstop
                        // (e.g. DI resolution itself failing) so one bad job
                        // can never take this consumer loop down.
                        _logger.LogError(ex,
                            "Attendance processing job {JobId} (triggered by {TriggeredBy}) failed to run.",
                            job.JobId, job.TriggeredBy);
                        await TryLogToErrorLogAsync(ex, nameof(ConsumeAttendanceProcessingJobsAsync));
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Host is shutting down - ReadAllAsync ends cleanly.
            }
        }

        /// <summary>
        /// One-time startup sweep - see ExecuteAsync's remarks. Never
        /// throws out of here; a failure to reconcile must not prevent the
        /// host from starting the two real sync loops.
        /// </summary>
        private async Task ReconcileStaleLocksOnStartupAsync(CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var stuckStates = await context.EsslAttendanceSyncStates
                    .Where(x => x.IsSyncRunning)
                    .ToListAsync(stoppingToken);

                if (stuckStates.Count == 0)
                    return;

                var now = DateTime.Now;

                foreach (var state in stuckStates)
                {
                    state.IsSyncRunning = false;
                    state.LastSyncCompletedAt = now;
                    state.LastSyncStatus = "Failed";
                    state.LastError = "Sync was interrupted by an application restart and has been automatically reset.";
                }

                await context.SaveChangesAsync(stoppingToken);

                _logger.LogWarning(
                    "eSSL sync: reconciled {Count} tenant(s) left stuck in the Running state by a previous app restart: [{TenantIds}].",
                    stuckStates.Count, string.Join(", ", stuckStates.Select(x => x.TenantId)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "eSSL sync: failed to reconcile stale sync locks on startup.");
                await TryLogToErrorLogAsync(ex, nameof(ReconcileStaleLocksOnStartupAsync));
            }
        }

        // Shared helper for every backstop catch in this file - a fresh
        // scope per call (this class's own scope may already be gone by the
        // time an exception unwinds up to a caller), and itself wrapped so
        // a logging failure can never cascade into a second unhandled
        // exception or take down whichever loop called it.
        private async Task TryLogToErrorLogAsync(Exception ex, string action, string? userId = null)
        {
            try
            {
                using var errorScope = _scopeFactory.CreateScope();
                var errorLogService = errorScope.ServiceProvider.GetRequiredService<IErrorLogService>();

                await errorLogService.LogAsync(
                    ex,
                    module: "Biometric Device Integration",
                    feature: "eSSL Attendance Sync",
                    controller: "EsslAttendanceSyncBackgroundService",
                    action: action,
                    userId: userId ?? "System",
                    userName: "System");
            }
            catch
            {
                // Logging must never itself take down the host.
            }
        }

        private async Task PeriodicAutoSyncLoopAsync(CancellationToken stoppingToken)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                var intervalMinutes = 5;

                try
                {
                    intervalMinutes = await RunCycleAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    // A failed cycle must never crash the host.
                    _logger.LogError(ex, "eSSL attendance sync cycle failed.");
                    await TryLogToErrorLogAsync(ex, nameof(RunCycleAsync));
                }

                if (intervalMinutes < 1) intervalMinutes = 5;

                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    // Host is shutting down.
                }
            }
        }

        /// <summary>
        /// Drains IEsslSyncJobQueue one job at a time (a fresh DI scope per
        /// job, exactly like RunCycleAsync's own scope-per-cycle pattern) -
        /// this is where a manual Sync Now / Historical Import / Retry
        /// Failed Sync request actually executes. Sequential by design:
        /// SyncAsync already serializes itself per tenant via
        /// EsslAttendanceSyncState.IsSyncRunning, so processing the queue
        /// one item at a time here adds no real throughput cost (two jobs
        /// for the SAME tenant could never usefully run at once anyway)
        /// while keeping this loop, and its error handling, simple.
        /// </summary>
        private async Task ConsumeManualJobsAsync(CancellationToken stoppingToken)
        {
            try
            {
                await foreach (var job in _jobQueue.ReadAllAsync(stoppingToken))
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var syncService = scope.ServiceProvider.GetRequiredService<IEsslAttendanceSyncService>();
                        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

                        // Bounded, not truly unlimited: a per-job maximum
                        // runtime (default 3 hours, configurable) linked to
                        // the host's own stoppingToken. This is deliberately
                        // NOT "stoppingToken directly" - a job already being
                        // processed should run to a natural completion/
                        // checkpoint rather than being cut off the instant a
                        // host shutdown begins; it's also deliberately NOT
                        // "CancellationToken.None" (unbounded forever) - if
                        // SyncAsync is ever genuinely stuck on some blocking
                        // I/O this fix's root-cause change did not
                        // anticipate, this timeout is what actually forces
                        // it to unwind through SyncAsync's now-guaranteed
                        // finally block and release the tenant's lock,
                        // rather than hanging indefinitely again.
                        var maxDurationMinutes = configuration.GetValue<int?>("EsslDatabase:MaxSyncDurationMinutes") ?? 180;

                        using var jobCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                        jobCts.CancelAfter(TimeSpan.FromMinutes(maxDurationMinutes));

                        // lockAlreadyClaimed: true - the controller's
                        // SyncNow action already claimed this tenant's
                        // lock synchronously (via ClaimSyncLockAsync)
                        // before this job was even enqueued, so SyncAsync
                        // must not try to claim it again here.
                        var result = await syncService.SyncAsync(
                            job.Request, job.TenantId, job.TriggeredBy, jobCts.Token, lockAlreadyClaimed: true);

                        _logger.LogInformation(
                            "eSSL manual sync job {JobId} (tenant {TenantId}, triggered by {TriggeredBy}) finished: " +
                            "found {Found}, imported {Imported}, skipped {Skipped}, errors {Errors}, {Duration}s.",
                            job.JobId, job.TenantId, job.TriggeredBy,
                            result.RecordsFound, result.RecordsImported, result.RecordsSkipped,
                            result.ErrorCount, result.DurationSeconds);
                    }
                    catch (Exception ex)
                    {
                        // SyncAsync already catches everything internally
                        // and always returns a result rather than throwing
                        // - this is only a backstop (e.g. DI resolution
                        // itself failing) so one bad job can never take
                        // this consumer loop down and stop every
                        // subsequent queued job from ever running.
                        _logger.LogError(ex,
                            "eSSL manual sync job {JobId} (tenant {TenantId}) failed to run.",
                            job.JobId, job.TenantId);
                        await TryLogToErrorLogAsync(ex, nameof(ConsumeManualJobsAsync), userId: job.TriggeredBy);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Host is shutting down - ReadAllAsync ends cleanly.
            }
        }

        /// <summary>Runs one sync pass for every active tenant and returns how many minutes to wait before the next cycle (see class remarks).</summary>
        private async Task<int> RunCycleAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var syncService = scope.ServiceProvider.GetRequiredService<IEsslAttendanceSyncService>();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            var maxDurationMinutes = configuration.GetValue<int?>("EsslDatabase:MaxSyncDurationMinutes") ?? 180;

            var tenantIds = await context.Tenants
                .AsNoTracking()
                .Where(t => t.IsActive)
                .Select(t => t.Id)
                .ToListAsync(ct);

            foreach (var tenantId in tenantIds)
            {
                try
                {
                    // Same bounded-not-unlimited timeout as the manual job
                    // path (see ConsumeManualJobsAsync's remarks) - one
                    // tenant's automatic cycle can never hang the whole
                    // periodic loop forever.
                    using var tenantCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    tenantCts.CancelAfter(TimeSpan.FromMinutes(maxDurationMinutes));

                    // FromDate/ToDate both null = automatic incremental mode
                    // (resumes from EsslAttendanceSyncState per tenant).
                    // SyncAsync itself checks this tenant's own
                    // EsslIntegrationSetting.IntegrationEnabled and returns
                    // early (Success = false, no-op) if it's off - nothing
                    // further to check here.
                    var result = await syncService.SyncAsync(
                        new EsslSyncRequestDto(),
                        tenantId,
                        "System",
                        tenantCts.Token);

                    if (result.Success && (result.RecordsImported > 0 || result.ErrorCount > 0))
                    {
                        _logger.LogInformation(
                            "eSSL sync (tenant {TenantId}): found {Found}, imported {Imported}, skipped {Skipped}, unknown employees {Unknown}, errors {Errors}, {Duration}s.",
                            tenantId, result.RecordsFound, result.RecordsImported, result.RecordsSkipped,
                            result.UnknownEmployeeCount, result.ErrorCount, result.DurationSeconds);
                    }
                }
                catch (Exception ex)
                {
                    // One tenant's failure must never stop the others.
                    _logger.LogError(ex, "eSSL sync failed for tenant {TenantId}.", tenantId);
                    await TryLogToErrorLogAsync(ex, nameof(RunCycleAsync));
                }
            }

            var configuredIntervals = await context.EsslIntegrationSettings
                .AsNoTracking()
                .Where(x => x.IntegrationEnabled)
                .Select(x => x.SyncIntervalMinutes)
                .ToListAsync(ct);

            var defaultInterval = _configuration.GetValue<int?>("EsslDatabase:SyncIntervalMinutes") ?? 5;

            return configuredIntervals.Count > 0 ? configuredIntervals.Min() : defaultInterval;
        }
    }
}
