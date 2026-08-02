using Application.DTOs.Payroll;
using Application.Interfaces.Payroll;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    #region Payslip Request API

    // Employee -> Reporting Manager -> Finance payslip request workflow.
    // See Application/Services/PayrollService/PayslipRequestService.cs for
    // the full state machine. Same thin-wrapper shape as
    // WfhRequestController - TenantId/ActingUserId read from JWT claims,
    // UnauthorizedAccessException -> 403, other Exception -> 400.
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PayslipRequestController : ControllerBase
    {
        private readonly IPayslipRequestService _service;

        public PayslipRequestController(IPayslipRequestService service)
        {
            _service = service;
        }

        private string? TenantId => User.FindFirst("TenantId")?.Value;
        private string? ActingUserId => User.FindFirst("UserId")?.Value;

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePayslipRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _service.CreateAsync(dto, TenantId, ActingUserId);
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

        // GET api/paysliprequest?status= (org-wide list, admin/support use)
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? status)
        {
            var result = await _service.GetAllAsync(TenantId, status);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                var result = await _service.GetByIdAsync(id, TenantId, ActingUserId);
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

        [HttpGet("my")]
        public async Task<IActionResult> GetMy()
        {
            var result = await _service.GetMyRequestsAsync(ActingUserId, TenantId);
            return Ok(result);
        }

        [HttpGet("pending-for-manager")]
        public async Task<IActionResult> GetPendingForManager()
        {
            var result = await _service.GetPendingForManagerAsync(ActingUserId, TenantId);
            return Ok(result);
        }

        [HttpPut("{id}/manager-approve")]
        public async Task<IActionResult> ManagerApprove(string id, [FromBody] PayslipRequestActionDto? dto)
        {
            try
            {
                var result = await _service.ManagerApproveAsync(id, dto?.Remarks, ActingUserId, TenantId);
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

        [HttpPut("{id}/manager-reject")]
        public async Task<IActionResult> ManagerReject(string id, [FromBody] PayslipRequestRejectDto dto)
        {
            try
            {
                var result = await _service.ManagerRejectAsync(id, dto?.Reason, ActingUserId, TenantId);
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

        [HttpGet("pending-for-finance")]
        public async Task<IActionResult> GetPendingForFinance()
        {
            var result = await _service.GetPendingForFinanceAsync(ActingUserId, TenantId);
            return Ok(result);
        }

        [HttpPut("{id}/finance-upload")]
        public async Task<IActionResult> FinanceUpload(string id, [FromBody] PayslipRequestUploadDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _service.FinanceUploadAsync(
                    id, dto.DocumentUrl, dto.DocumentFileName, dto.Remarks, ActingUserId, TenantId);
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

        [HttpPut("{id}/finance-complete")]
        public async Task<IActionResult> FinanceComplete(string id, [FromBody] PayslipRequestActionDto? dto)
        {
            try
            {
                var result = await _service.FinanceCompleteAsync(id, dto?.Remarks, ActingUserId, TenantId);
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

        [HttpPut("{id}/finance-reject")]
        public async Task<IActionResult> FinanceReject(string id, [FromBody] PayslipRequestRejectDto dto)
        {
            try
            {
                var result = await _service.FinanceRejectAsync(id, dto?.Reason, ActingUserId, TenantId);
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

        // The only path to a payslip's DocumentUrl - only succeeds when
        // Status == Completed and the caller is the owner (or Finance/HR/
        // Admin override). Logs a "Downloaded" audit row.
        [HttpGet("{id}/download")]
        public async Task<IActionResult> Download(string id)
        {
            try
            {
                var result = await _service.GetForDownloadAsync(id, ActingUserId, TenantId);
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

    #endregion
}
