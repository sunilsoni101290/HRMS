using Application.DTOs.Payroll;
using Application.Interfaces.Payroll;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    #region Salary Structure API

    [ApiController]
    [Route("api/salary-structure")]
    [Authorize]
    public class SalaryStructureController : ControllerBase
    {
        private readonly ISalaryStructureService _service;

        public SalaryStructureController(ISalaryStructureService service)
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
        public async Task<IActionResult> Create([FromBody] SalaryStructureDto dto)
        {
            var id = await _service.CreateAsync(dto);
            return Ok(new { Message = "Salary Structure Created Successfully", Id = id });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] SalaryStructureDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);
            return Ok(new { Message = "Salary Structure Updated Successfully", Id = result });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _service.DeleteAsync(id);
            if (!result) return NotFound();
            return Ok(new { Message = "Salary Structure Deleted Successfully" });
        }
    }

    #endregion
}
