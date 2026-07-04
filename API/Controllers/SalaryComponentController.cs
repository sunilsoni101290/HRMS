using Application.DTOs.Payroll;
using Application.Interfaces.Payroll;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    #region Salary Component API

    [ApiController]
    [Route("api/salary-component")]
    [Authorize]
    public class SalaryComponentController : ControllerBase
    {
        private readonly ISalaryComponentService _service;

        public SalaryComponentController(ISalaryComponentService service)
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
        public async Task<IActionResult> Create([FromBody] SalaryComponentDto dto)
        {
            var id = await _service.CreateAsync(dto);
            return Ok(new { Message = "Salary Component Created Successfully", Id = id });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] SalaryComponentDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);
            return Ok(new { Message = "Salary Component Updated Successfully", Id = result });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _service.DeleteAsync(id);
            if (!result) return NotFound();
            return Ok(new { Message = "Salary Component Deleted Successfully" });
        }
    }

    #endregion
}
