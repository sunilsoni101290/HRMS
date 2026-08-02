using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace APP.Controllers
{
    #region Payslip Request Controller

    // Employee -> Reporting Manager -> Finance payslip request workflow -
    // supersedes the old direct-access path on PayrollController
    // (MyPayslips/Payslip, now locked down to Admin only - see
    // PayrollController and APP/Attributes/EssRestrictionAttribute.cs).
    // Dual-audience controller, same shape as WfhRequestController -
    // deliberately NOT in EssRestrictionAttribute's AdminOnlyControllers/
    // AdminOnlyActionsByController, since a Reporting Manager or Finance
    // staff member may be logged in under the plain self-service role (org
    // hierarchy/permissions are independent of login role in this system).
    // The service-layer permission checks are the real authority.
    [JwtAuthorize]
    public class PayslipRequestController : Controller
    {
        private readonly IApiService _apiService;
        private string _tenantId;
        private string _userId;
        private readonly string? _employeeId;
        private readonly bool _isAdmin;

        private static readonly string[] AllowedExtensions = { ".pdf" };
        private const long MaxUploadBytes = 5 * 1024 * 1024;

        public PayslipRequestController(IApiService apiService)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
            _employeeId = SessionHelper.GetActiveEmployeeId;
            _isAdmin = SessionHelper.IsAdminRole();
        }

        #region Landing

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

            if (string.IsNullOrEmpty(_employeeId))
            {
                ViewBag.NoEmployeeProfile = true;
                return View(new List<PayslipRequestDto>());
            }

            var data = await _apiService.GetAsync<List<PayslipRequestDto>>("paysliprequest/my");

            return View(data ?? new List<PayslipRequestDto>());
        }

        #endregion

        #region Create

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.EmployeeDisplayName = SessionHelper.GetActiveFullName;
            ViewBag.NoEmployeeProfile = string.IsNullOrEmpty(_employeeId);

            await LoadEligiblePeriodsAsync();

            return View(new CreatePayslipRequestDto { EmployeeId = _employeeId ?? string.Empty });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreatePayslipRequestDto model)
        {
            if (string.IsNullOrEmpty(_employeeId))
            {
                TempData["GlobalError"] = "Your login isn't linked to an employee profile, so you can't request a payslip.";
                return RedirectToAction(nameof(Create));
            }

            // Never trust a client-supplied EmployeeId.
            model.EmployeeId = _employeeId;

            if (!ModelState.IsValid)
            {
                ViewBag.EmployeeDisplayName = SessionHelper.GetActiveFullName;
                ViewBag.NoEmployeeProfile = false;
                await LoadEligiblePeriodsAsync();
                return View(model);
            }

            try
            {
                var result = await _apiService.PostAsync<CreatePayslipRequestDto, PayslipRequestDto>(
                    "paysliprequest", model);

                if (result == null)
                {
                    TempData["GlobalError"] = "Unable to submit payslip request.";
                    return RedirectToAction(nameof(Create));
                }

                TempData["Success"] = "Payslip request submitted successfully.";
                return RedirectToAction(nameof(MyRequests));
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

        // Own payroll periods, filtered client-side (best-effort UX only -
        // the API is the real authority on duplicate/overlap prevention).
        private async Task LoadEligiblePeriodsAsync()
        {
            if (string.IsNullOrEmpty(_employeeId))
            {
                ViewBag.PeriodList = new List<PayrollListDto>();
                return;
            }

            try
            {
                var payrolls = await _apiService.GetAsync<List<PayrollListDto>>("payroll");
                var myPayrolls = (payrolls ?? new List<PayrollListDto>())
                    .Where(p => p.EmployeeId == _employeeId)
                    .OrderByDescending(p => p.SalaryYear).ThenByDescending(p => p.SalaryMonth)
                    .ToList();

                var myRequests = await _apiService.GetAsync<List<PayslipRequestDto>>("paysliprequest/my");
                var nonTerminalStatuses = new[]
                {
                    (int)APP.Helpers.EnumExtensions.PayslipRequestStatus.PendingManagerApproval,
                    (int)APP.Helpers.EnumExtensions.PayslipRequestStatus.ApprovedByManager,
                    (int)APP.Helpers.EnumExtensions.PayslipRequestStatus.PendingFinanceAction,
                    (int)APP.Helpers.EnumExtensions.PayslipRequestStatus.PayslipGenerated
                };
                var openPayrollIds = (myRequests ?? new List<PayslipRequestDto>())
                    .Where(r => nonTerminalStatuses.Contains(r.Status))
                    .Select(r => r.PayrollId)
                    .ToHashSet();

                ViewBag.PeriodList = myPayrolls.Where(p => !openPayrollIds.Contains(p.Id)).ToList();
            }
            catch
            {
                ViewBag.PeriodList = new List<PayrollListDto>();
            }
        }

        #endregion

        #region Details

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            try
            {
                var data = await _apiService.GetAsync<PayslipRequestDto>($"paysliprequest/{id}");

                if (data == null)
                    return NotFound();

                ViewBag.IsAdmin = _isAdmin;
                ViewBag.CurrentEmployeeId = _employeeId;

                return View(data);
            }
            catch (Exception)
            {
                return NotFound();
            }
        }

        #endregion

        #region Manager Approvals

        [HttpGet]
        public async Task<IActionResult> PendingApprovals()
        {
            ViewBag.IsAdmin = _isAdmin;

            var data = await _apiService.GetAsync<List<PayslipRequestDto>>("paysliprequest/pending-for-manager");

            return View(data ?? new List<PayslipRequestDto>());
        }

        private IActionResult RedirectBackOrTo(string action)
        {
            var referer = Request.Headers["Referer"].ToString();

            if (!string.IsNullOrEmpty(referer) && Url.IsLocalUrl(referer))
                return Redirect(referer);

            return RedirectToAction(action);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManagerApprove(string id, string? remarks)
        {
            try
            {
                var result = await _apiService.PutAsync<PayslipRequestDto>(
                    $"paysliprequest/{id}/manager-approve", new PayslipRequestActionDto { Remarks = remarks });

                TempData[result != null ? "Success" : "GlobalError"] = result != null
                    ? "Payslip request approved and forwarded to Finance."
                    : "Unable to approve this payslip request.";
            }
            catch (ApiException ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
            }
            catch (Exception ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.Message);
            }

            return RedirectBackOrTo(nameof(PendingApprovals));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManagerReject(string id, string reason)
        {
            try
            {
                var result = await _apiService.PutAsync<PayslipRequestDto>(
                    $"paysliprequest/{id}/manager-reject", new PayslipRequestRejectDto { Reason = reason });

                TempData[result != null ? "Success" : "GlobalError"] = result != null
                    ? "Payslip request rejected."
                    : "Unable to reject this payslip request.";
            }
            catch (ApiException ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
            }
            catch (Exception ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.Message);
            }

            return RedirectBackOrTo(nameof(PendingApprovals));
        }

        #endregion

        #region Finance Queue

        [HttpGet]
        public async Task<IActionResult> FinanceQueue()
        {
            ViewBag.IsAdmin = _isAdmin;

            var data = await _apiService.GetAsync<List<PayslipRequestDto>>("paysliprequest/pending-for-finance");

            return View(data ?? new List<PayslipRequestDto>());
        }

        [HttpGet]
        public IActionResult FinanceUpload(string id)
        {
            return View(new FinanceUploadViewModel { Id = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinanceUpload(FinanceUploadViewModel model)
        {
            if (model.UploadFile == null || model.UploadFile.Length == 0)
            {
                ModelState.AddModelError(nameof(model.UploadFile), "Please choose the payslip PDF to upload.");
                return View(model);
            }

            var saved = await SaveFileAsync(model.UploadFile);

            if (saved == null)
            {
                ModelState.AddModelError(nameof(model.UploadFile),
                    "Only PDF files up to 5 MB are allowed for a payslip document.");
                return View(model);
            }

            try
            {
                var result = await _apiService.PutAsync<PayslipRequestDto>(
                    $"paysliprequest/{model.Id}/finance-upload",
                    new PayslipRequestUploadDto
                    {
                        DocumentUrl = saved.Value.filePath,
                        DocumentFileName = saved.Value.fileName,
                        Remarks = model.Remarks
                    });

                TempData[result != null ? "Success" : "GlobalError"] = result != null
                    ? "Payslip document uploaded. You can now mark this request as Completed."
                    : "Unable to upload the payslip document.";
            }
            catch (ApiException ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
            }
            catch (Exception ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.Message);
            }

            return RedirectToAction(nameof(FinanceQueue));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinanceComplete(string id, string? remarks)
        {
            try
            {
                var result = await _apiService.PutAsync<PayslipRequestDto>(
                    $"paysliprequest/{id}/finance-complete", new PayslipRequestActionDto { Remarks = remarks });

                TempData[result != null ? "Success" : "GlobalError"] = result != null
                    ? "Payslip request marked Completed - the employee has been notified."
                    : "Unable to complete this payslip request.";
            }
            catch (ApiException ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
            }
            catch (Exception ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.Message);
            }

            return RedirectBackOrTo(nameof(FinanceQueue));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinanceReject(string id, string reason)
        {
            try
            {
                var result = await _apiService.PutAsync<PayslipRequestDto>(
                    $"paysliprequest/{id}/finance-reject", new PayslipRequestRejectDto { Reason = reason });

                TempData[result != null ? "Success" : "GlobalError"] = result != null
                    ? "Payslip request rejected by Finance."
                    : "Unable to reject this payslip request.";
            }
            catch (ApiException ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
            }
            catch (Exception ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.Message);
            }

            return RedirectBackOrTo(nameof(FinanceQueue));
        }

        #endregion

        #region Download

        // The ONLY path an employee can reach an actual payslip file
        // through - the API re-verifies Status == Completed and ownership
        // server-side before returning DocumentUrl, so this can never be
        // used to reach another employee's payslip or an incomplete one.
        [HttpGet]
        public async Task<IActionResult> Download(string id)
        {
            try
            {
                var data = await _apiService.GetAsync<PayslipRequestDto>($"paysliprequest/{id}/download");

                if (data == null || string.IsNullOrEmpty(data.DocumentUrl))
                {
                    TempData["GlobalError"] = "Payslip is not available for download.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                return Redirect(data.DocumentUrl);
            }
            catch (ApiException ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.Message);
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        #endregion

        #region Helpers

        // Same "controller saves IFormFile to wwwroot, service only ever
        // sees a string URL" pattern as
        // EmployeeDocumentController.SaveFileAsync.
        private async Task<(string fileName, string filePath, string extension, long size)?> SaveFileAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0) return null;

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension)) return null;
            if (file.Length > MaxUploadBytes) return null;

            var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Uploads", "Payslips");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var stored = Guid.NewGuid().ToString() + extension;
            var fullPath = Path.Combine(folder, stored);

            using (var stream = new FileStream(fullPath, FileMode.Create))
                await file.CopyToAsync(stream);

            return (file.FileName, "/Uploads/Payslips/" + stored, extension, file.Length);
        }

        // Same JSON-error-unwrapping pattern as
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

                return "Unable to process this payslip request.";
            }
            catch
            {
                return string.IsNullOrWhiteSpace(raw) ? "Unable to process this payslip request." : raw;
            }
        }

        #endregion
    }

    #endregion
}
