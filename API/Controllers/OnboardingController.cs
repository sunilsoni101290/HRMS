using Application.DTOs.Onboarding;
using Application.Interfaces.Onboarding;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static Domain.Enums.EnumExtensions;

namespace API.Controllers
{
    [ApiController]
    [Route("api/onboarding")]
    [Authorize]
    public class OnboardingController : ControllerBase
    {
        private readonly IOnboardingService _service;

        public OnboardingController(IOnboardingService service)
        {
            _service = service;
        }

        // TenantId/UserId are read from the JWT claims exactly like
        // EmployeeController.GetHierarchy / AuthController - see
        // Application/Services/JWT Token/JwtService.cs for how these claims
        // are issued at login.
        private string? TenantId => User.FindFirst("TenantId")?.Value;
        private string? ActingUserId => User.FindFirst("UserId")?.Value;

        #region Cases

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOnboardingCaseDto dto)
        {
            try
            {
                var result = await _service.CreateCaseAsync(dto, TenantId, ActingUserId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] string? departmentId, [FromQuery] string? search)
        {
            var data = await _service.GetAllAsync(TenantId, status, departmentId, search);
            return Ok(data);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var data = await _service.GetByIdAsync(id, TenantId);

            if (data == null)
                return NotFound();

            return Ok(data);
        }

        [HttpGet("employee/{employeeId}")]
        public async Task<IActionResult> GetByEmployee(string employeeId)
        {
            var data = await _service.GetByEmployeeIdAsync(employeeId, TenantId);

            if (data == null)
                return NotFound();

            return Ok(data);
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateOnboardingCaseStatusDto dto)
        {
            try
            {
                var result = await _service.UpdateCaseStatusAsync(id, dto.Status, dto.Remarks, TenantId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        #endregion

        #region Checklist Items

        [HttpPut("checklist-item/{itemId}/status")]
        public async Task<IActionResult> UpdateChecklistItemStatus(string itemId, [FromBody] UpdateChecklistItemStatusDto dto)
        {
            try
            {
                var result = await _service.UpdateChecklistItemStatusAsync(itemId, dto, ActingUserId, TenantId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("{id}/checklist-item")]
        public async Task<IActionResult> AddChecklistItem(string id, [FromBody] AddChecklistItemDto dto)
        {
            try
            {
                var result = await _service.AddChecklistItemAsync(id, dto, TenantId, ActingUserId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpDelete("checklist-item/{itemId}")]
        public async Task<IActionResult> DeleteChecklistItem(string itemId)
        {
            var result = await _service.DeleteChecklistItemAsync(itemId, TenantId, ActingUserId);

            if (!result)
                return NotFound();

            return Ok(new { Message = "Checklist item deleted successfully" });
        }

        #endregion

        #region Templates

        [HttpGet("templates")]
        public async Task<IActionResult> GetTemplates([FromQuery] OnboardingStageType? stage)
        {
            var data = await _service.GetTemplateItemsAsync(TenantId, stage);
            return Ok(data);
        }

        [HttpPost("templates")]
        public async Task<IActionResult> CreateTemplate([FromBody] UpsertOnboardingTemplateItemDto dto)
        {
            try
            {
                var result = await _service.UpsertTemplateItemAsync(dto, null, TenantId, ActingUserId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("templates/{id}")]
        public async Task<IActionResult> UpdateTemplate(string id, [FromBody] UpsertOnboardingTemplateItemDto dto)
        {
            try
            {
                var result = await _service.UpsertTemplateItemAsync(dto, id, TenantId, ActingUserId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpDelete("templates/{id}")]
        public async Task<IActionResult> DeleteTemplate(string id)
        {
            var result = await _service.DeleteTemplateItemAsync(id, TenantId);

            if (!result)
                return NotFound();

            return Ok(new { Message = "Template item deleted successfully" });
        }

        #endregion
    }
}
