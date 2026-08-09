using Application.DTOs.Taxation;
using Application.Interfaces.Taxation;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    // Admin-only master-data CRUD for income-tax slabs - see
    // Domain/Entities/TaxSlab.cs / TaxSlabService. TenantId/ActingUserId
    // are resolved from JWT claims exactly like ProbationConfirmationController
    // (never accepted as bindable parameters - see that controller's
    // header comment for why).
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TaxSlabController : ControllerBase
    {
        private readonly ITaxSlabService _taxSlabService;

        public TaxSlabController(ITaxSlabService taxSlabService)
        {
            _taxSlabService = taxSlabService;
        }

        // GET api/taxslab?financialYearId=&regime=
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? financialYearId, [FromQuery] int? regime,string tenantId,string actingUserId)
        {
            try
            {
                var result = await _taxSlabService.GetAllAsync(tenantId, financialYearId, regime, actingUserId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id, string tenantId, string actingUserId)
        {
            try
            {
                var result = await _taxSlabService.GetByIdAsync(id, tenantId, actingUserId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TaxSlabDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _taxSlabService.CreateAsync(dto, dto.TenantId, dto.ActingUserId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] TaxSlabDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _taxSlabService.UpdateAsync(id, dto, dto.TenantId, dto.ActingUserId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(
        string id,
        string tenantId,
        string actingUserId)
        {
            try
            {
                var result = await _taxSlabService.DeleteAsync(
                    id,
                    tenantId,
                    actingUserId);

                return Ok(new { Success = result });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
