using Application.DTOs.Roles;
using Application.Interfaces.Roles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    #region Role API

    [ApiController]
    [Route("api/role")]
    [Authorize]
    public class RoleController : ControllerBase
    {
        private readonly IRoleService _service;

        public RoleController(IRoleService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _service.GetAllAsync();
            return Ok(data);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var data = await _service.GetByIdAsync(id);
            if (data == null) return NotFound();
            return Ok(data);
        }

        [HttpGet("{id}/detail")]
        public async Task<IActionResult> GetDetail(string id)
        {
            var data = await _service.GetDetailAsync(id);
            if (data == null) return NotFound();
            return Ok(data);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RoleDto dto)
        {
            var id = await _service.CreateAsync(dto);
            if (id == null)
                return BadRequest(new { Message = "A role with this name already exists, or the request was invalid." });

            return Ok(new { Message = "Role Created Successfully", Id = id });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] RoleDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);
            if (result == null) return NotFound();
            return Ok(new { Message = "Role Updated Successfully", Id = result });
        }

        [HttpPut("toggle-active/{id}")]
        public async Task<IActionResult> ToggleActive(string id)
        {
            var result = await _service.ToggleActiveAsync(id);
            if (!result) return NotFound();
            return Ok(new { Success = result });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _service.DeleteAsync(id);
            if (!result)
                return BadRequest(new { Message = "Role not found, or it still has users assigned to it." });

            return Ok(new { Message = "Role Deleted Successfully" });
        }

        [HttpPost("assign-permissions")]
        public async Task<IActionResult> AssignPermissions([FromBody] AssignRolePermissionsRequestDto request)
        {
            var result = await _service.AssignPermissionsAsync(request);
            if (!result) return BadRequest(new { Message = "Unable to assign permissions. Role not found." });
            return Ok(new { Message = "Permissions Assigned Successfully" });
        }
    }

    #endregion
}
