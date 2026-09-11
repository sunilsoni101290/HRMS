using Application.DTOs.Payroll;
using Application.Interfaces.Payroll;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    #region Payroll API

    [ApiController]
    [Route("api/payroll")]
    [Authorize]
    public class PayrollController : ControllerBase
    {
        private readonly IPayrollBusinessService _service;

        public PayrollController(IPayrollBusinessService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(int? year = null, int? month = null)
        {
            var data = await _service.GetAllAsync(year, month);
            return Ok(data);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var data = await _service.GetByIdAsync(id);
            if (data == null) return NotFound();
            return Ok(data);
        }

        [HttpPost("generate")]
        public async Task<IActionResult> Generate([FromBody] PayrollGenerateDto dto)
        {
            var result = await _service.GenerateAsync(dto);
            return Ok(result);
        }

        // Salary Processing - Select Month -> Load Attendance (this) ->
        // Review -> Process (below). Read-only.
        [HttpGet("preview")]
        public async Task<IActionResult> Preview(int salaryYear, int salaryMonth, string? companyId, string? branchId, string tenantId)
        {
            var result = await _service.PreviewAsync(salaryYear, salaryMonth, companyId, branchId, tenantId);
            return Ok(result);
        }

        // Writes exactly the employees the Review screen confirmed - see
        // SalaryProcessRequestDto/PayrollBusinessService.ProcessAsync's
        // remarks.
        [HttpPost("process")]
        public async Task<IActionResult> Process([FromBody] SalaryProcessRequestDto dto)
        {
            var result = await _service.ProcessAsync(dto);
            return Ok(result);
        }

        // Re-runs Salary Processing against an EXISTING Payroll - subject
        // to the Draft/Processed/Paid lock rules on
        // PayrollBusinessService.RecalculateAsync.
        [HttpPost("recalculate")]
        public async Task<IActionResult> Recalculate([FromBody] SalaryRecalculateRequestDto dto)
        {
            var result = await _service.RecalculateAsync(dto);

            if (result != null && result.StartsWith("ERROR:"))
                return BadRequest(new { Message = result.Substring("ERROR:".Length) });

            return Ok(new { Message = "Payroll recalculated successfully.", Id = result });
        }

        [HttpPut("status/{id}")]
        public async Task<IActionResult> ChangeStatus(string id, [FromQuery] string status, [FromQuery] string userId)
        {
            var result = await _service.ChangeStatusAsync(id, status, userId);
            return Ok(new { Message = $"Payroll marked as {status}", Id = result });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _service.DeleteAsync(id);
            if (!result) return NotFound();
            return Ok(new { Message = "Payroll Deleted Successfully" });
        }

        [HttpPost("payslip/{payrollId}")]
        public async Task<IActionResult> GeneratePayslip(string payrollId, [FromQuery] string userId)
        {
            var id = await _service.GeneratePayslipAsync(payrollId, userId);
            return Ok(new { Message = "Payslip Generated Successfully", Id = id });
        }

        [HttpGet("payslip/{payrollId}")]
        public async Task<IActionResult> GetPayslip(string payrollId)
        {
            var data = await _service.GetPayslipAsync(payrollId);
            if (data == null) return NotFound();
            return Ok(data);
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> Dashboard(int year, int month)
        {
            if (year <= 0) year = DateTime.UtcNow.Year;
            if (month <= 0) month = DateTime.UtcNow.Month;
            var data = await _service.GetDashboardAsync(year, month);
            return Ok(data);
        }
    }

    #endregion
}
