using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using static APP.Helpers.EnumExtensions;

namespace APP.Controllers
{
    // NOTE: named exactly "WfhRequest" to match the menu-seeding string in
    // Domain/Helper/AppFeatureConstants.cs (WFH_REQUEST_CONTROLLER =
    // "WfhRequest") and the API route (api/wfhrequest). This is a
    // dual-audience controller - both a plain Employee and HR/Admin can
    // reach every action here (see APP/Attributes/EssRestrictionAttribute.cs
    // - "WfhRequest" is deliberately NOT in AdminOnlyControllers and has no
    // entry in AdminOnlyActionsByController), same shape as
    // AttendanceRegularizationController's self-service actions.
    [JwtAuthorize]
    public class WfhRequestController : Controller
    {
        private readonly IApiService _apiService;
        private string _tenantId;
        private string _userId;

        // The employee record linked to whoever is logged in. A self-service
        // user always requests/cancels their own WFH using this - never a
        // value picked from a dropdown or posted from the client.
        private readonly string? _employeeId;
        private readonly bool _isAdmin;

        public WfhRequestController(IApiService apiService)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
            _employeeId = SessionHelper.GetActiveEmployeeId;
            _isAdmin = SessionHelper.IsAdminRole();
        }

        #region Landing

        // Both a plain employee and HR/Admin land on "My Requests" first -
        // everyone (including HR) can have WFH requests of their own, and
        // the org-wide/approval queues are one click away from there. Same
        // judgment call as AttendanceRegularizationController.Index, except
        // WFH has no separate admin-only org-wide list to branch to.
        public IActionResult Index()
        {
            return RedirectToAction(nameof(MyRequests));
        }

        #endregion

        #region My Requests

        [HttpGet]
        public async Task<IActionResult> MyRequests()
        {
            ViewBag.IsAdmin = _isAdmin;

            var data = await _apiService.GetAsync<List<WfhRequestDto>>("wfhrequest/my");

            return View(data ?? new List<WfhRequestDto>());
        }

        #endregion

        #region Create

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.EmployeeDisplayName = SessionHelper.GetActiveFullName;
            ViewBag.NoEmployeeProfile = string.IsNullOrEmpty(_employeeId);

            await LoadRemainingDaysAsync(DateTime.Today);

            return View(new CreateWfhRequestDto
            {
                // Self-service: always the logged-in employee, never a
                // pickable dropdown - the server ignores any other value
                // even if one were posted.
                EmployeeId = _employeeId ?? string.Empty,
                FromDate = DateTime.Today,
                ToDate = DateTime.Today
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateWfhRequestDto model)
        {
            if (string.IsNullOrEmpty(_employeeId))
            {
                TempData["GlobalError"] = "Your login isn't linked to an employee profile, so you can't request Work From Home.";
                return RedirectToAction(nameof(Create));
            }

            // Never trust a client-supplied EmployeeId - always overwrite it
            // with the one resolved from the caller's own session, exactly
            // like AttendanceRegularizationController.Create.
            model.EmployeeId = _employeeId;

            if (!ModelState.IsValid)
            {
                ViewBag.EmployeeDisplayName = SessionHelper.GetActiveFullName;
                ViewBag.NoEmployeeProfile = false;
                await LoadRemainingDaysAsync(model.FromDate == default ? DateTime.Today : model.FromDate);
                return View(model);
            }

            try
            {
                var result = await _apiService.PostAsync<CreateWfhRequestDto, WfhRequestDto>(
                    "wfhrequest", model);

                if (result == null)
                {
                    TempData["GlobalError"] = "Unable to submit Work From Home request.";
                    return RedirectToAction(nameof(Create));
                }

                TempData["Success"] = "Work From Home request submitted successfully.";
                return RedirectToAction(nameof(MyRequests));
            }
            catch (ApiException ex)
            {
                // The monthly-limit / overlap validation in the API comes
                // back as an exception message - surface it clearly, same
                // pattern as AttendanceRegularizationController.Create.
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
                return RedirectToAction(nameof(Create));
            }
            catch (Exception ex)
            {
                TempData["GlobalError"] = ex.Message;
                return RedirectToAction(nameof(Create));
            }
        }

        // Shows "X of Y WFH days used this month" (or "Unlimited" when
        // MaxWfhDaysPerMonth is null/0) on the Create form. Failing to load
        // this shouldn't block the employee from still submitting a request
        // - the API re-validates the real limit server-side regardless.
        private async Task LoadRemainingDaysAsync(DateTime forDate)
        {
            if (string.IsNullOrEmpty(_employeeId))
            {
                ViewBag.Remaining = null;
                return;
            }

            try
            {
                var url =
                    $"wfhrequest/remaining?employeeId={Uri.EscapeDataString(_employeeId)}" +
                    $"&month={forDate.Month}&year={forDate.Year}";

                ViewBag.Remaining = await _apiService.GetAsync<WfhRemainingDaysDto>(url);
            }
            catch
            {
                ViewBag.Remaining = null;
            }
        }

        #endregion

        #region Details

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var data = await _apiService.GetAsync<WfhRequestDto>($"wfhrequest/{id}");

            if (data == null)
                return NotFound();

            ViewBag.IsAdmin = _isAdmin;
            ViewBag.CurrentEmployeeId = _employeeId;

            return View(data);
        }

