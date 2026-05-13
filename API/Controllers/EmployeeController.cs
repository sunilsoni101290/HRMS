using Application.Common.Responses;
using Application.DTOs.Employee;
using Application.Interfaces.Employee;
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
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var id = await _service.CreateAsync(dto);

            return Ok(new
            {
                Success = true,
                Message = "Employee created successfully",
                Id = id
            });
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
