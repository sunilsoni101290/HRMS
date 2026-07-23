using Application.DTOs.Attendances;
using Application.Interfaces.Attendances;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AttendancePolicyController : ControllerBase
    {
        private readonly IAttendancePolicyService _attendancePolicyService;

        public AttendancePolicyController(IAttendancePolicyService attendancePolicyService)
        {
            _attendancePolicyService = attendancePolicyService;
        }

        // TenantId/UserId are read from the JWT claims exactly like
        // EmployeeBankController / OnboardingController - see
        // Application/Services/JWT Token/JwtService.cs for how these claims
        // are issued at login.
        private string? TenantId => User.FindFirst("TenantId")?.Value;
        private string? ActingUserId => User.FindFirst("UserId")?.Value;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _attendancePolicyService.GetAllAsync(TenantId);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _attendancePolicyService.GetByIdAsync(id, TenantId);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        // GET api/attendancepolicy/active?companyId=
        [HttpGet("active")]
        public async Task<IActionResult> GetActive([FromQuery] string? companyId)
        {
            var result = await _attendancePolicyService.GetActiveForTenantAsync(TenantId, companyId);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AttendancePolicyDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _attendancePolicyService.CreateAsync(dto, TenantId, ActingUserId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] AttendancePolicyDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _attendancePolicyService.UpdateAsync(id, dto, TenantId, ActingUserId);
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
            var result = await _attendancePolicyService.DeleteAsync(id, TenantId);

            if (!result)
                return NotFound();

            return Ok(result);
        }
    }
}
