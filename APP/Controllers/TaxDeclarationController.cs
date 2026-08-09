using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using static APP.Helpers.EnumExtensions;

namespace APP.Controllers
{
    // Employee self-service annual investment declaration - see
    // API/Controllers/TaxDeclarationController.cs (api/taxdeclaration).
    // Named exactly "TaxDeclaration" to match
    // AppFeatureConstants.TAX_DECLARATION_CONTROLLER. Employees manage
    // their OWN declaration via MyDeclaration/CreateOrEdit/Submit; HR
    // reviews everyone's via Index/Verify/Reject - the API is the real
    // authority on who may do what, this controller's own checks are
    // cosmetic UX only (same convention as ProbationConfirmationController).
    [JwtAuthorize]
    public class TaxDeclarationController : Controller
    {
        private readonly IApiService _apiService;
        private readonly string? _userId;
        private readonly string? _tenantId;
        private readonly string? _employeeId;

        public TaxDeclarationController(IApiService apiService)
        {
            _apiService = apiService;
            _userId = SessionHelper.GetActiveUserId;
            _tenantId = SessionHelper.GetActiveTenantId;
            _employeeId = SessionHelper.GetActiveEmployeeId;
        }

        #region HR Index (all employees)

        [HttpGet]
        public async Task<IActionResult> Index(string? financialYearId, string? status, string? departmentId, string? search)
        {
            ViewBag.FinancialYearId = financialYearId;
            ViewBag.Status = status;
            ViewBag.DepartmentId = departmentId;
            ViewBag.Search = search;

            await BindFinancialYearDropdown(financialYearId);
            await BindDepartmentDropdown(departmentId);
            ViewBag.StatusList = EnumHelper.GetEnumList<TaxDeclarationStatus>();

            var url =
                $"taxdeclaration?financialYearId={Uri.EscapeDataString(financialYearId ?? string.Empty)}" +
                $"&status={Uri.EscapeDataString(status ?? string.Empty)}" +
                $"&departmentId={Uri.EscapeDataString(departmentId ?? string.Empty)}" +
                $"&tenantId={Uri.EscapeDataString(_tenantId ?? string.Empty)}" +
                $"&actingUserId={Uri.EscapeDataString(_userId ?? string.Empty)}" +
                $"&search={Uri.EscapeDataString(search ?? string.Empty)}";

            try
            {
                var data = await _apiService.GetAsync<List<TaxDeclarationDto>>(url);
                return View(data ?? new List<TaxDeclarationDto>());
            }
            catch (UnauthorizedAccessException ex) when (ex.Message.StartsWith("Access Denied"))
            {
                TempData["GlobalError"] = "You are not authorized to view Tax Declarations.";
                return RedirectToAction("Index", "Dashboard");
            }
        }

        #endregion

        #region Employee Self-Service

