using Application.Common.Responses;
using Application.DTOs.Attendances;
using Application.Interfaces.Attendances;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace API.Controllers
{
    /// <summary>
    /// Admin-facing endpoints for the eSSL eTimeTrackLite1 direct-SQL
    /// attendance integration - Settings/Test Connection/Sync Now, Sync
    /// History, and Unmapped Employees (requirement #24's admin UI). Same
    /// [Authorize] baseline as BiometricDeviceController/
    /// EmployeeBiometricMappingController - the real screen-level gate is
    /// AppFeatureConstants.ESSL_INTEGRATION via the existing AppFeature/menu
    /// permission system, enforced on the APP side
    /// (EssRestrictionAttribute's AdminOnlyControllers list) exactly like
    /// every other admin-only screen in this codebase.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class EsslAttendanceController : ControllerBase
    {
        private readonly IEsslAttendanceSyncService _syncService;
        private readonly ITenantService _tenantService;
        private readonly IEsslSyncJobQueue _jobQueue;
        private readonly ILogger<EsslAttendanceController> _logger;

        public EsslAttendanceController(
            IEsslAttendanceSyncService syncService,
            ITenantService tenantService,
            IEsslSyncJobQueue jobQueue,
            ILogger<EsslAttendanceController> logger)
        {
            _syncService = syncService;
            _tenantService = tenantService;
            _jobQueue = jobQueue;
            _logger = logger;
        }

        /// <summary>Card 1 "Integration Status" - runtime/status only, never configuration.</summary>
        [HttpGet("settings")]
        public async Task<IActionResult> GetSettings()
        {
            var tenantId = _tenantService.GetTenantId();
            var settings = await _syncService.GetSettingsAsync(tenantId);

            return Ok(new ApiResponse<EsslSyncSettingsDto> { Success = true, Data = settings });
        }

        /// <summary>Card 2 "Database Configuration" GET - never returns a password, only HasPasswordConfigured.</summary>
        [HttpGet("configuration")]
        public async Task<IActionResult> GetConfiguration()
        {
            var tenantId = _tenantService.GetTenantId();
            var config = await _syncService.GetConfigurationAsync(tenantId);

            return Ok(new ApiResponse<EsslDatabaseConfigViewDto> { Success = true, Data = config });
        }

        /// <summary>
        /// Validates and persists the Database Configuration form. Does NOT
        /// test the connection and does NOT start a sync - the UI calls
        /// Test Connection separately if the admin wants that.
        /// </summary>
        [HttpPost("configuration")]
        public async Task<IActionResult> SaveConfiguration([FromBody] EsslDatabaseConfigDto dto)
        {
            if (!ModelState.IsValid)
            {
                var firstError = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .FirstOrDefault();

                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = firstError ?? "Please correct the highlighted fields."
                });
            }

            var tenantId = _tenantService.GetTenantId();
            var modifiedBy = User?.Identity?.Name ?? "Unknown";

            var (success, message) = await _syncService.SaveConfigurationAsync(dto, tenantId, modifiedBy);

            return Ok(new ApiResponse<object> { Success = success, Message = message });
        }

        /// <summary>
        /// Tests the values currently entered in the form (requirement #6) -
        /// never the saved configuration, never persisted, never starts a
        /// sync. See EsslAttendanceSyncService.TestConnectionAsync(dto, tenantId).
        /// </summary>
        [HttpPost("test-connection")]
        public async Task<IActionResult> TestConnection([FromBody] EsslDatabaseConfigDto dto)
        {
            var tenantId = _tenantService.GetTenantId();

            var (success, message) = await _syncService.TestConnectionAsync(dto ?? new EsslDatabaseConfigDto(), tenantId);

            return Ok(new ApiResponse<object>
            {
                Success = success,
                Message = message
            });
        }

        /// <summary>
        /// Both automatic (background service) and manual/historical sync
        /// go through this exact same SyncAsync call - see
        /// EsslAttendanceSyncService's class remarks. FromDate/ToDate both
        /// null = incremental mode ("Sync Now"); either set = the admin's
        /// explicit manual/historical window ("Historical Import" /
        /// "Retry Failed Sync" - both are just this same call with a
        /// From/To window, see Index.cshtml's retryFailedBtn remarks).
        ///
        /// Non-blocking: this used to await the ENTIRE sync (potentially
        /// tens of minutes for a 100,000+ record historical import) before
        /// returning, which made the browser's own request the thing most
        /// likely to time out - not the SQL command itself (that already
        /// has its own generous CommandTimeout). A long HTTP request is
        /// also, by itself, a poor way to show live progress. This now
        /// enqueues the request onto IEsslSyncJobQueue - consumed by
        /// EsslAttendanceSyncBackgroundService, the SAME already-running,
        /// framework-managed BackgroundService that runs the automatic
        /// sync (not a one-off detached Task.Run) - and responds
        /// immediately. The UI is expected to poll GET
        /// api/EsslAttendance/settings (already exposes IsSyncRunning
        /// plus, now, live RecordsRead/Imported/Skipped/Failed counters -
        /// see EsslSyncSettingsDto) every couple of seconds while a run is
        /// in progress. The one thing that must NOT change: SyncAsync
        /// itself is still the one and only sync engine, called exactly
        /// the same way - only WHERE it's awaited from has moved.
        /// </summary>
        [HttpPost("sync")]
        public async Task<IActionResult> SyncNow([FromBody] EsslSyncRequestDto request)
        {
            var tenantId = _tenantService.GetTenantId();
            var triggeredBy = User?.Identity?.Name ?? "Unknown";
            var syncRequest = request ?? new EsslSyncRequestDto();

            // Atomically claims the lock HERE, synchronously, BEFORE the
            // job is enqueued - not just a read-only check. This closes a
            // real race: a plain read-only check here (what this used to
            // be) leaves a window between "HTTP 200 Queued" and the
            // background consumer actually picking the job off the queue
            // and calling SyncAsync. A client that starts polling
            // GetStatus immediately (as the UI does) could poll inside
            // that window and see IsSyncRunning still false - or, for a
            // tenant with no EsslAttendanceSyncState row yet, no row at
            // all - and wrongly conclude the sync had already finished,
            // showing "Sync Complete" with blank/undefined counters while
            // the real sync was still running. Claiming the lock here
            // means the database already reflects "running" before this
            // action even returns.
            var (claimed, claimMessage) = await _syncService.ClaimSyncLockAsync(tenantId);

            if (!claimed)
            {
                return Ok(new ApiResponse<EsslSyncResultDto>
                {
                    Success = false,
                    Message = claimMessage,
                    Data = null
                });
            }

            var jobId = Guid.NewGuid().ToString("N");

            _jobQueue.Enqueue(new EsslSyncJobRequest
            {
                JobId = jobId,
                TenantId = tenantId,
                Request = syncRequest,
                TriggeredBy = triggeredBy
            });

            _logger.LogInformation(
                "eSSL sync: queued manual job {JobId} for tenant {TenantId}, triggered by {TriggeredBy}.",
                jobId, tenantId, triggeredBy);

            return Ok(new ApiResponse<EsslSyncResultDto>
            {
                Success = true,
                Message = "Sync started in the background. Watch the Integration Status card for live progress.",
                Data = new EsslSyncResultDto
                {
                    Success = true,
                    Message = "Queued as job " + jobId
                }
            });
        }

        /// <summary>
        /// Safe manual recovery for a tenant whose sync lock is genuinely
        /// stuck (see IEsslAttendanceSyncService.ResetStuckSyncAsync's
        /// remarks) - only ever succeeds when the lock has been held for
        /// at least the same staleness threshold the automatic take-over
        /// uses, so this can never interrupt a real in-progress run.
        /// </summary>
        [HttpPost("reset-stuck-sync")]
        public async Task<IActionResult> ResetStuckSync()
        {
            var tenantId = _tenantService.GetTenantId();

            var (success, message) = await _syncService.ResetStuckSyncAsync(tenantId);

            return Ok(new ApiResponse<object> { Success = success, Message = message });
        }

        [HttpGet("sync-history")]
        public async Task<IActionResult> GetSyncHistory([FromQuery] EsslSyncHistoryFilterDto filter)
        {
            var tenantId = _tenantService.GetTenantId();
            var result = await _syncService.GetSyncHistoryAsync(filter ?? new EsslSyncHistoryFilterDto(), tenantId);

            return Ok(new ApiResponse<Application.DTOs.Employee.PagedResult<EsslSyncHistoryDto>>
            {
                Success = true,
                Data = result
            });
        }

        [HttpGet("unmapped-employees")]
        public async Task<IActionResult> GetUnmappedEmployees()
        {
            var tenantId = _tenantService.GetTenantId();
            var result = await _syncService.GetUnmappedEmployeesAsync(tenantId);

            return Ok(new ApiResponse<List<EsslUnmappedEmployeeDto>> { Success = true, Data = result });
        }
    }
}
