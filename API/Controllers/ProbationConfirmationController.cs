using Application.DTOs.EmployeeLifecycle;
using Application.Interfaces.EmployeeLifecycle;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    // Foundational Maker-Checker (segregation-of-duties) feature - see
    // ProbationConfirmationService for the actingUserId != MakerId
    // invariant enforced inside Approve/Reject (before any permission
    // check, no override). Later PIP Outcome / Employee Transfer
    // controllers should mirror this route shape.
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProbationConfirmationController : ControllerBase
    {
        private readonly IProbationConfirmationService _probationConfirmationService;

        public ProbationConfirmationController(IProbationConfirmationService probationConfirmationService)
        {
            _probationConfirmationService = probationConfirmationService;
        }

        // TenantId/UserId are read from the JWT claims exactly like
        // WfhRequestController / AttendancePolicyController - see
        // Application/Services/JWT Token/JwtService.cs for how these
        // claims are issued at login. FIX (defect C1): these were
        // previously accepted as plain query-string/body parameters, which
        // let any authenticated user impersonate any other user/tenant by
        // simply supplying different values - the [Authorize] attribute
        // only proves *a* valid JWT exists, it does not validate which
        // tenant/user the caller claims to be unless the values are pulled
        // from the token itself. Do NOT reintroduce TenantId/ActingUserId
        // as bindable action parameters or DTO properties.
        private string? TenantId => User.FindFirst("TenantId")?.Value;
        private string? ActingUserId => User.FindFirst("UserId")?.Value;

        // GET api/probationconfirmation/due-for-review?departmentId=&search=
        [HttpGet("due-for-review")]
        public async Task<IActionResult> GetDueForReview([FromQuery] string? departmentId, [FromQuery] string? search)
        {
            try
            {
                var result = await _probationConfirmationService.GetDueForReviewAsync(TenantId, departmentId, search, ActingUserId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Message = ex.Message });
            }
        }

        // POST api/probationconfirmation - Maker action.
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProbationConfirmationDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _probationConfirmationService.CreateAsync(dto, TenantId, ActingUserId);
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

        // GET api/probationconfirmation?status=&departmentId=&search=
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] string? departmentId, [FromQuery] string? search)
        {
            try
            {
                var result = await _probationConfirmationService.GetAllAsync(TenantId, status, departmentId, search, ActingUserId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                var result = await _probationConfirmationService.GetByIdAsync(id, TenantId, ActingUserId);
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

        // PUT api/probationconfirmation/{id}/approve - Checker action.
        [HttpPut("{id}/approve")]
        public async Task<IActionResult> Approve(string id, [FromBody] CheckerActionDto dto)
        {
            try
            {
                var result = await _probationConfirmationService.ApproveAsync(id, dto, ActingUserId, TenantId);
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

        // PUT api/probationconfirmation/{id}/reject - Checker action.
        [HttpPut("{id}/reject")]
        public async Task<IActionResult> Reject(string id, [FromBody] CheckerActionDto dto)
        {
            try
            {
                var result = await _probationConfirmationService.RejectAsync(id, dto, ActingUserId, TenantId);
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
