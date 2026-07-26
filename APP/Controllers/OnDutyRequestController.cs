using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using static APP.Helpers.EnumExtensions;

namespace APP.Controllers
{
    // NOTE: named exactly "OnDutyRequest" to match the menu-seeding string in
    // Domain/Helper/AppFeatureConstants.cs (ON_DUTY_REQUEST_CONTROLLER =
    // "OnDutyRequest") and the API route (api/ondutyrequest). This is a
    // dual-audience controller - both a plain Employee and HR/Admin can
    // reach every action here (see APP/Attributes/EssRestrictionAttribute.cs
    // - "OnDutyRequest" is deliberately NOT in AdminOnlyControllers and has
    // no entry in AdminOnlyActionsByController), same shape as
    // WfhRequestController. Unlike WFH, OD has no monthly cap, so there is
    // no "remaining days" panel on the Create form and no /remaining call.
    [JwtAuthorize]
    public class OnDutyRequestController : Controller
    {
        private readonly IApiService _apiService;
        private string _tenantId;
        private string _userId;

        // The employee record linked to whoever is logged in. A self-service
        // user always requests/cancels their own On Duty using this - never a
        // value picked from a dropdown or posted from the client.
        private readonly string? _employeeId;
        private readonly bool _isAdmin;

        public OnDutyRequestController(IApiService apiService)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
            _employeeId = SessionHelper.GetActiveEmployeeId;
            _isAdmin = SessionHelper.IsAdminRole();
        }

        #region Landing

        // Both a plain employee and HR/Admin land on "My Requests" first -
        // everyone (including HR) can have On Duty requests of their own,
        // and the org-wide/approval queues are one click away from there.
        // Same judgment call as WfhRequestController.Index.
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

            var data = await _apiService.GetAsync<List<OnDutyRequestDto>>("ondutyrequest/my");

            return View(data ?? new List<OnDutyRequestDto>());
        }

        #endregion

        #region Create

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.EmployeeDisplayName = SessionHelper.GetActiveFullName;
            ViewBag.NoEmployeeProfile = string.IsNullOrEmpty(_employeeId);

            return View(new CreateOnDutyRequestDto
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
        public async Task<IActionResult> Create(CreateOnDutyRequestDto model)
        {
            if (string.IsNullOrEmpty(_employeeId))
            {
                TempData["GlobalError"] = "Your login isn't linked to an employee profile, so you can't request On Duty.";
                return RedirectToAction(nameof(Create));
            }

            // Never trust a client-supplied EmployeeId - always overwrite it
            // with the one resolved from the caller's own session, exactly
            // like WfhRequestController.Create.
            model.EmployeeId = _employeeId;

            if (!ModelState.IsValid)
            {
                ViewBag.EmployeeDisplayName = SessionHelper.GetActiveFullName;
                ViewBag.NoEmployeeProfile = false;
                return View(model);
            }

            try
            {
                var result = await _apiService.PostAsync<CreateOnDutyRequestDto, OnDutyRequestDto>(
                    "ondutyrequest", model);

                if (result == null)
                {
                    TempData["GlobalError"] = "Unable to submit On Duty request.";
                    return RedirectToAction(nameof(Create));
                }

                TempData["Success"] = "On Duty request submitted successfully.";
                return RedirectToAction(nameof(MyRequests));
            }
            catch (ApiException ex)
            {
                // The overlap/other validation in the API comes back as an
                // exception message - surface it clearly, same pattern as
                // WfhRequestController.Create.
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
                return RedirectToAction(nameof(Create));
            }
            catch (Exception ex)
            {
                TempData["GlobalError"] = ex.Message;
                return RedirectToAction(nameof(Create));
            }
        }

        #endregion

        #region Details

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var data = await _apiService.GetAsync<OnDutyRequestDto>($"ondutyrequest/{id}");

            if (data == null)
                return NotFound();

            ViewBag.IsAdmin = _isAdmin;
            ViewBag.CurrentEmployeeId = _employeeId;

            return View(data);
        }

        #endregion

        #region Pending Approvals

        // The queue of On Duty requests currently awaiting THIS logged-in
        // user's action - as Reporting Manager or HR/Admin override.
        // Available to anyone, admin or self-service, since org hierarchy is
        // independent of login role (same rule as
        // WfhRequestController.PendingApprovals).
        [HttpGet]
        public async Task<IActionResult> PendingApprovals()
        {
            ViewBag.IsAdmin = _isAdmin;

            var data = await _apiService.GetAsync<List<OnDutyRequestDto>>("ondutyrequest/pending-for-approver");

            return View(data ?? new List<OnDutyRequestDto>());
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
                var result = await _apiService.PutAsync<OnDutyRequestDto>($"ondutyrequest/{id}/approve", new { });

                TempData[result != null ? "Success" : "GlobalError"] = result != null
                    ? "On Duty request approved successfully."
                    : "Unable to approve this On Duty request.";
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
                var result = await _apiService.PutAsync<OnDutyRequestDto>(
                    $"ondutyrequest/{id}/reject",
                    new OnDutyRejectRequestDto { Reason = reason });

                TempData[result != null ? "Success" : "GlobalError"] = result != null
                    ? "On Duty request rejected."
                    : "Unable to reject this On Duty request.";
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
        // given request (same as WfhRequestController.Cancel).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(string id)
        {
            try
            {
                var result = await _apiService.PutAsync<OnDutyRequestDto>($"ondutyrequest/{id}/cancel", new { });

                TempData[result != null ? "Success" : "GlobalError"] = result != null
                    ? "On Duty request cancelled."
                    : "Unable to cancel this On Duty request.";
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
        // WfhRequestController.GetErrorMessage.
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

                return "Unable to process this On Duty request.";
            }
            catch
            {
                return string.IsNullOrWhiteSpace(raw) ? "Unable to process this On Duty request." : raw;
            }
        }

        #endregion
    }
}
