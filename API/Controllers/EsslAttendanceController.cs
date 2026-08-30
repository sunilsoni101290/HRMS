using Application.Common.Responses;
using Application.DTOs.Attendances;
using Application.Interfaces.Attendances;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

        public EsslAttendanceController(
            IEsslAttendanceSyncService syncService,
            ITenantService tenantService)
        {
            _syncService = syncService;
            _tenantService = tenantService;
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
        /// null = incremental mode; either set = the admin's explicit
        /// manual/historical window.
        /// </summary>
        [HttpPost("sync")]
        public async Task<IActionResult> SyncNow([FromBody] EsslSyncRequestDto request)
        {
            var tenantId = _tenantService.GetTenantId();
            var triggeredBy = User?.Identity?.Name ?? "Unknown";

            var result = await _syncService.SyncAsync(request ?? new EsslSyncRequestDto(), tenantId, triggeredBy);

            return Ok(new ApiResponse<EsslSyncResultDto>
            {
                Success = result.Success,
                Message = result.Message,
                Data = result
            });
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
