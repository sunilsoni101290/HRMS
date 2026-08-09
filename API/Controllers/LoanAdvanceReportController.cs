using Application.Interfaces.LoanAdvance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>
    /// Phase 14 - read-only dashboard/report endpoints over the Loan &amp;
    /// Advance module - AppFeatureConstants.LOAN_ADVANCE_DASHBOARD /
    /// LOAN_ADVANCE_REPORT (View action). See ILoanReportService for the
    /// scope split from the per-loan calculation and lifecycle services.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LoanAdvanceReportController : ControllerBase
    {
        private readonly ILoanReportService _service;

        public LoanAdvanceReportController(ILoanReportService service)
        {
            _service = service;
        }

        // GET api/loanadvancereport/dashboard
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard(string tenantId, string actingUserId)
        {
            var result = await _service.GetDashboardAsync(tenantId, actingUserId);
            return Ok(result);
        }

        // GET api/loanadvancereport/outstanding-balance?departmentId=...
        [HttpGet("outstanding-balance")]
        public async Task<IActionResult> GetOutstandingBalance([FromQuery] string? departmentId,string tenantId, string actingUserId)
        {
            var result = await _service.GetOutstandingBalanceReportAsync(tenantId, actingUserId, departmentId);
            return Ok(result);
        }

        // GET api/loanadvancereport/payment-history?fromDate=2026-01-01&toDate=2026-01-31
        [HttpGet("payment-history")]
        public async Task<IActionResult> GetPaymentHistory([FromQuery] DateTime fromDate, [FromQuery] DateTime toDate, string tenantId, string actingUserId)
        {
            var result = await _service.GetPaymentHistoryReportAsync(tenantId, actingUserId, fromDate, toDate);
            return Ok(result);
        }
    }
}
