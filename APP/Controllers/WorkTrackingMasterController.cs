using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace APP.Controllers
{
    /// <summary>
    /// HR/Admin-only masters screen for the Daily Work Entry module -
    /// Clients, Jobs, Job Types, Job Items, Work Activities, Idle/Downtime
    /// Reasons, Document Statuses - one tabbed screen, same grouping
    /// convention as the eSSL Integration Settings/Sync Logs/Unmapped
    /// Employees tabs. Gated via EssRestrictionAttribute's
    /// AdminOnlyControllers list.
    /// </summary>
    [JwtAuthorize]
    public class WorkTrackingMasterController : Controller
    {
        private readonly IApiService _apiService;

        public WorkTrackingMasterController(IApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<IActionResult> Index()
        {
            var clients = await _apiService.GetAsync<ApiResponse<List<ClientDto>>>("worktrackingmaster/clients");
            var jobs = await _apiService.GetAsync<ApiResponse<List<WorkJobDto>>>("worktrackingmaster/jobs");
            var jobTypes = await _apiService.GetAsync<ApiResponse<List<JobTypeDto>>>("worktrackingmaster/job-types");
            var reasons = await _apiService.GetAsync<ApiResponse<List<WorkEntryReasonDto>>>("worktrackingmaster/work-entry-reasons");
            var docStatuses = await _apiService.GetAsync<ApiResponse<List<DocumentStatusDto>>>("worktrackingmaster/document-statuses");

            ViewBag.Clients = clients?.Data ?? new List<ClientDto>();
            ViewBag.Jobs = jobs?.Data ?? new List<WorkJobDto>();
            ViewBag.JobTypes = jobTypes?.Data ?? new List<JobTypeDto>();
            ViewBag.Reasons = reasons?.Data ?? new List<WorkEntryReasonDto>();
            ViewBag.DocumentStatuses = docStatuses?.Data ?? new List<DocumentStatusDto>();

            return View();
        }

        [HttpGet]
        public async Task<JsonResult> GetJobItems(string jobId)
        {
            var response = await _apiService.GetAsync<ApiResponse<List<JobItemDto>>>($"worktrackingmaster/job-items?jobId={jobId}");
            return Json(new { success = true, data = response?.Data ?? new List<JobItemDto>() });
        }

        [HttpGet]
        public async Task<JsonResult> GetWorkActivities(string jobTypeId)
        {
            var response = await _apiService.GetAsync<ApiResponse<List<WorkActivityDto>>>($"worktrackingmaster/work-activities?jobTypeId={jobTypeId}");
            return Json(new { success = true, data = response?.Data ?? new List<WorkActivityDto>() });
        }

        [HttpPost]
        public async Task<JsonResult> SaveClient([FromBody] ClientDto model) => await Save("worktrackingmaster/clients", model);

        [HttpPost]
        public async Task<JsonResult> SaveJob([FromBody] WorkJobDto model) => await Save("worktrackingmaster/jobs", model);

        [HttpPost]
        public async Task<JsonResult> SaveJobItem([FromBody] JobItemDto model) => await Save("worktrackingmaster/job-items", model);

        [HttpPost]
        public async Task<JsonResult> SaveJobType([FromBody] JobTypeDto model) => await Save("worktrackingmaster/job-types", model);

        [HttpPost]
        public async Task<JsonResult> SaveWorkActivity([FromBody] WorkActivityDto model) => await Save("worktrackingmaster/work-activities", model);

        [HttpPost]
        public async Task<JsonResult> SaveWorkEntryReason([FromBody] WorkEntryReasonDto model) => await Save("worktrackingmaster/work-entry-reasons", model);

        [HttpPost]
        public async Task<JsonResult> SaveDocumentStatus([FromBody] DocumentStatusDto model) => await Save("worktrackingmaster/document-statuses", model);

        private async Task<JsonResult> Save<T>(string url, T model)
        {
            try
            {
                var response = await _apiService.PostAsync<T, ApiResponse<object>>(url, model);
                return Json(new { success = response?.Success ?? false, message = response?.Message });
            }
            catch (ApiException ex)
            {
                return Json(new { success = false, message = ex.ResponseContent });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
