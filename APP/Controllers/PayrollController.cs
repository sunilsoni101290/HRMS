using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace APP.Controllers
{
    #region Payroll Controller

    [JwtAuthorize]
    public class PayrollController : Controller
    {
        private readonly IApiService _apiService;
        private string _tenantId;
        private string _userId;

        // The employee record linked to whoever is logged in - used to
        // scope "My Payslip" to the caller's own records only.
        private readonly string? _employeeId;

        // Generating/processing/deleting payroll is an Admin/HR function
        // only - a plain employee can view and print their own payslips but
        // nothing else, enforced server-side here.
        private readonly bool _isAdmin;

        public PayrollController(IApiService apiService)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
            _employeeId = SessionHelper.GetActiveEmployeeId;
            _isAdmin = SessionHelper.IsAdminRole();
        }

        public async Task<IActionResult> Index(int? year = null, int? month = null)
        {
            if (!_isAdmin)
                return RedirectToAction(nameof(MyPayslips));

            var url = "payroll";
            var query = new List<string>();
            if (year.HasValue) query.Add($"year={year}");
            if (month.HasValue) query.Add($"month={month}");
            if (query.Any()) url += "?" + string.Join("&", query);

            var data = await _apiService.GetAsync<List<PayrollListDto>>(url);

            ViewBag.Year = year ?? DateTime.UtcNow.Year;
            ViewBag.Month = month ?? 0;
            return View(data);
        }

        /// <summary>
        /// Formerly the self-service "my payslips" list - an employee could
        /// view/print their own payslip here directly, with no approval
        /// gate. That self-service purpose is now fully superseded by
        /// PayslipRequestController's Employee -> Reporting Manager ->
        /// Finance approval workflow (see Domain/Entities/PayslipRequest.cs
        /// and APP/Attributes/EssRestrictionAttribute.cs, which now lists
        /// this action as admin-only). A non-admin caller is redirected to
        /// PayslipRequestController.MyRequests instead of ever seeing
        /// payroll data directly; an admin lands on the org-wide Index
        /// (this action's own list view is retained only as an admin-side
        /// alias of Index for any bookmarked links).
        /// </summary>
        public async Task<IActionResult> MyPayslips()
        {
            if (!_isAdmin)
                return RedirectToAction("MyRequests", "PayslipRequest");

            var data = await _apiService.GetAsync<List<PayrollListDto>>("payroll");

            return View(data ?? new List<PayrollListDto>());
        }

        // Salary Processing: Select Month -> Load Attendance -> Review ->
        // Process Salary. This single page replaced the old "Generate"
        // form (which posted straight to generation with no preview) - see
        // LoadPreview (AJAX, populates the Review table) and ProcessSalary
        // (POST, writes exactly the rows the user confirmed) below.
        [HttpGet]
        public async Task<IActionResult> Generate()
        {
            if (!_isAdmin)
                return RedirectToAction(nameof(MyPayslips));

            await LoadDropdowns();
            return View(new PayrollGenerateDto());
        }

        // AJAX: "Load Attendance" - runs SalaryCalculationService for every
        // eligible employee (never writes anything) so the Review table can
        // render Present/Paid Leave/Payable Days/Gross/Deductions/Net
        // before anything is saved.
        [HttpGet]
        public async Task<IActionResult> LoadPreview(int salaryYear, int salaryMonth, string? companyId, string? branchId)
        {
            if (!_isAdmin)
                return Json(new { error = "Not authorized." });

            var url = $"payroll/preview?salaryYear={salaryYear}&salaryMonth={salaryMonth}&tenantId={_tenantId}";
            if (!string.IsNullOrEmpty(companyId)) url += $"&companyId={companyId}";
            if (!string.IsNullOrEmpty(branchId)) url += $"&branchId={branchId}";

            try
            {
                var data = await _apiService.GetAsync<SalaryProcessingPreviewDto>(url);
                return Json(data);
            }
            catch (ApiException apiEx)
            {
                return Json(new { error = GetErrorMessage(apiEx.ResponseContent) });
            }
        }

        // "Process Salary" - the Review table posts back exactly the
        // employee ids the user checked (never a blind re-filter).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessSalary(SalaryProcessRequestDto dto)
        {
            if (!_isAdmin)
                return RedirectToAction(nameof(MyPayslips));

            if (dto == null || dto.EmployeeIds == null || !dto.EmployeeIds.Any())
            {
                TempData["Error"] = "Please select at least one employee to process.";
                return RedirectToAction(nameof(Generate));
            }

            dto.TenantId = _tenantId;
            dto.CreatedBy = _userId;

            try
            {
                var result = await _apiService.PostAsync<PayrollGenerateResultDto>("payroll/process", dto);

                TempData["Success"] = result != null
                    ? $"Processed {result.Generated} payroll(s), skipped {result.Skipped}."
                    : "Salary processing completed.";

                if (result != null && result.Messages != null && result.Messages.Count > 1)
                    TempData["Info"] = string.Join(" | ", result.Messages.Skip(1).Take(5));
            }
            catch (ApiException apiEx)
            {
                TempData["Error"] = GetErrorMessage(apiEx.ResponseContent);
            }

            return RedirectToAction(nameof(Index), new { year = dto.SalaryYear, month = dto.SalaryMonth });
        }

        // Re-runs Salary Processing for one already-generated payroll -
        // Draft recalculates freely; Processed requires Remarks (enforced
        // server-side too); Paid is refused. See
        // PayrollBusinessService.RecalculateAsync's remarks.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Recalculate(string payrollId, string? remarks)
        {
            if (!_isAdmin)
                return Forbid();

            var dto = new SalaryRecalculateRequestDto
            {
                PayrollId = payrollId,
                Remarks = remarks,
                PerformedBy = _userId
            };

            try
            {
                await _apiService.PostAsync<dynamic>("payroll/recalculate", dto);
                TempData["Success"] = "Payroll recalculated successfully.";
            }
            catch (ApiException apiEx)
            {
                TempData["Error"] = GetErrorMessage(apiEx.ResponseContent);
            }

            return RedirectToAction(nameof(Details), new { id = payrollId });
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            if (!_isAdmin)
                return RedirectToAction(nameof(MyPayslips));

            var data = await _apiService.GetAsync<PayrollDto>($"payroll/{id}");
            return View(data);
        }

        [HttpPost]
        public async Task<IActionResult> Process(string id)
        {
            if (!_isAdmin)
                return Forbid();

            await _apiService.PutAsync<dynamic>(
                $"payroll/status/{id}?status=Processed&userId={_userId}", new { });
            TempData["Success"] = "Payroll marked as Processed.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> MarkPaid(string id)
        {
            if (!_isAdmin)
                return Forbid();

            await _apiService.PutAsync<dynamic>(
                $"payroll/status/{id}?status=Paid&userId={_userId}", new { });
            TempData["Success"] = "Payroll marked as Paid.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(string id)
        {
            if (!_isAdmin)
                return Forbid();

            await _apiService.DeleteAsync($"payroll/{id}");
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard(int? year = null, int? month = null)
        {
            if (!_isAdmin)
                return RedirectToAction(nameof(MyPayslips));

            int y = year ?? DateTime.UtcNow.Year;
            int m = month ?? DateTime.UtcNow.Month;

            var data = await _apiService
                .GetAsync<PayrollDashboardDto>($"payroll/dashboard?year={y}&month={m}");

            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Report(int? year = null, int? month = null, string? status = null)
        {
            if (!_isAdmin)
                return RedirectToAction(nameof(MyPayslips));

            int y = year ?? DateTime.UtcNow.Year;

            var url = $"payroll?year={y}";
            if (month.HasValue && month.Value > 0) url += $"&month={month.Value}";

            var data = await _apiService.GetAsync<List<PayrollListDto>>(url);

            if (!string.IsNullOrEmpty(status))
                data = data.Where(x => x.Status == status).ToList();

            ViewBag.Year = y;
            ViewBag.Month = month ?? 0;
            ViewBag.Status = status ?? "";
            return View(data);
        }

        // Admin-only direct payslip lookup by Payroll id, kept for HR/Admin
        // support purposes (e.g. troubleshooting a specific salary period).
        // An employee can no longer reach a payslip this way at all - see
        // PayslipRequestController.MyRequests/Create/Details/Download for
        // the only path a self-service user now has to their own payslip,
        // gated behind Reporting Manager approval and Finance
        // generation/completion.
        [HttpGet]
        public async Task<IActionResult> Payslip(string id)
        {
            if (!_isAdmin)
                return RedirectToAction("MyRequests", "PayslipRequest");

            // Ensure a payslip record exists, then show the printable view
            await _apiService.PostAsync<dynamic>($"payroll/payslip/{id}?userId={_userId}", new { });

            var data = await _apiService.GetAsync<PayrollDto>($"payroll/payslip/{id}");

            ViewBag.IsAdmin = _isAdmin;
            return View(data);
        }

        #region Load Dropdowns

        private async Task LoadDropdowns()
        {
            var employees = await _apiService
                .GetAsync<List<DropdownDto>>("dropdown/employee");
            ViewBag.EmployeeList = new SelectList(employees, "Value", "Text");

            var companies = await _apiService
                .GetAsync<List<DropdownDto>>("dropdown/company");
            ViewBag.CompanyList = new SelectList(companies, "Value", "Text");
        }

        #endregion

        private string GetErrorMessage(string json)
        {
            try
            {
                var obj = Newtonsoft.Json.Linq.JObject.Parse(json);

                if (obj["Message"] != null)
                    return obj["Message"]!.ToString();

                if (obj["Errors"] is Newtonsoft.Json.Linq.JArray errors && errors.Count > 0)
                    return errors[0]?.ToString();
            }
            catch { }

            return "Something went wrong.";
        }
    }

    #endregion
}
