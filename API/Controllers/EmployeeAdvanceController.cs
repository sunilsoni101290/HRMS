using Application.DTOs.LoanAdvance;
using Application.Interfaces.LoanAdvance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>
    /// Full EmployeeAdvance lifecycle - AppFeatureConstants.EMPLOYEE_ADVANCE.
    /// Single-level approval (Reporting Manager or Finance override) -
    /// see EmployeeAdvanceService. Mirrors EmployeeLoanController's route
    /// shape minus pre-closure (advances don't amortize).
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class EmployeeAdvanceController : ControllerBase
    {
        private readonly IEmployeeAdvanceService _service;

        public EmployeeAdvanceController(IEmployeeAdvanceService service)
        {
            _service = service;
        }

        private string? TenantId => User.FindFirst("TenantId")?.Value;
        private string? ActingUserId => User.FindFirst("UserId")?.Value;

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] string? employeeId)
        {
            var result = await _service.GetAllAsync(TenantId, ActingUserId, status, employeeId);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _service.GetByIdAsync(id, TenantId, ActingUserId);
            return Ok(result);
        }

        [HttpGet("pending-on-me")]
        public async Task<IActionResult> GetPendingOnMe()
        {
            var result = await _service.GetPendingOnMeAsync(TenantId, ActingUserId);
            return Ok(result);
        }

        [HttpPost("submit")]
        public async Task<IActionResult> Submit([FromBody] AdvanceSubmitDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _service.SubmitAsync(dto, TenantId, ActingUserId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpPut("approve")]
        public async Task<IActionResult> Approve([FromBody] AdvanceApprovalActionDto dto)
        {
            var result = await _service.ApproveAsync(dto, TenantId, ActingUserId);
            return Ok(result);
        }

        [HttpPut("reject")]
        public async Task<IActionResult> Reject([FromBody] AdvanceApprovalActionDto dto)
        {
            var result = await _service.RejectAsync(dto, TenantId, ActingUserId);
            return Ok(result);
        }

        // POST api/employeeadvance/disburse - Finance action, generates the flat installment schedule.
        [HttpPost("disburse")]
        public async Task<IActionResult> Disburse([FromBody] AdvanceDisbursementDto dto)
        {
            var result = await _service.DisburseAsync(dto, TenantId, ActingUserId);
            return Ok(result);
        }

        // POST api/employeeadvance/settle - Finance action, manual settlement of any remaining balance.
        [HttpPost("settle")]
        public async Task<IActionResult> Settle([FromBody] AdvanceSettlementDto dto)
        {
            var result = await _service.SettleAsync(dto, TenantId, ActingUserId);
            return Ok(result);
        }
    }
}
