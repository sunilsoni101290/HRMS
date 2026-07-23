using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using static APP.Helpers.EnumExtensions;

namespace APP.Controllers
{
    [JwtAuthorize]
    public class OnboardingController : Controller
    {
        private readonly IApiService _apiService;
        private readonly string _tenantId;
        private readonly string _userId;

        public OnboardingController(IApiService apiService)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
        }

        #region Index / Tracker

        [HttpGet]
        public async Task<IActionResult> Index(string? status, string? departmentId, string? search)
        {
            var url = "onboarding";

            var queryParts = new List<string>();

            if (!string.IsNullOrWhiteSpace(status))
                queryParts.Add($"status={Uri.EscapeDataString(status)}");

            if (!string.IsNullOrWhiteSpace(departmentId))
                queryParts.Add($"departmentId={Uri.EscapeDataString(departmentId)}");

            if (!string.IsNullOrWhiteSpace(search))
                queryParts.Add($"search={Uri.EscapeDataString(search)}");

            if (queryParts.Count > 0)
                url += "?" + string.Join("&", queryParts);

            var data = await _apiService.GetAsync<List<OnboardingCaseDto>>(url) ?? new();

            await LoadFilterDropdowns();

            ViewBag.SelectedStatus = status;
            ViewBag.SelectedDepartmentId = departmentId;
            ViewBag.SelectedSearch = search;

            return View(data);
        }

        #endregion

        #region Create

