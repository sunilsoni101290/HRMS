using Application.Common.Exceptions;
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

        [HttpGet("health")]
        public async Task<IActionResult> Health()
        {
            return Ok(await _service.GetHealthSummaryAsync());
        }

        [HttpGet("dashboard-summary")]
        public async Task<IActionResult> DashboardSummary()
        {
            return Ok(await _service.GetDashboardSummaryAsync());
        }

        [HttpGet("sync-logs")]
        public async Task<IActionResult> SyncLogs([FromQuery] string? deviceId, [FromQuery] int take = 50)
        {
            return Ok(await _syncService.GetRecentSyncLogsAsync(deviceId, take));
        }

        /// <summary>
        /// Kicks off a REAL Test Connection (Device -> assigned BiometricAgent
        /// -> ESSL SDK -> device, not just an LastSyncDate/heartbeat check -
        /// see BiometricDeviceService.RequestTestConnectionAsync). Returns
        /// immediately with Status = Pending; poll GET
        /// {id}/test-connection/{requestId} for the outcome once the agent
        /// picks it up on its next cycle.
        ///
        /// Distinct failure scenarios are mapped to distinct HTTP codes/
        /// messages rather than a single generic "Connection failed" bool,
        /// per this endpoint's requirements: device not found -> 404,
        /// device inactive / no agent assigned / agent offline -> 400 with a
        /// specific message, anything unexpected -> 500 with a safe generic
        /// message (the real exception is logged server-side only, never
        /// returned to the caller).
        /// </summary>
        [HttpPost("{id}/test-connection")]
        public async Task<IActionResult> TestConnection(string id, [FromQuery] string tenantId, [FromQuery] string requestedBy)
        {
            try
            {
                var result = await _service.RequestTestConnectionAsync(id, tenantId, requestedBy);

                return Ok(new ApiResponse<DeviceTestConnectionResultDto>
                {
                    Success = true,
                    Message = "Test connection requested - the assigned agent will attempt the device connection on its next poll cycle.",
                    Data = result
                });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (BadRequestException ex)
            {
                // Device inactive / misconfigured / no agent / agent offline -
                // an expected business outcome, not a server error - still
                // HTTP 200-class "business failure" per the spec (400 here
                // is fine too since these are genuinely bad requests, but the
                // message is always the specific, safe one from the service).
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error requesting test connection for device {Id}.", id);

                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An internal error occurred while testing the device connection."
                });
            }
        }

        /// <summary>Polled by the UI after POST {id}/test-connection until Data.IsComplete is true.</summary>
        [HttpGet("{id}/test-connection/{requestId}")]
        public async Task<IActionResult> TestConnectionStatus(string id, string requestId, [FromQuery] string tenantId)
        {
            try
            {
                var result = await _service.GetTestConnectionResultAsync(requestId, tenantId);

                if (result == null)
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Test connection request not found."
                    });

                return Ok(new ApiResponse<DeviceTestConnectionResultDto>
                {
                    Success = true,
                    Message = "OK",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error polling test connection {RequestId} for device {Id}.", requestId, id);

                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An internal error occurred while checking the test connection status."
                });
            }
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
