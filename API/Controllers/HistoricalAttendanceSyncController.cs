using Application.Common.Responses;
using Application.DTOs.Attendances;
using Application.DTOs.Employee;
using Application.Interfaces.Attendances;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace API.Controllers
{
    /// <summary>
    /// Admin-facing endpoints for the standalone Historical Attendance Sync
    /// feature (spec sections 1/6) - Preview / Start / Status / History.
    /// A COMPLETELY SEPARATE controller from EsslAttendanceController.cs
    /// (never modified, never referenced here) - this never calls
    /// IEsslAttendanceSyncService or touches EsslAttendanceSyncState.
    /// Admin/HR-only via EssRestrictionAttribute's AdminOnlyControllers list
    /// on the APP side, same gating style as EsslAttendance/
    /// EmployeeBiometricMapping.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class HistoricalAttendanceSyncController : ControllerBase
    {
        private readonly IHistoricalAttendanceSyncService _service;
        private readonly ITenantService _tenantService;
        private readonly IHistoricalAttendanceSyncJobQueue _jobQueue;
        private readonly ILogger<HistoricalAttendanceSyncController> _logger;

        public HistoricalAttendanceSyncController(
            IHistoricalAttendanceSyncService service,
            ITenantService tenantService,
            IHistoricalAttendanceSyncJobQueue jobQueue,
            ILogger<HistoricalAttendanceSyncController> logger)
        {
            _service = service;
            _tenantService = tenantService;
            _jobQueue = jobQueue;
            _logger = logger;
        }

        /// <summary>Read-only - "Preview Records" button. Never writes anything, never enqueues a job.</summary>
        [HttpPost("preview")]
        public async Task<IActionResult> Preview([FromBody] HistoricalSyncRequestDto request)
        {
            var tenantId = _tenantService.GetTenantId();
            var result = await _service.PreviewAsync(request ?? new HistoricalSyncRequestDto(), tenantId);

            return Ok(new ApiResponse<HistoricalSyncPreviewDto> { Success = true, Data = result });
        }

        /// <summary>
        /// "Start Historical Sync" button. Atomically claims this feature's
        /// own lock and creates the job row (Status = Queued), enqueues it
        /// onto IHistoricalAttendanceSyncJobQueue, and returns immediately -
        /// the actual processing happens in
        /// HistoricalAttendanceSyncBackgroundService (spec section 6). Never
        /// touches IEsslSyncJobQueue or EsslAttendanceSyncState.
        /// </summary>
        [HttpPost("start")]
        public async Task<IActionResult> Start([FromBody] HistoricalSyncRequestDto request)
        {
            var tenantId = _tenantService.GetTenantId();
            var requestedBy = User?.Identity?.Name ?? "Unknown";
            var syncRequest = request ?? new HistoricalSyncRequestDto();

            var (claimed, message, jobId) = await _service.ClaimAndCreateJobAsync(syncRequest, tenantId, requestedBy);

            if (!claimed)
            {
                return Ok(new ApiResponse<HistoricalSyncStartResultDto>
                {
                    Success = false,
                    Message = message,
                    Data = null
                });
            }

            _jobQueue.Enqueue(new HistoricalSyncJobRequest
            {
                JobId = jobId!,
                TenantId = tenantId,
                TriggeredBy = requestedBy
            });

            _logger.LogInformation(
                "Historical Attendance Sync: queued job {JobId} for tenant {TenantId} ({From:dd-MMM-yyyy} to {To:dd-MMM-yyyy}), triggered by {TriggeredBy}.",
                jobId, tenantId, syncRequest.FromDate, syncRequest.ToDate, requestedBy);

            return Ok(new ApiResponse<HistoricalSyncStartResultDto>
            {
                Success = true,
                Message = "Historical Attendance Sync started in the background. Watch the Progress/Status card for live progress.",
                Data = new HistoricalSyncStartResultDto { Success = true, Message = message, JobId = jobId }
            });
        }

        /// <summary>Status poll - backs the UI's live Progress/Status card while a job is Queued/Running.</summary>
        [HttpGet("status/{jobId}")]
        public async Task<IActionResult> GetStatus(string jobId)
        {
            var tenantId = _tenantService.GetTenantId();
            var job = await _service.GetJobStatusAsync(jobId, tenantId);

            if (job == null)
                return Ok(new ApiResponse<HistoricalSyncJobDto> { Success = false, Message = "Job not found.", Data = null });

            return Ok(new ApiResponse<HistoricalSyncJobDto> { Success = true, Data = job });
        }

        /// <summary>Job history list - backs the "Processing Summary" history table.</summary>
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] HistoricalSyncJobFilterDto filter)
        {
            var tenantId = _tenantService.GetTenantId();
            var result = await _service.GetJobHistoryAsync(filter ?? new HistoricalSyncJobFilterDto(), tenantId);

            return Ok(new ApiResponse<PagedResult<HistoricalSyncJobDto>> { Success = true, Data = result });
        }
    }
}
