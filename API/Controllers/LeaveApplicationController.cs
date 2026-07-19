using Application.DTOs.Leaves;
using Application.Interfaces.Leaves;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LeaveApplicationController : ControllerBase
    {
        private readonly ILeaveApplicationService _leaveApplicationService;

        public LeaveApplicationController(ILeaveApplicationService leaveApplicationService)
        {
            _leaveApplicationService = leaveApplicationService;
        }

        #region CRUD

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _leaveApplicationService.GetAllAsync();
            return Ok(data);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var data = await _leaveApplicationService.GetByIdAsync(id);

            if (data == null)
                return NotFound();

            return Ok(data);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ApplyLeaveRequestDto request)
        {
            try
            {
                var result = await _leaveApplicationService.CreateAsync(request);

                if (result == null)
                    return BadRequest(new { Message = "Unable to submit leave application." });

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Business-rule rejection (e.g. an already-pending leave
                // request, or an invalid employee) - surface the real
                // reason instead of a 500.
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] ApplyLeaveRequestDto request)
        {
            var result =
                await _leaveApplicationService.UpdateAsync(
                    id,
                    request);

            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var result =
                await _leaveApplicationService.DeleteAsync(id);

            return Ok(result);
        }

        #endregion

        #region Workflow

        [HttpPost("apply")]
        public async Task<IActionResult> ApplyLeave([FromBody] ApplyLeaveRequestDto request)
        {
            var result =
                await _leaveApplicationService
                    .ApplyLeaveAsync(request);

            return Ok(result);
        }

        [HttpPost("approve")]
        public async Task<IActionResult> ApproveLeave([FromBody] ApproveLeaveRequestDto request)
        {
            try
            {
                var result = await _leaveApplicationService.ApproveLeaveAsync(request);
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

        [HttpPost("reject")]
        public async Task<IActionResult> RejectLeave([FromBody] RejectLeaveRequestDto request)
        {
            try
            {
                var result = await _leaveApplicationService.RejectLeaveAsync(request);
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

        [HttpPost("send-back")]
        public async Task<IActionResult> SendBackLeave([FromBody] SendBackLeaveRequestDto request)
        {
            try
            {
                var result = await _leaveApplicationService.SendBackLeaveAsync(request);
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

        [HttpPut("{id}/resubmit")]
        public async Task<IActionResult> Resubmit(string id, [FromQuery] string resubmittedBy, [FromBody] ApplyLeaveRequestDto request)
        {
            try
            {
                var result = await _leaveApplicationService.ResubmitAsync(id, request, resubmittedBy);

                if (result == null)
                    return BadRequest(new { Message = "Unable to resubmit leave application." });

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

        [HttpPost("cancel")]
        public async Task<IActionResult> CancelLeave([FromBody] CancelLeaveRequestDto request)
        {
            var result =
                await _leaveApplicationService
                    .CancelLeaveAsync(request);

            return Ok(result);
        }

        #endregion

        #region Day Calculation

        [HttpGet("calculate-days")]
        public async Task<IActionResult> CalculateDays(
            [FromQuery] DateTime fromDate,
            [FromQuery] DateTime toDate,
            [FromQuery] bool isHalfDay,
            [FromQuery] string? tenantId)
        {
            var totalDays = await _leaveApplicationService
                .CalculateTotalDaysAsync(fromDate, toDate, isHalfDay, tenantId);

            return Ok(new { totalDays });
        }

        #endregion

        #region Queries

        [HttpGet("employee/{employeeId}")]
        public async Task<IActionResult> GetEmployeeLeaves(string employeeId)
        {
            var data =
                await _leaveApplicationService
                    .GetEmployeeLeavesAsync(employeeId);

            return Ok(data);
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingLeaves()
        {
            var data =
                await _leaveApplicationService
                    .GetPendingLeavesAsync();

            return Ok(data);
        }

        [HttpGet("pending-for-approver")]
        public async Task<IActionResult> GetPendingForApprover([FromQuery] string? employeeId, [FromQuery] string? userId)
        {
            var data =
                await _leaveApplicationService
                    .GetPendingForApproverAsync(employeeId, userId);

            return Ok(data);
        }

        // Cosmetic-only helper for the APP self-service UI (button
        // visibility) - the real Level 3 enforcement always happens inside
        // ApproveLeaveAsync/RejectLeaveAsync/SendBackLeaveAsync via
        // IsAuthorizedForLevelAsync regardless of what this returns.
        [HttpGet("is-hr-approver")]
        public async Task<IActionResult> IsHrApprover([FromQuery] string? userId)
        {
            var result = await _leaveApplicationService.IsHrApproverAsync(userId);
            return Ok(result);
        }

        [HttpGet("approved")]
        public async Task<IActionResult> GetApprovedLeaves()
        {
            var data =
                await _leaveApplicationService
                    .GetApprovedLeavesAsync();

            return Ok(data);
        }

        [HttpGet("rejected")]
        public async Task<IActionResult> GetRejectedLeaves()
        {
            var data =
                await _leaveApplicationService
                    .GetRejectedLeavesAsync();

            return Ok(data);
        }

        [HttpGet("cancelled")]
        public async Task<IActionResult> GetCancelledLeaves()
        {
            var data =
                await _leaveApplicationService
                    .GetCancelledLeavesAsync();

            return Ok(data);
        }

        [HttpPost("filter")]
        public async Task<IActionResult> Filter([FromBody] LeaveApplicationFilterRequestDto request)
        {
            var data =
                await _leaveApplicationService
                    .GetFilteredAsync(request);

            return Ok(data);
        }

        #endregion

        #region Dashboard Counts

        [HttpGet("count/pending")]
        public async Task<IActionResult> GetPendingCount()
        {
            var count =
                await _leaveApplicationService
                    .GetPendingLeaveCountAsync();

            return Ok(count);
        }

        [HttpGet("count/approved")]
        public async Task<IActionResult> GetApprovedCount()
        {
            var count =
                await _leaveApplicationService
                    .GetApprovedLeaveCountAsync();

            return Ok(count);
        }

        [HttpGet("count/today")]
        public async Task<IActionResult> GetTodayLeaveCount()
        {
            var count =
                await _leaveApplicationService
                    .GetTodayLeaveCountAsync();

            return Ok(count);
        }

        #endregion

        #region Calendar

        [HttpGet("calendar")]
        public async Task<IActionResult> Calendar(
            [FromQuery] int year,
            [FromQuery] int month,
            [FromQuery] string? employeeId,
            [FromQuery] bool isAdmin,
            [FromQuery] string? tenantId,
            [FromQuery] string? departmentId)
        {
            var data = await _leaveApplicationService.GetCalendarAsync(
                year, month, employeeId, isAdmin, tenantId, departmentId);

            return Ok(data);
        }

        #endregion

        #region Approval History Details
        [HttpGet("approval-history")]
        public async Task<IActionResult>GetAllApprovalHistory()
        {
            return Ok(
                await _leaveApplicationService
                    .GetAllApprovalHistoryAsync());
        }

        [HttpGet("approval-history/{id}")]
        public async Task<IActionResult>GetApprovalHistoryById(string id)
        {
            return Ok(
                await _leaveApplicationService
                    .GetApprovalHistoryByIdAsync(id));
        }

        [HttpGet("{leaveApplicationId}/approval-history")]
        public async Task<IActionResult>GetApprovalHistory(string leaveApplicationId)
        {
            return Ok(await _leaveApplicationService.GetApprovalHistoryAsync(leaveApplicationId));
        }
        #endregion
    }
}
