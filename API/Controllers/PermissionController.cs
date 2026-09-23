using Application.DTOs.Permissions;
using Application.Interfaces.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    #region Permission API

    // Permission Management is System Configurator ONLY -
    // [Authorize(Roles=...)] is the coarse baseline (any logged-in user
    // with a valid JWT bearing that role claim reaches these actions); the
    // REAL, data-driven check is IPermissionService's EnsurePermissionAsync
    // (RolePermission/Permission against AppFeatureConstants.PERMISSION),
    // same convention as ErrorLogController/DatabaseManagementController -
    // so the acting user is always passed explicitly rather than trusted
    // implicitly.
    [ApiController]
    [Route("api/permission")]
    [Authorize(Roles = "System Configurator")]
    public class PermissionController : ControllerBase
    {
        private readonly IPermissionService _service;

        public PermissionController(IPermissionService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string actingUserId)
        {
            try
            {
                var data = await _service.GetAllAsync(actingUserId);
                return Ok(data);
            }
            catch (Application.Common.Exceptions.UnauthorizedException ex)
            {
                return Unauthorized(new { Message = ex.Message, ErrorCode = ex.ErrorCode });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id, [FromQuery] string actingUserId)
        {
            try
            {
                var data = await _service.GetByIdAsync(id, actingUserId);
                if (data == null) return NotFound();
                return Ok(data);
            }
            catch (Application.Common.Exceptions.UnauthorizedException ex)
            {
                return Unauthorized(new { Message = ex.Message, ErrorCode = ex.ErrorCode });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PermissionDto dto, [FromQuery] string actingUserId)
        {
            try
            {
                var id = await _service.CreateAsync(dto, actingUserId);
                if (id == null)
                    return BadRequest(new { Message = "A permission with this code already exists, or the request was invalid." });

                return Ok(new { Message = "Permission Created Successfully", Id = id });
            }
            catch (Application.Common.Exceptions.UnauthorizedException ex)
            {
                return Unauthorized(new { Message = ex.Message, ErrorCode = ex.ErrorCode });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] PermissionDto dto, [FromQuery] string actingUserId)
        {
            try
            {
                var result = await _service.UpdateAsync(id, dto, actingUserId);
                if (result == null) return NotFound();
                return Ok(new { Message = "Permission Updated Successfully", Id = result });
            }
            catch (Application.Common.Exceptions.UnauthorizedException ex)
            {
                return Unauthorized(new { Message = ex.Message, ErrorCode = ex.ErrorCode });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id, [FromQuery] string actingUserId)
        {
            try
            {
                var result = await _service.DeleteAsync(id, actingUserId);
                if (!result) return NotFound();
                return Ok(new { Message = "Permission Deleted Successfully" });
            }
            catch (Application.Common.Exceptions.UnauthorizedException ex)
            {
                return Unauthorized(new { Message = ex.Message, ErrorCode = ex.ErrorCode });
            }
        }
    }

    #endregion
}
