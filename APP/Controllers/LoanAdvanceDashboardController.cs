using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace APP.Controllers
{
    // Loan & Advance summary dashboard - see
    // AppFeatureConstants.LOAN_ADVANCE_DASHBOARD_CONTROLLER. Named exactly
    // "LoanAdvanceDashboard" to match the seeded menu entry (Phase 10).
    // PHASE 14: KPIs now come from the real api/loanadvancereport/dashboard
    // endpoint (ILoanReportService.GetDashboardAsync) instead of being
    // computed client-side from the list endpoints; only the "Recent
    // Activity" panels still use the cheap list endpoints directly.
    [JwtAuthorize]
    public class LoanAdvanceDashboardController : Controller
    {
        private readonly IApiService _apiService;
        private string _tenantId;
        private string _userId;

        public LoanAdvanceDashboardController(IApiService apiService)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var tenantId = Uri.EscapeDataString(_tenantId ?? string.Empty);
                var actingUserId = Uri.EscapeDataString(_userId ?? string.Empty);

                var dashboardUrl = $"loanadvancereport/dashboard?tenantId={tenantId}&actingUserId={actingUserId}";
                var loanUrl = $"employeeloan?tenantId={tenantId}&actingUserId={actingUserId}";
                var advanceUrl = $"employeeadvance?tenantId={tenantId}&actingUserId={actingUserId}";

                // Execute all API calls in parallel
                var dashboardTask = _apiService.GetAsync<LoanAdvanceDashboardDto>(dashboardUrl);
                var loanTask = _apiService.GetAsync<List<EmployeeLoanListDto>>(loanUrl);
                var advanceTask = _apiService.GetAsync<List<EmployeeAdvanceListDto>>(advanceUrl);

                await Task.WhenAll(dashboardTask, loanTask, advanceTask);

                var dto = dashboardTask.Result ?? new LoanAdvanceDashboardDto();
                var loans = loanTask.Result ?? new List<EmployeeLoanListDto>();
                var advances = advanceTask.Result ?? new List<EmployeeAdvanceListDto>();

                dto.RecentLoans = loans
                    .OrderByDescending(x => x.CreatedOn)
                    .Take(5)
                    .ToList();

                dto.RecentAdvances = advances
                    .OrderByDescending(x => x.CreatedOn)
                    .Take(5)
                    .ToList();

                return View(dto);
            }
            catch (Exception ex)
            {
                TempData["GlobalError"] = ex.Message;
                return View(new LoanAdvanceDashboardDto());
            }
        }
    }
}
