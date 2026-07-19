using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace APP.Controllers
{
    [JwtAuthorize]
    public class LeaveApplicationController : Controller
    {
        private readonly IApiService _apiService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private string _tenantId;
        private string _userId;
        private string _companyId;
        private string _branchId;

        // The employee record linked to whoever is logged in. When the user
        // is a plain employee (not admin/HR) this is always the one used for
        // "Apply Leave" - never a value picked from a dropdown or posted from
        // the client - so an employee can only ever apply leave for themselves.
        private readonly string? _employeeId;
        private readonly bool _isAdmin;

        public LeaveApplicationController(IApiService apiService, IHttpContextAccessor httpContextAccessor)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
            _companyId = SessionHelper.GetActiveCompanyId;
            _branchId = SessionHelper.GetActiveBranchId;
            _employeeId = SessionHelper.GetActiveEmployeeId;
            _isAdmin = SessionHelper.IsAdminRole();
            _httpContextAccessor = httpContextAccessor;
        }


        #region CRUD

        public async Task<IActionResult> Index()
        {
            ViewBag.IsAdmin = _isAdmin;
            await SetApprovalViewBagAsync();
            await LoadDropdowns();
            var data = await _apiService.GetAsync<List<LeaveApplicationDto>>("LeaveApplication");

            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();

            ViewBag.IsAdmin = _isAdmin;
            ViewBag.EmployeeDisplayName = SessionHelper.GetActiveFullName;
            ViewBag.NoEmployeeProfile = !_isAdmin && string.IsNullOrEmpty(_employeeId);

            // Self-service users can see up front (not just after a failed
            // submit) that they already have a leave request awaiting
            // approval, so the form can warn them and disable Submit instead
            // of letting them fill the whole form only to be rejected.
            ViewBag.HasPendingLeave = !_isAdmin && await EmployeeHasPendingLeave(_employeeId);

            return View(new ApplyLeaveRequestDto
            {
                // Self-service users always apply for their own linked employee
                // record. Admin/HR still pick from the dropdown in the view.
                EmployeeId = _isAdmin ? string.Empty : (_employeeId ?? string.Empty),
                FromDate = DateTime.Today,
                ToDate = DateTime.Today
            });
        }

        // Lets the Create view (admin's Employee dropdown included) ask,
        // via AJAX, whether a given employee already has a leave request
        // awaiting approval - so the alert/disabled Submit button can react
        // live instead of only after a failed post.
        [HttpGet]
        public async Task<JsonResult> HasPendingLeave(string employeeId)
        {
            return Json(new { hasPending = await EmployeeHasPendingLeave(employeeId) });
        }

        // Lets the Create/Edit view ask, as the user picks From/To dates, how
        // many days that range is actually worth - week-offs and holidays for
        // the tenant don't count against leave balance, so this must match
        // exactly what the server will persist on submit.
        [HttpGet]
        public async Task<JsonResult> CalculateTotalDays(DateTime fromDate, DateTime toDate, bool isHalfDay)
        {
            var url =
                $"LeaveApplication/calculate-days?fromDate={fromDate:yyyy-MM-dd}" +
                $"&toDate={toDate:yyyy-MM-dd}" +
                $"&isHalfDay={isHalfDay}" +
                $"&tenantId={Uri.EscapeDataString(_tenantId ?? string.Empty)}";

            var result = await _apiService.GetAsync<TotalDaysResultDto>(url);

            return Json(new { totalDays = result?.TotalDays ?? 0 });
        }

        private async Task<bool> EmployeeHasPendingLeave(string? employeeId)
        {
            if (string.IsNullOrEmpty(employeeId))
                return false;

            var leaves = await _apiService.GetAsync<List<LeaveApplicationDto>>(
                $"LeaveApplication/employee/{employeeId}");

            return leaves != null &&
                leaves.Any(x => x.Status == EnumExtensions.ApprovalStatus.Pending);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            ApplyLeaveRequestDto model)
        {
            if (model==null)
            {
                await LoadDropdowns();
                ViewBag.IsAdmin = _isAdmin;
                ViewBag.EmployeeDisplayName = SessionHelper.GetActiveFullName;
                return View(model);
            }

            // Never trust a client-supplied EmployeeId for a self-service user -
            // always overwrite it with the one resolved from their own session,
            // exactly like the attendance punch flow. Only admin/HR (who use the
            // dropdown to file leave on someone else's behalf) keep the posted value.
            if (!_isAdmin)
            {
                if (string.IsNullOrEmpty(_employeeId))
                {
                    TempData["GlobalError"] = "Your login isn't linked to an employee profile, so you can't apply leave.";
                    return RedirectToAction(nameof(Create));
                }

                model.EmployeeId = _employeeId;
            }

            model.TenantId = _tenantId;
            model.CreatedBy = _userId;
            model.CompanyId = _companyId;
            model.BranchId = _branchId;

            #region Upload Image
            // Upload Folder
            string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/LeaveDocument");

            // Create Folder if not exists
            if (!Directory.Exists(uploadFolder))
                Directory.CreateDirectory(uploadFolder);

            // Upload Image
            if (model.UploadDocument != null && model.UploadDocument.Length > 0)
            {
                
                // Get File Extension
                var extension = Path.GetExtension(model.UploadDocument.FileName).ToLower();

               
                // Generate Unique File Name
                string fileName = DateTime.Now.Hour + DateTime.Now.Minute + DateTime.Now.Second + DateTime.Now.Millisecond + extension;

                // Full File Path
                string filePath = Path.Combine(uploadFolder, fileName);

                // Save File
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.UploadDocument.CopyToAsync(stream);
                }

                // Save Relative Path in DB
                model.DocumentUrl = "/LeaveDocument/" + fileName;
            }

            #endregion

            try
            {
                var result = await _apiService.PostAsync<ApplyLeaveRequestDto, LeaveApplicationDto>("LeaveApplication", model);

                if (result == null)
                {
                    TempData["GlobalError"] = "Unable to submit leave application.";
                    return RedirectToAction(nameof(Create));
                }

                TempData["Success"] = "Leave applied successfully.";

                // Admin/HR land on the org-wide list; a self-service employee
                // should stay inside their own portal and see their own history,
                // not the shared admin list.
                if (!_isAdmin)
                    return RedirectToAction(nameof(EmployeeLeaves), new { employeeId = _employeeId });

                return RedirectToAction(nameof(Index));
            }
            catch (ApiException ex)
            {
                // e.g. "you already have a leave request awaiting approval" -
                // a real business-rule rejection from the API, not a crash.
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

                return "Unable to submit leave application.";
            }
            catch
            {
                return "Unable to submit leave application.";
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var data =
                await _apiService.GetAsync<LeaveApplicationDto>(
                    $"LeaveApplication/{id}");

            await LoadDropdowns();

            ViewBag.IsAdmin = _isAdmin;
            ViewBag.CurrentEmployeeId = _employeeId;

            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var data =
                await _apiService.GetAsync<ApplyLeaveRequestDto>(
                    $"LeaveApplication/{id}");

            await LoadDropdowns();

            return View(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            string id,
            ApplyLeaveRequestDto model)
        {
            if (model==null)
            {
                await LoadDropdowns();
                return View(model);
            }

            model.TenantId = _tenantId;
            model.CreatedBy = _userId;
            model.ModifiedBy = _tenantId;
            model.ModifiedOn = DateTime.Now;
            model.CompanyId = _companyId;
            model.BranchId = _branchId;

            var result =
                await _apiService.PutAsync<
                    ApplyLeaveRequestDto,
                    bool>(
                    $"LeaveApplication/{id}",
                    model);

            if (result)
            {
                TempData["Success"] = "Leave updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            TempData["Error"] = "Unable to update leave.";

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var result =
                await _apiService.DeleteAsync(
                    $"LeaveApplication/{id}");

            return Json(new
            {
                success = result
            });
        }

        #endregion

        #region Workflow

        // Approve/Reject/Send Back are no longer gated by "_isAdmin" - the
        // API now enforces the real multi-level rule (Reporting Manager for
        // Level 1, Department Head for Level 2, anyone with the HR role for
        // Level 3) regardless of whether the acting user's own login role
        // happens to be flagged admin. Any unauthorized attempt is rejected
        // server-side with a clear message rather than a bare Forbid.

        // Approve/Reject/Send Back can be triggered from more than one list
        // (the org-wide admin Index, "My Leave History", or "Pending My
        // Approval") - send the user back to whichever of those they acted
        // from instead of always dropping them on MyApprovals. Falls back to
        // MyApprovals only when there's no safe local page to return to.
        private IActionResult RedirectBackOrToMyApprovals()
        {
            var referer = Request.Headers["Referer"].ToString();

            if (!string.IsNullOrEmpty(referer) && Url.IsLocalUrl(referer))
                return Redirect(referer);

            return RedirectToAction(nameof(MyApprovals));
        }

        [HttpPost]
        public async Task<IActionResult> Approve(
            ApproveLeaveRequestDto model)
        {
            if (model == null)
                return RedirectToAction(nameof(MyApprovals));

            model.CreatedBy = _userId;
            model.ApprovedBy = _userId;
            model.TenantId = _tenantId;

            try
            {
                var result = await _apiService.PostAsync<ApproveLeaveRequestDto, bool>(
                    "LeaveApplication/approve",
                    model);

                TempData[result ? "Success" : "GlobalError"] = result
                    ? "Leave application approved successfully."
                    : "Unable to approve this leave request.";
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
        public async Task<IActionResult> Reject(
            RejectLeaveRequestDto model)
        {
            model.RejectedBy = _userId;
            model.TenantId = _tenantId;

            try
            {
                var result = await _apiService.PostAsync<RejectLeaveRequestDto, bool>(
                    "LeaveApplication/reject",
                    model);

                TempData[result ? "Success" : "GlobalError"] = result
                    ? "Leave application rejected."
                    : "Unable to reject this leave request.";
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
        public async Task<IActionResult> SendBack(
            SendBackLeaveRequestDto model)
        {
            model.SentBackBy = _userId;

            try
            {
                var result = await _apiService.PostAsync<SendBackLeaveRequestDto, bool>(
                    "LeaveApplication/send-back",
                    model);

                TempData[result ? "Success" : "GlobalError"] = result
                    ? "Leave application sent back to the employee."
                    : "Unable to send this leave request back.";
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
            var data = await _apiService.GetAsync<LeaveApplicationDto>($"LeaveApplication/{id}");

            if (data == null)
                return NotFound();

            if (data.EmployeeId != _employeeId && !_isAdmin)
                return Forbid();

            if (data.Status != EnumExtensions.ApprovalStatus.ReturnedToEmployee)
            {
                TempData["GlobalError"] = "Only a leave request that was sent back can be resubmitted.";
                return RedirectToAction(nameof(EmployeeLeaves), new { employeeId = data.EmployeeId });
            }

            await LoadDropdowns();

            ViewBag.IsAdmin = _isAdmin;
            ViewBag.EmployeeDisplayName = data.EmployeeName;
            ViewBag.SendBackReason = data.SendBackReason;

            return View(new ApplyLeaveRequestDto
            {
                Id = data.Id,
                EmployeeId = data.EmployeeId,
                LeaveTypeId = data.LeaveTypeId,
                FromDate = data.FromDate,
                ToDate = data.ToDate,
                IsHalfDay = data.IsHalfDay,
                HalfDayType = data.HalfDayType,
                Reason = data.Reason,
                DocumentUrl = data.DocumentUrl
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Resubmit(string id, ApplyLeaveRequestDto model)
        {
            if (model == null)
            {
                await LoadDropdowns();
                ViewBag.IsAdmin = _isAdmin;
                return View(model);
            }

            model.TenantId = _tenantId;

            // Never trust a client-supplied EmployeeId for a self-service user -
            // the hidden field in the Resubmit form is only a display
            // convenience and is fully attacker-controlled in the browser.
            // Always overwrite it with the one resolved from the caller's own
            // session; the Application layer then verifies this actually
            // matches the leave being resubmitted, so a self-service user
            // can't resubmit (or claim) someone else's returned leave just by
            // editing the posted value. Only admin/HR keep the posted value.
            model.EmployeeId = _isAdmin
                ? (model.EmployeeId ?? _employeeId ?? string.Empty)
                : (_employeeId ?? string.Empty);

            #region Upload Image

            string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/LeaveDocument");

            if (!Directory.Exists(uploadFolder))
                Directory.CreateDirectory(uploadFolder);

            if (model.UploadDocument != null && model.UploadDocument.Length > 0)
            {
                var extension = Path.GetExtension(model.UploadDocument.FileName).ToLower();
                string fileName = DateTime.Now.Hour + DateTime.Now.Minute + DateTime.Now.Second + DateTime.Now.Millisecond + extension;
                string filePath = Path.Combine(uploadFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.UploadDocument.CopyToAsync(stream);
                }

                model.DocumentUrl = "/LeaveDocument/" + fileName;
            }

            #endregion

            try
            {
                var result = await _apiService.PutAsync<ApplyLeaveRequestDto, LeaveApplicationDto>(
                    $"LeaveApplication/{id}/resubmit?resubmittedBy={Uri.EscapeDataString(_userId ?? string.Empty)}",
                    model);

                if (result == null)
                {
                    TempData["GlobalError"] = "Unable to resubmit leave application.";
                    return RedirectToAction(nameof(Resubmit), new { id });
                }

                TempData["Success"] = "Leave request resubmitted for approval.";

                return _isAdmin
                    ? RedirectToAction(nameof(Index))
                    : RedirectToAction(nameof(EmployeeLeaves), new { employeeId = _employeeId });
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

        // The queue of leave requests currently awaiting THIS logged-in
        // user's action - as Reporting Manager (Level 1), Department Head
        // (Level 2), or HR (Level 3). Available to anyone, admin or
        // self-service, since org hierarchy is independent of login role.
        [HttpGet]
        public async Task<IActionResult> MyApprovals()
        {
            await LoadDropdowns();

            ViewBag.IsAdmin = _isAdmin;
            ViewBag.ListTitle = "Pending My Approval";
            await SetApprovalViewBagAsync();

            var url =
                $"LeaveApplication/pending-for-approver?employeeId={Uri.EscapeDataString(_employeeId ?? string.Empty)}" +
                $"&userId={Uri.EscapeDataString(_userId ?? string.Empty)}";

            var data = await _apiService.GetAsync<List<LeaveApplicationDto>>(url);

            return View("Index", data);
        }

        // Exposed to Index.cshtml so it can decide, per row, whether the
        // Approve/Reject/Send Back buttons should be shown to the person
        // currently viewing the list - the API still re-checks for real
        // when a button is actually clicked, this is purely a UI convenience.
        // ViewBag.IsHR now reflects the same permission-based check the API
        // enforces (IsHrApproverAsync) instead of a substring match on the
        // session's cached role display name.
        private async Task SetApprovalViewBagAsync()
        {
            ViewBag.CurrentEmployeeId = _employeeId;

            ViewBag.IsHR = await _apiService.GetAsync<bool>(
                $"LeaveApplication/is-hr-approver?userId={Uri.EscapeDataString(_userId ?? string.Empty)}");
        }

        [HttpPost]
        public async Task<IActionResult> Cancel(
            CancelLeaveRequestDto model)
        {
            // Cancelling one's own pending leave is a legitimate self-service
            // action, but a non-admin must not be able to cancel someone
            // else's request just by posting a different LeaveApplicationId.
            if (!_isAdmin)
            {
                var existing = await _apiService.GetAsync<LeaveApplicationDto>(
                    $"LeaveApplication/{model.LeaveApplicationId}");

                if (existing == null || existing.EmployeeId != _employeeId)
                    return Json(new { success = false, message = "You can only cancel your own leave requests." });
            }

            model.CancelledBy = _userId;
            model.TenantId = _tenantId;

            var result =
                await _apiService.PostAsync<
                    CancelLeaveRequestDto,
                    bool>(
                    "LeaveApplication/cancel",
                    model);

            return Json(new
            {
                success = result
            });
        }

        #endregion

        #region Queries

        [HttpGet]
        public async Task<IActionResult> Pending()
        {
            ViewBag.IsAdmin = _isAdmin;
            await SetApprovalViewBagAsync();

            var data =
                await _apiService.GetAsync<List<LeaveApplicationDto>>(
                    "LeaveApplication/pending");

            return View("Index", data);
        }

        [HttpGet]
        public async Task<IActionResult> Approved()
        {
            ViewBag.IsAdmin = _isAdmin;
            await SetApprovalViewBagAsync();

            var data =
                await _apiService.GetAsync<List<LeaveApplicationDto>>(
                    "LeaveApplication/approved");

            return View("Index", data);
        }

        [HttpGet]
        public async Task<IActionResult> Rejected()
        {
            ViewBag.IsAdmin = _isAdmin;
            await SetApprovalViewBagAsync();

            var data =
                await _apiService.GetAsync<List<LeaveApplicationDto>>(
                    "LeaveApplication/rejected");

            return View("Index", data);
        }

        [HttpGet]
        public async Task<IActionResult> Cancelled()
        {
            ViewBag.IsAdmin = _isAdmin;
            await SetApprovalViewBagAsync();

            var data =
                await _apiService.GetAsync<List<LeaveApplicationDto>>(
                    "LeaveApplication/cancelled");

            return View("Index", data);
        }

        [HttpGet]
        public async Task<IActionResult> EmployeeLeaves(
            string employeeId)
        {
            // Never trust a client-supplied employeeId for a self-service
            // user - always use the one resolved from their own session, so
            // an employee can't view someone else's leave history just by
            // editing the querystring. Only admin/HR may look up another
            // employee's history this way.
            if (!_isAdmin)
                employeeId = _employeeId ?? string.Empty;

            await LoadDropdowns();

            ViewBag.IsAdmin = _isAdmin;
            ViewBag.ListTitle = _isAdmin ? "Leave Application Management" : "My Leave History";
            await SetApprovalViewBagAsync();

            if (string.IsNullOrEmpty(employeeId))
                return View("Index", new List<LeaveApplicationDto>());

            var data =
                await _apiService.GetAsync<List<LeaveApplicationDto>>(
                    $"LeaveApplication/employee/{employeeId}");

            return View("Index", data);
        }

        [HttpPost]
        public async Task<IActionResult> Filter(
            LeaveApplicationFilterRequestDto model)
        {
            var data =
                await _apiService.PostAsync<
                    LeaveApplicationFilterRequestDto,
                    List<LeaveApplicationDto>>(
                    "LeaveApplication/filter",
                    model);

            return PartialView("_LeaveList", data);
        }

        #endregion

        #region Calendar

        // Month-grid view of approved leaves. Admin/HR sees org-wide
        // (optionally narrowed by the Department dropdown); a self-service
        // employee is scoped server-side to their own department so they
        // can plan around teammates without seeing every other
        // department's leave - same admin-vs-ESS convention as
        // EmployeeLeaves/MyApprovals elsewhere in this controller.
        [HttpGet]
        public async Task<IActionResult> Calendar(int? year, int? month, string? departmentId)
        {
            var today = DateTime.Today;

            int y = year ?? today.Year;
            int m = month ?? today.Month;

            // Normalize an out-of-range month (from Prev/Next navigation
            // crossing a year boundary, or a hand-edited querystring)
            // instead of letting `new DateTime(y, m, 1)` throw downstream.
            while (m < 1) { m += 12; y -= 1; }
            while (m > 12) { m -= 12; y += 1; }

            ViewBag.IsAdmin = _isAdmin;
            ViewBag.SelectedDepartmentId = departmentId;

            if (_isAdmin)
            {
                var departments = await _apiService.GetAsync<List<DropdownDto>>("dropdown/department");
                ViewBag.DepartmentList = new SelectList(departments, "Value", "Text", departmentId);
            }

            var url =
                $"LeaveApplication/calendar?year={y}&month={m}" +
                $"&employeeId={Uri.EscapeDataString(_employeeId ?? string.Empty)}" +
                $"&isAdmin={(_isAdmin ? "true" : "false")}" +
                $"&tenantId={Uri.EscapeDataString(_tenantId ?? string.Empty)}";

            if (_isAdmin && !string.IsNullOrEmpty(departmentId))
                url += $"&departmentId={Uri.EscapeDataString(departmentId)}";

            var data = await _apiService.GetAsync<LeaveCalendarResponseDto>(url);

            return View(data ?? new LeaveCalendarResponseDto { Year = y, Month = m, MonthName = new DateTime(y, m, 1).ToString("MMMM yyyy") });
        }

        #endregion

        #region Dashboard

        [HttpGet]
        public async Task<JsonResult> GetCounts()
        {
            var pending =
                await _apiService.GetAsync<int>(
                    "LeaveApplication/count/pending");

            var approved =
                await _apiService.GetAsync<int>(
                    "LeaveApplication/count/approved");

            var today =
                await _apiService.GetAsync<int>(
                    "LeaveApplication/count/today");

            return Json(new
            {
                Pending = pending,
                Approved = approved,
                Today = today
            });
        }

        #endregion

        #region Dropdowns

        private async Task LoadDropdowns()
        {
            // Employee
            var employees = await _apiService
                .GetAsync<List<DropdownDto>>($"dropdown/employee");

            ViewBag.EmployeeList = new SelectList(
                employees,
                "Value",
                "Text");

            ViewBag.EmployeeNames = employees.ToDictionary(x => x.Value, x => x.Text);

            // Leave Type
            var leaveTypes = await _apiService
                .GetAsync<List<DropdownDto>>($"dropdown/leave-type");

            ViewBag.LeaveTypeList = new SelectList(
                leaveTypes,
                "Value",
                "Text");

            ViewBag.LeaveTypeNames = leaveTypes.ToDictionary(x => x.Value, x => x.Text);
        }

        #endregion
    }
}
