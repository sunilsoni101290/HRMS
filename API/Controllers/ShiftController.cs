using Application.DTOs.Attendances;
using Application.Interfaces.Attendances;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    [Route("api/shift")]
    [ApiController]
    public class ShiftController : ControllerBase
    {
        private readonly IShiftService _shiftService;

        public ShiftController(IShiftService shiftService)
        {
            _shiftService = shiftService;
        }

        #region Get All

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _shiftService.GetAllAsync();

            return Ok(result);
        }

        #endregion

        #region Get By Id

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _shiftService.GetByIdAsync(id);

            if (result == null)
                return NotFound(new
                {
                    Message = "Shift not found"
                });

            return Ok(result);
        }

        #endregion

        #region Create

        [HttpPost("add-shift")]
        public async Task<IActionResult> Create([FromBody] ShiftDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _shiftService.CreateAsync(dto);

            return Ok(new
            {
                Message = "Shift created successfully",
                Data = result
            });
        }

        #endregion

        #region Update

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] ShiftDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != dto.Id)
            {
                return BadRequest(new
                {
                    Message = "Invalid shift id"
                });
            }

            var result = await _shiftService.UpdateAsync(dto);

            return Ok(new
            {
                Message = "Shift updated successfully",
                Data = result
            });
        }

        #endregion

        #region Delete

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _shiftService.DeleteAsync(id);

            if (!result)
            {
                return NotFound(new
                {
                    Message = "Shift not found"
                });
            }

            return Ok(new
            {
                Message = "Shift deleted successfully"
            });
        }

        #endregion
    }
}
