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

        public PayrollController(IApiService apiService)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
        }

        public async Task<IActionResult> Index(int? year = null, int? month = null)
        {
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

        [HttpGet]
        public async Task<IActionResult> Generate()
        {
            await LoadDropdowns();
            return View(new PayrollGenerateDto());
        }

        [HttpPost]
        public async Task<IActionResult> Generate(PayrollGenerateDto dto)
        {
            if (dto != null)
            {
                dto.TenantId = _tenantId;
                dto.CreatedBy = _userId;

                var result = await _apiService
                    .PostAsync<PayrollGenerateResultDto>("payroll/generate", dto);

                TempData["Success"] = result != null
                    ? $"Generated {result.Generated}, skipped {result.Skipped}."
                    : "Payroll generation completed.";

                return RedirectToAction(nameof(Index),
                    new { year = dto.SalaryYear, month = dto.SalaryMonth });
            }

            await LoadDropdowns();
            return View(dto);
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var data = await _apiService.GetAsync<PayrollDto>($"payroll/{id}");
            return View(data);
        }

        [HttpPost]
        public async Task<IActionResult> Process(string id)
        {
            await _apiService.PutAsync<dynamic>(
                $"payroll/status/{id}?status=Processed&userId={_userId}", new { });
            TempData["Success"] = "Payroll marked as Processed.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> MarkPaid(string id)
        {
            await _apiService.PutAsync<dynamic>(
                $"payroll/status/{id}?status=Paid&userId={_userId}", new { });
            TempData["Success"] = "Payroll marked as Paid.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(string id)
        {
            await _apiService.DeleteAsync($"payroll/{id}");
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard(int? year = null, int? month = null)
        {
            int y = year ?? DateTime.UtcNow.Year;
            int m = month ?? DateTime.UtcNow.Month;

            var data = await _apiService
                .GetAsync<PayrollDashboardDto>($"payroll/dashboard?year={y}&month={m}");

            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Report(int? year = null, int? month = null, string? status = null)
        {
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

        [HttpGet]
        public async Task<IActionResult> Payslip(string id)
        {
            // Ensure a payslip record exists, then show the printable view
            await _apiService.PostAsync<dynamic>($"payroll/payslip/{id}?userId={_userId}", new { });

            var data = await _apiService.GetAsync<PayrollDto>($"payroll/payslip/{id}");
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
    }

    #endregion
}
