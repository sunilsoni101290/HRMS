using Application.Common.Responses;
using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // 🔐 Secure all endpoints
    public class EmployeeController : ControllerBase
    {
        private readonly IEmployeeService _employeeService; 
        public EmployeeController(IEmployeeService employeeService) 
        {
            _employeeService = employeeService;
        }

        // ✅ CREATE EMPLOYEE
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] EmployeeDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (!string.IsNullOrEmpty(dto.Id))
                    return BadRequest("Id should not be provided for create");

                var result = await _employeeService.CreateAsync(dto);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Employee created successfully",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        // ✅ GET ALL
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _employeeService.GetAllAsync();

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = data,
                Message = "Employees fetched successfully"
            });
        }

        // ✅ GET BY ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var data = await _employeeService.GetByIdAsync(id);

            if (data == null)
                return NotFound(new ApiResponse<string>
                {
                    Success = false,
                    Message = "Employee not found"
                });

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = data
            });
        }


        // ✅ UPDATE
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] EmployeeDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (string.IsNullOrEmpty(dto.Id) || dto.Id != id)
                    return BadRequest("Invalid employee Id");

                var result = await _employeeService.UpdateAsync(dto);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Employee updated successfully",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        // ✅ DELETE (SOFT DELETE)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _employeeService.DeleteAsync(id);

                return Ok(new ApiResponse<string>
                {
                    Success = true,
                    Message = "Employee deleted successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }
    }
}
