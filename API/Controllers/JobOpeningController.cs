using Application.DTOs.Recruitment;
using Application.Interfaces.Recruitment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    #region Job Opening API

    [ApiController]
    [Route("api/job-opening")]
    [Authorize]
    public class JobOpeningController : ControllerBase
    {
        private readonly IJobOpeningService _service;

        public JobOpeningController(IJobOpeningService service)
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
        public async Task<IActionResult> Create([FromBody] JobOpeningDto dto)
        {
            var id = await _service.CreateAsync(dto);
            return Ok(new { Message = "Job Opening Created Successfully", Id = id });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] JobOpeningDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);
            return Ok(new { Message = "Job Opening Updated Successfully", Id = result });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _service.DeleteAsync(id);
            if (!result) return NotFound();
            return Ok(new { Message = "Job Opening Deleted Successfully" });
        }
    }

    #endregion
}
