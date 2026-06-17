using Application.Common.Responses;
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
    public class BiometricDeviceController
    : ControllerBase
    {
        private readonly
            IBiometricDeviceService _service;

        public BiometricDeviceController(IBiometricDeviceService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            return Ok(await _service.GetAllAsync());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult>Get(string id)
        {
            return Ok(
                await _service.GetByIdAsync(id));
        }

        [HttpPost]
        public async Task<IActionResult> Create(BiometricDeviceDto dto)
        {
            try
            {
                var result = await _service.CreateAsync(dto);

                return Ok(new ApiResponse<BiometricDeviceDto>
                {
                    Success = true,
                    Message = "Biometric device created successfully.",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Failed to create biometric device."
                });
            }
        }

        [HttpPut]
        public async Task<IActionResult>Update(BiometricDeviceDto dto)
        {
            return Ok(
                await _service.UpdateAsync(dto));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult>Delete(string id)
        {
            return Ok(
                await _service.DeleteAsync(id));
        }

        [HttpGet("test/{id}")]
        public async Task<IActionResult>Test(string id)
        {
            return Ok(
                await _service
                .TestConnectionAsync(id));
        }
    }
}
