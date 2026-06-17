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
    public class FinancialYearController : ControllerBase
    {
        private readonly IFinancialYearService _financialYearService;

        public FinancialYearController(IFinancialYearService financialYearService)
        {
            _financialYearService = financialYearService;
        }

        // ======================================================
        // GET ALL
        // ======================================================

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _financialYearService.GetAllAsync();

            return Ok(data);
        }

        // ======================================================
        // GET CURRENT FY
        // ======================================================

        [HttpGet("current")]
        public async Task<IActionResult> GetCurrent()
        {
            var data = await _financialYearService.GetCurrentFinancialYearAsync();

            if (data == null)
                return NotFound();

            return Ok(data);
        }

        // ======================================================
        // GET BY ID
        // ======================================================

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var data = await _financialYearService.GetByIdAsync(id);

            if (data == null)
                return NotFound();

            return Ok(data);
        }

        // ======================================================
        // CREATE
        // ======================================================

        [HttpPost]
        public async Task<IActionResult> Create(FinancialYearDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _financialYearService.CreateAsync(dto);

            return Ok(result);
        }

        // ======================================================
        // UPDATE
        // ======================================================

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, FinancialYearDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _financialYearService.UpdateAsync(id, dto);

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
            var result = await _financialYearService.DeleteAsync(id);

            if (!result)
                return NotFound();

            return Ok(new
            {
                Message = "Financial Year deleted successfully"
            });
        }
    }
}
