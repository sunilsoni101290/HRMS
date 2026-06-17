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