        [HttpGet]
        public async Task<IActionResult> MyDeclaration(string? financialYearId)
        {
            if (string.IsNullOrEmpty(financialYearId))
            {
                var current = await _apiService.GetAsync<FinancialYearDto>("financialyear/current");
                financialYearId = current?.Id;
            }

            ViewBag.FinancialYearId = financialYearId;

            if (string.IsNullOrEmpty(financialYearId))
            {
                TempData["GlobalError"] = "No current Financial Year is configured.";
                return RedirectToAction("Index", "Dashboard");
            }

            var data = await _apiService.GetAsync<TaxDeclarationDto>(
                $"taxdeclaration/my?financialYearId={Uri.EscapeDataString(financialYearId)}");

            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> CreateOrEdit(string financialYearId)
        {
            await BindFinancialYearDropdown(financialYearId);
            ViewBag.RegimeList = EnumHelper.GetEnumList<TaxRegime>();

            var url =
                $"taxdeclaration/my?financialYearId={Uri.EscapeDataString(financialYearId ?? string.Empty)}" +
                $"&tenantId={Uri.EscapeDataString(_tenantId ?? string.Empty)}" +
                $"&actingUserId={Uri.EscapeDataString(_userId ?? string.Empty)}";

            var existing = await _apiService.GetAsync<TaxDeclarationDto>(url);

            var model = existing != null
                ? new CreateTaxDeclarationDto
                {
                    EmployeeId = existing.EmployeeId,
                    FinancialYearId = existing.FinancialYearId,
                    Regime = existing.Regime,
                    Section80C = existing.Section80C,
                    Section80CCD1B = existing.Section80CCD1B,
                    Section80D = existing.Section80D,
                    Section24B = existing.Section24B,
                    OtherDeductions = existing.OtherDeductions,
                    AnnualRentPaid = existing.AnnualRentPaid,
                    IsMetroCity = existing.IsMetroCity,
                    LandlordPAN = existing.LandlordPAN
                }
                : new CreateTaxDeclarationDto { FinancialYearId = financialYearId,TenantId=_tenantId,ActingUserId=_userId,CreatedBy=_userId, EmployeeId = _employeeId ?? string.Empty };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOrEdit(CreateTaxDeclarationDto model)
        {
            if (!ModelState.IsValid)
            {
                await BindFinancialYearDropdown(model.FinancialYearId);
                ViewBag.RegimeList = EnumHelper.GetEnumList<TaxRegime>();
                return View(model);
            }

            try
            {
                var result = await _apiService.PostAsync<CreateTaxDeclarationDto, TaxDeclarationDto>("taxdeclaration", model);

                if (result == null)
                {
                    TempData["GlobalError"] = "Unable to save Tax Declaration.";
                    return RedirectToAction(nameof(CreateOrEdit), new { financialYearId = model.FinancialYearId, TenantId = _tenantId, ActingUserId = _userId, CreatedBy = _userId });
                }

                TempData["Success"] = "Tax Declaration saved as Draft.";
                return RedirectToAction(nameof(MyDeclaration), new { financialYearId = model.FinancialYearId, TenantId = _tenantId, ActingUserId = _userId, CreatedBy = _userId });
            }
            catch (ApiException ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
                return RedirectToAction(nameof(CreateOrEdit), new { financialYearId = model.FinancialYearId, TenantId = _tenantId, ActingUserId = _userId, CreatedBy = _userId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(string id, string financialYearId)
        {
            try
            {
                var url =
                    $"taxdeclaration/submit?id={Uri.EscapeDataString(id ?? string.Empty)}" +
                    $"&tenantId={Uri.EscapeDataString(_tenantId ?? string.Empty)}" +
                    $"&actingUserId={Uri.EscapeDataString(_userId ?? string.Empty)}";

                var result = await _apiService.PutAsync<TaxDeclarationDto>(
                    url,
                    new { }      // Empty request body
                );

                if (result != null)
                {
                    TempData["Success"] = "Tax Declaration submitted successfully.";
                }
                else
                {
                    TempData["Error"] = "Unable to submit Tax Declaration.";
                }
            }
            catch (ApiException ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
            }
            catch (Exception ex)
            {
                TempData["GlobalError"] = ex.Message;
            }

            return RedirectToAction(nameof(MyDeclaration), new
            {
                financialYearId = financialYearId
            });
        }
        #endregion

        #region Details / HR Workflow

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var url =
                    $"taxdeclaration?id={Uri.EscapeDataString(id ?? string.Empty)}" +
                    $"&tenantId={Uri.EscapeDataString(_tenantId ?? string.Empty)}" +
                    $"&actingUserId={Uri.EscapeDataString(_userId ?? string.Empty)}";

            var data = await _apiService.GetAsync<TaxDeclarationDto>(url);

            if (data == null)
                return NotFound();

            ViewBag.CurrentUserId = _userId;

            return View(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Verify(string id, string? remarks)
        {
            try
            {
                var result = await _apiService.PutAsync<TaxDeclarationDto>(
                    $"taxdeclaration/{id}/verify",
                    new TaxDeclarationVerifyActionDto { VerifierRemarks = remarks,TenantId=_tenantId,ActingUserId=_userId });

                TempData[result != null ? "Success" : "GlobalError"] = result != null
                    ? "Tax Declaration verified successfully."
                    : "Unable to verify this Tax Declaration.";
            }
            catch (ApiException ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(string id, string remarks)
        {
            try
            {
                var result = await _apiService.PutAsync<TaxDeclarationDto>(
                    $"taxdeclaration/{id}/reject",
                    new TaxDeclarationVerifyActionDto { VerifierRemarks = remarks, TenantId = _tenantId, ActingUserId = _userId });

                TempData[result != null ? "Success" : "GlobalError"] = result != null
                    ? "Tax Declaration rejected."
                    : "Unable to reject this Tax Declaration.";
            }
            catch (ApiException ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        #endregion

        #region Helpers

        private async Task BindFinancialYearDropdown(string? selectedId)
        {
            var years = await _apiService.GetAsync<List<FinancialYearDto>>("financialyear")
                ?? new List<FinancialYearDto>();

            var options = years
                .OrderByDescending(x => x.StartDate)
                .Select(x => new { Value = x.Id, Text = x.Name })
                .ToList();

            ViewBag.FinancialYearList = new SelectList(options, "Value", "Text", selectedId);
        }

        private async Task BindDepartmentDropdown(string? selectedDepartmentId)
        {
            var departments = await _apiService.GetAsync<List<DropdownDto>>("dropdown/department")
                ?? new List<DropdownDto>();

            ViewBag.DepartmentList = new SelectList(departments, "Value", "Text", selectedDepartmentId);
        }

        private string GetErrorMessage(string raw)
        {
            try
            {
                var jsonStart = raw.IndexOf('{');
                var json = jsonStart >= 0 ? raw.Substring(jsonStart) : raw;

                var obj = Newtonsoft.Json.Linq.JObject.Parse(json);

                if (obj["Message"] != null)
                    return obj["Message"]!.ToString();

                if (obj["errors"] is Newtonsoft.Json.Linq.JObject validationErrors)
                {
                    foreach (var property in validationErrors.Properties())
                    {
                        if (property.Value is Newtonsoft.Json.Linq.JArray arr && arr.Count > 0)
                            return arr[0]?.ToString();
                    }
                }

                return "Unable to process this Tax Declaration request.";
            }
            catch
            {
                return string.IsNullOrWhiteSpace(raw) ? "Unable to process this Tax Declaration request." : raw;
            }
        }

        #endregion
    }
}
