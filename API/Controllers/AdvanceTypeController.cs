using Application.DTOs.LoanAdvance;
using Application.Interfaces.LoanAdvance;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>Master CRUD for Advance Types - AppFeatureConstants.ADVANCE_TYPE. See LoanTypeController for the no-try/catch convention.</summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AdvanceTypeController : ControllerBase
    {
        private readonly IAdvanceTypeService _service;

        public AdvanceTypeController(IAdvanceTypeService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string tenantId, string actingUserId, bool includeInactive = false)
        {
            var result = await _service.GetAllAsync(tenantId, actingUserId, includeInactive);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id, string tenantId, string actingUserId)
        {
            var result = await _service.GetByIdAsync(id, tenantId, actingUserId);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AdvanceTypeDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _service.CreateAsync(dto, dto.TenantId, dto.ActingUserId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] AdvanceTypeDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            dto.Id = id;
            var result = await _service.UpdateAsync(dto, dto.TenantId, dto.ActingUserId);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id,string tenantId, string actingUserId)
        {
            var result = await _service.DeleteAsync(id, tenantId, actingUserId);
            return Ok(new { success = result });
        }
    }
}
