using Application.Common.Responses;
using Application.DTOs.ErrorLogs;
using Application.Interfaces.ErrorLog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>
    /// Centralized Error Log management (AppFeatureConstants.ERROR_LOG) -
    /// System Configurator ONLY (Admin/Super Admin deliberately excluded,
    /// per explicit instruction - see the ERROR_LOG carve-out in
    /// AppFeatureSeeder.ReconcilePermissionsAsync). [Authorize] is the
    /// baseline (any logged-in user with a valid JWT reaches these
    /// actions); the REAL, data-driven check is IErrorLogService's
    /// EnsurePermissionAsync (RolePermission/Permission against
    /// ERROR_LOG's View/Edit actions), same convention as
    /// LoanAdvanceAuditLogController - so the acting user is always passed
    /// explicitly rather than trusted implicitly. [Authorize(Roles=...)] is
    /// layered on top as a second, coarse gate using the Role.Name claim
    /// already issued at login (defense in depth).
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "System Configurator")]
    public class ErrorLogController : ControllerBase
    {
        private readonly IErrorLogService _service;

        public ErrorLogController(IErrorLogService service)
        {
            _service = service;
        }

        // GET api/ErrorLog?actingUserId=...&pageNumber=1&pageSize=20&...
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] ErrorLogFilterDto filter, [FromQuery] string actingUserId)
        {
            try
            {
                var result = await _service.GetAllAsync(filter, actingUserId);
                return Ok(new ApiResponse<Application.DTOs.Employee.PagedResult<ErrorLogDto>>
                {
                    Success = true,
                    Data = result
                });
            }
            catch (Application.Common.Exceptions.UnauthorizedException ex)
            {
                return Unauthorized(new ApiResponse<object> { Success = false, Message = ex.Message, ErrorCode = ex.ErrorCode });
            }
        }

        // GET api/ErrorLog/{id}?actingUserId=...
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id, [FromQuery] string actingUserId)
        {
            try
            {
                var result = await _service.GetByIdAsync(id, actingUserId);

                if (result == null)
                    return NotFound(new ApiResponse<object> { Success = false, Message = "Error log entry not found." });

                return Ok(new ApiResponse<ErrorLogDto> { Success = true, Data = result });
            }
            catch (Application.Common.Exceptions.UnauthorizedException ex)
            {
                return Unauthorized(new ApiResponse<object> { Success = false, Message = ex.Message, ErrorCode = ex.ErrorCode });
            }
        }

        // POST api/ErrorLog/resolve?actingUserId=...
        [HttpPost("resolve")]
        public async Task<IActionResult> Resolve([FromBody] ResolveErrorLogDto dto, [FromQuery] string actingUserId)
        {
            try
            {
                var ok = await _service.ResolveAsync(dto, actingUserId);

                return Ok(new ApiResponse<object>
                {
                    Success = ok,
                    Message = ok ? "Error marked as resolved." : "Error log entry not found."
                });
            }
            catch (Application.Common.Exceptions.UnauthorizedException ex)
            {
                return Unauthorized(new ApiResponse<object> { Success = false, Message = ex.Message, ErrorCode = ex.ErrorCode });
            }
        }
    }
}
