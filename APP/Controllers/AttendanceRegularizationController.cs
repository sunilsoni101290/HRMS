using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace APP.Controllers
{
    [JwtAuthorize]
    public class AttendanceRegularizationController : Controller
    {
        private readonly IApiService _apiService;
        private string _tenantId;
        private string _userId;

        // The employee record linked to whoever is logged in. When the user
        // is a plain employee (not admin/HR) this is always the one used for
        // "Request Regularization" - never a value picked from a dropdown or
        // posted from the client - so an employee can only ever regularize
        // their own attendance.
        private readonly string? _employeeId;
        private readonly bool _isAdmin;

        public AttendanceRegularizationController(IApiService apiService)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
            _employeeId = SessionHelper.GetActiveEmployeeId;
            _isAdmin = SessionHelper.IsAdminRole();
        }

        #region CRUD / Lists

        public async Task<IActionResult> Index()
        {
            ViewBag.IsAdmin = _isAdmin;
            await SetApprovalViewBagAsync();
            await LoadDropdowns();

            var data = await _apiService.GetAsync<List<AttendanceRegularizationDto>>("AttendanceRegularization");

            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();

            ViewBag.IsAdmin = _isAdmin;
            ViewBag.EmployeeDisplayName = SessionHelper.GetActiveFullName;
            ViewBag.NoEmployeeProfile = !_isAdmin && string.IsNullOrEmpty(_employeeId);

            return View(new RequestRegularizationDto
            {
                // Self-service users always request for their own linked
                // employee record. Admin/HR still pick from the dropdown in
                // the view.
                EmployeeId = _isAdmin ? string.Empty : (_employeeId ?? string.Empty),
                Date = DateTime.Today
            });
        }

        // Lets the Create view ask, via AJAX, what Attendance already has on
        // record (if anything) for the chosen Employee/Date - shown
        // read-only so the employee can see exactly what they are
        // correcting instead of guessing.
        [HttpGet]
        public async Task<JsonResult> GetOriginalAttendance(string employeeId, DateTime date)
        {
            if (string.IsNullOrEmpty(employeeId))
                return Json(new { firstIn = (DateTime?)null, lastOut = (DateTime?)null });

            var url =
                $"attendance/monthly?employeeId={Uri.EscapeDataString(employeeId)}" +
                $"&month={date.Month}&year={date.Year}";

            var list = await _apiService.GetAsync<List<AttendanceDto>>(url);

            var match = list?.FirstOrDefault(x => x.Date.Date == date.Date);

            return Json(new
            {
                firstIn = match?.FirstIn,
                lastOut = match?.LastOut
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RequestRegularizationDto model)
        {
            if (model == null)
            {
                await LoadDropdowns();
                ViewBag.IsAdmin = _isAdmin;
                ViewBag.EmployeeDisplayName = SessionHelper.GetActiveFullName;
                return View(model);
            }

            // Never trust a client-supplied EmployeeId for a self-service
            // user - always overwrite it with the one resolved from their
            // own session. Only admin/HR (who use the dropdown to file a
            // request on someone else's behalf) keep the posted value.
            if (!_isAdmin)
            {
                if (string.IsNullOrEmpty(_employeeId))
                {
                    TempData["GlobalError"] = "Your login isn't linked to an employee profile, so you can't request regularization.";
                    return RedirectToAction(nameof(Create));
                }

                model.EmployeeId = _employeeId;
            }

            model.TenantId = _tenantId;
            model.CreatedBy = _userId;

            try
            {
                var result = await _apiService.PostAsync<RequestRegularizationDto, AttendanceRegularizationDto>(
                    "AttendanceRegularization", model);

                if (result == null)
                {
                    TempData["GlobalError"] = "Unable to submit regularization request.";
                    return RedirectToAction(nameof(Create));
                }

                TempData["Success"] = "Regularization request submitted successfully.";

                // Admin/HR land on the org-wide list; a self-service employee
                // should stay inside their own portal and see their own
                // history, not the shared admin list.
                if (!_isAdmin)
                    return RedirectToAction(nameof(MyRequests));

                return RedirectToAction(nameof(Index));
            }
            catch (ApiException ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
                return RedirectToAction(nameof(Create));
            }
            catch (Exception ex)
            {
                TempData["GlobalError"] = ex.Message;
                return RedirectToAction(nameof(Create));
            }
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

                return "Unable to submit regularization request.";
            }
            catch
            {
                return "Unable to submit regularization request.";
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var data = await _apiService.GetAsync<AttendanceRegularizationDto>($"AttendanceRegularization/{id}");

            ViewBag.IsAdmin = _isAdmin;
            ViewBag.CurrentEmployeeId = _employeeId;

            return View(data);
        }

        #endregion

        #region Workflow

        // Approve/Reject/Send Back are not gated by "_isAdmin" - the API
        // enforces the real multi-level rule (Reporting Manager for Level 1,
        // Department Head for Level 2, anyone with the HR role for Level 3)
        // regardless of whether the acting user's own login role happens to
        // be flagged admin. Any unauthorized attempt is rejected server-side
        // with a clear message rather than a bare Forbid.
        private IActionResult RedirectBackOrToMyApprovals()
        {
            var referer = Request.Headers["Referer"].ToString();

            if (!string.IsNullOrEmpty(referer) && Url.IsLocalUrl(referer))
                return Redirect(referer);

            return RedirectToAction(nameof(MyApprovals));
        }

        [HttpPost]
        public async Task<IActionResult> Approve(ApproveRegularizationRequestDto model)
        {
            if (model == null)
                return RedirectToAction(nameof(MyApprovals));

            model.CreatedBy = _userId;
            model.ApprovedBy = _userId;
            model.TenantId = _tenantId;

            try
            {
                var result = await _apiService.PostAsync<ApproveRegularizationRequestDto, bool>(
                    "AttendanceRegularization/approve", model);

                TempData[result ? "Success" : "GlobalError"] = result
                    ? "Regularization request approved successfully."
                    : "Unable to approve this regularization request.";
            }
            catch (ApiException ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
            }
            catch (Exception ex)
            {
                TempData["GlobalError"] = ex.Message;
            }

            return RedirectBackOrToMyApprovals();
        }

        [HttpPost]
        public async Task<IActionResult> Reject(RejectRegularizationRequestDto model)
        {
            model.RejectedBy = _userId;
            model.TenantId = _tenantId;

            try
            {
                var result = await _apiService.PostAsync<RejectRegularizationRequestDto, bool>(
                    "AttendanceRegularization/reject", model);

                TempData[result ? "Success" : "GlobalError"] = result
                    ? "Regularization request rejected."
                    : "Unable to reject this regularization request.";
            }
            catch (ApiException ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
            }
            catch (Exception ex)
            {
                TempData["GlobalError"] = ex.Message;
            }

            return RedirectBackOrToMyApprovals();
        }

        [HttpPost]
        public async Task<IActionResult> SendBack(SendBackRegularizationRequestDto model)
        {
            model.ActionBy = _userId;

            try
            {
                var result = await _apiService.PostAsync<SendBackRegularizationRequestDto, bool>(
                    "AttendanceRegularization/send-back", model);

                TempData[result ? "Success" : "GlobalError"] = result
                    ? "Regularization request sent back to the employee."
                    : "Unable to send this regularization request back.";
            }
            catch (ApiException ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
            }
            catch (Exception ex)
            {
                TempData["GlobalError"] = ex.Message;
            }

            return RedirectBackOrToMyApprovals();
        }

        [HttpGet]
        public async Task<IActionResult> Resubmit(string id)
        {
            var data = await _apiService.GetAsync<AttendanceRegularizationDto>($"AttendanceRegularization/{id}");

            if (data == null)
                return NotFound();

            if (data.EmployeeId != _employeeId && !_isAdmin)
                return Forbid();

            if (data.Status != EnumExtensions.ApprovalStatus.ReturnedToEmployee)
            {
                TempData["GlobalError"] = "Only a regularization request that was sent back can be resubmitted.";
                return RedirectToAction(nameof(MyRequests));
            }

            await LoadDropdowns();

            ViewBag.IsAdmin = _isAdmin;
            ViewBag.EmployeeDisplayName = data.EmployeeName;
            ViewBag.SendBackReason = data.SendBackReason;

            return View(new RequestRegularizationDto
            {
                Id = data.Id,
                EmployeeId = data.EmployeeId,
                Date = data.Date,
                RequestedFirstIn = data.RequestedFirstIn,
                RequestedLastOut = data.RequestedLastOut,
                Reason = data.Reason
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Resubmit(string id, RequestRegularizationDto model)
        {
            if (model == null)
            {
                await LoadDropdowns();
                ViewBag.IsAdmin = _isAdmin;
                return View(model);
            }

            model.TenantId = _tenantId;

            // Never trust a client-supplied EmployeeId for a self-service
            // user - always overwrite it with the one resolved from the
            // caller's own session; the Application layer then verifies
            // this actually matches the request being resubmitted. Only
            // admin/HR keep the posted value.
            model.EmployeeId = _isAdmin
                ? (model.EmployeeId ?? _employeeId ?? string.Empty)
                : (_employeeId ?? string.Empty);

            try
            {
                var result = await _apiService.PutAsync<RequestRegularizationDto, AttendanceRegularizationDto>(
                    $"AttendanceRegularization/{id}/resubmit?resubmittedBy={Uri.EscapeDataString(_userId ?? string.Empty)}",
                    model);

                if (result == null)
                {
                    TempData["GlobalError"] = "Unable to resubmit regularization request.";
                    return RedirectToAction(nameof(Resubmit), new { id });
                }

                TempData["Success"] = "Regularization request resubmitted for approval.";

                return _isAdmin
                    ? RedirectToAction(nameof(Index))
                    : RedirectToAction(nameof(MyRequests));
            }
            catch (ApiException ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
                return RedirectToAction(nameof(Resubmit), new { id });
            }
            catch (Exception ex)
            {
                TempData["GlobalError"] = ex.Message;
                return RedirectToAction(nameof(Resubmit), new { id });
            }
        }

        // The queue of regularization requests currently awaiting THIS
        // logged-in user's action - as Reporting Manager (Level 1),
        // Department Head (Level 2), or HR (Level 3). Available to anyone,
        // admin or self-service, since org hierarchy is independent of
        // login role.
        [HttpGet]
        public async Task<IActionResult> MyApprovals()
        {
            await LoadDropdowns();

            ViewBag.IsAdmin = _isAdmin;
            ViewBag.ListTitle = "Pending My Approval";
            await SetApprovalViewBagAsync();

            var url =
                $"AttendanceRegularization/pending-for-approver?employeeId={Uri.EscapeDataString(_employeeId ?? string.Empty)}" +
                $"&userId={Uri.EscapeDataString(_userId ?? string.Empty)}";

            var data = await _apiService.GetAsync<List<AttendanceRegularizationDto>>(url);

            return View("Index", data);
        }

        // Exposed to Index.cshtml so it can decide, per row, whether the
        // Approve/Reject/Send Back buttons should even be shown to the
        // person currently viewing the list - the API still re-checks this
        // for real (via IsAuthorizedForLevelAsync/IsHrApproverAsync) when a
        // button is actually clicked, so this is cosmetic-only.
        private async Task SetApprovalViewBagAsync()
        {
            ViewBag.CurrentEmployeeId = _employeeId;

            ViewBag.IsHR = await _apiService.GetAsync<bool>(
                $"AttendanceRegularization/is-hr-approver?userId={Uri.EscapeDataString(_userId ?? string.Empty)}");
        }

        [HttpPost]
        public async Task<IActionResult> Cancel(CancelRegularizationRequestDto model)
        {
            // Cancelling one's own pending request is a legitimate
            // self-service action, but a non-admin must not be able to
            // cancel someone else's request just by posting a different
            // AttendanceRegularizationId.
            if (!_isAdmin)
            {
                var existing = await _apiService.GetAsync<AttendanceRegularizationDto>(
                    $"AttendanceRegularization/{model.AttendanceRegularizationId}");

                if (existing == null || existing.EmployeeId != _employeeId)
                    return Json(new { success = false, message = "You can only cancel your own regularization requests." });
            }

            model.CancelledBy = _userId;
            model.TenantId = _tenantId;

            var result = await _apiService.PostAsync<CancelRegularizationRequestDto, bool>(
                "AttendanceRegularization/cancel", model);

            return Json(new { success = result });
        }

        #endregion

        #region Queries

        [HttpGet]
        public async Task<IActionResult> Pending()
        {
            ViewBag.IsAdmin = _isAdmin;
            await SetApprovalViewBagAsync();

            var data = await _apiService.GetAsync<List<AttendanceRegularizationDto>>("AttendanceRegularization/pending");

            return View("Index", data);
        }

        [HttpGet]
        public async Task<IActionResult> Approved()
        {
            ViewBag.IsAdmin = _isAdmin;
            await SetApprovalViewBagAsync();

            var data = await _apiService.GetAsync<List<AttendanceRegularizationDto>>("AttendanceRegularization/approved");

            return View("Index", data);
        }

        [HttpGet]
        public async Task<IActionResult> Rejected()
        {
            ViewBag.IsAdmin = _isAdmin;
            await SetApprovalViewBagAsync();

            var data = await _apiService.GetAsync<List<AttendanceRegularizationDto>>("AttendanceRegularization/rejected");

            return View("Index", data);
        }

        [HttpGet]
        public async Task<IActionResult> Cancelled()
        {
            ViewBag.IsAdmin = _isAdmin;
            await SetApprovalViewBagAsync();

            var data = await _apiService.GetAsync<List<AttendanceRegularizationDto>>("AttendanceRegularization/cancelled");

            return View("Index", data);
        }

        // Self-service: "my own regularization request history" - never
        // trusts a client-supplied employeeId, always the one resolved from
        // the caller's own session. Only admin/HR may look up another
        // employee's history (e.g. from the org-wide Index -> Details).
        [HttpGet]
        public async Task<IActionResult> MyRequests()
        {
            await LoadDropdowns();

            ViewBag.IsAdmin = _isAdmin;
            ViewBag.ListTitle = "My Regularization Requests";
            await SetApprovalViewBagAsync();

            if (string.IsNullOrEmpty(_employeeId))
                return View("Index", new List<AttendanceRegularizationDto>());

            var data = await _apiService.GetAsync<List<AttendanceRegularizationDto>>(
                $"AttendanceRegularization/employee/{_employeeId}");

            return View("Index", data);
        }

        [HttpPost]
        public async Task<IActionResult> Filter(AttendanceRegularizationFilterRequestDto model)
        {
            // Non-admins may only ever filter their own requests - never trust
            // a posted EmployeeId to widen the query beyond that.
            if (!_isAdmin)
                model.EmployeeId = _employeeId;

            ViewBag.IsAdmin = _isAdmin;
            await SetApprovalViewBagAsync();

            var data = await _apiService.PostAsync<AttendanceRegularizationFilterRequestDto, List<AttendanceRegularizationDto>>(
                "AttendanceRegularization/filter", model);

            return PartialView("_RegularizationList", data);
        }

        #endregion

        #region Dropdowns

        private async Task LoadDropdowns()
        {
            var employees = await _apiService.GetAsync<List<DropdownDto>>($"dropdown/employee");

            ViewBag.EmployeeList = new SelectList(employees, "Value", "Text");

            ViewBag.EmployeeNames = employees.ToDictionary(x => x.Value, x => x.Text);
        }

        #endregion
    }
}
