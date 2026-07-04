using Application.DTOs.Recruitment;
using Application.Interfaces.Recruitment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    #region Interview Schedule API

    [ApiController]
    [Route("api/interview-schedule")]
    [Authorize]
    public class InterviewScheduleController : ControllerBase
    {
        private readonly IInterviewScheduleService _service;

        public InterviewScheduleController(IInterviewScheduleService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _service.GetAllAsync();
            return Ok(data);
        }

        [HttpGet("by-application/{applicationId}")]
        public async Task<IActionResult> GetByApplication(string applicationId)
        {
            var data = await _service.GetByApplicationAsync(applicationId);
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
        public async Task<IActionResult> Create([FromBody] InterviewScheduleDto dto)
        {
            var id = await _service.CreateAsync(dto);
            return Ok(new { Message = "Interview Scheduled Successfully", Id = id });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] InterviewScheduleDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);
            return Ok(new { Message = "Interview Updated Successfully", Id = result });
        }

        [HttpPut("status/{id}")]
        public async Task<IActionResult> ChangeStatus(string id, [FromQuery] int status, [FromQuery] string? feedback, [FromQuery] string userId)
        {
            var result = await _service.ChangeStatusAsync(id, status, feedback, userId);
            return Ok(new { Message = "Interview Status Updated", Id = result });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _service.DeleteAsync(id);
            if (!result) return NotFound();
            return Ok(new { Message = "Interview Deleted Successfully" });
        }
    }

    #endregion
}
