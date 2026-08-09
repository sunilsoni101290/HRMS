using Application.DTOs.LoanAdvance;
using Application.Interfaces.LoanAdvance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>
    /// CRUD + versioning for Loan Policies (and their nested approval
    /// matrix) - AppFeatureConstants.LOAN_POLICY. See LoanTypeController
    /// for the no-try/catch convention. Update() intentionally does NOT
    /// PUT in place - it returns a NEW policy row (next VersionNumber);
    /// callers should treat the response as a distinct resource, not an
    /// in-place edit of {id}.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LoanPolicyController : ControllerBase
    {
        private readonly ILoanPolicyService _service;

        public LoanPolicyController(ILoanPolicyService service)
        {
            _service = service;
        }

        // GET api/loanpolicy?loanTypeId=&companyId=
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? loanTypeId, [FromQuery] string? companyId, string tenantId, string actingUserId)
        {
            var result = await _service.GetAllAsync(tenantId, actingUserId, loanTypeId, companyId);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id, string tenantId, string actingUserId)
        {
            var result = await _service.GetByIdAsync(id, tenantId, actingUserId);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] LoanPolicyDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _service.CreateAsync(dto, dto.TenantId, dto.ActingUserId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        // PUT api/loanpolicy/{id} - closes {id}'s version and creates the next one; see class remarks.
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] LoanPolicyDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            dto.Id = id;
            var result = await _service.UpdateAsync(dto, dto.TenantId, dto.ActingUserId);
            return Ok(result);
        }

        [HttpPost("{id}/deactivate")]
        public async Task<IActionResult> Deactivate(string id, string tenantId, string actingUserId)
        {
            var result = await _service.DeactivateAsync(id, tenantId, actingUserId);
            return Ok(new { success = result });
        }
    }
}
