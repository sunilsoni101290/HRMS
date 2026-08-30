using Application.DTOs.Attendances;
using Application.Interfaces.Attendances;
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
        private readonly ILogger<EsslAttendanceSyncBackgroundService> _logger;

        public EsslAttendanceSyncBackgroundService(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            ILogger<EsslAttendanceSyncBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
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

        /// <summary>Runs one sync pass for every active tenant and returns how many minutes to wait before the next cycle (see class remarks).</summary>
        private async Task<int> RunCycleAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var syncService = scope.ServiceProvider.GetRequiredService<IEsslAttendanceSyncService>();

            var tenantIds = await context.Tenants
                .AsNoTracking()
                .Where(t => t.IsActive)
                .Select(t => t.Id)
                .ToListAsync(ct);

            foreach (var tenantId in tenantIds)
            {
                try
                {
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
                        ct);

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
