using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace APP.Controllers
{
    /// <summary>
    /// Job/Work Assignment - manager-facing "Assign Work" screen plus the
    /// employee-facing "My Assigned Jobs" screen. Assign/Reassign are
    /// deliberately NOT admin-gated (see EssRestrictionAttribute's remarks
    /// on DailyWorkEntryController) - a Team Leader/Manager logged in under
    /// the plain self-service role must reach this page; authorization is
    /// fully enforced server-side by WorkAssignmentService (spec section
    /// 23), never by hiding the menu item alone.
    /// </summary>
    [JwtAuthorize]
    public class EmployeeWorkAssignmentController : Controller
    {
        private readonly IApiService _apiService;

        public EmployeeWorkAssignmentController(IApiService apiService)
        {
            _apiService = apiService;
        }

        // "My Assigned Jobs" (spec section 7) - every logged-in employee.
        public async Task<IActionResult> Index()
        {
            var response = await _apiService.GetAsync<ApiResponse<List<EmployeeWorkAssignmentSummaryDto>>>("employeeworkassignment/my");
            return View(response?.Data ?? new List<EmployeeWorkAssignmentSummaryDto>());
        }

        public async Task<IActionResult> Details(string id)
        {
            var response = await _apiService.GetAsync<ApiResponse<EmployeeWorkAssignmentDto>>($"employeeworkassignment/{id}");
            if (response?.Data == null)
            {
                TempData["GlobalError"] = response?.Message ?? "Assignment not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(response.Data);
        }

        // "Assign Job/Work to Employee" (spec section 4) - Manager/Team
        // Leader/HR-Admin.
        public IActionResult Assign()
        {
            return View(new SaveEmployeeWorkAssignmentDto());
        }

        // Assignments this manager made, or their team's (spec section 20).
        public async Task<IActionResult> AssignedByMeOrTeam()
        {
            var response = await _apiService.GetAsync<ApiResponse<List<EmployeeWorkAssignmentSummaryDto>>>("employeeworkassignment/assigned-by-me-or-team");
            return View(response?.Data ?? new List<EmployeeWorkAssignmentSummaryDto>());
        }

        // Employee Assignment Report (spec section 26) - same "assigned by
        // me or my team" visibility scope as AssignedByMeOrTeam, so this
        // stays on the non-admin-gated controller rather than
        // WorkTrackingReportController (which is HR/Admin only).
        public async Task<IActionResult> EmployeeAssignmentReport()
        {
            var response = await _apiService.GetAsync<ApiResponse<List<EmployeeAssignmentReportRowDto>>>("employeeworkassignment/reports/employee-assignment");
            return View(response?.Data ?? new List<EmployeeAssignmentReportRowDto>());
        }

        // ==================================================================
        // AJAX endpoints (SweetAlert-driven, mirrors DailyWorkEntryController)
        // ==================================================================

        [HttpGet]
        public async Task<JsonResult> GetEmployeesDropdown()
        {
            // Reuses the existing Employee/employee-list API (same one the
            // Employee/Index page already calls) instead of the bare
            // dropdown/employee endpoint, purely because it carries the
            // Department/Designation/RelievingDate fields the searchable
            // employee selector needs - no new backend API was added.
            // Filtered to active employees only (RelievingDate == null),
            // same "Active" rule Employee/Index uses for its status badge.
            var employees = await _apiService.GetAsync<List<EmployeeListDto>>("Employee/employee-list")
                ?? new List<EmployeeListDto>();

            var data = employees
                .Where(e => e.RelievingDate == null)
                .OrderBy(e => e.FirstName)
                .Select(e => new
                {
                    id = e.Id,
                    code = e.EmployeeCode,
                    name = (e.FirstName + " " + e.LastName).Trim(),
                    department = e.DepartmentName ?? "",
                    designation = e.DesignationName ?? ""
                })
                .ToList();

            return Json(new { success = true, data });
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
        public async Task<JsonResult> GetWorkActivities(string jobTypeId)
        {
            var response = await _apiService.GetAsync<ApiResponse<List<WorkActivityDto>>>($"worktrackingmaster/work-activities?jobTypeId={jobTypeId}&activeOnly=true");
            return Json(new { success = true, data = response?.Data ?? new List<WorkActivityDto>() });
        }

        [HttpPost]
        public async Task<JsonResult> SaveAssignment([FromBody] SaveEmployeeWorkAssignmentDto model)
        {
            try
            {
                var response = await _apiService.PostAsync<SaveEmployeeWorkAssignmentDto, ApiResponse<EmployeeWorkAssignmentDto>>(
                    "employeeworkassignment/assign", model ?? new SaveEmployeeWorkAssignmentDto());
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
        public async Task<JsonResult> UpdateStatus(string id, [FromBody] UpdateAssignmentStatusDto model)
        {
            try
            {
                var response = await _apiService.PutAsync<UpdateAssignmentStatusDto, ApiResponse<EmployeeWorkAssignmentDto>>(
                    $"employeeworkassignment/{id}/status", model ?? new UpdateAssignmentStatusDto());
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
        public async Task<JsonResult> Reassign(string id, [FromBody] ReassignEmployeeWorkAssignmentDto model)
        {
            try
            {
                var response = await _apiService.PostAsync<ReassignEmployeeWorkAssignmentDto, ApiResponse<EmployeeWorkAssignmentDto>>(
                    $"employeeworkassignment/{id}/reassign", model ?? new ReassignEmployeeWorkAssignmentDto());
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

        [HttpGet]
        public async Task<JsonResult> GetMyWorkDashboard()
        {
            var response = await _apiService.GetAsync<ApiResponse<MyWorkDashboardDto>>("employeeworkassignment/my/dashboard");
            return Json(new { success = true, data = response?.Data ?? new MyWorkDashboardDto() });
        }

        [HttpGet]
        public async Task<JsonResult> GetTeamWorkOverview()
        {
            var response = await _apiService.GetAsync<ApiResponse<TeamWorkOverviewDto>>("employeeworkassignment/team/overview");
            return Json(new { success = true, data = response?.Data ?? new TeamWorkOverviewDto() });
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
}
