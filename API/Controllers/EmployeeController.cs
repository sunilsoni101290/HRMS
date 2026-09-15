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
        private readonly IEmployeeImportExportService _importExportService;

        public EmployeeController(IEmployeeService service, IEmployeeImportExportService importExportService)
        {
            _service = service;
            _importExportService = importExportService;
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
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // UpdateAsync now throws (rather than swallowing the reason
                // and returning false) for "not found" and "duplicate
                // Employee Code" - see EmployeeService.UpdateAsync - so a
                // false return here is no longer expected, but is still
                // handled defensively rather than assumed unreachable.
                var result = await _service.UpdateAsync(dto);

                return Ok(new ApiResponse<object>
                {
                    Success = result,
                    Message = result
                        ? "Employee updated successfully"
                        : "Employee update failed"
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
        // CHECK EMPLOYEE CODE (duplicate check)
        // ==============================
        // Backs the Create/Edit form's blur-triggered AJAX validation.
        // employeeId is the employee currently being edited (omit/blank on
        // Create) so an employee's own unchanged code is never flagged.
        [HttpGet("check-employee-code")]
        public async Task<IActionResult> CheckEmployeeCode(
            [FromQuery] string employeeCode,
            [FromQuery] string? employeeId)
        {
            if (string.IsNullOrWhiteSpace(employeeCode))
                return Ok(new ApiResponse<object> { Success = true, Data = new { exists = false } });

            var exists = await _service.CheckEmployeeCodeExistsAsync(employeeCode, employeeId);

            return Ok(new ApiResponse<object> { Success = true, Data = new { exists } });
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

        // ==============================
        // BULK IMPORT - VALIDATE (preview only, nothing written)
        // ==============================
        // rows are the raw, unresolved cell values the APP layer parsed out
        // of the uploaded .xlsx with ClosedXML - all validation (required
        // fields, formats, master-data lookups, duplicate checks) happens
        // here, against the live database, so Preview and Commit can never
        // disagree about what is/isn't valid. See EmployeeImportExportService.
        [HttpPost("import/validate")]
        public async Task<IActionResult> ValidateImport([FromBody] List<EmployeeImportRowInputDto> rows)
        {
            var tenantId = User.FindFirst("TenantId")?.Value ?? "";
            var userId = User.FindFirst("UserId")?.Value ?? "";
            var userName = User.Identity?.Name ?? "";

            var result = await _importExportService.ValidateImportAsync(rows ?? new(), tenantId, userId, userName);

            return Ok(result);
        }

        // ==============================
        // BULK IMPORT - COMMIT (validate again, then all-or-nothing insert)
        // ==============================
        [HttpPost("import/commit")]
        public async Task<IActionResult> CommitImport([FromBody] List<EmployeeImportRowInputDto> rows)
        {
            var tenantId = User.FindFirst("TenantId")?.Value ?? "";
            var userId = User.FindFirst("UserId")?.Value ?? "";
            var userName = User.Identity?.Name ?? "";

            var result = await _importExportService.CommitImportAsync(rows ?? new(), tenantId, userId, userName);

            return Ok(result);
        }
    }
}
