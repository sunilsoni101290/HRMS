using Application.DTOs.Employee;
using Application.DTOs.Masters;
using Application.Interfaces.Masters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    #region Country API

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CountryController : ControllerBase
    {
        private readonly ICountryService _countryService;

        public CountryController(ICountryService countryService)
        {
            _countryService = countryService;
        }

        // ======================================================
        // GET ALL
        // ======================================================

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _countryService.GetAllAsync();

            return Ok(data);
        }

        // ======================================================
        // GET BY ID
        // ======================================================

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var data = await _countryService.GetByIdAsync(id);

            if (data == null)
                return NotFound();

            return Ok(data);
        }

        // ======================================================
        // CREATE
        // ======================================================

        [HttpPost]
        public async Task<IActionResult> Create(CountryDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _countryService.CreateAsync(dto);

            return Ok(result);
        }

        // ======================================================
        // UPDATE
        // ======================================================

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, CountryDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _countryService.UpdateAsync(id, dto);

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
            var result = await _countryService.DeleteAsync(id);

            if (!result)
                return NotFound();

            return Ok(new
            {
                Message = "Country deleted successfully"
            });
        }
    }

    #endregion
}
