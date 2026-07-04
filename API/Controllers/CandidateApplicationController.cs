using Application.DTOs.Recruitment;
using Application.Interfaces.Recruitment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    #region Candidate Application API

    [ApiController]
    [Route("api/candidate-application")]
    [Authorize]
    public class CandidateApplicationController : ControllerBase
    {
        private readonly ICandidateApplicationService _service;

        public CandidateApplicationController(ICandidateApplicationService service)
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

        [HttpGet("edit/{id}")]
        public async Task<IActionResult> GetForEdit(string id)
        {
            var data = await _service.GetForEditAsync(id);
            if (data == null) return NotFound();
            return Ok(data);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CandidateApplicationDto dto)
        {
            var id = await _service.CreateAsync(dto);
            return Ok(new { Message = "Application Created Successfully", Id = id });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] CandidateApplicationDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);
            return Ok(new { Message = "Application Updated Successfully", Id = result });
        }

        [HttpPut("stage/{id}")]
        public async Task<IActionResult> ChangeStage(string id, [FromQuery] int status, [FromQuery] string userId)
        {
            var result = await _service.ChangeStageAsync(id, status, userId);
            return Ok(new { Message = "Stage Updated Successfully", Id = result });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _service.DeleteAsync(id);
            if (!result) return NotFound();
            return Ok(new { Message = "Application Deleted Successfully" });
        }
    }

    #endregion
}
