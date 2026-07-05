using Application.DTOs.Employee;
using Application.Interfaces.EmployeeInterface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static Domain.Enums.EnumExtensions;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class EmployeeDocumentController : ControllerBase
    {
        private readonly IEmployeeDocumentService _employeeDocumentService;

        public EmployeeDocumentController(IEmployeeDocumentService employeeDocumentService)
        {
            _employeeDocumentService = employeeDocumentService;
        }

        #region CRUD

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _employeeDocumentService.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _employeeDocumentService.GetByIdAsync(id);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpGet("employee/{employeeId}")]
        public async Task<IActionResult> GetByEmployee(string employeeId)
        {
            var result = await _employeeDocumentService
                .GetByEmployeeAsync(employeeId);

            return Ok(result);
        }

        [HttpGet("document-type/{documentType}")]
        public async Task<IActionResult> GetByDocumentType(
            EmployeeDocumentType documentType)
        {
            var result = await _employeeDocumentService
                .GetByDocumentTypeAsync(documentType);

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] EmployeeDocumentDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result =
                await _employeeDocumentService.CreateAsync(dto);

            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(
            string id,
            [FromBody] EmployeeDocumentDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result =
                await _employeeDocumentService.UpdateAsync(id, dto);

            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var result =
                await _employeeDocumentService.DeleteAsync(id);

            if (!result)
                return NotFound();

            return Ok(result);
        }

        #endregion

        #region Verification

        [HttpPost("{id}/verify")]
        public async Task<IActionResult> Verify(string id,[FromQuery] string verifiedBy,[FromQuery] string? remarks)
        {
            var result =
                await _employeeDocumentService.VerifyDocumentAsync(
                    id,
                    verifiedBy,
                    remarks);

            return Ok(result);
        }

        [HttpPost("{id}/unverify")]
        public async Task<IActionResult> UnVerify(string id)
        {
            var result =
                await _employeeDocumentService.UnVerifyDocumentAsync(id);

            return Ok(result);
        }

        #endregion

        #region Expiry

        [HttpGet("expired")]
        public async Task<IActionResult> GetExpiredDocuments()
        {
            var result =
                await _employeeDocumentService.GetExpiredDocumentsAsync();

            return Ok(result);
        }

        [HttpGet("expiring/{days}")]
        public async Task<IActionResult> GetExpiringDocuments(int days)
        {
            var result =
                await _employeeDocumentService
                    .GetExpiringDocumentsAsync(days);

            return Ok(result);
        }

        #endregion

        #region Validation

        [HttpGet("exists")]
        public async Task<IActionResult> DocumentExists(
            string employeeId,
            EmployeeDocumentType documentType)
        {
            var result =
                await _employeeDocumentService
                    .DocumentExistsAsync(
                        employeeId,
                        documentType);

            return Ok(result);
        }

        #endregion

    }
}
