using Application.Common.Responses;
using Application.DTOs.Attendances;
using Application.DTOs.Employee;
using Application.Interfaces.EmployeeInterface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EmployeeController : ControllerBase
    {
        private readonly IEmployeeService _service;

        public EmployeeController(IEmployeeService service)
        {
            _service = service;
        }

        // ==============================
        // SEARCH EMPLOYEE
        // ==============================
        [HttpPost("search")]
        public async Task<IActionResult> Search([FromBody] EmployeeSearchRequest request)
        {
            var result = await _service.SearchAsync(request);

            return Ok(result);
        }

        // ==============================
        // CREATE EMPLOYEE
        // ==============================
        [HttpPost("add-employee")]
        public async Task<IActionResult> Create([FromBody] EmployeeDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // CreateAsync returns "Username : xxx , Password : yyy" for
                // the just-created login - this is the ONLY point in the
                // system where that plaintext password exists (it's BCrypt
                // hashed before this call returns and never stored in the
                // clear). Surface it in the response so the admin can see
                // and hand it to the new employee once; it is never
                // retrievable again after this response - same "show once"
                // rule as UserController's ResetPassword.
                var credentials = await _service.CreateAsync(dto);

                // CreateAsync mutates dto.Id in place to the newly-generated
                // Employee Id (see EmployeeService.CreateAsync) - surface it
                // here so callers (e.g. the APP's Recruitment-to-Onboarding
                // bridge) can redirect straight to the new employee's record
                // without a second round trip.
                return Ok(new ApiResponse<EmployeeDto>
                {
                    Success = true,
                    Message = $"Employee created successfully. {credentials}",
                    Data = new EmployeeDto { Id = dto.Id }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        // ==============================
        // UPDATE EMPLOYEE
        // ==============================
        [HttpPut("update-employee")]
        public async Task<IActionResult> Update([FromBody] EmployeeDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _service.UpdateAsync(dto);

            return Ok(new
            {
                Success = result,
                Message = result
                    ? "Employee updated successfully"
                    : "Employee update failed"
            });
        }

        // ==============================
        // UPDATE PROFILE PHOTO ONLY
        // ==============================
        // Deliberately separate from the full Update endpoint - the "My
        // Profile" self-service page only ever needs to change FilePath,
        // and routing that through the generic update would mean trusting
        // a self-service client to submit (and not tamper with) every
        // other employee field too.
        [HttpPut("update-photo/{id}")]
        public async Task<IActionResult> UpdatePhoto(
            [FromRoute] string id,
            [FromBody] UpdateEmployeePhotoRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.FilePath))
                return BadRequest(new { Success = false, Message = "FilePath is required." });

            var result = await _service.UpdatePhotoAsync(id, request.FilePath);

            return Ok(new
            {
                Success = result,
                Message = result
                    ? "Profile photo updated successfully"
                    : "Profile photo update failed"
            });
        }

        // ==============================
        // GET ALL EMPLOYEES
        // ==============================
        [HttpGet("employee-list")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();

            return Ok(result);
        }

        // ==============================
        // GET EMPLOYEE BY ID
        // ==============================
        [HttpGet("get-employee-detail/{id}")]
        public async Task<IActionResult> Get([FromRoute] string id)
        {
            var result = await _service.GetByIdAsync(id);

            if (result == null)
            {
                return NotFound(new
                {
                    Success = false,
                    Message = "Employee not found"
                });
            }

            return Ok(result);
        }

        // ==============================
        // DELETE MULTIPLE EMPLOYEES
        // ==============================
        [HttpPost("delete")]
        public async Task<IActionResult> DeleteMultiple([FromBody] List<string> ids)
        {
            var result = await _service.DeleteMultipleAsync(ids);

            return Ok(new
            {
                Success = result,
                Message = result
                    ? "Employees deleted successfully"
                    : "Delete operation failed"
            });
        }

        // ==============================
        // GET EMPLOYEE HIERARCHY
        // ==============================
        [HttpGet("hierarchy")]
        public async Task<IActionResult> GetHierarchy()
        {
            var tenantId = User.FindFirst("TenantId")?.Value;

            var data = await _service.GetHierarchyAsync(tenantId);

            return Ok(data);
        }
    }
}
