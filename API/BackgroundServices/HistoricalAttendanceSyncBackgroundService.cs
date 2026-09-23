using Application.Interfaces.Attendances;
using Application.Interfaces.ErrorLog;
using Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace API.BackgroundServices
{
    /// <summary>
    /// Background worker for the standalone Historical Attendance Sync
    /// feature - a COMPLETELY SEPARATE, NEW BackgroundService from
    /// EsslAttendanceSyncBackgroundService.cs (which is NEVER modified or
    /// even referenced here). Consumes IHistoricalAttendanceSyncJobQueue
    /// (its own, separate in-process queue - never IEsslSyncJobQueue or
    /// IAttendanceProcessingJobQueue) and runs each job through
    /// IHistoricalAttendanceSyncService.RunJobAsync, which in turn calls
    /// dbo.ProcessHistoricalBiometricAttendance.
    ///
    /// Never blocks, is never blocked by, and shares no lock with the eSSL
    /// pipeline - EsslAttendanceSyncState.IsSyncRunning and
    /// HistoricalAttendanceSyncJob.IsRunning are two entirely independent
    /// flags, so an eSSL "Sync Now" and a Historical Attendance Sync run can
    /// execute at the same time for the same tenant without interfering
    /// (see the deliverable's manual test plan item "simultaneous eSSL sync
    /// + historical sync").
    /// </summary>
    public class HistoricalAttendanceSyncBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHistoricalAttendanceSyncJobQueue _jobQueue;
        private readonly ILogger<HistoricalAttendanceSyncBackgroundService> _logger;

        public HistoricalAttendanceSyncBackgroundService(
            IServiceScopeFactory scopeFactory,
            IHistoricalAttendanceSyncJobQueue jobQueue,
            ILogger<HistoricalAttendanceSyncBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _jobQueue = jobQueue;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Startup sweep #1: any job left IsRunning/"Running" by a
            // previous process instance is provably stale (this process
            // just started) - reconcile it before anything else, same
            // reasoning as EsslAttendanceSyncBackgroundService's
            // ReconcileStaleLocksOnStartupAsync.
            await ReconcileStaleLocksOnStartupAsync(stoppingToken);

            // Startup sweep #2: any job still "Queued" (created but never
            // picked up - e.g. the in-memory queue entry was lost by a
            // restart between enqueue and consume, see
            // IHistoricalAttendanceSyncJobQueue's remarks) is re-enqueued
            // here so it is never silently stuck forever.
            await RequeueOrphanedQueuedJobsAsync(stoppingToken);

            await ConsumeJobsAsync(stoppingToken);
        }

        private async Task ReconcileStaleLocksOnStartupAsync(CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IHistoricalAttendanceSyncService>();
                await service.ReconcileStaleLocksAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Historical Attendance Sync: failed to reconcile stale locks on startup.");
                await TryLogToErrorLogAsync(ex, nameof(ReconcileStaleLocksOnStartupAsync));
            }
        }

        private async Task RequeueOrphanedQueuedJobsAsync(CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var queued = await context.HistoricalAttendanceSyncJobs
                    .Where(j => j.Status == "Queued" && !j.IsDeleted)
                    .ToListAsync(stoppingToken);

                foreach (var job in queued)
                {
                    _jobQueue.Enqueue(new HistoricalSyncJobRequest
                    {
                        JobId = job.Id,
                        TenantId = job.TenantId,
                        TriggeredBy = "System (startup re-queue)"
                    });
                }

                if (queued.Count > 0)
                {
                    _logger.LogInformation(
                        "Historical Attendance Sync: re-enqueued {Count} job(s) left Queued by a previous app restart.", queued.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Historical Attendance Sync: failed to re-queue orphaned jobs on startup.");
                await TryLogToErrorLogAsync(ex, nameof(RequeueOrphanedQueuedJobsAsync));
            }
        }

        private async Task ConsumeJobsAsync(CancellationToken stoppingToken)
        {
            try
            {
                await foreach (var job in _jobQueue.ReadAllAsync(stoppingToken))
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var service = scope.ServiceProvider.GetRequiredService<IHistoricalAttendanceSyncService>();

                        // Bounded, not truly unlimited - same reasoning as
                        // EsslAttendanceSyncBackgroundService's manual-job
                        // timeout (a huge historical range legitimately runs
                        // long; this only guards against a genuinely stuck
                        // run so a crashed connection can't hang this
                        // consumer loop forever).
                        using var jobCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                        jobCts.CancelAfter(TimeSpan.FromHours(6));

                        await service.RunJobAsync(job.JobId, jobCts.Token);
                    }
                    catch (Exception ex)
                    {
                        // RunJobAsync already catches everything internally
                        // and always leaves the job row in a terminal state
                        // - this is only a backstop (e.g. DI resolution
                        // itself failing) so one bad job can never take this
                        // consumer loop down.
                        _logger.LogError(ex,
                            "Historical Attendance Sync job {JobId} (tenant {TenantId}) failed to run.",
                            job.JobId, job.TenantId);
                        await TryLogToErrorLogAsync(ex, nameof(ConsumeJobsAsync), userId: job.TriggeredBy);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Host is shutting down - ReadAllAsync ends cleanly.
            }
        }

        private async Task TryLogToErrorLogAsync(Exception ex, string action, string? userId = null)
        {
            try
            {
                using var errorScope = _scopeFactory.CreateScope();
                var errorLogService = errorScope.ServiceProvider.GetRequiredService<IErrorLogService>();

                await errorLogService.LogAsync(
                    ex,
                    module: "Attendance",
                    feature: "Historical Attendance Sync",
                    controller: "HistoricalAttendanceSyncBackgroundService",
                    action: action,
                    userId: userId ?? "System",
                    userName: "System");
            }
            catch
            {
                // Logging must never itself take down the host.
            }
        }
    }
}
