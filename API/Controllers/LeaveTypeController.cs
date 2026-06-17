using Application.DTOs.Attendances;
using Application.DTOs.Auth;
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
    public class LeaveTypeController : ControllerBase
    {
        private readonly ILeaveTypeService _leaveTypeService;

        public LeaveTypeController(ILeaveTypeService leaveTypeService)
        {
            _leaveTypeService = leaveTypeService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
        
            var result = await _leaveTypeService.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var data = await _leaveTypeService.GetByIdAsync(id);

            if (data == null)
                return NotFound();

            return Ok(data);
        }

        [HttpPost]
        public async Task<IActionResult> Create(LeaveTypeDto dto)
        {
            try
            {
                var result = await _leaveTypeService.CreateAsync(dto);

                return Ok(new ApiResponse<LeaveTypeDto>
                {
                    Success = true,
                    Message = "Leave Type created successfully.",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Failed to create leave type."
                });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, LeaveTypeDto dto)
        {       
           try
            {
                var result = await _leaveTypeService.UpdateAsync(id, dto);

                return Ok(new ApiResponse<LeaveTypeDto>
                {
                    Success = true,
                    Message = "Leave Type updated successfully.",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Failed to update leave type."
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _leaveTypeService.DeleteAsync(id);

            if (!result)
                return NotFound();

            return Ok(new
            {
                Message = "Leave Type deleted successfully."
            });
        }
    }
}
