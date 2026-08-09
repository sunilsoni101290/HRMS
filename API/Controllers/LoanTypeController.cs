using Application.DTOs.LoanAdvance;
using Application.Interfaces.LoanAdvance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>
    /// Master CRUD for Loan Types - AppFeatureConstants.LOAN_TYPE.
    /// No per-action try/catch: Application.Common.Exceptions thrown by
    /// the service (NotFoundException/BadRequestException/
    /// UnauthorizedException) are mapped to their proper HTTP status
    /// codes by API/Middleware/ExceptionMiddleware.cs (Phase 6), so
    /// controllers here stay a thin pass-through - single responsibility,
    /// no duplicated status-code logic per action.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LoanTypeController : ControllerBase
    {
        private readonly ILoanTypeService _service;

        public LoanTypeController(ILoanTypeService service)
        {
            _service = service;
        }

        // Read from the JWT claims only - see ProbationConfirmationController's
        // note on why TenantId/ActingUserId must never be bindable action
        // parameters.
        // GET api/loantype?includeInactive=
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string tenantId, string actingUserId, bool includeInactive = false)
        {
            var result = await _service.GetAllAsync(tenantId, actingUserId, includeInactive);
            return Ok(result);
        }

        // GET api/loantype/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id, string tenantId, string actingUserId)
        {
            var result = await _service.GetByIdAsync(id, tenantId, actingUserId);
            return Ok(result);
        }

        // POST api/loantype
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] LoanTypeDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _service.CreateAsync(dto, dto.TenantId, dto.ActingUserId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        // PUT api/loantype/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] LoanTypeDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            dto.Id = id;
            var result = await _service.UpdateAsync(dto, dto.TenantId, dto.ActingUserId);
            return Ok(result);
        }

        // DELETE api/loantype/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id, string tenantId, string actingUserId)
        {
            var result = await _service.DeleteAsync(id, tenantId, actingUserId);
            return Ok(new { success = result });
        }
    }
}
