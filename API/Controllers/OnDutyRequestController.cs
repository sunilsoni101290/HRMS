using Application.DTOs.Attendances;
using Application.Interfaces.Attendances;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OnDutyRequestController : ControllerBase
    {
        private readonly IOnDutyRequestService _onDutyRequestService;

        public OnDutyRequestController(IOnDutyRequestService onDutyRequestService)
        {
            _onDutyRequestService = onDutyRequestService;
        }

        // TenantId/UserId are read from the JWT claims exactly like
        // WfhRequestController - see Application/Services/JWT Token/
        // JwtService.cs for how these claims are issued at login.
        private string? TenantId => User.FindFirst("TenantId")?.Value;
        private string? ActingUserId => User.FindFirst("UserId")?.Value;

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOnDutyRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _onDutyRequestService.CreateAsync(dto, TenantId, ActingUserId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // GET api/ondutyrequest?status=&departmentId=&search=  (admin/HR list)
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] string? departmentId, [FromQuery] string? search)
        {
            var result = await _onDutyRequestService.GetAllAsync(TenantId, status, departmentId, search);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                var result = await _onDutyRequestService.GetByIdAsync(id, TenantId, ActingUserId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }

        // GET api/ondutyrequest/my - current user's own requests. There is
        // no "EmployeeId" JWT claim in this codebase (see JwtService), so -
        // exactly like WfhRequestController's "my" endpoint - the acting
        // User.Id is passed down and the service resolves the linked
        // EmployeeId internally.
        [HttpGet("my")]
        public async Task<IActionResult> GetMy()
        {
            var result = await _onDutyRequestService.GetMyRequestsAsync(ActingUserId, TenantId);
            return Ok(result);
        }

        // GET api/ondutyrequest/pending-for-approver - resolved via claims.
        [HttpGet("pending-for-approver")]
        public async Task<IActionResult> GetPendingForApprover()
        {
            var result = await _onDutyRequestService.GetPendingForApproverAsync(ActingUserId, TenantId);
            return Ok(result);
        }

        [HttpPut("{id}/approve")]
        public async Task<IActionResult> Approve(string id)
        {
            try
            {
                var result = await _onDutyRequestService.ApproveAsync(id, ActingUserId, TenantId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("{id}/reject")]
        public async Task<IActionResult> Reject(string id, [FromBody] OnDutyRejectRequestDto dto)
        {
            try
            {
                var result = await _onDutyRequestService.RejectAsync(id, dto?.Reason, ActingUserId, TenantId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> Cancel(string id)
        {
            try
            {
                var result = await _onDutyRequestService.CancelAsync(id, ActingUserId, TenantId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
