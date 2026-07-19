using Application.DTOs.Leaves;
using Application.Interfaces.Leaves;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    // Out-of-office proxy approver (Part 2) - reachable by any authenticated
    // user, not admin-gated, since any employee could plausibly be a
    // Reporting Manager or Department Head. Ownership scoping (a caller can
    // only see/revoke their OWN delegations) is enforced by the APP
    // controller passing the caller's own session EmployeeId/UserId rather
    // than trusting a client-supplied one - same convention as
    // LeaveApplicationController.
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ApprovalDelegationController : ControllerBase
    {
        private readonly IApprovalDelegationService _service;

        public ApprovalDelegationController(IApprovalDelegationService service)
        {
            _service = service;
        }

        [HttpGet("employee/{employeeId}")]
        public async Task<IActionResult> GetForEmployee(string employeeId)
        {
            var data = await _service.GetForEmployeeAsync(employeeId);
            return Ok(data);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateApprovalDelegationRequestDto request)
        {
            try
            {
                var result = await _service.CreateAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Business-rule rejection (self-delegation, overlapping
                // active delegation, invalid dates/employee) - surface the
                // real reason instead of a 500.
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("{id}/revoke")]
        public async Task<IActionResult> Revoke(string id, [FromQuery] string? revokedBy)
        {
            var result = await _service.RevokeAsync(id, revokedBy);
            return Ok(result);
        }

        // Used by LeaveApplicationService.IsAuthorizedForLevelAsync and
        // ResolveApproverUserIdsAsync (via IApprovalDelegationService
        // injected directly in-process) - this HTTP endpoint exists purely
        // so the APP layer / other callers can ask the same question over
        // the API surface if ever needed.
        [HttpGet("active-delegate")]
        public async Task<IActionResult> GetActiveDelegateFor([FromQuery] string delegatorEmployeeId, [FromQuery] DateTime? onDate)
        {
            var result = await _service.GetActiveDelegateForAsync(delegatorEmployeeId, onDate ?? DateTime.UtcNow);
            return Ok(new { delegateEmployeeId = result });
        }
    }
}
