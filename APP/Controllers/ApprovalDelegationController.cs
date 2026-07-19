using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace APP.Controllers
{
    // Out-of-office proxy approver (Part 2). Deliberately NOT listed in
    // EssRestrictionAttribute.AdminOnlyControllers/AdminOnlyActionsByController
    // - any employee could plausibly be a Reporting Manager or Department
    // Head, independent of their login role (same reasoning already applied
    // to LeaveApplication's Approve/Reject/SendBack/MyApprovals actions), so
    // this entire controller stays reachable by everyone.
    [JwtAuthorize]
    public class ApprovalDelegationController : Controller
    {
        private readonly IApiService _apiService;
        private readonly string _tenantId;
        private readonly string _userId;
        private readonly string? _employeeId;

        public ApprovalDelegationController(IApiService apiService)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
            _employeeId = SessionHelper.GetActiveEmployeeId;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (string.IsNullOrEmpty(_employeeId))
            {
                TempData["GlobalError"] = "Your login isn't linked to an employee profile, so you have no delegations to manage.";
                return View(new List<ApprovalDelegationDto>());
            }

            var data = await _apiService.GetAsync<List<ApprovalDelegationDto>>($"ApprovalDelegation/employee/{_employeeId}");

            return View(data ?? new List<ApprovalDelegationDto>());
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (string.IsNullOrEmpty(_employeeId))
            {
                TempData["GlobalError"] = "Your login isn't linked to an employee profile, so you can't assign a delegate.";
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdowns();

            return View(new CreateApprovalDelegationRequestDto
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateApprovalDelegationRequestDto model)
        {
            if (string.IsNullOrEmpty(_employeeId))
            {
                TempData["GlobalError"] = "Your login isn't linked to an employee profile, so you can't assign a delegate.";
                return RedirectToAction(nameof(Index));
            }

            // Never trust a client-supplied DelegatorEmployeeId - always the
            // caller's own linked employee, exactly like
            // LeaveApplicationController.Create's EmployeeId handling.
            model.DelegatorEmployeeId = _employeeId;
            model.TenantId = _tenantId;
            model.CreatedBy = _userId;

            if (!ModelState.IsValid)
            {
                await LoadDropdowns();
                return View(model);
            }

            try
            {
                var result = await _apiService.PostAsync<CreateApprovalDelegationRequestDto, ApprovalDelegationDto>(
                    "ApprovalDelegation", model);

                if (result == null)
                {
                    TempData["GlobalError"] = "Unable to create the delegation.";
                    await LoadDropdowns();
                    return View(model);
                }

                TempData["Success"] = "Delegate assigned successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (ApiException ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
                await LoadDropdowns();
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["GlobalError"] = ex.Message;
                await LoadDropdowns();
                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Revoke(string id)
        {
            var result = await _apiService.PostAsync<bool>(
                $"ApprovalDelegation/{id}/revoke?revokedBy={Uri.EscapeDataString(_userId ?? string.Empty)}",
                new { });

            return Json(new
            {
                success = result,
                message = result ? "Delegation revoked." : "Unable to revoke this delegation."
            });
        }

        private async Task LoadDropdowns()
        {
            var employees = await _apiService.GetAsync<List<DropdownDto>>("dropdown/employee");

            // Excluding myself client-side is a convenience only - the
            // service-layer validation (ApprovalDelegationService.CreateAsync)
            // is what actually rejects self-delegation.
            var delegateOptions = employees?.Where(x => x.Value != _employeeId).ToList() ?? new List<DropdownDto>();

            ViewBag.EmployeeList = new SelectList(delegateOptions, "Value", "Text");
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

                return "Unable to process this request.";
            }
            catch
            {
                return "Unable to process this request.";
            }
        }
    }
}
