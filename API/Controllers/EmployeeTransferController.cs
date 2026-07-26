using Application.DTOs.EmployeeLifecycle;
using Application.Interfaces.EmployeeLifecycle;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    // Phase 3 of "Probation & Confirmation" (Employee Lifecycle) - Employee
    // Transfer. See EmployeeTransferService for the actingUserId !=
    // MakerId invariant enforced inside Approve/Reject (before any
    // permission check, no override). Route shape mirrors
    // ProbationConfirmationController/PipController.
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class EmployeeTransferController : ControllerBase
    {
        private readonly IEmployeeTransferService _employeeTransferService;

        public EmployeeTransferController(IEmployeeTransferService employeeTransferService)
        {
            _employeeTransferService = employeeTransferService;
        }

        // TenantId/UserId are read from the JWT claims exactly like
        // ProbationConfirmationController/PipController.
        private string? TenantId => User.FindFirst("TenantId")?.Value;
        private string? ActingUserId => User.FindFirst("UserId")?.Value;

        // POST api/employeetransfer - Maker action.
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateEmployeeTransferDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _employeeTransferService.CreateAsync(dto, TenantId, ActingUserId);
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

        // GET api/employeetransfer?status=&departmentId=&search=
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] string? departmentId, [FromQuery] string? search)
        {
            var result = await _employeeTransferService.GetAllAsync(TenantId, status, departmentId, search);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                var result = await _employeeTransferService.GetByIdAsync(id, TenantId, ActingUserId);
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

        // GET api/employeetransfer/employee/{employeeId}/history
        [HttpGet("employee/{employeeId}/history")]
        public async Task<IActionResult> GetHistoryForEmployee(string employeeId)
        {
            var result = await _employeeTransferService.GetTransferHistoryForEmployeeAsync(employeeId, TenantId);
            return Ok(result);
        }

        // PUT api/employeetransfer/{id}/approve - Checker action.
        [HttpPut("{id}/approve")]
        public async Task<IActionResult> Approve(string id, [FromBody] CheckerActionDto dto)
        {
            try
            {
                var result = await _employeeTransferService.ApproveAsync(id, dto, ActingUserId, TenantId);
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

        // PUT api/employeetransfer/{id}/reject - Checker action.
        [HttpPut("{id}/reject")]
        public async Task<IActionResult> Reject(string id, [FromBody] CheckerActionDto dto)
        {
            try
            {
                var result = await _employeeTransferService.RejectAsync(id, dto, ActingUserId, TenantId);
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
