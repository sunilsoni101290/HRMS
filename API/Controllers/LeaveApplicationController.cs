using Application.DTOs.Leaves;
using Application.Interfaces.Leaves;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LeaveApplicationController : ControllerBase
    {
        private readonly ILeaveApplicationService _service;

        public LeaveApplicationController(
            ILeaveApplicationService service)
        {
            _service = service;
        }

        // =====================================================
        // GET ALL
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _service.GetAllAsync();

            return Ok(data);
        }

        // =====================================================
        // GET BY ID
        // =====================================================

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var data = await _service.GetByIdAsync(id);

            if (data == null)
                return NotFound();

            return Ok(data);
        }

        // =====================================================
        // GET BY EMPLOYEE
        // =====================================================

        [HttpGet("employee/{employeeId}")]
        public async Task<IActionResult> GetByEmployee(string employeeId)
        {
            var data = await _service.GetByEmployeeAsync(employeeId);

            return Ok(data);
        }

        // =====================================================
        // APPLY LEAVE
        // =====================================================

        [HttpPost("apply")]
        public async Task<IActionResult> ApplyLeave(
            [FromBody] LeaveApplicationDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _service.ApplyLeaveAsync(dto);

            return Ok(result);
        }

        // =====================================================
        // APPROVE LEAVE
        // =====================================================

        [HttpPost("approve")]
        public async Task<IActionResult> ApproveLeave(
            string leaveApplicationId,
            string approvedBy)
        {
            var result = await _service
                .ApproveLeaveAsync(
                    leaveApplicationId,
                    approvedBy);

            return Ok(new
            {
                Success = result,
                Message = "Leave approved successfully."
            });
        }

        // =====================================================
        // REJECT LEAVE
        // =====================================================

        [HttpPost("reject")]
        public async Task<IActionResult> RejectLeave(
            string leaveApplicationId,
            string approvedBy,
            string rejectionReason)
        {
            var result = await _service
                .RejectLeaveAsync(
                    leaveApplicationId,
                    approvedBy,
                    rejectionReason);

            return Ok(new
            {
                Success = result,
                Message = "Leave rejected successfully."
            });
        }

        // =====================================================
        // CANCEL LEAVE
        // =====================================================

        [HttpPost("cancel")]
        public async Task<IActionResult> CancelLeave(
            string leaveApplicationId)
        {
            var result = await _service
                .CancelLeaveAsync(
                    leaveApplicationId);

            return Ok(new
            {
                Success = result,
                Message = "Leave cancelled successfully."
            });
        }

        // =====================================================
        // DELETE
        // =====================================================

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _service.DeleteAsync(id);

            if (!result)
                return NotFound();

            return Ok(new
            {
                Success = true,
                Message = "Leave application deleted successfully."
            });
        }

        // =====================================================
        // PENDING APPROVALS
        // =====================================================

        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingApprovals()
        {
            var data = await _service.GetPendingApprovalsAsync();

            return Ok(data);
        }

        // =====================================================
        // APPROVED LEAVES
        // =====================================================

        [HttpGet("approved")]
        public async Task<IActionResult> GetApprovedLeaves()
        {
            var data = await _service.GetApprovedLeavesAsync();

            return Ok(data);
        }

        // =====================================================
        // REJECTED LEAVES
        // =====================================================

        [HttpGet("rejected")]
        public async Task<IActionResult> GetRejectedLeaves()
        {
            var data = await _service.GetRejectedLeavesAsync();

            return Ok(data);
        }

        // =====================================================
        // DATE RANGE REPORT
        // =====================================================

        [HttpGet("date-range")]
        public async Task<IActionResult> GetByDateRange(
            DateTime fromDate,
            DateTime toDate)
        {
            var data = await _service
                .GetByDateRangeAsync(
                    fromDate,
                    toDate);

            return Ok(data);
        }

        // =====================================================
        // PENDING COUNT
        // =====================================================

        [HttpGet("count/pending")]
        public async Task<IActionResult> GetPendingCount()
        {
            var count =
                await _service.GetPendingLeaveCountAsync();

            return Ok(count);
        }

        // =====================================================
        // APPROVED COUNT
        // =====================================================

        [HttpGet("count/approved")]
        public async Task<IActionResult> GetApprovedCount()
        {
            var count =
                await _service.GetApprovedLeaveCountAsync();

            return Ok(count);
        }

        // =====================================================
        // TODAY LEAVE COUNT
        // =====================================================

        [HttpGet("count/today")]
        public async Task<IActionResult> GetTodayLeaveCount()
        {
            var count =
                await _service.GetTodayLeaveCountAsync();

            return Ok(count);
        }
    }
}
