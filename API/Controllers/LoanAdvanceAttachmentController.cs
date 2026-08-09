using Application.DTOs.LoanAdvance;
using Application.Interfaces.LoanAdvance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace API.Controllers
{
    /// <summary>
    /// Metadata CRUD for LoanAdvanceAttachment - NOT a file-upload
    /// endpoint. Per this codebase's existing convention (see
    /// APP/Controllers/EmployeeDocumentController.cs's SaveFileAsync),
    /// the APP/MVC layer saves the actual file to its own wwwroot/Uploads
    /// folder and calls this controller only with the resulting
    /// FileName/FilePath/ContentType/FileSizeBytes - the Web API layer
    /// never receives raw IFormFile bytes for this module.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LoanAdvanceAttachmentController : ControllerBase
    {
        private readonly ILoanAdvanceAttachmentService _service;

        public LoanAdvanceAttachmentController(ILoanAdvanceAttachmentService service)
        {
            _service = service;
        }

        private string? TenantId => User.FindFirst("TenantId")?.Value;
        private string? ActingUserId => User.FindFirst("UserId")?.Value;

        // GET api/loanadvanceattachment?entityType=1&entityId=LNR-0001
        [HttpGet]
        public async Task<IActionResult> GetForEntity([FromQuery] int entityType, [FromQuery] string entityId)
        {
            var result = await _service.GetForEntityAsync(entityType, entityId, TenantId, ActingUserId);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> AddMetadata([FromBody] LoanAdvanceAttachmentCreateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _service.AddMetadataAsync(
                request.EntityType, request.EntityId, request.FileName, request.FilePath,
                request.ContentType, request.FileSizeBytes, TenantId, ActingUserId);

            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _service.DeleteAsync(id, TenantId, ActingUserId);
            return Ok(new { success = result });
        }
    }

    /// <summary>Request body shape for AddMetadata - kept controller-local since it's a thin wrapper around the service's positional parameters, not a domain DTO.</summary>
    public class LoanAdvanceAttachmentCreateRequest
    {
        /// <summary>1=Loan, 2=Advance - see Domain.Enums.EnumExtensions.LoanAttachmentEntityType.</summary>
        [Range(1, 2, ErrorMessage = "Entity Type must be 1 (Loan) or 2 (Advance).")]
        public int EntityType { get; set; }

        [Required(ErrorMessage = "EntityId is required.")]
        public string EntityId { get; set; } = string.Empty;

        [Required(ErrorMessage = "File Name is required.")]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required(ErrorMessage = "File Path is required.")]
        [StringLength(500)]
        public string FilePath { get; set; } = string.Empty;

        [Required(ErrorMessage = "Content Type is required.")]
        [StringLength(100)]
        public string ContentType { get; set; } = string.Empty;

        [Range(1, 26214400, ErrorMessage = "File size must be between 1 byte and 25 MB.")]
        public long FileSizeBytes { get; set; }
    }
}
