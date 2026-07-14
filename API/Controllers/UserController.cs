using Application.DTOs.Users;
using Application.Interfaces.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    #region User API

    [ApiController]
    [Route("api/user")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _service;

        public UserController(IUserService service)
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
        public async Task<IActionResult> Create([FromBody] UserDto dto)
        {
            var id = await _service.CreateAsync(dto);
            return Ok(new { Message = "User Created Successfully", Id = id });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UserDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);
            return Ok(new { Message = "User Updated Successfully", Id = result });
        }

        // Body is optional: if the admin typed a specific password it's
        // validated and used as-is; if omitted/blank, the server generates
        // a random one. Either way the password is returned exactly once
        // in this response and is never stored/shown again after this.
        [HttpPut("reset-password/{id}")]
        public async Task<IActionResult> ResetPassword(string id, [FromBody] ResetPasswordRequest request)
        {
            try
            {
                var newPassword = await _service.ResetPasswordAsync(id, request?.NewPassword);

                if (newPassword == null)
                    return NotFound(new { Message = "User not found." });

                return Ok(new { Success = true, NewPassword = newPassword });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
        }

        [HttpPut("toggle-active/{id}")]
        public async Task<IActionResult> ToggleActive(string id)
        {
            var result = await _service.ToggleActiveAsync(id);
            return Ok(new { Success = result });
        }

        [HttpPut("toggle-lock/{id}")]
        public async Task<IActionResult> ToggleLock(string id)
        {
            var result = await _service.ToggleLockAsync(id);
            return Ok(new { Success = result });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _service.DeleteAsync(id);
            if (!result) return NotFound();
            return Ok(new { Message = "User Deleted Successfully" });
        }
    }

    #endregion
}
