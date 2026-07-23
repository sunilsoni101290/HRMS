using APP.Attributes;
using APP.Excel;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace APP.Controllers
{
    // Own controller - same "AttendanceRegularization/AttendancePolicy get
    // their own controller" precedent this app already follows, rather than
    // folding a whole extra admin screen into AttendanceController. Admin/HR
    // only - see EssRestrictionAttribute. Backed by
    // API/Controllers/AttendanceInsightsController.cs (api/attendanceinsights/summary).
    [JwtAuthorize]
    public class AttendanceSummaryController : Controller
    {
        private readonly IApiService _apiService;
        private readonly IExcelEngine _excelEngine;

        public AttendanceSummaryController(IApiService apiService, IExcelEngine excelEngine)
        {
            _apiService = apiService;
            _excelEngine = excelEngine;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? month, int? year, string? departmentId, string? employeeId)
        {
            var today = DateTime.Today;
            int m = month ?? today.Month;
            int y = year ?? today.Year;

            ViewBag.Month = m;
            ViewBag.Year = y;
            ViewBag.MonthName = new DateTime(y, m, 1).ToString("MMMM yyyy");
            ViewBag.SelectedDepartmentId = departmentId;
            ViewBag.SelectedEmployeeId = employeeId;

            await BindDropdowns(departmentId, employeeId);

            var data = await _apiService.GetAsync<List<AttendanceSummaryRowDto>>(BuildUrl(m, y, departmentId, employeeId));

            return View(data ?? new List<AttendanceSummaryRowDto>());
        }

        [HttpGet]
        public async Task<IActionResult> Export(int? month, int? year, string? departmentId, string? employeeId)
        {
            var today = DateTime.Today;
            int m = month ?? today.Month;
            int y = year ?? today.Year;

            var data = await _apiService.GetAsync<List<AttendanceSummaryRowDto>>(BuildUrl(m, y, departmentId, employeeId))
                ?? new List<AttendanceSummaryRowDto>();

            var bytes = _excelEngine.Export(data, GetExportColumns(), "Attendance Summary");

            var fileName = $"Attendance_Summary_{y}_{m:00}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        private static string BuildUrl(int month, int year, string? departmentId, string? employeeId)
        {
            var url = $"attendanceinsights/summary?month={month}&year={year}";

            if (!string.IsNullOrWhiteSpace(departmentId))
                url += $"&departmentId={Uri.EscapeDataString(departmentId)}";

            if (!string.IsNullOrWhiteSpace(employeeId))
                url += $"&employeeId={Uri.EscapeDataString(employeeId)}";

            return url;
        }

        private static List<ExcelColumn<AttendanceSummaryRowDto>> GetExportColumns()
        {
            return new List<ExcelColumn<AttendanceSummaryRowDto>>
            {
                new ExcelColumn<AttendanceSummaryRowDto>("Employee Code", d => d.EmployeeCode, (d, v) => d.EmployeeCode = v ?? string.Empty),
                new ExcelColumn<AttendanceSummaryRowDto>("Employee Name", d => d.EmployeeName, (d, v) => d.EmployeeName = v ?? string.Empty),
                new ExcelColumn<AttendanceSummaryRowDto>("Department", d => d.DepartmentName, (d, v) => d.DepartmentName = v),
                new ExcelColumn<AttendanceSummaryRowDto>("Present Days", d => d.PresentDays, (d, v) => d.PresentDays = int.TryParse(v, out var i) ? i : 0),
                new ExcelColumn<AttendanceSummaryRowDto>("Absent Days", d => d.AbsentDays, (d, v) => d.AbsentDays = int.TryParse(v, out var i) ? i : 0),
                new ExcelColumn<AttendanceSummaryRowDto>("Late Days", d => d.LateDays, (d, v) => d.LateDays = int.TryParse(v, out var i) ? i : 0),
                new ExcelColumn<AttendanceSummaryRowDto>("Half Days", d => d.HalfDays, (d, v) => d.HalfDays = int.TryParse(v, out var i) ? i : 0),
                new ExcelColumn<AttendanceSummaryRowDto>("Leave Days", d => d.LeaveDays, (d, v) => d.LeaveDays = int.TryParse(v, out var i) ? i : 0),
                new ExcelColumn<AttendanceSummaryRowDto>("Holiday Days", d => d.HolidayDays, (d, v) => d.HolidayDays = int.TryParse(v, out var i) ? i : 0),
                new ExcelColumn<AttendanceSummaryRowDto>("Week Off Days", d => d.WeekOffDays, (d, v) => d.WeekOffDays = int.TryParse(v, out var i) ? i : 0),
                new ExcelColumn<AttendanceSummaryRowDto>("Total Working Hours", d => d.TotalWorkingHours, (d, v) => d.TotalWorkingHours = decimal.TryParse(v, out var dec) ? dec : 0),
                new ExcelColumn<AttendanceSummaryRowDto>("Overtime Hours", d => d.OvertimeHours, (d, v) => d.OvertimeHours = decimal.TryParse(v, out var dec) ? dec : 0),
                new ExcelColumn<AttendanceSummaryRowDto>("Regularization Count", d => d.RegularizationCount, (d, v) => d.RegularizationCount = int.TryParse(v, out var i) ? i : 0),
            };
        }

        private async Task BindDropdowns(string? selectedDepartmentId, string? selectedEmployeeId)
        {
            var departments = await _apiService.GetAsync<List<DropdownDto>>("dropdown/department");
            ViewBag.DepartmentList = new SelectList(departments, "Value", "Text", selectedDepartmentId);

            var employees = await _apiService.GetAsync<List<DropdownDto>>("dropdown/employee");
            ViewBag.EmployeeList = new SelectList(employees, "Value", "Text", selectedEmployeeId);
        }
    }
}
