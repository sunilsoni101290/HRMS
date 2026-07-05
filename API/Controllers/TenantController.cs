using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TenantController : ControllerBase
    {
        private readonly ITenantBusinessService _tenantService;

        public TenantController(ITenantBusinessService tenantService)
        {
            _tenantService = tenantService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _tenantService.GetAllAsync());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            return Ok(await _tenantService.GetByIdAsync(id));
        }

        [HttpPost]
        public async Task<IActionResult> Create(TenantDto dto)
        {
            return Ok(await _tenantService.CreateAsync(dto));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(
            string id,
            TenantDto dto)
        {
            return Ok(await _tenantService.UpdateAsync(id, dto));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            return Ok(await _tenantService.DeleteAsync(id));
        }

        [HttpPost("{id}/activate")]
        public async Task<IActionResult> Activate(string id)
        {
            return Ok(await _tenantService.ActivateAsync(id));
        }

        [HttpPost("{id}/deactivate")]
        public async Task<IActionResult> Deactivate(string id)
        {
            return Ok(await _tenantService.DeactivateAsync(id));
        }

        [HttpGet("code/{code}")]
        public async Task<IActionResult> GetByCode(string code)
        {
            return Ok(await _tenantService.GetByCodeAsync(code));
        }

        [HttpGet("domain/{domain}")]
        public async Task<IActionResult> GetByDomain(string domain)
        {
            return Ok(await _tenantService.GetByDomainAsync(domain));
        }
    }
}
