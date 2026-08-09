using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace APP.Controllers
{
    // Admin-only CRUD + versioning for Loan Policies (and their nested
    // approval matrix) - see API/Controllers/LoanPolicyController.cs
    // (api/loanpolicy). Named exactly "LoanPolicy" to match
    // AppFeatureConstants.LOAN_POLICY_CONTROLLER. Edit() intentionally
    // posts to the same Update endpoint that returns a NEW policy row
    // (next VersionNumber) - see LoanPolicyDto's remarks.
    [JwtAuthorize]
    public class LoanPolicyController : Controller
    {
        private readonly IApiService _apiService;
        private string _tenantId;
        private string _userId;
        public LoanPolicyController(IApiService apiService)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? loanTypeId, string? companyId)
        {
            ViewBag.LoanTypeId = loanTypeId;
            ViewBag.CompanyId = companyId;

            await BindLoanTypeDropdown(loanTypeId);
            await BindCompanyDropdown(companyId);

            var url =
           $"loanpolicy?loanTypeId={Uri.EscapeDataString(loanTypeId ?? string.Empty)}" +
           $"?companyId={Uri.EscapeDataString(companyId ?? string.Empty)}" +
           $"?tenantId={Uri.EscapeDataString(_tenantId ?? string.Empty)}" +
           $"&actingUserId={Uri.EscapeDataString(_userId ?? string.Empty)}";

            try
            {
                var data = await _apiService.GetAsync<List<LoanPolicyDto>>(url);
                return View(data ?? new List<LoanPolicyDto>());
            }
            catch (UnauthorizedAccessException ex) when (ex.Message.StartsWith("Access Denied"))
            {
                TempData["GlobalError"] = "You are not authorized to view Loan Policies.";
                return RedirectToAction("Index", "Dashboard");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadFormViewDataAsync(null);

            return View(new LoanPolicyDto
            {
                ApprovalLevels = new List<LoanPolicyApprovalLevelDto>
                {
                    new() { LevelNumber = 1, ApproverType = 1 }
                },
                TenantId = _tenantId,
                ActingUserId= _userId
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LoanPolicyDto model)
        {
            if (!ModelState.IsValid)
            {
                await LoadFormViewDataAsync(model.CompanyId);
                return View(model);
            }

            try
            {
                var result = await _apiService.PostAsync<LoanPolicyDto, LoanPolicyDto>("loanpolicy", model);

                TempData[result != null ? "Success" : "GlobalError"] = result != null
                    ? "Loan Policy created successfully."
                    : "Unable to create Loan Policy.";

                return RedirectToAction(nameof(Index));
            }
            catch (ApiException ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
                await LoadFormViewDataAsync(model.CompanyId);
                return View(model);
            }
        }

        // Loads an EXISTING policy as the starting point for a new version
        // (see class remarks) - the form posts to Update, which closes the
        // current version and inserts VersionNumber + 1.
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var data = await _apiService.GetAsync<LoanPolicyDto>($"loanpolicy/{id}");
            if (data == null) return NotFound();

            data.TenantId= _tenantId;
            data.ActingUserId= _userId;

            await LoadFormViewDataAsync(data.CompanyId);
            ViewBag.IsNewVersion = true;
            return View("Create", data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, LoanPolicyDto model)
        {
            if (!ModelState.IsValid)
            {
                await LoadFormViewDataAsync(model.CompanyId);
                ViewBag.IsNewVersion = true;
                return View("Create", model);
            }

            try
            {
                var result = await _apiService.PutAsync<LoanPolicyDto>($"loanpolicy/{id}", model);

                TempData[result != null ? "Success" : "GlobalError"] = result != null
                    ? "A new Loan Policy version was created successfully."
                    : "Unable to update Loan Policy.";

                return RedirectToAction(nameof(Index));
            }
            catch (ApiException ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
                await LoadFormViewDataAsync(model.CompanyId);
                ViewBag.IsNewVersion = true;
                return View("Create", model);
            }
        }

        // AJAX endpoint backing the Company -> Branch cascade in Create.cshtml.
        [HttpGet]
        public async Task<IActionResult> GetBranches(string companyId)
        {
            if (string.IsNullOrEmpty(companyId))
                return Json(new List<DropdownDto>());

            var branches = await _apiService.GetAsync<List<DropdownDto>>($"dropdown/branch/{companyId}")
                ?? new List<DropdownDto>();

            return Json(branches);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(string id)
        {
            try
            {
                var result = await _apiService.PostAsync<object>($"loanpolicy/{id}/deactivate", new { });
                TempData["Success"] = "Loan Policy deactivated successfully.";
            }
            catch (ApiException ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadFormViewDataAsync(string? companyId)
        {
            await BindLoanTypeDropdown(null);
            await BindCompanyDropdown(companyId);
            await BindBranchDropdown(companyId, null);
            await BindRoleDropdown();

            ViewBag.ApproverTypeList = EnumHelper.GetEnumList<EnumExtensions.LoanApproverType>();
        }

        // No "dropdown/loantype" entry in the generic DropdownListController -
        // this new module's masters predate it, so fetch the master list
        // directly from api/loantype instead (same approach TaxSlabController
        // uses for FinancialYear).
        private async Task BindLoanTypeDropdown(string? selectedId)
        {
            var types = await _apiService.GetAsync<List<LoanTypeDto>>("loantype")
                ?? new List<LoanTypeDto>();

            var options = types.Where(x => x.IsActive).Select(x => new { Value = x.Id, Text = x.Name }).ToList();
            ViewBag.LoanTypeList = new SelectList(options, "Value", "Text", selectedId);
        }

        private async Task BindCompanyDropdown(string? selectedId)
        {
            var companies = await _apiService.GetAsync<List<DropdownDto>>("dropdown/company")
                ?? new List<DropdownDto>();

            ViewBag.CompanyList = new SelectList(companies, "Value", "Text", selectedId);
        }

        private async Task BindBranchDropdown(string? companyId, string? selectedId)
        {
            var branches = string.IsNullOrEmpty(companyId)
                ? new List<DropdownDto>()
                : await _apiService.GetAsync<List<DropdownDto>>($"dropdown/branch/{companyId}") ?? new List<DropdownDto>();

            ViewBag.BranchList = new SelectList(branches, "Value", "Text", selectedId);
        }

        private async Task BindRoleDropdown()
        {
            var roles = await _apiService.GetAsync<List<DropdownDto>>("dropdown/role")
                ?? new List<DropdownDto>();

            ViewBag.RoleList = new SelectList(roles, "Value", "Text");
        }

        private string GetErrorMessage(string raw)
        {
            try
            {
                var jsonStart = raw.IndexOf('{');
                var json = jsonStart >= 0 ? raw.Substring(jsonStart) : raw;
                var obj = Newtonsoft.Json.Linq.JObject.Parse(json);

                if (obj["Message"] != null) return obj["Message"]!.ToString();

                if (obj["errors"] is Newtonsoft.Json.Linq.JObject validationErrors)
                {
                    foreach (var property in validationErrors.Properties())
                    {
                        if (property.Value is Newtonsoft.Json.Linq.JArray arr && arr.Count > 0)
                            return arr[0]?.ToString();
                    }
                }

                return "Unable to process this Loan Policy request.";
            }
            catch
            {
                return string.IsNullOrWhiteSpace(raw) ? "Unable to process this Loan Policy request." : raw;
            }
        }
    }
}
