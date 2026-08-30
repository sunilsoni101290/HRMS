using APP.Attributes;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace APP.Controllers
{
    /// <summary>Daily Work Entry reports - HR/Admin only (EssRestrictionAttribute.AdminOnlyControllers).</summary>
    [JwtAuthorize]
    public class WorkTrackingReportController : Controller
    {
        private readonly IApiService _apiService;

        public WorkTrackingReportController(IApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<IActionResult> MonthlyEmployeeReport(WorkReportFilterDto filter)
        {
            filter ??= new WorkReportFilterDto();
            var response = await _apiService.GetAsync<ApiResponse<PagedResult<MonthlyEmployeeWorkReportDto>>>(
                "worktrackingreport/monthly-employee" + BuildQuery(filter));

            ViewBag.Filter = filter;
            return View(response?.Data ?? new PagedResult<MonthlyEmployeeWorkReportDto>());
        }

        public async Task<IActionResult> JobWiseReport(WorkReportFilterDto filter)
        {
            filter ??= new WorkReportFilterDto();
            var response = await _apiService.GetAsync<ApiResponse<List<JobWiseWorkReportDto>>>(
                "worktrackingreport/job-wise" + BuildQuery(filter));

            ViewBag.Filter = filter;
            return View(response?.Data ?? new List<JobWiseWorkReportDto>());
        }

        public async Task<IActionResult> StructureReport(WorkReportFilterDto filter)
        {
            filter ??= new WorkReportFilterDto();
            var response = await _apiService.GetAsync<ApiResponse<List<StructureWorkReportDto>>>(
                "worktrackingreport/structure-wise" + BuildQuery(filter));

            ViewBag.Filter = filter;
            return View(response?.Data ?? new List<StructureWorkReportDto>());
        }

        public async Task<IActionResult> UtilizationReport(WorkReportFilterDto filter)
        {
            filter ??= new WorkReportFilterDto();
            var response = await _apiService.GetAsync<ApiResponse<List<UtilizationReportDto>>>(
                "worktrackingreport/utilization" + BuildQuery(filter));

            ViewBag.Filter = filter;
            return View(response?.Data ?? new List<UtilizationReportDto>());
        }

        private static string BuildQuery(WorkReportFilterDto f)
        {
            var qs = new List<string>();
            if (!string.IsNullOrEmpty(f.EmployeeId)) qs.Add($"EmployeeId={f.EmployeeId}");
            if (!string.IsNullOrEmpty(f.DepartmentId)) qs.Add($"DepartmentId={f.DepartmentId}");
            if (f.FromDate.HasValue) qs.Add($"FromDate={f.FromDate:yyyy-MM-dd}");
            if (f.ToDate.HasValue) qs.Add($"ToDate={f.ToDate:yyyy-MM-dd}");
            if (!string.IsNullOrEmpty(f.WorkJobId)) qs.Add($"WorkJobId={f.WorkJobId}");
            if (!string.IsNullOrEmpty(f.ClientId)) qs.Add($"ClientId={f.ClientId}");
            if (!string.IsNullOrEmpty(f.JobTypeId)) qs.Add($"JobTypeId={f.JobTypeId}");
            if (f.Status.HasValue) qs.Add($"Status={f.Status}");
            qs.Add($"PageNumber={f.PageNumber}");
            qs.Add($"PageSize={f.PageSize}");

            return qs.Count > 0 ? "?" + string.Join("&", qs) : "";
        }
    }
}
