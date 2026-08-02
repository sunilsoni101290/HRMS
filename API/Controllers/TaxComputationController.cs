using Application.Interfaces.Taxation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    // HR/Payroll surface over the Income Tax / TDS calculation engine -
    // see Domain/Entities/EmployeeTaxComputation.cs / TaxComputationService.
    // TenantId/ActingUserId are resolved from JWT claims exactly like
    // ProbationConfirmationController.
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TaxComputationController : ControllerBase
    {
        private readonly ITaxComputationService _taxComputationService;

        public TaxComputationController(ITaxComputationService taxComputationService)
        {
            _taxComputationService = taxComputationService;
        }

        private string? TenantId => User.FindFirst("TenantId")?.Value;
        private string? ActingUserId => User.FindFirst("UserId")?.Value;

        // POST api/taxcomputation/compute?employeeId=&financialYearId= - runs/re-runs the calculation.
        [HttpPost("compute")]
        public async Task<IActionResult> Compute([FromQuery] string employeeId, [FromQuery] string financialYearId)
        {
            try
            {
                var result = await _taxComputationService.ComputeAsync(employeeId, financialYearId, TenantId, ActingUserId);
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

        // GET api/taxcomputation?employeeId=&financialYearId=
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string employeeId, [FromQuery] string financialYearId)
        {
            try
            {
                var result = await _taxComputationService.GetAsync(employeeId, financialYearId, TenantId, ActingUserId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Message = ex.Message });
            }
        }

        // GET api/taxcomputation/all?financialYearId=&departmentId=&search=
        [HttpGet("all")]
        public async Task<IActionResult> GetAll([FromQuery] string financialYearId, [FromQuery] string? departmentId, [FromQuery] string? search)
        {
            try
            {
                var result = await _taxComputationService.GetAllAsync(TenantId, financialYearId, departmentId, search, ActingUserId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Message = ex.Message });
            }
        }
    }
}
