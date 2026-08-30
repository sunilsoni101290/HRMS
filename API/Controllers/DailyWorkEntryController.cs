using Application.Common.Responses;
using Application.DTOs.WorkTracking;
using Application.Interfaces.WorkTracking;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>
    /// Daily Work Entry / Employee Work Tracking - Draft/Submit/Approve/
    /// Reject workflow. EmployeeId is NEVER accepted from the client for
    /// self-service actions - every action resolves the acting employee
    /// server-side from the JWT's UserId claim (spec section 35), same
    /// convention as WfhRequestController/AttendanceRegularizationController.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DailyWorkEntryController : ControllerBase
    {
        private readonly IDailyWorkEntryService _service;

        private string? TenantId => User.FindFirst("TenantId")?.Value;
        private string? ActingUserId => User.FindFirst("UserId")?.Value;

        public DailyWorkEntryController(IDailyWorkEntryService service)
        {
            _service = service;
        }

        [HttpGet("by-date")]
        public async Task<IActionResult> GetByDate([FromQuery] string workDate)
        {
            try
            {
                var result = await _service.GetMyEntryForDateAsync(workDate, ActingUserId!, TenantId!);
                return Ok(new ApiResponse<DailyWorkLogDto> { Success = true, Data = result });
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

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                var result = await _service.GetByIdAsync(id, ActingUserId!, TenantId!);
                return Ok(new ApiResponse<DailyWorkLogDto> { Success = true, Data = result });
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
        public async Task<IActionResult> GetMyEntries([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            var result = await _service.GetMyEntriesAsync(ActingUserId!, TenantId!, fromDate, toDate);
            return Ok(new ApiResponse<List<DailyWorkLogSummaryDto>> { Success = true, Data = result });
        }

        [HttpPost("draft")]
        public async Task<IActionResult> SaveDraft([FromBody] SaveDailyWorkLogDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ApiResponse<object> { Success = false, Message = FirstModelError() });

            try
            {
                var result = await _service.SaveDraftAsync(dto, ActingUserId!, TenantId!);
                return Ok(new ApiResponse<DailyWorkLogDto> { Success = true, Data = result, Message = "Draft saved successfully." });
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

        [HttpPost("submit")]
        public async Task<IActionResult> Submit([FromBody] SaveDailyWorkLogDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ApiResponse<object> { Success = false, Message = FirstModelError() });

            try
            {
                var result = await _service.SubmitAsync(dto, ActingUserId!, TenantId!);
                return Ok(new ApiResponse<DailyWorkLogDto> { Success = true, Data = result, Message = "Daily work submitted for approval." });
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

        [HttpGet("pending-approval")]
        public async Task<IActionResult> GetPendingApproval()
        {
            var result = await _service.GetPendingApprovalAsync(ActingUserId!, TenantId!);
            return Ok(new ApiResponse<List<DailyWorkLogSummaryDto>> { Success = true, Data = result });
        }

        [HttpPost("{id}/approve")]
        public async Task<IActionResult> Approve(string id)
        {
            try
            {
                var result = await _service.ApproveAsync(id, ActingUserId!, TenantId!);
                return Ok(new ApiResponse<DailyWorkLogDto> { Success = true, Data = result, Message = "Entry approved successfully." });
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

        [HttpPost("{id}/reject")]
        public async Task<IActionResult> Reject(string id, [FromBody] RejectDailyWorkLogDto dto)
        {
            try
            {
                var result = await _service.RejectAsync(id, dto, ActingUserId!, TenantId!);
                return Ok(new ApiResponse<DailyWorkLogDto> { Success = true, Data = result, Message = "Entry rejected." });
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

        private string FirstModelError() =>
            ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault()
            ?? "Please correct the highlighted fields.";
    }
}
