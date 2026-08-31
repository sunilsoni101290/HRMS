using Application.Common.Responses;
using Application.DTOs.WorkTracking;
using Application.Interfaces.WorkTracking;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>
    /// Manager -> Employee Job/Work Assignment. Same conventions as
    /// DailyWorkEntryController: acting user/tenant resolved from the JWT,
    /// never trusted from the request body; every write re-validated
    /// server-side by WorkAssignmentService (spec section 23).
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class EmployeeWorkAssignmentController : ControllerBase
    {
        private readonly IWorkAssignmentService _service;

        private string? TenantId => User.FindFirst("TenantId")?.Value;
        private string? ActingUserId => User.FindFirst("UserId")?.Value;

        public EmployeeWorkAssignmentController(IWorkAssignmentService service)
        {
            _service = service;
        }

        [HttpPost("assign")]
        public async Task<IActionResult> Assign([FromBody] SaveEmployeeWorkAssignmentDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ApiResponse<object> { Success = false, Message = FirstModelError() });

            try
            {
                var result = await _service.AssignAsync(dto, ActingUserId!, TenantId!);
                return Ok(new ApiResponse<EmployeeWorkAssignmentDto> { Success = true, Data = result, Message = "Work assigned successfully." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new ApiResponse<object> { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object> { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMyAssignments()
        {
            var result = await _service.GetMyAssignmentsAsync(ActingUserId!, TenantId!);
            return Ok(new ApiResponse<List<EmployeeWorkAssignmentSummaryDto>> { Success = true, Data = result });
        }

        [HttpGet("my/work-combo")]
        public async Task<IActionResult> GetMyAssignedWorkCombo()
        {
            var result = await _service.GetMyAssignedWorkComboAsync(ActingUserId!, TenantId!);
            return Ok(new ApiResponse<List<AssignedWorkComboDto>> { Success = true, Data = result });
        }

        [HttpGet("assigned-by-me-or-team")]
        public async Task<IActionResult> GetAssignedByMeOrTeam()
        {
            var result = await _service.GetAssignedByMeOrTeamAsync(ActingUserId!, TenantId!);
            return Ok(new ApiResponse<List<EmployeeWorkAssignmentSummaryDto>> { Success = true, Data = result });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                var result = await _service.GetByIdAsync(id, ActingUserId!, TenantId!);
                return Ok(new ApiResponse<EmployeeWorkAssignmentDto> { Success = true, Data = result });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new ApiResponse<object> { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object> { Success = false, Message = ex.Message });
            }
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateAssignmentStatusDto dto)
        {
            try
            {
                var result = await _service.UpdateStatusAsync(id, dto, ActingUserId!, TenantId!);
                return Ok(new ApiResponse<EmployeeWorkAssignmentDto> { Success = true, Data = result, Message = "Assignment status updated." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new ApiResponse<object> { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object> { Success = false, Message = ex.Message });
            }
        }

        [HttpPost("{id}/reassign")]
        public async Task<IActionResult> Reassign(string id, [FromBody] ReassignEmployeeWorkAssignmentDto dto)
        {
            try
            {
                var result = await _service.ReassignAsync(id, dto, ActingUserId!, TenantId!);
                return Ok(new ApiResponse<EmployeeWorkAssignmentDto> { Success = true, Data = result, Message = "Assignment reassigned successfully." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new ApiResponse<object> { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object> { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("my/dashboard")]
        public async Task<IActionResult> GetMyWorkDashboard()
        {
            var result = await _service.GetMyWorkDashboardAsync(ActingUserId!, TenantId!);
            return Ok(new ApiResponse<MyWorkDashboardDto> { Success = true, Data = result });
        }

        [HttpGet("team/overview")]
        public async Task<IActionResult> GetTeamWorkOverview()
        {
            var result = await _service.GetTeamWorkOverviewAsync(ActingUserId!, TenantId!);
            return Ok(new ApiResponse<TeamWorkOverviewDto> { Success = true, Data = result });
        }

        [HttpGet("reports/employee-assignment")]
        public async Task<IActionResult> GetEmployeeAssignmentReport()
        {
            var result = await _service.GetEmployeeAssignmentReportAsync(ActingUserId!, TenantId!);
            return Ok(new ApiResponse<List<EmployeeAssignmentReportRowDto>> { Success = true, Data = result });
        }

        private string FirstModelError() =>
            ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault()
            ?? "Please correct the highlighted fields.";
    }
}
