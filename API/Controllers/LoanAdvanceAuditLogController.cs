using Application.Interfaces.LoanAdvance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>Read-only audit trail - AppFeatureConstants.LOAN_ADVANCE_AUDIT_LOG (Auditor/Admin-gated). Writes happen server-side only (see ILoanAdvanceAuditLogService.LogAsync), never via this controller.</summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LoanAdvanceAuditLogController : ControllerBase
    {
        private readonly ILoanAdvanceAuditLogService _service;

        public LoanAdvanceAuditLogController(ILoanAdvanceAuditLogService service)
        {
            _service = service;
        }

        // GET api/loanadvanceauditlog?entityType=EmployeeLoan&entityId=LNR-0001
        [HttpGet]
        public async Task<IActionResult> GetForEntity([FromQuery] string entityType, [FromQuery] string entityId, string tenantId, string actingUserId)
        {
            var result = await _service.GetForEntityAsync(entityType, entityId, tenantId, actingUserId);
            return Ok(result);
        }

        // GET api/loanadvanceauditlog/recent?entityType=EmployeeLoan&take=200
        [HttpGet("recent")]
        public async Task<IActionResult> GetRecent([FromQuery] string? entityType, string tenantId, string actingUserId, [FromQuery] int take = 200)
        {
            var result = await _service.GetRecentAsync(tenantId, actingUserId, entityType, take);
            return Ok(result);
        }
    }
}
