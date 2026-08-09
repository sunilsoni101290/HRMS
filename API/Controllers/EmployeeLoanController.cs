using Application.DTOs.LoanAdvance;
using Application.Interfaces.LoanAdvance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>
    /// Full EmployeeLoan lifecycle - AppFeatureConstants.EMPLOYEE_LOAN.
    /// Dual-audience: an Employee submits/views their OWN requests
    /// (self-service), while HR/Finance additionally act tenant-wide per
    /// the resolved N-level approval matrix - see
    /// EmployeeLoanService for the exact authorization rules on each
    /// action. See LoanTypeController for the no-try/catch convention.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class EmployeeLoanController : ControllerBase
    {
        private readonly IEmployeeLoanService _service;

        public EmployeeLoanController(IEmployeeLoanService service)
        {
            _service = service;
        }

        // GET api/employeeloan?status=&employeeId=
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] string? employeeId,string tenantId,string actingUserId)
        {
            var result = await _service.GetAllAsync(tenantId, actingUserId, status, employeeId);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id, string tenantId, string actingUserId)
        {
            var result = await _service.GetByIdAsync(id, tenantId, actingUserId);
            return Ok(result);
        }

        // GET api/employeeloan/pending-on-me - requests awaiting the caller's action at their resolved level.
        [HttpGet("pending-on-me")]
        public async Task<IActionResult> GetPendingOnMe(string tenantId, string actingUserId)
        {
            var result = await _service.GetPendingOnMeAsync(tenantId, actingUserId);
            return Ok(result);
        }

        // GET api/employeeloan/eligibility?employeeId=&loanTypeId=&amount=&tenureMonths=
        // Live preview shown on the request form BEFORE submit - see LoanEligibilityDto.
        [HttpGet("eligibility")]
        public async Task<IActionResult> CheckEligibility(
            [FromQuery] string employeeId, [FromQuery] string loanTypeId,
            [FromQuery] decimal amount, [FromQuery] int tenureMonths,string tenantId, string actingUserId)
        {
            var result = await _service.CheckEligibilityAsync(employeeId, loanTypeId, amount, tenureMonths, tenantId, actingUserId);
            return Ok(result);
        }

        // POST api/employeeloan/preview-emi - live, non-persisted amortization preview for the request form.
        [HttpPost("preview-emi")]
        public async Task<IActionResult> PreviewEmi([FromBody] EmiPreviewRequestDto dto)
        {
            var result = await _service.PreviewEmiAsync(dto, dto.TenantId, dto.ActingUserId);
            return Ok(result);
        }

        // POST api/employeeloan/submit - Maker action.
        [HttpPost("submit")]
        public async Task<IActionResult> Submit([FromBody] LoanSubmitDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _service.SubmitAsync(dto, dto.TenantId, dto.ActingUserId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        // PUT api/employeeloan/approve - Checker action at the request's current level.
        [HttpPut("approve")]
        public async Task<IActionResult> Approve([FromBody] LoanApprovalActionDto dto)
        {
            var result = await _service.ApproveAsync(dto, dto.TenantId, dto.ActingUserId);
            return Ok(result);
        }

        // PUT api/employeeloan/reject - Checker action at the request's current level.
        [HttpPut("reject")]
        public async Task<IActionResult> Reject([FromBody] LoanApprovalActionDto dto)
        {
            var result = await _service.RejectAsync(dto, dto.TenantId, dto.ActingUserId);
            return Ok(result);
        }

        // POST api/employeeloan/disburse - Finance action, generates the EMI schedule.
        [HttpPost("disburse")]
        public async Task<IActionResult> Disburse([FromBody] LoanDisbursementDto dto)
        {
            var result = await _service.DisburseAsync(dto, dto.TenantId, dto.ActingUserId);
            return Ok(result);
        }

        // GET api/employeeloan/{id}/pre-closure-quote
        [HttpGet("{id}/pre-closure-quote")]
        public async Task<IActionResult> GetPreClosureQuote(string id, string tenantId, string actingUserId)
        {
            var result = await _service.GetPreClosureQuoteAsync(id, tenantId, actingUserId);
            return Ok(result);
        }

        // POST api/employeeloan/{id}/request-pre-closure
        [HttpPost("{id}/request-pre-closure")]
        public async Task<IActionResult> RequestPreClosure(string id, string tenantId, string actingUserId)
        {
            var result = await _service.RequestPreClosureAsync(id, tenantId, actingUserId);
            return Ok(result);
        }

        // POST api/employeeloan/settle - Finance action; confirms the lump-sum payment and closes the loan.
        [HttpPost("settle")]
        public async Task<IActionResult> Settle([FromBody] LoanSettlementDto dto)
        {
            var result = await _service.SettleAsync(dto, dto.TenantId, dto.ActingUserId);
            return Ok(result);
        }
    }
}
