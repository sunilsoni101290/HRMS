using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using static APP.Helpers.EnumExtensions;

namespace APP.Controllers
{
    // NOTE: named exactly "EmployeeFeedback" to match the menu-seeding
    // string in Domain/Helper/AppFeatureConstants.cs
    // (EMPLOYEE_FEEDBACK_CONTROLLER = "EmployeeFeedback") and the API route
    // (api/employeefeedback). Phase 4 of the "Probation & Confirmation"
    // (Employee Lifecycle) module - NO maker-checker workflow, plain CRUD.
    // Dual-audience: Index doubles as "my feedback" (self-service, no
    // employeeId) and "feedback about employee X" (HR/manager,
    // employeeId supplied) - the API's own visibility filter
    // (IsVisibleToEmployee) decides what a self-service caller actually
    // sees, the APP layer just renders whatever comes back. Deliberately
    // NOT added to EssRestrictionAttribute's AdminOnlyControllers - a plain
    // employee legitimately needs Index (their own feedback) reachable,
    // same reasoning as LeaveApplication/TeamAttendance in that file.
    [JwtAuthorize]
    public class EmployeeFeedbackController : Controller
    {
        private readonly IApiService _apiService;
        private readonly string _tenantId;
        private readonly string _userId;
        private readonly string? _employeeId;
        private readonly bool _isAdmin;

        public EmployeeFeedbackController(IApiService apiService)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
            _employeeId = SessionHelper.GetActiveEmployeeId;
            _isAdmin = SessionHelper.IsAdminRole();
        }

        #region Index

        // employeeId omitted -> the logged-in user's own feedback
        // (self-service "My Feedback"). employeeId supplied -> HR/manager
        // viewing a specific employee's feedback (also reachable from
        // Employee/Details' "Feedback" panel).
        [HttpGet]
        public async Task<IActionResult> Index(string? employeeId)
        {
            var targetEmployeeId = string.IsNullOrWhiteSpace(employeeId) ? _employeeId : employeeId;

            if (string.IsNullOrWhiteSpace(targetEmployeeId))
            {
                TempData["GlobalError"] = "Your login isn't linked to an employee profile.";
                return RedirectToAction("Index", "EmployeeDashboard");
            }

            try
            {
                var data = await _apiService.GetAsync<List<EmployeeFeedbackDto>>($"employeefeedback/employee/{targetEmployeeId}");

                ViewBag.EmployeeId = targetEmployeeId;
                ViewBag.IsOwnFeedback = targetEmployeeId == _employeeId;
                ViewBag.IsAdmin = _isAdmin;
                ViewBag.CurrentUserId = _userId;
                ViewBag.Employee = await TryGetEmployeeAsync(targetEmployeeId);

                return View(data ?? new List<EmployeeFeedbackDto>());
            }
            catch (UnauthorizedAccessException ex) when (ex.Message.StartsWith("Access Denied"))
            {
                // A genuine session-expiry UnauthorizedAccessException
                // (different message) is intentionally NOT caught here - it
                // propagates to the global ApiSessionExpiredFilter, same as
                // ProbationConfirmationController/PipController.
                TempData["GlobalError"] = "You are not authorized to view this employee's feedback.";
                return RedirectToAction("Index", "EmployeeDashboard");
            }
        }

        #endregion

        #region Given By Me

        // A manager's "feedback I've given" list.
        [HttpGet]
        public async Task<IActionResult> GivenByMe()
        {
            var data = await _apiService.GetAsync<List<EmployeeFeedbackDto>>("employeefeedback/given-by-me");

            return View(data ?? new List<EmployeeFeedbackDto>());
        }

        #endregion

        #region Create

