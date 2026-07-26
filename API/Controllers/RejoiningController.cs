using Application.DTOs.EmployeeLifecycle;
using Application.Interfaces.EmployeeLifecycle;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    // Rejoining - Phase 5 (final) of "Probation & Confirmation" (Employee
    // Lifecycle). Simple, single-step, HR-permission-gated rehire action,
    // NO maker-checker workflow - see RejoiningService for the
    // authorization/validation rules. Route shape mirrors
    // EmployeeTransferController/EmployeeFeedbackController.
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RejoiningController : ControllerBase
    {
        private readonly IRejoiningService _rejoiningService;

        public RejoiningController(IRejoiningService rejoiningService)
        {
            _rejoiningService = rejoiningService;
        }

        // TenantId/UserId are read from the JWT claims exactly like
        // EmployeeTransferController/EmployeeFeedbackController.
        private string? TenantId => User.FindFirst("TenantId")?.Value;
        private string? ActingUserId => User.FindFirst("UserId")?.Value;

        // GET api/rejoining/eligible?search= - "picker" list of former
        // employees eligible to rejoin.
        [HttpGet("eligible")]
        public async Task<IActionResult> GetEligible([FromQuery] string? search)
        {
            var result = await _rejoiningService.GetEligibleForRejoinAsync(TenantId, search);
            return Ok(result);
        }

        // POST api/rejoining - the single rejoin action.
        [HttpPost]
        public async Task<IActionResult> Rejoin([FromBody] RejoinEmployeeDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _rejoiningService.RejoinAsync(dto, TenantId, ActingUserId);
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

        // GET api/rejoining?search= - HR audit list, tenant-wide.
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? search)
        {
            var result = await _rejoiningService.GetAllAsync(TenantId, search);
            return Ok(result);
        }

        // GET api/rejoining/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                var result = await _rejoiningService.GetByIdAsync(id, TenantId, ActingUserId);
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

        // GET api/rejoining/employee/{employeeId}/history
        [HttpGet("employee/{employeeId}/history")]
        public async Task<IActionResult> GetHistoryForEmployee(string employeeId)
        {
            var result = await _rejoiningService.GetHistoryForEmployeeAsync(employeeId, TenantId);
            return Ok(result);
        }
    }
}
