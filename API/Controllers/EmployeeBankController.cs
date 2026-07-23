using Application.DTOs.Employee;
using Application.Interfaces.EmployeeInterface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    // NOTE: named "EmployeeBank" (not "EmployeeBankDetail") to match the
    // menu-seeding string in Domain/Helper/AppFeatureConstants.cs
    // (EMPLOYEE_BANK_CONTROLLER = "EmployeeBank").
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class EmployeeBankController : ControllerBase
    {
        private readonly IEmployeeBankDetailService _employeeBankDetailService;

        public EmployeeBankController(IEmployeeBankDetailService employeeBankDetailService)
        {
            _employeeBankDetailService = employeeBankDetailService;
        }

        // TenantId/UserId are read from the JWT claims exactly like
        // OnboardingController / EmployeeController.GetHierarchy - see
        // Application/Services/JWT Token/JwtService.cs for how these claims
        // are issued at login.
        private string? TenantId => User.FindFirst("TenantId")?.Value;
        private string? ActingUserId => User.FindFirst("UserId")?.Value;

        #region CRUD

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? search)
        {
            var result = await _employeeBankDetailService.GetAllAsync(TenantId, search);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _employeeBankDetailService.GetByIdAsync(id, TenantId);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpGet("employee/{employeeId}")]
        public async Task<IActionResult> GetByEmployee(string employeeId)
        {
            var result = await _employeeBankDetailService
                .GetByEmployeeIdAsync(employeeId, TenantId);

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] EmployeeBankDetailDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _employeeBankDetailService
                    .CreateAsync(dto, TenantId, ActingUserId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] EmployeeBankDetailDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _employeeBankDetailService
                    .UpdateAsync(id, dto, TenantId, ActingUserId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _employeeBankDetailService.DeleteAsync(id, TenantId);

            if (!result)
                return NotFound();

            return Ok(result);
        }

        #endregion

        #region Set Primary

        [HttpPut("{id}/set-primary")]
        public async Task<IActionResult> SetPrimary(string id)
        {
            try
            {
                var result = await _employeeBankDetailService
                    .SetPrimaryAsync(id, TenantId, ActingUserId);

                if (!result)
                    return NotFound();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        #endregion
    }
}
