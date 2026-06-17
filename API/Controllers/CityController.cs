using Application.DTOs.Employee;
using Application.DTOs.Masters;
using Application.Interfaces.EmployeeInterface;
using Application.Interfaces.Masters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    #region City API

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CityController : ControllerBase
    {
        private readonly ICityService _cityService;

        public CityController(ICityService cityService)
        {
            _cityService = cityService;
        }

        // ======================================================
        // GET ALL
        // ======================================================

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _cityService.GetAllAsync();

            return Ok(data);
        }

        // ======================================================
        // GET BY ID
        // ======================================================

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var data = await _cityService.GetByIdAsync(id);

            if (data == null)
                return NotFound();

            return Ok(data);
        }

        // ======================================================
        // CREATE
        // ======================================================

        [HttpPost]
        public async Task<IActionResult> Create(CityDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _cityService.CreateAsync(dto);

            return Ok(result);
        }

        // ======================================================
        // UPDATE
        // ======================================================

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, CityDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _cityService.UpdateAsync(id, dto);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        // ======================================================
        // DELETE
        // ======================================================

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _cityService.DeleteAsync(id);

            if (!result)
                return NotFound();

            return Ok(new
            {
                Message = "City deleted successfully"
            });
        }
    }

    #endregion
}
