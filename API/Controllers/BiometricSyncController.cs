using Application.DTOs.Attendances;
using Application.Interfaces.Attendances;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BiometricSyncController
    : ControllerBase
    {
        private readonly  IBiometricSyncService _service;

        // Enqueues onto the same background-worker pipeline eSSL sync
        // already uses (IEsslSyncJobQueue -> EsslAttendanceSyncBackgroundService),
        // instead of calling IAttendanceProcessorService directly and
        // blocking this HTTP request on it. AttendanceProcessorService now
        // drains the WHOLE unprocessed backlog every time it runs (not just
        // what this one request imported), so keeping that call inline here
        // would mean a routine agent push could block on tens of thousands
        // of unrelated rows - see IAttendanceProcessingJobQueue's remarks.
        private readonly IAttendanceProcessingJobQueue _processingQueue;

        public BiometricSyncController(IBiometricSyncService service, IAttendanceProcessingJobQueue processingQueue)
        {
            _service = service;
            _processingQueue = processingQueue;
        }

        /// <summary>
        /// Called by the on-site BiometricAgent (running on a machine at the
        /// client's location, on the same LAN as the biometric device) to push
        /// newly collected punches. Authenticated via DeviceCode + DeviceKey in
        /// the body rather than a user JWT, since the agent is not a logged-in
        /// user. Still requires the X-Tenant-ID header (see TenantMiddleware).
        /// </summary>
        [HttpPost("ingest")]
        [AllowAnonymous]
        public async Task<IActionResult> Ingest(
            [FromBody] PunchIngestRequestDto request)
        {
            var result = await _service.IngestPunchesAsync(request);

            if (!result.Success)
                return Unauthorized(result);

            if (result.InsertedCount > 0)
                _processingQueue.Enqueue(new AttendanceProcessingJobRequest { JobId = Guid.NewGuid().ToString(), TriggeredBy = "BiometricSyncController.Ingest" });

            return Ok(result);
        }

        [HttpPost("sync/{deviceId}")]
        public async Task<IActionResult>Sync(string deviceId)
        {
            await _service
                .SyncDeviceLogsAsync(deviceId);

            _processingQueue.Enqueue(new AttendanceProcessingJobRequest { JobId = Guid.NewGuid().ToString(), TriggeredBy = $"BiometricSyncController.Sync({deviceId})" });

            return Ok("Attendance sync queued.");
        }

        [HttpPost("sync-all")]
        public async Task<IActionResult>SyncAll()
        {
            await _service
                .SyncAllDevicesAsync();

            _processingQueue.Enqueue(new AttendanceProcessingJobRequest { JobId = Guid.NewGuid().ToString(), TriggeredBy = "BiometricSyncController.SyncAll" });

            return Ok("All devices synced; attendance processing queued.");
        }

        [HttpGet("logs/{deviceId}")]
        public async Task<IActionResult>Logs(string deviceId)
        {
            return Ok(
                await _service
                .GetRawLogsAsync(deviceId));
        }
    }
}
