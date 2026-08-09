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


        // POST api/taxcomputation/compute?employeeId=&financialYearId= - runs/re-runs the calculation.
        [HttpPost("compute")]
        public async Task<IActionResult> Compute([FromQuery] string employeeId, [FromQuery] string financialYearId, string tenantId, string actingUserId)
        {
            try
            {
                var result = await _taxComputationService.ComputeAsync(employeeId, financialYearId, tenantId, actingUserId);
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
        public async Task<IActionResult> Get([FromQuery] string employeeId, [FromQuery] string financialYearId, string tenantId, string actingUserId)
        {
            try
            {
                var result = await _taxComputationService.GetAsync(employeeId, financialYearId, tenantId, actingUserId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Message = ex.Message });
            }
        }

        // GET api/taxcomputation/all?financialYearId=&departmentId=&search=
        [HttpGet("all")]
        public async Task<IActionResult> GetAll([FromQuery] string financialYearId, [FromQuery] string? departmentId, 
            [FromQuery] string? search, string tenantId, string actingUserId)
        {
            try
            {
                var result = await _taxComputationService.GetAllAsync(tenantId, financialYearId, departmentId, search, actingUserId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Message = ex.Message });
            }
        }
    }
}
