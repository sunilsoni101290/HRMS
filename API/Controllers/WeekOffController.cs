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
        // GET WEEK OFF DATES FOR MONTH (new - the computational core of
        // the "Nth weekday of month" pattern, e.g. "2nd and 4th Saturday").
        // Resolves BOTH patterns (EveryWeek + NthWeekdayOfMonth) for the
        // tenant and returns the concrete calendar dates that are a
        // week-off in that month.
        // ======================================================

        [HttpGet("dates")]
        public async Task<IActionResult> GetWeekOffDates([FromQuery] int year, [FromQuery] int month, [FromQuery] string tenantId)
        {
            if (year < 1 || month < 1 || month > 12 || string.IsNullOrWhiteSpace(tenantId))
                return BadRequest("year, month and tenantId are required (month must be 1-12).");

            var dates = await _weekOffService.GetWeekOffDatesForMonth(year, month, tenantId);

            return Ok(dates);
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
