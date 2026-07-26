using Application.DTOs.Attendances;
using Application.Interfaces.Attendances;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    // Comp Off candidates are only ever created by
    // API/BackgroundServices/CompOffDetectionService.cs - there is
    // deliberately no POST/Create endpoint here (see ICompOffService).
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CompOffController : ControllerBase
    {
        private readonly ICompOffService _compOffService;

        public CompOffController(ICompOffService compOffService)
        {
            _compOffService = compOffService;
        }

        // TenantId/UserId are read from the JWT claims exactly like
        // WfhRequestController/ShortLeaveRequestController - see
        // Application/Services/JWT Token/JwtService.cs for how these claims
        // are issued at login.
        private string? TenantId => User.FindFirst("TenantId")?.Value;
        private string? ActingUserId => User.FindFirst("UserId")?.Value;

        // GET api/compoff/pending-review?departmentId=&search=  (HR list)
        [HttpGet("pending-review")]
        public async Task<IActionResult> GetPendingReview([FromQuery] string? departmentId, [FromQuery] string? search)
        {
            var result = await _compOffService.GetPendingReviewAsync(TenantId, departmentId, search);
            return Ok(result);
        }

        // GET api/compoff/my - current user's own Comp Off history (pending
        // + reviewed). There is no "EmployeeId" JWT claim in this codebase
        // (see JwtService), so - exactly like WfhRequestController - the
        // acting User.Id is passed down and the service resolves the linked
        // EmployeeId internally.
        [HttpGet("my")]
        public async Task<IActionResult> GetMy()
        {
            var result = await _compOffService.GetMyCreditsAsync(ActingUserId, TenantId);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                var result = await _compOffService.GetByIdAsync(id, TenantId, ActingUserId);
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

        [HttpPut("{id}/approve")]
        public async Task<IActionResult> Approve(string id)
        {
            try
            {
                var result = await _compOffService.ApproveAsync(id, ActingUserId, TenantId);
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
        public async Task<IActionResult> Reject(string id, [FromBody] CompOffRejectRequestDto dto)
        {
            try
            {
                var result = await _compOffService.RejectAsync(id, dto?.Reason, ActingUserId, TenantId);
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
