using Application.DTOs.Payroll;
using Application.Interfaces.Payroll;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    #region Salary Template (reusable master) API

    [ApiController]
    [Route("api/salary-template")]
    [Authorize]
    public class SalaryTemplateController : ControllerBase
    {
        private readonly ISalaryTemplateService _service;

        public SalaryTemplateController(ISalaryTemplateService service)
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
        public async Task<IActionResult> Create([FromBody] SalaryTemplateDto dto)
        {
            var result = await _service.CreateAsync(dto);

            if (result != null && result.StartsWith("ERROR:"))
                return BadRequest(new { Message = result.Substring("ERROR:".Length) });

            return Ok(new { Message = "Salary Structure created successfully.", Id = result });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] SalaryTemplateDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);

            if (result != null && result.StartsWith("ERROR:"))
                return BadRequest(new { Message = result.Substring("ERROR:".Length) });

            return Ok(new { Message = "Salary Structure updated successfully.", Id = result });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var error = await _service.DeleteAsync(id);

            if (!string.IsNullOrEmpty(error))
                return BadRequest(new { Message = error });

            return Ok(new { Message = "Salary Structure deleted successfully." });
        }

        // Bulk "Assign Salary Structure" - applies this template to every
        // employee in dto.EmployeeIds as of dto.EffectiveFrom. Always
        // returns 200 with a per-employee result breakdown (applied vs
        // skipped, and why) rather than failing the whole call for a
        // partial/business-rule skip - see SalaryTemplateService.
        // AssignAsync's remarks.
        [HttpPost("assign")]
        public async Task<IActionResult> Assign([FromBody] SalaryTemplateAssignDto dto)
        {
            var result = await _service.AssignAsync(dto);
            return Ok(result);
        }

        [HttpPut("toggle-active/{id}")]
        public async Task<IActionResult> ToggleActive(string id)
        {
            var result = await _service.ToggleActiveAsync(id);
            if (!result) return NotFound();
            return Ok(new { Message = "Status updated successfully." });
        }
    }

    #endregion
}
