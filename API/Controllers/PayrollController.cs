using Application.DTOs.Payroll;
using Application.Interfaces.Payroll;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
        public async Task<IActionResult> GetAll(
            int? year = null,
            int? month = null,
            string? departmentId = null,
            string? designationId = null,
            string? employeeStatus = null,
            string? paymentStatus = null,
            string? search = null)
        {
            var data = await _service.GetAllAsync(year, month, departmentId, designationId, employeeStatus, paymentStatus, search);
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

        // Redesigned payslip document (PayslipDto) - Admin/HR/Manager only,
        // same role gate MVC's PayrollController already applies for this
        // action (SessionHelper.IsAdminRole's "admin"/"manager"/
        // "configurator" substring match), enforced again here so a
        // non-admin caller can't reach another employee's payslip by
        // calling this API directly, bypassing the MVC-layer check.
        // TenantId is taken from the caller's own JWT claim, never from a
        // client-supplied parameter - see
        // PayrollBusinessService.GetPayslipDocumentAsync's tenant scoping.
        [HttpGet("payslip-document/{payrollId}")]
        public async Task<IActionResult> GetPayslipDocument(string payrollId)
        {
            if (!IsCallerAdminRole())
                return Forbid();

            var tenantId = User.FindFirst("TenantId")?.Value;
            if (string.IsNullOrEmpty(tenantId))
                return Forbid();

            var data = await _service.GetPayslipDocumentAsync(payrollId, tenantId);
            if (data == null) return NotFound();
            return Ok(data);
        }

        // "Scan to Verify Payslip" - reachable with no login (a printed
        // payslip's QR code is scanned on a personal phone, outside the
        // app). The token itself is the credential (HMAC-signed, see
        // Domain.Helper.PayslipVerificationHelper) - the response is
        // deliberately minimal (see PayslipVerificationResultDto).
        [HttpGet("verify-payslip")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyPayslip([FromQuery] string token)
        {
            var result = await _service.VerifyPayslipAsync(token)
                ?? new PayslipVerificationResultDto { IsValid = false };

            return Ok(result);
        }

        // "Email Payslip" toolbar action - same admin + tenant gate as
        // GetPayslipDocument above.
        [HttpPost("payslip-document/{payrollId}/email")]
        public async Task<IActionResult> EmailPayslipDocument(string payrollId, [FromQuery] string? verificationUrl)
        {
            if (!IsCallerAdminRole())
                return Forbid();

            var tenantId = User.FindFirst("TenantId")?.Value;
            if (string.IsNullOrEmpty(tenantId))
                return Forbid();

            var sent = await _service.EmailPayslipAsync(payrollId, tenantId, verificationUrl ?? string.Empty);

            return Ok(new
            {
                Sent = sent,
                Message = sent
                    ? "Payslip emailed successfully."
                    : "Could not email this payslip - the employee may have no email on file, or outbound email isn't configured."
            });
        }

        // Same loose substring convention as
        // APP.Helpers.SessionHelper.IsAdminRole (this codebase's existing,
        // documented "admin gate" rule) - re-implemented here because the
        // API layer authorizes off JWT role claims directly, it has no
        // access to APP's session-backed SessionHelper.
        private bool IsCallerAdminRole()
        {
            var roles = User.FindAll(ClaimTypes.Role).Select(r => r.Value?.ToLowerInvariant() ?? "");
            return roles.Any(r => r.Contains("admin") || r.Contains("manager") || r.Contains("configurator"));
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
