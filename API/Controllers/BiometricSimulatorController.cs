using Application.Common.Exceptions;
using Application.Common.Responses;
using Application.DTOs.Attendances;
using Application.Interfaces.Attendances;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>
    /// Development-only. Lets the ERP UI fire hand-crafted punches through
    /// the real BiometricSyncService.IngestPunchesAsync pipeline without a
    /// physical eSSL device or a running BiometricAgent Windows Service - see
    /// IBiometricSimulatorService.
    ///
    /// Environment-gated here (IWebHostEnvironment.IsDevelopment()) as the
    /// first line of defense so this never becomes reachable in production
    /// regardless of what the UI does. A "Biometric Simulator" permission is
    /// layered on top separately (see Task D / PermissionService) - this gate
    /// alone is not meant to be the only control.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BiometricSimulatorController : ControllerBase
    {
        private readonly IBiometricSimulatorService _service;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<BiometricSimulatorController> _logger;

        public BiometricSimulatorController(
            IBiometricSimulatorService service,
            IWebHostEnvironment env,
            ILogger<BiometricSimulatorController> logger)
        {
            _service = service;
            _env = env;
            _logger = logger;
        }

        private bool EnsureDevelopment(out IActionResult? forbidResult)
        {
            if (_env.IsDevelopment())
            {
                forbidResult = null;
                return true;
            }

            forbidResult = StatusCode(403, new ApiResponse<object>
            {
                Success = false,
                Message = "The Biometric Simulator is only available in the Development environment."
            });

            return false;
        }

        [HttpGet("mapped-employees")]
        public async Task<IActionResult> GetMappedEmployees([FromQuery] string tenantId)
        {
            if (!EnsureDevelopment(out var forbid)) return forbid!;

            return Ok(await _service.GetMappedEmployeesAsync(tenantId));
        }

        [HttpPost("punch")]
        public async Task<IActionResult> SimulatePunch([FromBody] SimulatePunchRequestDto request, [FromQuery] string tenantId)
        {
            if (!EnsureDevelopment(out var forbid)) return forbid!;

            return await RunAsync(() => _service.SimulatePunchAsync(request, tenantId));
        }

        [HttpPost("full-day")]
        public async Task<IActionResult> GenerateFullDay([FromBody] GenerateFullDayRequestDto request, [FromQuery] string tenantId)
        {
            if (!EnsureDevelopment(out var forbid)) return forbid!;

            return await RunAsync(() => _service.GenerateFullDayAsync(request, tenantId));
        }

        [HttpPost("scenario")]
        public async Task<IActionResult> RunScenario([FromBody] RunScenarioRequestDto request, [FromQuery] string tenantId)
        {
            if (!EnsureDevelopment(out var forbid)) return forbid!;

            return await RunAsync(() => _service.RunScenarioAsync(request, tenantId));
        }

        [HttpPost("simulate-api-failure")]
        public async Task<IActionResult> SimulateApiFailure([FromQuery] string deviceId, [FromQuery] string tenantId)
        {
            if (!EnsureDevelopment(out var forbid)) return forbid!;

            return await RunAsync(() => _service.SimulateApiFailureAsync(deviceId, tenantId));
        }

        [HttpPost("simulate-device-offline")]
        public async Task<IActionResult> SimulateDeviceOffline([FromQuery] string deviceId, [FromQuery] string tenantId)
        {
            if (!EnsureDevelopment(out var forbid)) return forbid!;

            return await RunAsync(() => _service.SimulateDeviceOfflineAsync(deviceId, tenantId));
        }

        [HttpPost("clear-test-data")]
        public async Task<IActionResult> ClearTestData([FromBody] ClearTestDataRequestDto request, [FromQuery] string tenantId)
        {
            if (!EnsureDevelopment(out var forbid)) return forbid!;

            try
            {
                var count = await _service.ClearTestDataAsync(request, tenantId);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = $"Cleared {count} raw punch(es) and their derived attendance."
                });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ApiResponse<object> { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Simulator: failed to clear test data.");

                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An internal error occurred while clearing test data."
                });
            }
        }

        private async Task<IActionResult> RunAsync(Func<Task<SimulatorResultDto>> action)
        {
            try
            {
                var result = await action();

                return Ok(new ApiResponse<SimulatorResultDto>
                {
                    Success = result.Success,
                    Message = result.Message,
                    Data = result
                });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ApiResponse<object> { Success = false, Message = ex.Message });
            }
            catch (BadRequestException ex)
            {
                return BadRequest(new ApiResponse<object> { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Simulator: unexpected error running a simulation action.");

                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An internal error occurred while simulating the punch."
                });
            }
        }
    }
}
