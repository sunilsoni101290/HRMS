using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace APP.Controllers
{
    #region Salary Template (reusable "Salary Structure" master) Controller

    // Naming note: the HR-facing screens under this controller are titled
    // "Salary Structure" (matching the feature spec), while the existing
    // per-employee assignment screens (APP/Controllers/
    // SalaryStructureController.cs, unchanged) are titled "Salary
    // Assignments" in the menu - see the DbSeeder menu entries for both.
    // The two are intentionally different controllers/entities; see
    // Domain/Entities/SalaryTemplate.cs for why.
    [JwtAuthorize]
    public class SalaryTemplateController : Controller
    {
        private readonly IApiService _apiService;
        private string _tenantId;
        private string _userId;

        public SalaryTemplateController(IApiService apiService)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _apiService.GetAsync<List<SalaryTemplateListDto>>("salary-template");
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();
            return View(new SalaryTemplateDto());
        }

        [HttpPost]
        public async Task<IActionResult> Create(SalaryTemplateDto dto)
        {
            if (dto != null && dto.Details != null && dto.Details.Any(d => !string.IsNullOrEmpty(d.SalaryComponentId)))
            {
                dto.TenantId = _tenantId;
                dto.CreatedBy = _userId;

                try
                {
                    await _apiService.PostAsync<dynamic>("salary-template", dto);
                    TempData["Success"] = "Salary structure saved successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (ApiException apiEx)
                {
                    TempData["Error"] = GetErrorMessage(apiEx.ResponseContent);
                    await LoadDropdowns();
                    return View(dto);
                }
            }

            TempData["Error"] = "Please add at least one salary component.";
            await LoadDropdowns();
            return View(dto);
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var data = await _apiService.GetAsync<SalaryTemplateDto>($"salary-template/{id}");
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var data = await _apiService.GetAsync<SalaryTemplateDto>($"salary-template/{id}");
            await LoadDropdowns();
            return View("Create", data);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string id, SalaryTemplateDto dto)
        {
            if (dto != null && dto.Details != null && dto.Details.Any(d => !string.IsNullOrEmpty(d.SalaryComponentId)))
            {
                dto.TenantId = _tenantId;
                dto.ModifiedBy = _userId;
                dto.ModifiedOn = DateTime.UtcNow;

                try
                {
                    await _apiService.PutAsync<dynamic>($"salary-template/{id}", dto);
                    TempData["Success"] = "Salary structure updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (ApiException apiEx)
                {
                    TempData["Error"] = GetErrorMessage(apiEx.ResponseContent);
                    await LoadDropdowns();
                    return View("Create", dto);
                }
            }

            TempData["Error"] = "Please add at least one salary component.";
            await LoadDropdowns();
            return View("Create", dto);
        }

        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _apiService.DeleteAsync($"salary-template/{id}");
                TempData["Success"] = "Salary structure deleted successfully.";
            }
            catch (ApiException apiEx)
            {
                TempData["Error"] = GetErrorMessage(apiEx.ResponseContent);
            }

            return RedirectToAction(nameof(Index));
        }

        #region Assign (bulk apply to employees)

        // GET: Assign?templateId=... - templateId is optional so the same
        // page can be opened either from a template's row action (pre-
        // selects that template) or from the Employee list bulk toolbar
        // (pre-selects the chosen employees instead, template picked here).
        [HttpGet]
        public async Task<IActionResult> Assign(string? templateId, string? employeeIds)
        {
            var templates = await _apiService.GetAsync<List<SalaryTemplateListDto>>("salary-template") ?? new();
            var activeTemplates = templates.Where(t => t.IsActive).ToList();

            ViewBag.TemplateList = new SelectList(activeTemplates, "Id", "Name", templateId);
            ViewBag.Templates = activeTemplates;

            var employees = await _apiService.GetAsync<List<EmployeeListDto>>("Employee/employee-list") ?? new();
            ViewBag.Employees = employees;

            var dto = new SalaryTemplateAssignDto
            {
                SalaryTemplateId = templateId ?? string.Empty,
                EffectiveFrom = DateTime.Today
            };

            if (!string.IsNullOrWhiteSpace(employeeIds))
            {
                dto.EmployeeIds = employeeIds
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToList();
            }

            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(SalaryTemplateAssignDto dto)
        {
            dto.TenantId = _tenantId;
            dto.CreatedBy = _userId;

            var result = await _apiService.PostAsync<SalaryTemplateAssignResultDto>("salary-template/assign", dto);

            if (result == null)
            {
                TempData["Error"] = "Could not apply the salary structure. Please try again.";
                return RedirectToAction(nameof(Assign), new { templateId = dto.SalaryTemplateId });
            }

            if (result.AppliedCount > 0 && result.SkippedCount == 0)
            {
                TempData["Success"] = $"Salary structure applied to {result.AppliedCount} employee(s) successfully.";
            }
            else if (result.AppliedCount > 0 && result.SkippedCount > 0)
            {
                TempData["Info"] = $"Applied to {result.AppliedCount} of {result.TotalSelected} employee(s). {result.SkippedCount} skipped - see below.";
            }
            else
            {
                var firstMessage = result.Results.FirstOrDefault()?.Message ?? "Nothing was applied.";
                TempData["Error"] = firstMessage;
            }

            TempData["AssignResult"] = Newtonsoft.Json.JsonConvert.SerializeObject(result);
            return RedirectToAction(nameof(Index));
        }

        #endregion

        [HttpPost]
        public async Task<IActionResult> ToggleActive(string id)
        {
            await _apiService.PutAsync<dynamic>($"salary-template/toggle-active/{id}", new { });
            TempData["Success"] = "Status updated.";
            return RedirectToAction(nameof(Index));
        }

        #region Load Dropdowns

        private async Task LoadDropdowns()
        {
            // The full component list (not just Value/Text) is needed here
            // - the Create/Edit grid shows Component Type per row and
            // computes Gross Salary from Earning lines only, client-side.
            var components = await _apiService.GetAsync<List<SalaryComponentListDto>>("salary-component") ?? new();
            ViewBag.ComponentList = components.OrderBy(c => c.Name).ToList();
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