        [HttpGet]
        public async Task<IActionResult> Create(string? employeeId, string? candidateId)
        {
            await LoadEmployeeDropdown(employeeId);

            ViewBag.LockEmployee = !string.IsNullOrWhiteSpace(employeeId);
            ViewBag.FromCandidate = !string.IsNullOrWhiteSpace(candidateId);

            return View(new CreateOnboardingCaseDto
            {
                EmployeeId = employeeId ?? string.Empty,
                CandidateId = candidateId
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateOnboardingCaseDto model)
        {
            if (!ModelState.IsValid)
            {
                await LoadEmployeeDropdown();
                return View(model);
            }

            try
            {
                var result = await _apiService.PostAsync<CreateOnboardingCaseDto, OnboardingCaseDto>(
                    "onboarding", model);

                if (result == null || string.IsNullOrEmpty(result.Id))
                {
                    TempData["Error"] = "Unable to start onboarding for this employee.";
                    await LoadEmployeeDropdown();
                    return View(model);
                }

                TempData["Success"] = "Onboarding case started successfully.";

                return RedirectToAction(nameof(Details), new { id = result.Id });
            }
            catch (ApiException ex)
            {
                TempData["Error"] = GetErrorMessage(ex.ResponseContent);
                await LoadEmployeeDropdown();
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                await LoadEmployeeDropdown();
                return View(model);
            }
        }

        #endregion

        #region Details

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var data = await _apiService.GetAsync<OnboardingCaseDto>($"onboarding/{id}");

            if (data == null)
                return NotFound();

            return View(data);
        }

        #endregion

        #region Checklist Items (AJAX)

        // AJAX endpoint used by Details.cshtml so the progress bar / case
        // status can refresh without a full page reload. UpdateStatusAsync
        // on the API only returns the updated checklist item (not the whole
        // case), so the case is re-fetched afterwards to pick up the
        // recomputed ProgressPercent / Status - see
        // Application/Services/Onboarding/OnboardingService.cs.
        // Called via raw $.ajax (not an asp-action <form> post) from
        // Details.cshtml so the progress bar can refresh inline - matches
        // AttendanceRegularizationController.Cancel/Filter, which are also
        // plain [HttpPost] without [ValidateAntiForgeryToken] for the same
        // reason (JWT auth via [JwtAuthorize] is the real security boundary
        // for this kind of call in this codebase, not the cookie-based
        // antiforgery token).
        [HttpPost]
        public async Task<JsonResult> UpdateChecklistItemStatus(string itemId, int status, string? remarks)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return Json(new { success = false, message = "Invalid checklist item." });

            try
            {
                var item = await _apiService.PutAsync<UpdateChecklistItemStatusDto, OnboardingChecklistItemDto>(
                    $"onboarding/checklist-item/{itemId}/status",
                    new UpdateChecklistItemStatusDto
                    {
                        Status = (OnboardingChecklistItemStatus)status,
                        Remarks = remarks
                    });

                if (item == null)
                    return Json(new { success = false, message = "Unable to update checklist item." });

                var caseData = await _apiService.GetAsync<OnboardingCaseDto>($"onboarding/{item.OnboardingCaseId}");

                return Json(new
                {
                    success = true,
                    itemId = item.Id,
                    itemStatus = (int)item.Status,
                    itemStatusName = item.StatusName,
                    progressPercent = caseData?.ProgressPercent ?? 0,
                    caseStatus = caseData != null ? (int)caseData.Status : 0,
                    caseStatusName = caseData?.StatusName
                });
            }
            catch (ApiException ex)
            {
                return Json(new { success = false, message = GetErrorMessage(ex.ResponseContent) });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddChecklistItem(string caseId, AddChecklistItemDto model)
        {
            if (string.IsNullOrWhiteSpace(caseId))
                return NotFound();

            try
            {
                await _apiService.PostAsync<AddChecklistItemDto, OnboardingChecklistItemDto>(
                    $"onboarding/{caseId}/checklist-item", model);

                TempData["Success"] = "Checklist item added successfully.";
            }
            catch (ApiException ex)
            {
                TempData["Error"] = GetErrorMessage(ex.ResponseContent);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id = caseId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteChecklistItem(string itemId, string caseId)
        {
            if (!string.IsNullOrWhiteSpace(itemId))
            {
                var result = await _apiService.DeleteAsync($"onboarding/checklist-item/{itemId}");

                TempData[result ? "Success" : "Error"] = result
                    ? "Checklist item deleted successfully."
                    : "Unable to delete checklist item.";
            }

            return RedirectToAction(nameof(Details), new { id = caseId });
        }

        #endregion

        #region Case Status Transitions

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(string id, int status, string? remarks)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            try
            {
                await _apiService.PutAsync<UpdateOnboardingCaseStatusDto, OnboardingCaseDto>(
                    $"onboarding/{id}/status",
                    new UpdateOnboardingCaseStatusDto
                    {
                        Status = (OnboardingCaseStatus)status,
                        Remarks = remarks
                    });

                TempData["Success"] = "Onboarding case status updated successfully.";
            }
            catch (ApiException ex)
            {
                TempData["Error"] = GetErrorMessage(ex.ResponseContent);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        #endregion

        #region Dropdowns / Helpers

        private async Task LoadEmployeeDropdown(string? selectedEmployeeId = null)
        {
            var employees = await _apiService.GetAsync<List<DropdownDto>>("dropdown/employee");

            ViewBag.EmployeeList = new SelectList(employees, "Value", "Text", selectedEmployeeId);
        }

        private async Task LoadFilterDropdowns()
        {
            var departments = await _apiService.GetAsync<List<DropdownDto>>("dropdown/department");

            ViewBag.DepartmentList = new SelectList(departments, "Value", "Text");

            ViewBag.StatusList = EnumHelper.GetEnumList<OnboardingCaseStatus>();
        }

        private string GetErrorMessage(string json)
        {
            try
            {
                var obj = Newtonsoft.Json.Linq.JObject.Parse(json);

                if (obj["Message"] != null)
                    return obj["Message"]!.ToString();

                if (obj["Errors"] is Newtonsoft.Json.Linq.JArray errors && errors.Count > 0)
                    return errors[0]?.ToString();

                if (obj["errors"] is Newtonsoft.Json.Linq.JObject validationErrors)
                {
                    foreach (var property in validationErrors.Properties())
                    {
                        if (property.Value is Newtonsoft.Json.Linq.JArray arr && arr.Count > 0)
                            return arr[0]?.ToString();
                    }
                }

                return "Unable to complete the requested onboarding action.";
            }
            catch
            {
                return "Unable to complete the requested onboarding action.";
            }
        }

        #endregion
    }
}
