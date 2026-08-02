using Application.DTOs.Taxation;
using Application.Interfaces.Taxation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    // Employee self-service annual investment declaration - see
    // Domain/Entities/TaxDeclaration.cs / TaxDeclarationService.
    // TenantId/ActingUserId are resolved from JWT claims exactly like
    // ProbationConfirmationController.
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TaxDeclarationController : ControllerBase
    {
        private readonly ITaxDeclarationService _taxDeclarationService;

        public TaxDeclarationController(ITaxDeclarationService taxDeclarationService)
        {
            _taxDeclarationService = taxDeclarationService;
        }

        private string? TenantId => User.FindFirst("TenantId")?.Value;
        private string? ActingUserId => User.FindFirst("UserId")?.Value;

        // GET api/taxdeclaration/my?financialYearId=
        [HttpGet("my")]
        public async Task<IActionResult> GetMy([FromQuery] string financialYearId)
        {
            var result = await _taxDeclarationService.GetMyDeclarationAsync(financialYearId, TenantId, ActingUserId);
            return Ok(result);
        }

        // POST api/taxdeclaration - create/update the Draft (upsert).
        [HttpPost]
        public async Task<IActionResult> CreateOrUpdate([FromBody] CreateTaxDeclarationDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _taxDeclarationService.CreateOrUpdateAsync(dto, TenantId, ActingUserId);
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

        // PUT api/taxdeclaration/{id}/submit
        [HttpPut("{id}/submit")]
        public async Task<IActionResult> Submit(string id)
        {
            try
            {
                var result = await _taxDeclarationService.SubmitAsync(id, TenantId, ActingUserId);
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

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                var result = await _taxDeclarationService.GetByIdAsync(id, TenantId, ActingUserId);
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

        // GET api/taxdeclaration?financialYearId=&status=&departmentId=&search= - HR view.
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? financialYearId, [FromQuery] string? status, [FromQuery] string? departmentId, [FromQuery] string? search)
        {
            try
            {
                var result = await _taxDeclarationService.GetAllAsync(TenantId, financialYearId, status, departmentId, search, ActingUserId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Message = ex.Message });
            }
        }

        // PUT api/taxdeclaration/{id}/verify - HR action.
        [HttpPut("{id}/verify")]
        public async Task<IActionResult> Verify(string id, [FromBody] TaxDeclarationVerifyActionDto dto)
        {
            try
            {
                var result = await _taxDeclarationService.VerifyAsync(id, dto, TenantId, ActingUserId);
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

        // PUT api/taxdeclaration/{id}/reject - HR action.
        [HttpPut("{id}/reject")]
        public async Task<IActionResult> Reject(string id, [FromBody] TaxDeclarationVerifyActionDto dto)
        {
            try
            {
                var result = await _taxDeclarationService.RejectAsync(id, dto, TenantId, ActingUserId);
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
    }
}
