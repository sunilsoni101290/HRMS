using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace APP.Controllers
{
    // NOTE: named exactly "CompOff" to match the menu-seeding string in
    // Domain/Helper/AppFeatureConstants.cs (COMP_OFF_CONTROLLER = "CompOff")
    // and the API route (api/compoff). Comp Off candidates are only ever
    // created by a background job (API/BackgroundServices/
    // CompOffDetectionService.cs) - there is deliberately no Create action
    // here (see CompOffCandidateDto's remarks and API/Controllers/
    // CompOffController.cs).
    //
    // This is a dual-audience controller, but unlike WfhRequest/OnDutyRequest/
    // ShortLeaveRequest (where a Reporting Manager might be logged in under
    // the plain self-service role), Comp Off review has no
    // reporting-manager angle - it is strictly HR/Admin. So only the
    // PendingReview action (the HR review queue) is admin-gated, via
    // APP/Attributes/EssRestrictionAttribute.cs's
    // AdminOnlyActionsByController["CompOff"] = { "PendingReview" } - the
    // rest of the controller (Index, MyHistory, Details) stays reachable by
    // a plain employee so they can see their own Comp Off credits.
    [JwtAuthorize]
    public class CompOffController : Controller
    {
        private readonly IApiService _apiService;

        // The employee record linked to whoever is logged in - used only to
        // decide the Index redirect target; MyHistory/Details themselves
        // resolve "who am I" server-side via the API (same as
        // WfhRequestController.MyRequests calling wfhrequest/my).
        private readonly string? _employeeId;
        private readonly bool _isAdmin;

        public CompOffController(IApiService apiService)
        {
            _apiService = apiService;
            _employeeId = SessionHelper.GetActiveEmployeeId;
            _isAdmin = SessionHelper.IsAdminRole();
        }

        #region Landing

        // HR/Admin land on the review queue (their real job here); a plain
        // employee lands on their own history instead, since PendingReview
        // is blocked for them by EssRestrictionAttribute regardless.
        public IActionResult Index()
        {
            return _isAdmin
                ? RedirectToAction(nameof(PendingReview))
                : RedirectToAction(nameof(MyHistory));
        }

        #endregion

        #region Pending Review (HR/Admin only)

        [HttpGet]
        public async Task<IActionResult> PendingReview(string? departmentId, string? search)
        {
            ViewBag.DepartmentId = departmentId;
            ViewBag.Search = search;

            var url =
                $"compoff/pending-review?departmentId={Uri.EscapeDataString(departmentId ?? string.Empty)}" +
                $"&search={Uri.EscapeDataString(search ?? string.Empty)}";

            var data = await _apiService.GetAsync<List<CompOffCandidateDto>>(url);

            return View(data ?? new List<CompOffCandidateDto>());
        }

        #endregion

        #region My History

        // The logged-in employee's own Comp Off candidates/history (pending
        // + reviewed) for transparency - read-only, no actions. HR/Admin can
        // also reach this to see their own credits, same as
        // WfhRequestController.MyRequests.
        [HttpGet]
        public async Task<IActionResult> MyHistory()
        {
            var data = await _apiService.GetAsync<List<CompOffCandidateDto>>("compoff/my");

            return View(data ?? new List<CompOffCandidateDto>());
        }

        #endregion

        #region Details

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var data = await _apiService.GetAsync<CompOffCandidateDto>($"compoff/{id}");

            if (data == null)
                return NotFound();

            ViewBag.IsAdmin = _isAdmin;
            ViewBag.CurrentEmployeeId = _employeeId;

            return View(data);
        }

        #endregion

        #region Workflow

        private IActionResult RedirectBackOrToPendingReview()
        {
            var referer = Request.Headers["Referer"].ToString();

            if (!string.IsNullOrEmpty(referer) && Url.IsLocalUrl(referer))
                return Redirect(referer);

            return RedirectToAction(nameof(PendingReview));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(string id)
        {
            try
            {
                var result = await _apiService.PutAsync<CompOffCandidateDto>($"compoff/{id}/approve", new { });

                TempData[result != null ? "Success" : "GlobalError"] = result != null
                    ? "Comp Off candidate approved successfully."
                    : "Unable to approve this Comp Off candidate.";
            }
            catch (ApiException ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
            }
            catch (Exception ex)
            {
                // PutAsync<T> throws a plain Exception/ApplicationException
                // (not ApiException) with the API's JSON body embedded in
                // the message - see GetErrorMessage's remarks below.
                TempData["GlobalError"] = GetErrorMessage(ex.Message);
            }

            return RedirectBackOrToPendingReview();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(string id, string reason)
        {
            try
            {
                var result = await _apiService.PutAsync<CompOffCandidateDto>(
                    $"compoff/{id}/reject",
                    new CompOffRejectRequestDto { Reason = reason });

                TempData[result != null ? "Success" : "GlobalError"] = result != null
                    ? "Comp Off candidate rejected."
                    : "Unable to reject this Comp Off candidate.";
            }
            catch (ApiException ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
            }
            catch (Exception ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.Message);
            }

            return RedirectBackOrToPendingReview();
        }

        #endregion

        #region Helpers

        // Handles both shapes seen in this codebase: pure JSON (from
        // ApiException.ResponseContent) AND a human-readable prefix in
        // front of the JSON such as "Bad Request (400): {...}" (from the
        // plain Exception/ApplicationException that ApiService's
        // HandleResponse throws for the single-generic PutAsync<T> overload
        // used by Approve/Reject) - same approach as
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

                return "Unable to process this Comp Off candidate.";
            }
            catch
            {
                return string.IsNullOrWhiteSpace(raw) ? "Unable to process this Comp Off candidate." : raw;
            }
        }

        #endregion
    }
}
