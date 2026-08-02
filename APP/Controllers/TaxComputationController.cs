using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace APP.Controllers
{
    // HR/Payroll surface over the Income Tax / TDS calculation engine -
    // see API/Controllers/TaxComputationController.cs
    // (api/taxcomputation). Named exactly "TaxComputation" to match
    // AppFeatureConstants.TAX_COMPUTATION_CONTROLLER.
    [JwtAuthorize]
    public class TaxComputationController : Controller
    {
        private readonly IApiService _apiService;

        public TaxComputationController(IApiService apiService)
        {
            _apiService = apiService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? financialYearId, string? departmentId, string? search)
        {
            if (string.IsNullOrEmpty(financialYearId))
            {
                var current = await _apiService.GetAsync<FinancialYearDto>("financialyear/current");
                financialYearId = current?.Id;
            }

            ViewBag.FinancialYearId = financialYearId;
            ViewBag.DepartmentId = departmentId;
            ViewBag.Search = search;

            await BindFinancialYearDropdown(financialYearId);
            await BindDepartmentDropdown(departmentId);

            if (string.IsNullOrEmpty(financialYearId))
                return View(new List<EmployeeTaxComputationDto>());

            var url =
                $"taxcomputation/all?financialYearId={Uri.EscapeDataString(financialYearId)}" +
                $"&departmentId={Uri.EscapeDataString(departmentId ?? string.Empty)}" +
                $"&search={Uri.EscapeDataString(search ?? string.Empty)}";

            try
            {
                var data = await _apiService.GetAsync<List<EmployeeTaxComputationDto>>(url);
                return View(data ?? new List<EmployeeTaxComputationDto>());
            }
            catch (UnauthorizedAccessException ex) when (ex.Message.StartsWith("Access Denied"))
            {
                TempData["GlobalError"] = "You are not authorized to view Tax Computations.";
                return RedirectToAction("Index", "Dashboard");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Compute(string employeeId, string financialYearId)
        {
            try
            {
                var result = await _apiService.PostAsync<EmployeeTaxComputationDto>(
                    $"taxcomputation/compute?employeeId={Uri.EscapeDataString(employeeId)}&financialYearId={Uri.EscapeDataString(financialYearId)}",
                    new { });

                TempData[result != null ? "Success" : "GlobalError"] = result != null
                    ? "Tax computed successfully."
                    : "Unable to compute Tax for this employee.";
            }
            catch (ApiException ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
            }

            return RedirectToAction(nameof(Index), new { financialYearId });
        }

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

                return obj["Message"] != null
                    ? obj["Message"]!.ToString()
                    : "Unable to process this Tax Computation request.";
            }
            catch
            {
                return string.IsNullOrWhiteSpace(raw) ? "Unable to process this Tax Computation request." : raw;
            }
        }
    }
}
