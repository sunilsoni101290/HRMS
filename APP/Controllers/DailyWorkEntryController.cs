using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace APP.Controllers
{
    /// <summary>
    /// Daily Work Entry - self-service Create/MyEntries/Details plus the
    /// Team Leader Approval queue (Approve/Reject/PendingApproval are
    /// deliberately NOT admin-gated, same convention as
    /// LeaveApplication/AttendanceRegularization - see
    /// EssRestrictionAttribute's remarks). EmployeeId is never posted from
    /// this controller - the API always resolves it from the JWT.
    /// </summary>
    [JwtAuthorize]
    public class DailyWorkEntryController : Controller
    {
        private readonly IApiService _apiService;

        public DailyWorkEntryController(IApiService apiService)
        {
            _apiService = apiService;
        }

        // Daily Work Entry - today's (or a chosen) date's editable form.
        public async Task<IActionResult> Index(string? date)
        {
            var workDate = string.IsNullOrEmpty(date) ? DateTime.Today : DateTime.Parse(date);
            ViewBag.WorkDate = workDate;

            var response = await _apiService.GetAsync<ApiResponse<DailyWorkLogDto>>($"dailyworkentry/by-date?workDate={workDate:yyyy-MM-dd}");
            return View(response?.Data ?? new DailyWorkLogDto { WorkDate = workDate });
        }

        public async Task<IActionResult> MyEntries(DateTime? fromDate, DateTime? toDate)
        {
            var qs = new List<string>();
            if (fromDate.HasValue) qs.Add($"fromDate={fromDate:yyyy-MM-dd}");
            if (toDate.HasValue) qs.Add($"toDate={toDate:yyyy-MM-dd}");

            var url = "dailyworkentry/my" + (qs.Count > 0 ? "?" + string.Join("&", qs) : "");
            var response = await _apiService.GetAsync<ApiResponse<List<DailyWorkLogSummaryDto>>>(url);

            return View(response?.Data ?? new List<DailyWorkLogSummaryDto>());
        }

        public async Task<IActionResult> Details(string id)
        {
            var response = await _apiService.GetAsync<ApiResponse<DailyWorkLogDto>>($"dailyworkentry/{id}");
            if (response?.Data == null)
            {
                TempData["GlobalError"] = response?.Message ?? "Entry not found.";
                return RedirectToAction("MyEntries");
            }

            return View(response.Data);
        }

        public async Task<IActionResult> PendingApproval()
        {
            var response = await _apiService.GetAsync<ApiResponse<List<DailyWorkLogSummaryDto>>>("dailyworkentry/pending-approval");
            return View(response?.Data ?? new List<DailyWorkLogSummaryDto>());
        }

        // ==================================================================
        // AJAX endpoints (SweetAlert-driven, mirrors the WfhRequest/eSSL
        // pattern used elsewhere in this app)
        // ==================================================================

        [HttpGet]
        public async Task<JsonResult> GetConfiguration(string workDate)
        {
            try
            {
                var response = await _apiService.GetAsync<ApiResponse<DailyWorkLogDto>>($"dailyworkentry/by-date?workDate={workDate}");
                return Json(new { success = true, data = response?.Data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = GetErrorMessage(ex.Message) });
            }
        }

        // Backs the Team Leader Approval modal's drill-down (PendingApproval.cshtml) -
        // a JSON-friendly counterpart to the full Details view.
        [HttpGet]
        public async Task<JsonResult> GetEntryJson(string id)
        {
            try
            {
                var response = await _apiService.GetAsync<ApiResponse<DailyWorkLogDto>>($"dailyworkentry/{id}");
                return Json(new { success = response?.Success ?? false, message = response?.Message, data = response?.Data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = GetErrorMessage(ex.Message) });
            }
        }

        // The employee's own assignment-scoped Job/Structure/Activity combo
        // list (spec section 9) - Daily Work Entry's "Assigned Work"
        // dropdown filters THIS in-memory rather than the full Job master,
        // so an employee can only charge normal project work against
        // something a manager actually assigned to them.
        [HttpGet]
        public async Task<JsonResult> GetMyAssignedWorkCombo()
        {
            var response = await _apiService.GetAsync<ApiResponse<List<AssignedWorkComboDto>>>("employeeworkassignment/my/work-combo");
            return Json(new { success = true, data = response?.Data ?? new List<AssignedWorkComboDto>() });
        }

        [HttpGet]
        public async Task<JsonResult> GetJobTypes()
        {
            var response = await _apiService.GetAsync<ApiResponse<List<JobTypeDto>>>("worktrackingmaster/job-types?activeOnly=true");
            return Json(new { success = true, data = response?.Data ?? new List<JobTypeDto>() });
        }

        [HttpGet]
        public async Task<JsonResult> GetJobsDropdown()
        {
            var response = await _apiService.GetAsync<ApiResponse<List<DropdownItem>>>("worktrackingmaster/jobs/dropdown");
            return Json(new { success = true, data = response?.Data ?? new List<DropdownItem>() });
        }

        [HttpGet]
        public async Task<JsonResult> GetJobItems(string jobId)
        {
            var response = await _apiService.GetAsync<ApiResponse<List<JobItemDto>>>($"worktrackingmaster/job-items?jobId={jobId}&activeOnly=true");
            return Json(new { success = true, data = response?.Data ?? new List<JobItemDto>() });
        }

        [HttpGet]
        public async Task<JsonResult> GetWorkActivities(string jobTypeId, int? skidsDiscipline)
        {
            var url = $"worktrackingmaster/work-activities?jobTypeId={jobTypeId}&activeOnly=true";
            if (skidsDiscipline.HasValue) url += $"&skidsDiscipline={skidsDiscipline}";

            var response = await _apiService.GetAsync<ApiResponse<List<WorkActivityDto>>>(url);
            return Json(new { success = true, data = response?.Data ?? new List<WorkActivityDto>() });
        }

        [HttpGet]
        public async Task<JsonResult> GetWorkEntryReasons(int category)
        {
            var response = await _apiService.GetAsync<ApiResponse<List<WorkEntryReasonDto>>>($"worktrackingmaster/work-entry-reasons?category={category}&activeOnly=true");
            return Json(new { success = true, data = response?.Data ?? new List<WorkEntryReasonDto>() });
        }

        [HttpPost]
        public async Task<JsonResult> SaveDraft([FromBody] SaveDailyWorkLogDto model)
        {
            try
            {
                var response = await _apiService.PostAsync<SaveDailyWorkLogDto, ApiResponse<DailyWorkLogDto>>("dailyworkentry/draft", model ?? new SaveDailyWorkLogDto());
                return Json(new { success = response?.Success ?? false, message = response?.Message, data = response?.Data });
            }
            catch (ApiException ex)
            {
                return Json(new { success = false, message = GetErrorMessage(ex.ResponseContent) });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = GetErrorMessage(ex.Message) });
            }
        }

        [HttpPost]
        public async Task<JsonResult> Submit([FromBody] SaveDailyWorkLogDto model)
        {
            try
            {
                var response = await _apiService.PostAsync<SaveDailyWorkLogDto, ApiResponse<DailyWorkLogDto>>("dailyworkentry/submit", model ?? new SaveDailyWorkLogDto());
                return Json(new { success = response?.Success ?? false, message = response?.Message, data = response?.Data });
            }
            catch (ApiException ex)
            {
                return Json(new { success = false, message = GetErrorMessage(ex.ResponseContent) });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = GetErrorMessage(ex.Message) });
            }
        }

        [HttpPost]
        public async Task<JsonResult> Approve(string id)
        {
            try
            {
                var response = await _apiService.PostAsync<ApiResponse<DailyWorkLogDto>>($"dailyworkentry/{id}/approve", new { });
                return Json(new { success = response?.Success ?? false, message = response?.Message });
            }
            catch (ApiException ex)
            {
                return Json(new { success = false, message = GetErrorMessage(ex.ResponseContent) });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = GetErrorMessage(ex.Message) });
            }
        }

        [HttpPost]
        public async Task<JsonResult> Reject(string id, [FromBody] RejectDailyWorkLogDto model)
        {
            try
            {
                var response = await _apiService.PostAsync<RejectDailyWorkLogDto, ApiResponse<DailyWorkLogDto>>($"dailyworkentry/{id}/reject", model ?? new RejectDailyWorkLogDto());
                return Json(new { success = response?.Success ?? false, message = response?.Message });
            }
            catch (ApiException ex)
            {
                return Json(new { success = false, message = GetErrorMessage(ex.ResponseContent) });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = GetErrorMessage(ex.Message) });
            }
        }

        private static string GetErrorMessage(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return "The ERP API returned an error.";

            try
            {
                var jsonStart = raw.IndexOf('{');
                var json = jsonStart >= 0 ? raw.Substring(jsonStart) : raw;
                var obj = Newtonsoft.Json.Linq.JObject.Parse(json);

                if (obj["Message"] != null) return obj["Message"]!.ToString();
                if (obj["message"] != null) return obj["message"]!.ToString();

                if (obj["errors"] is Newtonsoft.Json.Linq.JObject errors)
                {
                    foreach (var prop in errors.Properties())
                    {
                        if (prop.Value is Newtonsoft.Json.Linq.JArray arr && arr.Count > 0)
                            return arr[0]!.ToString();
                    }
                }

                return raw;
            }
            catch
            {
                return raw;
            }
        }
    }

    public class DropdownItem
    {
        public string Value { get; set; } = "";
        public string Text { get; set; } = "";
    }
}
