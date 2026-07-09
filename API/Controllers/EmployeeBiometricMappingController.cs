using Application.Common.Responses;
using Application.DTOs.Attendances;
using Application.Interfaces.Attendances;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class EmployeeBiometricMappingController : ControllerBase
    {
        private readonly IEmployeeBiometricMappingService _service;

        public EmployeeBiometricMappingController(IEmployeeBiometricMappingService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            return Ok(await _service.GetAllAsync());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            var result = await _service.GetByIdAsync(id);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(EmployeeBiometricMappingDto dto)
        {
            try
            {
                var result = await _service.CreateAsync(dto);

                return Ok(new ApiResponse<EmployeeBiometricMappingDto>
                {
                    Success = true,
                    Message = "Employee mapped to biometric code successfully.",
                    Data = result
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

        [HttpPut]
        public async Task<IActionResult> Update(EmployeeBiometricMappingDto dto)
        {
            try
            {
                var success = await _service.UpdateAsync(dto);

                return Ok(new ApiResponse<object>
                {
                    Success = success,
                    Message = success
                        ? "Mapping updated successfully."
                        : "Mapping not found."
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

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            return Ok(await _service.DeleteAsync(id));
        }
    }
}
