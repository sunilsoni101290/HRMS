using Application.Common.Responses;
using Application.DTOs.Attendances;
using Application.Interfaces.Attendances;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace API.Controllers
{
    /// <summary>
    /// Admin-facing endpoint for the standalone "Sync Biometric Attendance
    /// (Month Wise)" tool - a brand-new, COMPLETELY SEPARATE controller from
    /// EsslAttendanceController.cs and HistoricalAttendanceSyncController.cs
    /// (neither is modified or referenced here). Bounded to one tenant/
    /// company/shift/calendar-month, so this runs pure C#/EF Core logic
    /// (see MonthWiseBiometricSyncService - no stored procedure, no raw
    /// ADO.NET) inline on the request instead of enqueuing a background job
    /// like the other two attendance-sync features. Admin/HR-only via EssRestrictionAttribute's
    /// AdminOnlyControllers list on the APP side, same gating style as
    /// EsslAttendance/HistoricalAttendanceSync.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MonthWiseBiometricSyncController : ControllerBase
    {
        private readonly IMonthWiseBiometricSyncService _service;
        private readonly ITenantService _tenantService;
        private readonly ILogger<MonthWiseBiometricSyncController> _logger;

        public MonthWiseBiometricSyncController(
            IMonthWiseBiometricSyncService service,
            ITenantService tenantService,
            ILogger<MonthWiseBiometricSyncController> logger)
        {
            _service = service;
            _tenantService = tenantService;
            _logger = logger;
        }

        /// <summary>"Execute" button - runs MonthWiseBiometricSyncService.SyncAsync once, synchronously, and returns its summary row.</summary>
        [HttpPost("execute")]
        public async Task<IActionResult> Execute([FromBody] MonthWiseSyncRequestDto request)
        {
            var syncRequest = request ?? new MonthWiseSyncRequestDto();

            if (syncRequest.Month < 1 || syncRequest.Month > 12)
                return Ok(new ApiResponse<MonthWiseSyncResultDto> { Success = false, Message = "Please select a valid month." });

            if (syncRequest.Year < 2000 || syncRequest.Year > 2100)
                return Ok(new ApiResponse<MonthWiseSyncResultDto> { Success = false, Message = "Please select a valid year." });

            if (string.IsNullOrWhiteSpace(syncRequest.CompanyId))
                return Ok(new ApiResponse<MonthWiseSyncResultDto> { Success = false, Message = "Company is required." });

            if (string.IsNullOrWhiteSpace(syncRequest.ShiftId))
                return Ok(new ApiResponse<MonthWiseSyncResultDto> { Success = false, Message = "Shift is required." });

            // TenantId is always resolved server-side from the caller's own
            // session/token (ITenantService.GetTenantId()), same convention
            // as HistoricalAttendanceSyncController - any TenantId on the
            // request body is informational only and never trusted.
            var tenantId = _tenantService.GetTenantId();

            try
            {
                var result = await _service.SyncAsync(syncRequest, tenantId, HttpContext.RequestAborted);

                return Ok(new ApiResponse<MonthWiseSyncResultDto>
                {
                    Success = true,
                    Message = "Sync Biometric Attendance (Month Wise) completed.",
                    Data = result
                });
            }
            catch (ArgumentException ex)
            {
                return Ok(new ApiResponse<MonthWiseSyncResultDto> { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Month Wise Biometric Sync: failed for tenant {TenantId}, {Year}-{Month}.",
                    tenantId, syncRequest.Year, syncRequest.Month);

                return Ok(new ApiResponse<MonthWiseSyncResultDto>
                {
                    Success = false,
                    Message = "Sync Biometric Attendance (Month Wise) failed: " + ex.Message
                });
            }
        }
    }
}
