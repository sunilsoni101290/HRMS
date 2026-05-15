using Application.DTOs.Attendances;
using Application.Interfaces.Attendances;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeShiftMappingController : ControllerBase
    {
        private readonly IEmployeeShiftMappingService _service;

        public EmployeeShiftMappingController(IEmployeeShiftMappingService service)
        {
            _service = service;
        }

        #region Get All

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();

            return Ok(result);
        }

        #endregion

        #region Get By Id

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _service.GetByIdAsync(id);

            if (result == null)
            {
                return NotFound(new
                {
                    Message = "Record not found"
                });
            }

            return Ok(result);
        }

        #endregion

        #region Create

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] EmployeeShiftMappingDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _service.CreateAsync(dto);

            return Ok(new
            {
                Message = "Employee shift mapping created successfully",
                Data = result
            });
        }

        #endregion

        #region Update

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id,
            [FromBody] EmployeeShiftMappingDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != dto.Id)
            {
                return BadRequest(new
                {
                    Message = "Invalid mapping id"
                });
            }

            var result = await _service.UpdateAsync(dto);

            return Ok(new
            {
                Message = "Employee shift mapping updated successfully",
                Data = result
            });
        }

        #endregion

        #region Delete

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _service.DeleteAsync(id);

            if (!result)
            {
                return NotFound(new
                {
                    Message = "Record not found"
                });
            }

            return Ok(new
            {
                Message = "Employee shift mapping deleted successfully"
            });
        }

        #endregion
    }
}
