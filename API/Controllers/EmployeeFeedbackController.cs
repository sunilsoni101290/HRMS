using Application.DTOs.EmployeeLifecycle;
using Application.Interfaces.EmployeeLifecycle;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    // Employee Feedback - Phase 4 of the "Probation & Confirmation"
    // (Employee Lifecycle) module. Plain CRUD, NO maker-checker workflow -
    // see EmployeeFeedbackService for the Reporting-Manager-or-HR
    // authorization rules and the IsVisibleToEmployee-driven visibility
    // filter.
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class EmployeeFeedbackController : ControllerBase
    {
        private readonly IEmployeeFeedbackService _employeeFeedbackService;

        public EmployeeFeedbackController(IEmployeeFeedbackService employeeFeedbackService)
        {
            _employeeFeedbackService = employeeFeedbackService;
        }

        // TenantId/UserId are read from the JWT claims exactly like
        // ProbationConfirmationController.
        // POST api/employeefeedback
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUpdateEmployeeFeedbackDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _employeeFeedbackService.CreateAsync(dto, dto.TenantId, dto.ActingUserId);
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

        // PUT api/employeefeedback/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] CreateUpdateEmployeeFeedbackDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _employeeFeedbackService.UpdateAsync(id, dto, dto.TenantId, dto.ActingUserId);
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

        // DELETE api/employeefeedback/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id,string tenantId,string actingUserId)
        {
            try
            {
                await _employeeFeedbackService.DeleteAsync(id, tenantId, actingUserId);
                return Ok(new { Message = "Feedback record deleted." });
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

        // GET api/employeefeedback/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id, string tenantId, string actingUserId)
        {
            try
            {
                var result = await _employeeFeedbackService.GetByIdAsync(id, tenantId, actingUserId);
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

        // GET api/employeefeedback/employee/{employeeId} - self-service "my
        // feedback" view and HR/manager "feedback about employee X" view
        // both use this same endpoint; the returned rows differ per caller
        // via the service's per-record visibility filter.
        [HttpGet("employee/{employeeId}")]
        public async Task<IActionResult> GetForEmployee(string employeeId, string tenantId, string actingUserId)
        {
            try
            {
                var result = await _employeeFeedbackService.GetForEmployeeAsync(employeeId, tenantId, actingUserId);
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

        // GET api/employeefeedback/given-by-me
        [HttpGet("given-by-me")]
        public async Task<IActionResult> GetGivenByMe(string tenantId, string actingUserId)
        {
            var result = await _employeeFeedbackService.GetGivenByMeAsync(tenantId, actingUserId);
            return Ok(result);
        }
    }
}
