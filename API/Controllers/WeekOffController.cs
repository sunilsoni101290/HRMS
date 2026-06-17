using Application.DTOs.Masters;
using Application.Interfaces.Masters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WeekOffController : ControllerBase
    {
        private readonly IWeekOffService _weekOffService;

        public WeekOffController(IWeekOffService weekOffService)
        {
            _weekOffService = weekOffService;
        }

        // ======================================================
        // GET ALL
        // ======================================================

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _weekOffService.GetAllAsync();

            return Ok(data);
        }

        // ======================================================
        // GET BY ID
        // ======================================================

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var data = await _weekOffService.GetByIdAsync(id);

            if (data == null)
                return NotFound();

            return Ok(data);
        }

        // ======================================================
        // CREATE
        // ======================================================

        [HttpPost]
        public async Task<IActionResult> Create(WeekOffDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _weekOffService.CreateAsync(dto);

            return Ok(result);
        }

        // ======================================================
        // UPDATE
        // ======================================================

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, WeekOffDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _weekOffService.UpdateAsync(id, dto);

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
            var result = await _weekOffService.DeleteAsync(id);

            if (!result)
                return NotFound();

            return Ok(new
            {
                Message = "Week Off deleted successfully"
            });
        }
    }
}
