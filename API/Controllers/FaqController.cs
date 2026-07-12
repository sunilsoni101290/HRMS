using Application.DTOs.Support;
using Application.Interfaces.Support;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    #region FAQ / Knowledge Base API

    [ApiController]
    [Route("api/faq")]
    [Authorize]
    public class FaqController : ControllerBase
    {
        private readonly IFaqService _service;

        public FaqController(IFaqService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _service.GetAllAsync();
            return Ok(data);
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActive()
        {
            var data = await _service.GetActiveAsync();
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
        public async Task<IActionResult> Create([FromBody] FaqItemDto dto)
        {
            var id = await _service.CreateAsync(dto);
            if (id == null) return BadRequest(new { Message = "Failed to create FAQ item." });
            return Ok(new { Message = "FAQ Item Created Successfully", Id = id });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] FaqItemDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);
            if (result == null) return NotFound();
            return Ok(new { Message = "FAQ Item Updated Successfully", Id = result });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _service.DeleteAsync(id);
            if (!result) return NotFound();
            return Ok(new { Message = "FAQ Item Deleted Successfully" });
        }

        [HttpPost("{id}/toggle-active")]
        public async Task<IActionResult> ToggleActive(string id)
        {
            var result = await _service.ToggleActiveAsync(id);
            if (!result) return NotFound();
            return Ok(new { Message = "FAQ Item Status Toggled Successfully" });
        }
    }

    #endregion
}
