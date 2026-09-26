using APP.Attributes;
using APP.Excel;
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
        private readonly IExcelEngine _excelEngine;
        private string _tenantId;
        private string _userId;

        // The employee record linked to whoever is logged in - used to
        // scope "My Payslip" to the caller's own records only.
        private readonly string? _employeeId;

        // Generating/processing/deleting payroll is an Admin/HR function
        // only - a plain employee can view and print their own payslips but
        // nothing else, enforced server-side here.
        private readonly bool _isAdmin;

        public PayrollController(IApiService apiService, IExcelEngine excelEngine)
        {
            _apiService = apiService;
            _excelEngine = excelEngine;
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
        //
        // Redesigned Generate.cshtml (Process Salary page) calls this via
        // AJAX and expects a JSON {success,message,processedCount,
        // skippedCount,failedCount} response so it can show a SweetAlert
        // and refresh LoadPreview in place, instead of a full-page
        // redirect. This is the ONLY caller of this action (confirmed -
        // no other view posts to Payroll/ProcessSalary), so switching its
        // response shape is safe. The underlying call - _apiService.
        // PostAsync("payroll/process", dto), the DTO shape, the
        // [ValidateAntiForgeryToken]/admin gate, and
        // PayrollBusinessService.ProcessAsync on the API side - are all
        // unchanged.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessSalary(SalaryProcessRequestDto dto)
        {
            if (!_isAdmin)
                return Json(new { success = false, message = "You are not authorized to process salary." });

            if (dto == null || dto.EmployeeIds == null || !dto.EmployeeIds.Any())
                return Json(new { success = false, message = "Please select at least one employee to process." });

            dto.TenantId = _tenantId;
            dto.CreatedBy = _userId;

            try
            {
                var result = await _apiService.PostAsync<PayrollGenerateResultDto>("payroll/process", dto);

                var message = result != null
                    ? $"Processed {result.Generated} payroll(s), skipped {result.Skipped}."
                    : "Salary processing completed.";

                return Json(new
                {
                    success = true,
                    message,
                    processedCount = result?.Generated ?? 0,
                    skippedCount = result?.Skipped ?? 0,
                    failedCount = 0,
                    messages = result?.Messages ?? new List<string>()
                });
            }
            catch (ApiException apiEx)
            {
                return Json(new { success = false, message = GetErrorMessage(apiEx.ResponseContent) });
            }
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
        public async Task<IActionResult> Report(
            int? year = null,
            int? month = null,
            string? status = null,
            string? departmentId = null,
            string? designationId = null,
            string? employeeStatus = null,
            string? search = null)
        {
            if (!_isAdmin)
                return RedirectToAction(nameof(MyPayslips));

            int y = year ?? DateTime.UtcNow.Year;
            int m = month ?? DateTime.UtcNow.Month;

            List<PayrollListDto> data;
            try
            {
                data = await _apiService.GetAsync<List<PayrollListDto>>(BuildRegisterQuery(y, m, status, departmentId, designationId, employeeStatus, search)) ?? new();
            }
            catch (ApiException)
            {
                data = new List<PayrollListDto>();
                ViewBag.LoadError = "Could not load the payroll register right now. Please try again in a moment.";
            }

            ViewBag.Year = y;
            ViewBag.Month = m;
            ViewBag.Status = status ?? "";
            ViewBag.DepartmentId = departmentId ?? "";
            ViewBag.DesignationId = designationId ?? "";
            ViewBag.EmployeeStatus = string.IsNullOrEmpty(employeeStatus) ? "active" : employeeStatus;
            ViewBag.Search = search ?? "";

            var departments = await _apiService.GetAsync<List<DropdownDto>>("dropdown/department") ?? new();
            var designations = await _apiService.GetAsync<List<DropdownDto>>("dropdown/designation") ?? new();
            ViewBag.Departments = departments;
            ViewBag.Designations = designations;

            return View(data);
        }

        // Excel export for the Payroll Register - reuses the shared
        // IExcelEngine (same engine behind Employee/SalaryComponent export)
        // instead of a one-off ClosedXML implementation here, and re-applies
        // the same filters as the on-screen Register so the file always
        // matches what the user is looking at.
        [HttpGet]
        public async Task<IActionResult> ExportRegister(
            int? year = null,
            int? month = null,
            string? status = null,
            string? departmentId = null,
            string? designationId = null,
            string? employeeStatus = null,
            string? search = null)
        {
            if (!_isAdmin)
                return Forbid();

            int y = year ?? DateTime.UtcNow.Year;
            int m = month ?? DateTime.UtcNow.Month;

            var data = await _apiService.GetAsync<List<PayrollListDto>>(BuildRegisterQuery(y, m, status, departmentId, designationId, employeeStatus, search)) ?? new();

            var columns = new List<ExcelColumn<PayrollListDto>>
            {
                new ExcelColumn<PayrollListDto>("Employee Code", x => x.EmployeeCode, (_, _) => { }),
                new ExcelColumn<PayrollListDto>("Employee Name", x => x.EmployeeName, (_, _) => { }),
                new ExcelColumn<PayrollListDto>("Department", x => x.DepartmentName, (_, _) => { }),
                new ExcelColumn<PayrollListDto>("Designation", x => x.DesignationName, (_, _) => { }),
                new ExcelColumn<PayrollListDto>("Basic Salary", x => x.BasicSalary, (_, _) => { }),
                new ExcelColumn<PayrollListDto>("Allowances", x => x.Allowances, (_, _) => { }),
                new ExcelColumn<PayrollListDto>("Gross Salary", x => x.GrossSalary, (_, _) => { }),
                new ExcelColumn<PayrollListDto>("Deductions", x => x.Deductions, (_, _) => { }),
                new ExcelColumn<PayrollListDto>("Net Pay", x => x.NetSalary, (_, _) => { }),
                new ExcelColumn<PayrollListDto>("Payment Status", x => x.Status, (_, _) => { }),
            };

            var bytes = _excelEngine.Export(data, columns, "Payroll Register");
            var fileName = $"PayrollRegister_{y}_{m:00}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        private static string BuildRegisterQuery(
            int year, int month, string? status, string? departmentId,
            string? designationId, string? employeeStatus, string? search)
        {
            var url = $"payroll?year={year}&month={month}";
            if (!string.IsNullOrEmpty(status)) url += $"&paymentStatus={Uri.EscapeDataString(status)}";
            if (!string.IsNullOrEmpty(departmentId)) url += $"&departmentId={Uri.EscapeDataString(departmentId)}";
            if (!string.IsNullOrEmpty(designationId)) url += $"&designationId={Uri.EscapeDataString(designationId)}";
            if (!string.IsNullOrEmpty(employeeStatus)) url += $"&employeeStatus={Uri.EscapeDataString(employeeStatus)}";
            if (!string.IsNullOrEmpty(search)) url += $"&search={Uri.EscapeDataString(search)}";
            return url;
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

            // Ensure a Payslip audit record exists (unchanged behavior),
            // then load the redesigned, fully-formatted payslip document.
            await _apiService.PostAsync<dynamic>($"payroll/payslip/{id}?userId={_userId}", new { });

            PayslipDto? data;
            try
            {
                data = await _apiService.GetAsync<PayslipDto>($"payroll/payslip-document/{id}");
            }
            catch (ApiException)
            {
                // 403/404 from the API (wrong tenant, or the payroll no
                // longer exists) - never distinguish the two to the caller.
                return NotFound();
            }

            if (data == null)
                return NotFound();

            // Presentation-only: the absolute verification URL and its QR
            // code image are built here, never in the API/Application
            // layer (see PayslipDto's VerificationUrl remarks).
            if (!string.IsNullOrEmpty(data.VerificationToken))
            {
                data.VerificationUrl = Url.Action(
                    nameof(PayslipVerificationController.Verify),
                    "PayslipVerification",
                    new { token = data.VerificationToken },
                    Request.Scheme);

                data.VerificationQrCodeDataUri = QrCodeHelper.GeneratePngDataUri(data.VerificationUrl);
            }

            ViewBag.IsAdmin = _isAdmin;
            return View(data);
        }

        // Toolbar "Email Payslip" button - posts back the same
        // verification URL the page rendered with (built client-side, no
        // second Url.Action round trip needed) and redirects back to the
        // payslip with a SweetAlert result.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmailPayslip(string id, string? verificationUrl)
        {
            if (!_isAdmin)
                return Forbid();

            try
            {
                var url = $"payroll/payslip-document/{id}/email";
                if (!string.IsNullOrEmpty(verificationUrl))
                    url += $"?verificationUrl={Uri.EscapeDataString(verificationUrl)}";

                var result = await _apiService.PostAsync<dynamic>(url, new { });

                bool sent = false;
                string? message = null;
                if (result is Newtonsoft.Json.Linq.JObject resultObj)
                {
                    sent = resultObj.Value<bool?>("Sent") ?? false;
                    message = resultObj.Value<string?>("Message");
                }

                TempData[sent ? "Success" : "Error"] = message ?? (sent ? "Payslip emailed successfully." : "Could not email this payslip.");
            }
            catch (ApiException apiEx)
            {
                TempData["Error"] = GetErrorMessage(apiEx.ResponseContent);
            }

            return RedirectToAction(nameof(Payslip), new { id });
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
