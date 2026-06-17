using Application.DTOs.Attendances;
using Application.Interfaces.Attendances;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BiometricController : ControllerBase
    {
        private readonly IBiometricDeviceService _service;

        public BiometricController(IBiometricDeviceService service)
        {
            _service = service;
        }

        [HttpGet("devices")]
        public async Task<IActionResult> GetDevices()
        {
            return Ok(await _service.GetDevices());
        }

        [HttpPost("device")]
        public async Task<IActionResult> AddDevice(
            BiometricDeviceDto dto)
        {
            return Ok(await _service.AddDevice(dto));
        }

        [HttpPost("sync/{deviceId}")]
        public async Task<IActionResult> SyncAttendance(
            string deviceId)
        {
            return Ok(await _service.SyncAttendance(deviceId));
        }

        [HttpGet("logs/{deviceId}")]
        public async Task<IActionResult> GetLogs(
            string deviceId)
        {
            return Ok(await _service.FetchAttendanceLogs(deviceId));
        }
    }
}