        [HttpGet]
        public async Task<IActionResult> Create(string employeeId)
        {
            if (string.IsNullOrWhiteSpace(employeeId))
            {
                TempData["GlobalError"] = "No employee was specified for this feedback.";
                return RedirectToAction("Index", "EmployeeDashboard");
            }

            ViewBag.Employee = await TryGetEmployeeAsync(employeeId);
            ViewBag.CategoryList = EnumHelper.GetEnumList<FeedbackCategory>();

            var model = new CreateUpdateEmployeeFeedbackDto
            {
                EmployeeId = employeeId,
                FeedbackDate = DateTime.Today,
                IsVisibleToEmployee = true
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUpdateEmployeeFeedbackDto model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Employee = await TryGetEmployeeAsync(model.EmployeeId);
                ViewBag.CategoryList = EnumHelper.GetEnumList<FeedbackCategory>();
                return View(model);
            }

            try
            {
                var result = await _apiService.PostAsync<CreateUpdateEmployeeFeedbackDto, EmployeeFeedbackDto>(
                    "employeefeedback", model);

                if (result == null)
                {
                    TempData["GlobalError"] = "Unable to submit this feedback.";
                    return RedirectToAction(nameof(Create), new { employeeId = model.EmployeeId });
                }

                TempData["Success"] = "Feedback submitted successfully.";
                return RedirectToAction(nameof(Index), new { employeeId = model.EmployeeId });
            }
            catch (ApiException ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
                return RedirectToAction(nameof(Create), new { employeeId = model.EmployeeId });
            }
            catch (Exception ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.Message);
                return RedirectToAction(nameof(Create), new { employeeId = model.EmployeeId });
            }
        }

        #endregion

        #region Edit

        // Reachable by the original author or HR - the API's 403 is the
        // real gate; the Edit button is also hidden client-side (Index/
        // Details views) when the current user is neither.
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            try
            {
                var data = await _apiService.GetAsync<EmployeeFeedbackDto>($"employeefeedback/{id}");

                if (data == null)
                    return NotFound();

                ViewBag.Employee = await TryGetEmployeeAsync(data.EmployeeId);
                ViewBag.CategoryList = EnumHelper.GetEnumList<FeedbackCategory>();
                ViewBag.Id = id;

                var model = new CreateUpdateEmployeeFeedbackDto
                {
                    EmployeeId = data.EmployeeId,
                    FeedbackDate = data.FeedbackDate,
                    Category = data.Category,
                    Rating = data.Rating,
                    Strengths = data.Strengths,
                    AreasOfImprovement = data.AreasOfImprovement,
                    Comments = data.Comments,
                    IsVisibleToEmployee = data.IsVisibleToEmployee
                };

                return View(model);
            }
            catch (UnauthorizedAccessException ex) when (ex.Message.StartsWith("Access Denied"))
            {
                TempData["GlobalError"] = "You are not authorized to edit this feedback.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, CreateUpdateEmployeeFeedbackDto model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Employee = await TryGetEmployeeAsync(model.EmployeeId);
                ViewBag.CategoryList = EnumHelper.GetEnumList<FeedbackCategory>();
                ViewBag.Id = id;
                return View(model);
            }

            try
            {
                var result = await _apiService.PutAsync<CreateUpdateEmployeeFeedbackDto, EmployeeFeedbackDto>(
                    $"employeefeedback/{id}", model);

                TempData[result != null ? "Success" : "GlobalError"] = result != null
                    ? "Feedback updated successfully."
                    : "Unable to update this feedback.";

                return RedirectToAction(nameof(Details), new { id });
            }
            catch (UnauthorizedAccessException ex) when (ex.Message.StartsWith("Access Denied"))
            {
                TempData["GlobalError"] = "You are not authorized to edit this feedback.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (ApiException ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
                return RedirectToAction(nameof(Edit), new { id });
            }
            catch (Exception ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.Message);
                return RedirectToAction(nameof(Edit), new { id });
            }
        }

        #endregion

        #region Delete

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id, string? employeeId)
        {
            try
            {
                await _apiService.DeleteAsync($"employeefeedback/{id}");
                TempData["Success"] = "Feedback deleted successfully.";
            }
            catch (ApiException ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
            }
            catch (Exception ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.Message);
            }

            return !string.IsNullOrWhiteSpace(employeeId)
                ? RedirectToAction(nameof(Index), new { employeeId })
                : RedirectToAction(nameof(GivenByMe));
        }

        #endregion

        #region Details

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            try
            {
                var data = await _apiService.GetAsync<EmployeeFeedbackDto>($"employeefeedback/{id}");

                if (data == null)
                    return NotFound();

                ViewBag.CurrentUserId = _userId;
                ViewBag.IsAdmin = _isAdmin;

                return View(data);
            }
            catch (UnauthorizedAccessException ex) when (ex.Message.StartsWith("Access Denied"))
            {
                TempData["GlobalError"] = "You are not authorized to view this feedback.";
                return RedirectToAction("Index", "EmployeeDashboard");
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        #endregion

        #region Helpers

        private async Task<EmployeeListDto?> TryGetEmployeeAsync(string employeeId)
        {
            try
            {
                return await _apiService.GetAsync<EmployeeListDto>($"Employee/get-employee-detail/{employeeId}");
            }
            catch
            {
                return null;
            }
        }

        // Same shape as WfhRequestController/EmployeeTransferController's
        // GetErrorMessage - handles both pure JSON (ApiException.
        // ResponseContent) and a human-readable prefix in front of the JSON
        // (the plain Exception/UnauthorizedAccessException/
        // ApplicationException that ApiService's HandleResponse throws for
        // the single/2-generic Put/Post overloads).
        private string GetErrorMessage(string raw)
        {
            try
            {
                var jsonStart = raw.IndexOf('{');
                var json = jsonStart >= 0 ? raw.Substring(jsonStart) : raw;

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

                return "Unable to process this feedback request.";
            }
            catch
            {
                return string.IsNullOrWhiteSpace(raw) ? "Unable to process this feedback request." : raw;
            }
        }

        #endregion
    }
}
