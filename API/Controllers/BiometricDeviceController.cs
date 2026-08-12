using Application.Common.Responses;
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
    public class BiometricDeviceController
    : ControllerBase
    {
        private readonly
            IBiometricDeviceService _service;

        private readonly IBiometricSyncService _syncService;
        private readonly IAttendanceProcessorService _processor;
        private readonly ILogger<BiometricDeviceController> _logger;

        public BiometricDeviceController(
            IBiometricDeviceService service,
            IBiometricSyncService syncService,
            IAttendanceProcessorService processor,
            ILogger<BiometricDeviceController> logger)
        {
            _service = service;
            _syncService = syncService;
            _processor = processor;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            return Ok(await _service.GetAllAsync());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult>Get(string id)
        {
            return Ok(
                await _service.GetByIdAsync(id));
        }

        [HttpPost]
        public async Task<IActionResult> Create(BiometricDeviceDto dto)
        {
            try
            {
                var result = await _service.CreateAsync(dto);

                return Ok(new ApiResponse<BiometricDeviceDto>
                {
                    Success = true,
                    Message = "Biometric device created successfully.",
                    Data = result
                });
            }
            catch (InvalidOperationException ex)
            {
                // e.g. AgentId doesn't belong to this tenant - a caller
                // mistake worth surfacing verbatim, not a generic 400.
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create biometric device.");

                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Failed to create biometric device."
                });
            }
        }

        [HttpPut]
        public async Task<IActionResult>Update(BiometricDeviceDto dto)
        {
            try
            {
                var ok = await _service.UpdateAsync(dto);

                return Ok(new ApiResponse<BiometricDeviceDto>
                {
                    Success = ok,
                    Message = ok ? "Biometric device updated successfully." : "Device not found.",
                    Data = ok ? dto : null
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update biometric device {Id}.", dto?.Id);

                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Failed to update biometric device."
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult>Delete(string id)
        {
            return Ok(
                await _service.DeleteAsync(id));
        }

        [HttpGet("test/{id}")]
        public async Task<IActionResult>Test(string id)
        {
            return Ok(
                await _service
                .TestConnectionAsync(id));
        }

        [HttpGet("health")]
        public async Task<IActionResult> Health()
        {
            return Ok(await _service.GetHealthSummaryAsync());
        }

        /// <summary>
        /// Spec-shaped alias of GET test/{id} (POST /api/BiometricDevice/{id}/test-connection).
        /// Kept alongside the legacy GET test/{id} route - both call the same
        /// service method, so there is no duplicated business logic.
        /// </summary>
        [HttpPost("{id}/test-connection")]
        public async Task<IActionResult> TestConnection(string id)
        {
            var success = await _service.TestConnectionAsync(id);

            return Ok(new ApiResponse<bool>
            {
                Success = success,
                Message = success ? "Connection successful." : "Connection failed. The device did not respond.",
                Data = success
            });
        }

        /// <summary>
        /// Pulls/processes whatever punches are queued for this device and
        /// runs them through AttendanceProcessorService. Same underlying
        /// calls as BiometricSyncController's sync/{deviceId}, exposed here
        /// too so device-scoped operations live under one resource path.
        /// </summary>
        [HttpPost("{id}/sync")]
        public async Task<IActionResult> Sync(string id)
        {
            var ok = await _syncService.SyncDeviceLogsAsync(id);

            await _processor.ProcessAttendanceAsync();

            return Ok(new ApiResponse<object>
            {
                Success = ok,
                Message = ok ? "Device synced and attendance processed." : "Device not found or sync failed."
            });
        }

        /// <summary>Operational snapshot: connection state, last sync/seen, assigned agent liveness, pending punch count, last error.</summary>
        [HttpGet("{id}/status")]
        public async Task<IActionResult> Status(string id)
        {
            var status = await _service.GetStatusAsync(id);

            if (status == null)
                return NotFound();

            return Ok(status);
        }
    }
}
