using Application.DTOs.Support;
using Application.Interfaces.Support;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    #region Support Ticket API

    [ApiController]
    [Route("api/support-ticket")]
    [Authorize]
    public class SupportTicketController : ControllerBase
    {
        private readonly ISupportTicketService _service;

        public SupportTicketController(ISupportTicketService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _service.GetAllAsync();
            return Ok(data);
        }

        [HttpGet("by-employee/{employeeId}")]
        public async Task<IActionResult> GetByEmployee(string employeeId)
        {
            var data = await _service.GetByEmployeeAsync(employeeId);
            return Ok(data);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var data = await _service.GetByIdAsync(id);
            if (data == null) return NotFound();
            return Ok(data);
        }

        [HttpGet("open-count")]
        public async Task<IActionResult> GetOpenCount()
        {
            var count = await _service.GetOpenCountAsync();
            return Ok(new { OpenCount = count });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSupportTicketRequestDto request)
        {
            var id = await _service.CreateAsync(request);
            if (id == null) return BadRequest(new { Message = "Failed to create support ticket." });
            return Ok(new { Message = "Support Ticket Raised Successfully", Id = id });
        }

        [HttpPost("reply")]
        public async Task<IActionResult> AddReply([FromBody] AddReplyRequestDto request)
        {
            var result = await _service.AddReplyAsync(request);
            if (!result) return BadRequest(new { Message = "Failed to add reply. Ticket may not exist." });
            return Ok(new { Message = "Reply Added Successfully" });
        }

        [HttpPost("status")]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateTicketStatusRequestDto request)
        {
            var result = await _service.UpdateStatusAsync(request);
            if (!result) return BadRequest(new { Message = "Failed to update status. Ticket may not exist." });
            return Ok(new { Message = "Ticket Status Updated Successfully" });
        }
    }

    #endregion
}
