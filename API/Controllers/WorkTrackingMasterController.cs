using Application.Common.Responses;
using Application.DTOs.WorkTracking;
using Application.Interfaces.WorkTracking;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>
    /// Master/lookup data for the Daily Work Entry module - Clients,
    /// WorkJobs, JobTypes, JobItems, WorkActivities, WorkEntryReasons,
    /// DocumentStatuses. Read endpoints are open to any authenticated
    /// tenant user (needed to populate the Daily Work Entry form's
    /// dropdowns); Save endpoints are restricted to HR/Admin via the
    /// WORK_TRACKING_MASTERS feature's Edit permission, enforced the same
    /// way the APP layer's EssRestrictionAttribute gates the masters
    /// screen to admins.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WorkTrackingMasterController : ControllerBase
    {
        private readonly IWorkTrackingMasterService _service;

        private string? TenantId => User.FindFirst("TenantId")?.Value;
        private string? ActingUserId => User.FindFirst("UserId")?.Value;

        public WorkTrackingMasterController(IWorkTrackingMasterService service)
        {
            _service = service;
        }

        // Clients
        [HttpGet("clients")]
        public async Task<IActionResult> GetClients([FromQuery] bool activeOnly = false)
            => Ok(new ApiResponse<List<ClientDto>> { Success = true, Data = await _service.GetClientsAsync(TenantId!, activeOnly) });

        [HttpPost("clients")]
        public async Task<IActionResult> SaveClient([FromBody] ClientDto dto) => await TrySave(() => _service.SaveClientAsync(dto, TenantId!, ActingUserId!));

        // WorkJobs
        [HttpGet("jobs")]
        public async Task<IActionResult> GetJobs([FromQuery] bool activeOnly = false)
            => Ok(new ApiResponse<List<WorkJobDto>> { Success = true, Data = await _service.GetWorkJobsAsync(TenantId!, activeOnly) });

        [HttpGet("jobs/dropdown")]
        public async Task<IActionResult> GetJobsDropdown()
            => Ok(new ApiResponse<object> { Success = true, Data = await _service.GetActiveJobsForEmployeeDropdownAsync(TenantId!) });

        [HttpPost("jobs")]
        public async Task<IActionResult> SaveJob([FromBody] WorkJobDto dto) => await TrySave(() => _service.SaveWorkJobAsync(dto, TenantId!, ActingUserId!));

        // JobItems
        [HttpGet("job-items")]
        public async Task<IActionResult> GetJobItems([FromQuery] string jobId, [FromQuery] bool activeOnly = false)
            => Ok(new ApiResponse<List<JobItemDto>> { Success = true, Data = await _service.GetJobItemsAsync(jobId, TenantId!, activeOnly) });

        [HttpPost("job-items")]
        public async Task<IActionResult> SaveJobItem([FromBody] JobItemDto dto) => await TrySave(() => _service.SaveJobItemAsync(dto, TenantId!, ActingUserId!));

        // JobTypes
        [HttpGet("job-types")]
        public async Task<IActionResult> GetJobTypes([FromQuery] bool activeOnly = false)
            => Ok(new ApiResponse<List<JobTypeDto>> { Success = true, Data = await _service.GetJobTypesAsync(TenantId!, activeOnly) });

        [HttpPost("job-types")]
        public async Task<IActionResult> SaveJobType([FromBody] JobTypeDto dto) => await TrySave(() => _service.SaveJobTypeAsync(dto, TenantId!, ActingUserId!));

        // WorkActivities
        [HttpGet("work-activities")]
        public async Task<IActionResult> GetWorkActivities([FromQuery] string jobTypeId, [FromQuery] int? skidsDiscipline, [FromQuery] bool activeOnly = false)
            => Ok(new ApiResponse<List<WorkActivityDto>> { Success = true, Data = await _service.GetWorkActivitiesAsync(jobTypeId, skidsDiscipline, TenantId!, activeOnly) });

        [HttpPost("work-activities")]
        public async Task<IActionResult> SaveWorkActivity([FromBody] WorkActivityDto dto) => await TrySave(() => _service.SaveWorkActivityAsync(dto, TenantId!, ActingUserId!));

        // WorkEntryReasons
        [HttpGet("work-entry-reasons")]
        public async Task<IActionResult> GetWorkEntryReasons([FromQuery] int? category, [FromQuery] bool activeOnly = false)
            => Ok(new ApiResponse<List<WorkEntryReasonDto>> { Success = true, Data = await _service.GetWorkEntryReasonsAsync(category, TenantId!, activeOnly) });

        [HttpPost("work-entry-reasons")]
        public async Task<IActionResult> SaveWorkEntryReason([FromBody] WorkEntryReasonDto dto) => await TrySave(() => _service.SaveWorkEntryReasonAsync(dto, TenantId!, ActingUserId!));

        // DocumentStatuses
        [HttpGet("document-statuses")]
        public async Task<IActionResult> GetDocumentStatuses([FromQuery] bool activeOnly = false)
            => Ok(new ApiResponse<List<DocumentStatusDto>> { Success = true, Data = await _service.GetDocumentStatusesAsync(TenantId!, activeOnly) });

        [HttpPost("document-statuses")]
        public async Task<IActionResult> SaveDocumentStatus([FromBody] DocumentStatusDto dto) => await TrySave(() => _service.SaveDocumentStatusAsync(dto, TenantId!, ActingUserId!));

        private async Task<IActionResult> TrySave<T>(Func<Task<T>> action)
        {
            try
            {
                var result = await action();
                return Ok(new ApiResponse<T> { Success = true, Data = result, Message = "Saved successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object> { Success = false, Message = ex.Message });
            }
        }
    }
}
