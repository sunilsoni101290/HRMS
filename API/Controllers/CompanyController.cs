using Application.DTOs.Company;
using Application.DTOs.Employee;
using Application.Interfaces.Company;
using Application.Interfaces.EmployeeInterface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    #region Company API

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CompanyController : ControllerBase
    {
        private readonly ICompanyService _companyService;

        public CompanyController(ICompanyService companyService)
        {
            _companyService = companyService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _companyService.GetAllAsync());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var data = await _companyService.GetByIdAsync(id);

            if (data == null)
                return NotFound();

            return Ok(data);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CompanyDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return Ok(await _companyService.CreateAsync(dto));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, CompanyDto dto)
        {
            var data = await _companyService.UpdateAsync(id, dto);

            if (data == null)
                return NotFound();

            return Ok(data);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _companyService.DeleteAsync(id);

            if (!result)
                return NotFound();

            return Ok();
        }
    }

    #endregion
}
