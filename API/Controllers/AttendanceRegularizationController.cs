using Application.DTOs.Attendances;
using Application.Interfaces.Attendances;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AttendanceRegularizationController : ControllerBase
    {
        private readonly IAttendanceRegularizationService _attendanceRegularizationService;

        public AttendanceRegularizationController(IAttendanceRegularizationService attendanceRegularizationService)
        {
            _attendanceRegularizationService = attendanceRegularizationService;
        }

        #region CRUD

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _attendanceRegularizationService.GetAllAsync();
            return Ok(data);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var data = await _attendanceRegularizationService.GetByIdAsync(id);

            if (data == null)
                return NotFound();

            return Ok(data);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RequestRegularizationDto request)
        {
            try
            {
                var result = await _attendanceRegularizationService.CreateAsync(request);

                if (result == null)
                    return BadRequest(new { Message = "Unable to submit regularization request." });

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Business-rule rejection (e.g. an already-pending request
                // for this date, or an invalid employee) - surface the real
                // reason instead of a 500.
                return BadRequest(new { Message = ex.Message });
            }
        }

        #endregion

        #region Workflow

        [HttpPost("request")]
        public async Task<IActionResult> Request([FromBody] RequestRegularizationDto request)
        {
            var result =
                await _attendanceRegularizationService
                    .RequestRegularizationAsync(request);

            return Ok(result);
        }

        [HttpPost("approve")]
        public async Task<IActionResult> Approve([FromBody] ApproveRegularizationRequestDto request)
        {
            try
            {
                var result = await _attendanceRegularizationService.ApproveAsync(request);
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
        public async Task<IActionResult> Reject([FromBody] RejectRegularizationRequestDto request)
        {
            try
            {
                var result = await _attendanceRegularizationService.RejectAsync(request);
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
        public async Task<IActionResult> SendBack([FromBody] SendBackRegularizationRequestDto request)
        {
            try
            {
                var result = await _attendanceRegularizationService.SendBackAsync(request);
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
        public async Task<IActionResult> Resubmit(string id, [FromQuery] string resubmittedBy, [FromBody] RequestRegularizationDto request)
        {
            try
            {
                var result = await _attendanceRegularizationService.ResubmitAsync(id, request, resubmittedBy);

                if (result == null)
                    return BadRequest(new { Message = "Unable to resubmit regularization request." });

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
        public async Task<IActionResult> Cancel([FromBody] CancelRegularizationRequestDto request)
        {
            try
            {
                var result = await _attendanceRegularizationService.CancelAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        #endregion

        #region Queries

        [HttpGet("employee/{employeeId}")]
        public async Task<IActionResult> GetEmployeeRequests(string employeeId)
        {
            var data =
                await _attendanceRegularizationService
                    .GetEmployeeRequestsAsync(employeeId);

            return Ok(data);
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPending()
        {
            var data =
                await _attendanceRegularizationService
                    .GetPendingAsync();

            return Ok(data);
        }

        [HttpGet("pending-for-approver")]
        public async Task<IActionResult> GetPendingForApprover([FromQuery] string? employeeId, [FromQuery] string? userId)
        {
            var data =
                await _attendanceRegularizationService
                    .GetPendingForApproverAsync(employeeId, userId);

            return Ok(data);
        }

        // Cosmetic-only helper for the APP self-service UI (button
        // visibility) - the real Level 3 enforcement always happens inside
        // ApproveAsync/RejectAsync/SendBackAsync via IsAuthorizedForLevelAsync
        // regardless of what this returns.
        [HttpGet("is-hr-approver")]
        public async Task<IActionResult> IsHrApprover([FromQuery] string? userId)
        {
            var result = await _attendanceRegularizationService.IsHrApproverAsync(userId);
            return Ok(result);
        }

        [HttpGet("approved")]
        public async Task<IActionResult> GetApproved()
        {
            var data =
                await _attendanceRegularizationService
                    .GetApprovedAsync();

            return Ok(data);
        }

        [HttpGet("rejected")]
        public async Task<IActionResult> GetRejected()
        {
            var data =
                await _attendanceRegularizationService
                    .GetRejectedAsync();

            return Ok(data);
        }

        [HttpGet("cancelled")]
        public async Task<IActionResult> GetCancelled()
        {
            var data =
                await _attendanceRegularizationService
                    .GetCancelledAsync();

            return Ok(data);
        }

        [HttpPost("filter")]
        public async Task<IActionResult> Filter([FromBody] AttendanceRegularizationFilterRequestDto request)
        {
            var data =
                await _attendanceRegularizationService
                    .GetFilteredAsync(request);

            return Ok(data);
        }

        #endregion

        #region Approval History Details

        [HttpGet("approval-history")]
        public async Task<IActionResult> GetAllApprovalHistory()
        {
            return Ok(
                await _attendanceRegularizationService
                    .GetAllApprovalHistoryAsync());
        }

        [HttpGet("approval-history/{id}")]
        public async Task<IActionResult> GetApprovalHistoryById(string id)
        {
            return Ok(
                await _attendanceRegularizationService
                    .GetApprovalHistoryByIdAsync(id));
        }

        [HttpGet("{attendanceRegularizationId}/approval-history")]
        public async Task<IActionResult> GetApprovalHistory(string attendanceRegularizationId)
        {
            return Ok(await _attendanceRegularizationService.GetApprovalHistoryAsync(attendanceRegularizationId));
        }

        #endregion
    }
}
