using APP.Attributes;
using APP.Excel;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace APP.Controllers
{
    // Phase 14 - Outstanding Balance + Payment History reports over the
    // Loan & Advance module. Named exactly "LoanAdvanceReport" to match
    // AppFeatureConstants.LOAN_ADVANCE_REPORT_CONTROLLER (seeded menu entry,
    // Phase 10). Separate from LoanAdvanceDashboardController (KPI summary)
    // - this controller is the drill-down/export surface, backed by
    // api/loanadvancereport/outstanding-balance and .../payment-history.
    [JwtAuthorize]
    public class LoanAdvanceReportController : Controller
    {
        private readonly IApiService _apiService;
        private readonly IExcelEngine _excelEngine;
        private string _tenantId;
        private string _userId;
        public LoanAdvanceReportController(IApiService apiService, IExcelEngine excelEngine)
        {
            _apiService = apiService;
            _excelEngine = excelEngine;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? departmentId, DateTime? fromDate, DateTime? toDate)
        {
            var effectiveFrom = fromDate ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var effectiveTo = toDate ?? DateTime.Today;

            ViewBag.DepartmentId = departmentId;
            ViewBag.FromDate = effectiveFrom;
            ViewBag.ToDate = effectiveTo;

            await BindDepartmentDropdown();

            var outstanding = await GetOutstandingBalanceAsync(departmentId);
            var payments = await GetPaymentHistoryAsync(effectiveFrom, effectiveTo);

            ViewBag.PaymentHistory = payments;

            return View(outstanding);
        }

        [HttpGet]
        public async Task<IActionResult> ExportOutstandingBalance(string? departmentId)
        {
            var data = await GetOutstandingBalanceAsync(departmentId);
            var bytes = _excelEngine.Export(data, GetOutstandingBalanceColumns(), "Outstanding Balance");

            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"LoanAdvance_OutstandingBalance_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> ExportPaymentHistory(DateTime fromDate, DateTime toDate)
        {
            var data = await GetPaymentHistoryAsync(fromDate, toDate);
            var bytes = _excelEngine.Export(data, GetPaymentHistoryColumns(), "Payment History");

            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"LoanAdvance_PaymentHistory_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        }

        private async Task<List<OutstandingBalanceReportRowDto>> GetOutstandingBalanceAsync(string? departmentId)
        {
            //var url = $"loanadvancereport/outstanding-balance?departmentId={Uri.EscapeDataString(departmentId ?? string.Empty)}";
            
            var url = $"loanadvancereport/outstanding-balance?departmentId={Uri.EscapeDataString(departmentId ?? string.Empty)}" +
               $"?tenantId={Uri.EscapeDataString(_tenantId ?? string.Empty)}" +
               $"&actingUserId={Uri.EscapeDataString(_userId ?? string.Empty)}";

            return await _apiService.GetAsync<List<OutstandingBalanceReportRowDto>>(url) ?? new();
        }

        private async Task<List<LoanAdvancePaymentReportRowDto>> GetPaymentHistoryAsync(DateTime fromDate, DateTime toDate)
        {
            //var url = $"loanadvancereport/payment-history?fromDate={fromDate:yyyy-MM-dd}&toDate={toDate:yyyy-MM-dd}";

            var url = $"loanadvancereport/payment-history?fromDate={fromDate:yyyy-MM-dd}&toDate={toDate:yyyy-MM-dd}" +
               $"?tenantId={Uri.EscapeDataString(_tenantId ?? string.Empty)}" +
               $"&actingUserId={Uri.EscapeDataString(_userId ?? string.Empty)}";

            return await _apiService.GetAsync<List<LoanAdvancePaymentReportRowDto>>(url) ?? new();
        }

        private static List<ExcelColumn<OutstandingBalanceReportRowDto>> GetOutstandingBalanceColumns()
        {
            return new List<ExcelColumn<OutstandingBalanceReportRowDto>>
            {
                new("Employee", d => d.EmployeeName, (d, v) => { }),
                new("Employee Code", d => d.EmployeeCode, (d, v) => { }),
                new("Department", d => d.DepartmentName, (d, v) => { }),
                new("Active Loans", d => d.ActiveLoanCount, (d, v) => { }),
                new("Loan Outstanding", d => d.TotalLoanOutstanding, (d, v) => { }),
                new("Active Advances", d => d.ActiveAdvanceCount, (d, v) => { }),
                new("Advance Outstanding", d => d.TotalAdvanceOutstanding, (d, v) => { }),
                new("Next Due Date", d => d.NextDueDate?.ToString("dd-MMM-yyyy"), (d, v) => { }),
                new("Next Due Amount", d => d.NextDueAmount, (d, v) => { }),
                new("Max Days Past Due", d => d.MaxDaysPastDue, (d, v) => { }),
            };
        }

        private static List<ExcelColumn<LoanAdvancePaymentReportRowDto>> GetPaymentHistoryColumns()
        {
            return new List<ExcelColumn<LoanAdvancePaymentReportRowDto>>
            {
                new("Type", d => d.EntityType, (d, v) => { }),
                new("Employee", d => d.EmployeeName, (d, v) => { }),
                new("Employee Code", d => d.EmployeeCode, (d, v) => { }),
                new("Loan/Advance Type", d => d.TypeName, (d, v) => { }),
                new("Source", d => d.PaymentSourceName, (d, v) => { }),
                new("Amount Paid", d => d.AmountPaid, (d, v) => { }),
                new("Payment Date", d => d.PaymentDate.ToString("dd-MMM-yyyy"), (d, v) => { }),
                new("Payroll Id", d => d.PayrollId, (d, v) => { }),
                new("Receipt Reference", d => d.ReceiptReference, (d, v) => { }),
            };
        }

        private async Task BindDepartmentDropdown()
        {
            var departments = await _apiService.GetAsync<List<DropdownDto>>("dropdown/department")
                ?? new List<DropdownDto>();

            ViewBag.DepartmentList = new SelectList(departments, "Value", "Text");
        }
    }
}
