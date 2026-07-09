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

        private readonly IAttendanceProcessorService _processor;

        public BiometricSyncController(IBiometricSyncService service,IAttendanceProcessorService processor)
        {
            _service = service;
            _processor = processor;
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
                await _processor.ProcessAttendanceAsync();

            return Ok(result);
        }

        [HttpPost("sync/{deviceId}")]
        public async Task<IActionResult>Sync(string deviceId)
        {
            await _service
                .SyncDeviceLogsAsync(deviceId);

            await _processor
                .ProcessAttendanceAsync();

            return Ok("Attendance Synced");
        }

        [HttpPost("sync-all")]
        public async Task<IActionResult>SyncAll()
        {
            await _service
                .SyncAllDevicesAsync();

            await _processor
                .ProcessAttendanceAsync();

            return Ok("All Devices Synced");
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