        #endregion

        #region Pending Approvals

        // The queue of WFH requests currently awaiting THIS logged-in
        // user's action - as Reporting Manager or HR/Admin override.
        // Available to anyone, admin or self-service, since org hierarchy is
        // independent of login role (same rule as
        // AttendanceRegularizationController.MyApprovals).
        [HttpGet]
        public async Task<IActionResult> PendingApprovals()
        {
            ViewBag.IsAdmin = _isAdmin;

            var data = await _apiService.GetAsync<List<WfhRequestDto>>("wfhrequest/pending-for-approver");

            return View(data ?? new List<WfhRequestDto>());
        }

        #endregion

        #region Workflow

        private IActionResult RedirectBackOrToPendingApprovals()
        {
            var referer = Request.Headers["Referer"].ToString();

            if (!string.IsNullOrEmpty(referer) && Url.IsLocalUrl(referer))
                return Redirect(referer);

            return RedirectToAction(nameof(PendingApprovals));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(string id)
        {
            try
            {
                var result = await _apiService.PutAsync<WfhRequestDto>($"wfhrequest/{id}/approve", new { });

                TempData[result != null ? "Success" : "GlobalError"] = result != null
                    ? "Work From Home request approved successfully."
                    : "Unable to approve this Work From Home request.";
            }
            catch (ApiException ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
            }
            catch (Exception ex)
            {
                // PutAsync<T> throws a plain Exception/ApplicationException
                // (not ApiException) with the API's JSON body embedded in
                // the message - see GetErrorMessage's remarks above.
                TempData["GlobalError"] = GetErrorMessage(ex.Message);
            }

            return RedirectBackOrToPendingApprovals();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(string id, string reason)
        {
            try
            {
                var result = await _apiService.PutAsync<WfhRequestDto>(
                    $"wfhrequest/{id}/reject",
                    new WfhRejectRequestDto { Reason = reason });

                TempData[result != null ? "Success" : "GlobalError"] = result != null
                    ? "Work From Home request rejected."
                    : "Unable to reject this Work From Home request.";
            }
            catch (ApiException ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
            }
            catch (Exception ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.Message);
            }

            return RedirectBackOrToPendingApprovals();
        }

        // Cancelling one's own pending request is a legitimate self-service
        // action; the API itself is the real authority on who may cancel a
        // given request (same as AttendanceRegularizationController.Cancel).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(string id)
        {
            try
            {
                var result = await _apiService.PutAsync<WfhRequestDto>($"wfhrequest/{id}/cancel", new { });

                TempData[result != null ? "Success" : "GlobalError"] = result != null
                    ? "Work From Home request cancelled."
                    : "Unable to cancel this Work From Home request.";
            }
            catch (ApiException ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
            }
            catch (Exception ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.Message);
            }

            var referer = Request.Headers["Referer"].ToString();

            if (!string.IsNullOrEmpty(referer) && Url.IsLocalUrl(referer))
                return Redirect(referer);

            return RedirectToAction(nameof(MyRequests));
        }

        #endregion

        #region Helpers

        // Handles both shapes seen in this codebase: pure JSON (from
        // ApiException.ResponseContent, thrown by the 2-generic
        // PostAsync/DeleteAsync overloads used by Create) AND a
        // human-readable prefix in front of the JSON such as
        // "Bad Request (400): {...}" (from the plain Exception/
        // ApplicationException that ApiService's HandleResponse throws for
        // the single-generic PutAsync<T> overload used by
        // Approve/Reject/Cancel) - same approach as
        // AttendancePolicyController.GetErrorMessage.
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

                return "Unable to process this Work From Home request.";
            }
            catch
            {
                return string.IsNullOrWhiteSpace(raw) ? "Unable to process this Work From Home request." : raw;
            }
        }

        #endregion
    }
}
