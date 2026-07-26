using Application.DTOs.EmployeeLifecycle;
using Application.Interfaces.EmployeeLifecycle;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    // Phase 2 of "Probation & Confirmation" - Performance Improvement Plan
    // (PIP). See PipService for the actingUserId != MakerId invariant
    // enforced inside ApproveOutcome/RejectOutcome (before any permission
    // check, no override). Route shape mirrors
    // ProbationConfirmationController.
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PipController : ControllerBase
    {
        private readonly IPipService _pipService;

        public PipController(IPipService pipService)
        {
            _pipService = pipService;
        }

        // TenantId/UserId are read from the JWT claims exactly like
        // ProbationConfirmationController.
        private string? TenantId => User.FindFirst("TenantId")?.Value;
        private string? ActingUserId => User.FindFirst("UserId")?.Value;

        // POST api/pip - HR/system creation, hand-off from an Approved
        // Probation Confirmation with Recommendation == PlaceOnPIP. No
        // maker-checker gate on this action.
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePipRecordDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _pipService.CreateAsync(dto, TenantId, ActingUserId);
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

        // GET api/pip/active?departmentId=&search= - HR "active PIPs"
        // tracking view (FinalOutcome == InProgress only).
        [HttpGet("active")]
        public async Task<IActionResult> GetActive([FromQuery] string? departmentId, [FromQuery] string? search)
        {
            try
            {
                var result = await _pipService.GetActivePipsAsync(TenantId, departmentId, search, ActingUserId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Message = ex.Message });
            }
        }

        // GET api/pip?finalOutcome=&departmentId=&search=
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? finalOutcome, [FromQuery] string? departmentId, [FromQuery] string? search)
        {
            try
            {
                var result = await _pipService.GetAllAsync(TenantId, finalOutcome, departmentId, search, ActingUserId);
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
                var result = await _pipService.GetByIdAsync(id, TenantId, ActingUserId);
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

        // PUT api/pip/{id}/propose-outcome - Maker action.
        [HttpPut("{id}/propose-outcome")]
        public async Task<IActionResult> ProposeOutcome(string id, [FromBody] ProposePipOutcomeDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _pipService.ProposeOutcomeAsync(id, dto, TenantId, ActingUserId);
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

        // PUT api/pip/{id}/approve-outcome - Checker action.
        [HttpPut("{id}/approve-outcome")]
        public async Task<IActionResult> ApproveOutcome(string id, [FromBody] CheckerActionDto dto)
        {
            try
            {
                var result = await _pipService.ApproveOutcomeAsync(id, dto, ActingUserId, TenantId);
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

        // PUT api/pip/{id}/reject-outcome - Checker action.
        [HttpPut("{id}/reject-outcome")]
        public async Task<IActionResult> RejectOutcome(string id, [FromBody] CheckerActionDto dto)
        {
            try
            {
                var result = await _pipService.RejectOutcomeAsync(id, dto, ActingUserId, TenantId);
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
