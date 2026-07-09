using Application.DTOs.Tasks;
using Application.Interfaces.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    #region Employee Task API

    [ApiController]
    [Route("api/employee-task")]
    [Authorize]
    public class EmployeeTaskController : ControllerBase
    {
        private readonly IEmployeeTaskService _service;

        public EmployeeTaskController(IEmployeeTaskService service)
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
        public async Task<IActionResult> Create([FromBody] EmployeeTaskDto dto)
        {
            var id = await _service.CreateAsync(dto);
            return Ok(new { Message = "Task Created Successfully", Id = id });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] EmployeeTaskDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);
            return Ok(new { Message = "Task Updated Successfully", Id = result });
        }

        [HttpPut("status/{id}")]
        public async Task<IActionResult> ChangeStatus(string id, [FromQuery] string status, [FromQuery] string userId, [FromQuery] string? remarks = null)
        {
            var result = await _service.ChangeStatusAsync(id, status, userId, remarks);
            return Ok(new { Message = $"Task marked {status}", Id = result });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _service.DeleteAsync(id);
            if (!result) return NotFound();
            return Ok(new { Message = "Task Deleted Successfully" });
        }
    }

    #endregion
}
