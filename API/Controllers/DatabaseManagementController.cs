using Application.Common.Responses;
using Application.DTOs.DatabaseManagement;
using Application.Interfaces.DatabaseManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>
    /// Database Management module (Phase A - scaffold, Settings CRUD,
    /// read-only live Database Info, Execution History reader). System
    /// Configurator ONLY, same auth combination as ErrorLogController:
    /// [Authorize(Roles="System Configurator")] is the baseline (any
    /// logged-in user with a valid JWT bearing that role claim reaches
    /// these actions); the REAL, data-driven check is
    /// IDatabaseManagementService's EnsurePermissionAsync (RolePermission/
    /// Permission against AppFeatureConstants.DATABASE_MANAGEMENT), so the
    /// acting user is always passed explicitly rather than trusted
    /// implicitly.
    ///
    /// Deliberately has NO endpoints for Backup/Execute/Restore/Swap yet -
    /// those ship in later phases. Do not add stub/fake endpoints here for
    /// them (see the Phase A scope's "never mark incomplete functionality
    /// as implemented").
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "System Configurator")]
    public class DatabaseManagementController : ControllerBase
    {
        private readonly IDatabaseManagementService _service;

        public DatabaseManagementController(IDatabaseManagementService service)
        {
            _service = service;
        }

        // GET api/DatabaseManagement/settings?tenantId=...&actingUserId=...
        [HttpGet("settings")]
        public async Task<IActionResult> GetSettings([FromQuery] string tenantId, [FromQuery] string actingUserId)
        {
            try
            {
                var result = await _service.GetSettingsAsync(tenantId, actingUserId);
                return Ok(new ApiResponse<DatabaseManagementSettingsDto> { Success = true, Data = result });
            }
            catch (Application.Common.Exceptions.UnauthorizedException ex)
            {
                return Unauthorized(new ApiResponse<object> { Success = false, Message = ex.Message, ErrorCode = ex.ErrorCode });
            }
        }

        // PUT api/DatabaseManagement/settings?tenantId=...&actingUserId=...
        [HttpPut("settings")]
        public async Task<IActionResult> SaveSettings(
            [FromBody] DatabaseManagementSettingsDto dto,
            [FromQuery] string tenantId,
            [FromQuery] string actingUserId)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Please correct the highlighted fields.", Data = ModelState });

            try
            {
                var (success, message) = await _service.SaveSettingsAsync(dto, tenantId, actingUserId);
                return Ok(new ApiResponse<object> { Success = success, Message = message });
            }
            catch (Application.Common.Exceptions.UnauthorizedException ex)
            {
                return Unauthorized(new ApiResponse<object> { Success = false, Message = ex.Message, ErrorCode = ex.ErrorCode });
            }
        }

        // GET api/DatabaseManagement/database-info?tenantId=...&actingUserId=...
        [HttpGet("database-info")]
        public async Task<IActionResult> GetDatabaseInfo([FromQuery] string tenantId, [FromQuery] string actingUserId)
        {
            try
            {
                var result = await _service.GetDatabaseInfoAsync(tenantId, actingUserId);
                return Ok(new ApiResponse<DatabaseInfoDto> { Success = true, Data = result });
            }
            catch (Application.Common.Exceptions.UnauthorizedException ex)
            {
                return Unauthorized(new ApiResponse<object> { Success = false, Message = ex.Message, ErrorCode = ex.ErrorCode });
            }
        }

        // GET api/DatabaseManagement/connection-info?tenantId=...&actingUserId=...
        [HttpGet("connection-info")]
        public async Task<IActionResult> GetConnectionInfo([FromQuery] string tenantId, [FromQuery] string actingUserId)
        {
            try
            {
                var result = await _service.GetConnectionInfoAsync(tenantId, actingUserId);
                return Ok(new ApiResponse<DatabaseConnectionInfoDto> { Success = true, Data = result });
            }
            catch (Application.Common.Exceptions.UnauthorizedException ex)
            {
                return Unauthorized(new ApiResponse<object> { Success = false, Message = ex.Message, ErrorCode = ex.ErrorCode });
            }
        }

        // GET api/DatabaseManagement/history?tenantId=...&actingUserId=...&pageNumber=1&pageSize=20&...
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory(
            [FromQuery] DatabaseOperationHistoryFilterDto filter,
            [FromQuery] string tenantId,
            [FromQuery] string actingUserId)
        {
            try
            {
                var result = await _service.GetOperationHistoryAsync(filter, tenantId, actingUserId);
                return Ok(new ApiResponse<Application.DTOs.Employee.PagedResult<DatabaseOperationHistoryDto>> { Success = true, Data = result });
            }
            catch (Application.Common.Exceptions.UnauthorizedException ex)
            {
                return Unauthorized(new ApiResponse<object> { Success = false, Message = ex.Message, ErrorCode = ex.ErrorCode });
            }
        }
    }
}
