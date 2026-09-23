using Application.DTOs.Roles;
using Application.Interfaces.Roles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    #region Role API

    // Role Management is System Configurator ONLY - [Authorize(Roles=...)]
    // is the coarse baseline (any logged-in user with a valid JWT bearing
    // that role claim reaches these actions); the REAL, data-driven check
    // is IRoleService's EnsurePermissionAsync (RolePermission/Permission
    // against AppFeatureConstants.ROLE), same convention as
    // ErrorLogController/DatabaseManagementController - so the acting user
    // is always passed explicitly rather than trusted implicitly.
    [ApiController]
    [Route("api/role")]
    [Authorize(Roles = "System Configurator")]
    public class RoleController : ControllerBase
    {
        private readonly IRoleService _service;

        public RoleController(IRoleService service)
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

        [HttpGet("{id}/detail")]
        public async Task<IActionResult> GetDetail(string id, [FromQuery] string actingUserId)
        {
            try
            {
                var data = await _service.GetDetailAsync(id, actingUserId);
                if (data == null) return NotFound();
                return Ok(data);
            }
            catch (Application.Common.Exceptions.UnauthorizedException ex)
            {
                return Unauthorized(new { Message = ex.Message, ErrorCode = ex.ErrorCode });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RoleDto dto, [FromQuery] string actingUserId)
        {
            try
            {
                var id = await _service.CreateAsync(dto, actingUserId);
                if (id == null)
                    return BadRequest(new { Message = "A role with this name already exists, or the request was invalid." });

                return Ok(new { Message = "Role Created Successfully", Id = id });
            }
            catch (Application.Common.Exceptions.UnauthorizedException ex)
            {
                return Unauthorized(new { Message = ex.Message, ErrorCode = ex.ErrorCode });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] RoleDto dto, [FromQuery] string actingUserId)
        {
            try
            {
                var result = await _service.UpdateAsync(id, dto, actingUserId);
                if (result == null) return NotFound();
                return Ok(new { Message = "Role Updated Successfully", Id = result });
            }
            catch (Application.Common.Exceptions.UnauthorizedException ex)
            {
                return Unauthorized(new { Message = ex.Message, ErrorCode = ex.ErrorCode });
            }
        }

        [HttpPut("toggle-active/{id}")]
        public async Task<IActionResult> ToggleActive(string id, [FromQuery] string actingUserId)
        {
            try
            {
                var result = await _service.ToggleActiveAsync(id, actingUserId);
                if (!result) return NotFound();
                return Ok(new { Success = result });
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
                if (!result)
                    return BadRequest(new { Message = "Role not found, or it still has users assigned to it." });

                return Ok(new { Message = "Role Deleted Successfully" });
            }
            catch (Application.Common.Exceptions.UnauthorizedException ex)
            {
                return Unauthorized(new { Message = ex.Message, ErrorCode = ex.ErrorCode });
            }
        }

        [HttpPost("assign-permissions")]
        public async Task<IActionResult> AssignPermissions([FromBody] AssignRolePermissionsRequestDto request)
        {
            try
            {
                var result = await _service.AssignPermissionsAsync(request);
                if (!result) return BadRequest(new { Message = "Unable to assign permissions. Role not found." });
                return Ok(new { Message = "Permissions Assigned Successfully" });
            }
            catch (Application.Common.Exceptions.UnauthorizedException ex)
            {
                return Unauthorized(new { Message = ex.Message, ErrorCode = ex.ErrorCode });
            }
        }
    }

    #endregion
}
