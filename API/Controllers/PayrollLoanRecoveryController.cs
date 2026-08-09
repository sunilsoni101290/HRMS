using Application.Interfaces.LoanAdvance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>
    /// Manual/admin re-trigger for IPayrollLoanRecoveryService - the normal
    /// path is the automatic hook inside PayrollBusinessService.GenerateAsync
    /// (Application/Services/PayrollService/PayrollBusinessService.cs), this
    /// controller exists only so Finance/Admin can safely re-run recovery
    /// for one payroll (e.g. after fixing a data issue) without re-running
    /// the whole payroll generation. Safe to call more than once - see
    /// IPayrollLoanRecoveryService's idempotency contract.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PayrollLoanRecoveryController : ControllerBase
    {
        private readonly IPayrollLoanRecoveryService _service;

        public PayrollLoanRecoveryController(IPayrollLoanRecoveryService service)
        {
            _service = service;
        }


        // POST api/payrollloanrecovery/{payrollId}/recover
        [HttpPost("{payrollId}/recover")]
        public async Task<IActionResult> Recover(string payrollId, string tenantId, string actingUserId)
        {
            var result = await _service.RecoverForPayrollAsync(payrollId, tenantId, actingUserId);
            return Ok(result);
        }
    }
}
