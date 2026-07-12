using Application.DTOs.Permissions;
using Application.Interfaces.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    #region Permission API

    [ApiController]
    [Route("api/permission")]
    [Authorize]
    public class PermissionController : ControllerBase
    {
        private readonly IPermissionService _service;

        public PermissionController(IPermissionService service)
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

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PermissionDto dto)
        {
            var id = await _service.CreateAsync(dto);
            if (id == null)
                return BadRequest(new { Message = "A permission with this code already exists, or the request was invalid." });

            return Ok(new { Message = "Permission Created Successfully", Id = id });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] PermissionDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);
            if (result == null) return NotFound();
            return Ok(new { Message = "Permission Updated Successfully", Id = result });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _service.DeleteAsync(id);
            if (!result) return NotFound();
            return Ok(new { Message = "Permission Deleted Successfully" });
        }
    }

    #endregion
}
