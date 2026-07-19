using APP.Attributes;
using APP.Excel;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Globalization;

namespace APP.Controllers
{
    #region Job Opening Controller

    [JwtAuthorize]
    public class JobOpeningController : Controller
    {
        private readonly IApiService _apiService;
        private readonly IExcelEngine _excelEngine;
        private string _tenantId;
        private string _userId;
        private readonly bool _isAdmin;

        public JobOpeningController(IApiService apiService, IExcelEngine excelEngine)
        {
            _apiService = apiService;
            _excelEngine = excelEngine;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
            _isAdmin = SessionHelper.IsAdminRole();
        }

        public async Task<IActionResult> Index()
        {
            var data = await _apiService.GetAsync<List<JobOpeningListDto>>("job-opening");
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();
            return View(new JobOpeningDto());
        }

        [HttpPost]
        public async Task<IActionResult> Create(JobOpeningDto dto)
        {
            if (dto != null)
            {
                dto.TenantId = _tenantId;
                dto.CreatedBy = _userId;

                await _apiService.PostAsync<dynamic>("job-opening", dto);

                TempData["Success"] = "Job opening saved successfully.";
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdowns(dto.DepartmentId);
            return View(dto);
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var data = await _apiService.GetAsync<JobOpeningDto>($"job-opening/{id}");
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var data = await _apiService.GetAsync<JobOpeningDto>($"job-opening/{id}");
            await LoadDropdowns(data.DepartmentId);
            return View("Create", data);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string id, JobOpeningDto dto)
        {
            if (dto != null)
            {
                dto.TenantId = _tenantId;
                dto.ModifiedBy = _userId;
                dto.ModifiedOn = DateTime.UtcNow;

                await _apiService.PutAsync<dynamic>($"job-opening/{id}", dto);

                TempData["Success"] = "Job opening updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdowns(dto.DepartmentId);
            return View("Create", dto);
        }

        public async Task<IActionResult> Delete(string id)
        {
            await _apiService.DeleteAsync($"job-opening/{id}");
            return RedirectToAction(nameof(Index));
        }

        #region Load Dropdowns

        private async Task LoadDropdowns(string? departmentId = null)
        {
            var departments = await _apiService.GetAsync<List<DropdownDto>>("dropdown/department");
            ViewBag.DepartmentList = new SelectList(departments, "Value", "Text");

            List<DropdownDto> designations = new();
            if (!string.IsNullOrEmpty(departmentId))
            {
                designations = await _apiService
                    .GetAsync<List<DropdownDto>>($"dropdown/designation/{departmentId}");
            }
            ViewBag.DesignationList = new SelectList(designations, "Value", "Text");
        }

        #endregion

        [HttpGet]
        public async Task<JsonResult> GetDesignationByDepartment(string departmentId)
        {
            var data = await _apiService
                .GetAsync<List<DropdownDto>>($"dropdown/designation/{departmentId}");
            var result = data.Select(x => new { value = x.Value, text = x.Text });
            return Json(result);
        }

        #region Import

        // Department Code is resolved globally (same as Designation
        // import); Designation Code is then scoped to the resolved
        // Department, exactly like Designation was scoped within Company -
        // a Designation Code shared by two designations in different
        // departments is fine, since Department disambiguates it.
        private static List<ExcelColumn<JobOpeningDto>> GetImportColumns(
            Dictionary<string, string> departmentLookup,
            HashSet<string> ambiguousDepartmentCodes,
            Dictionary<(string DepartmentId, string Code), string> designationLookup,
            HashSet<(string DepartmentId, string Code)> ambiguousDesignationCodes)
        {
            return new List<ExcelColumn<JobOpeningDto>>
            {
                new ExcelColumn<JobOpeningDto>(
                    "Job Title*",
                    d => d.Title,
                    (d, v) =>
                    {
                        if (string.IsNullOrWhiteSpace(v))
                            throw new Exception("Job Title is required.");
                        d.Title = v.Trim();
                    },
                    isRequired: true,
                    sampleValue: "Senior Software Engineer"),

                new ExcelColumn<JobOpeningDto>(
                    "Department Code*",
                    d => d.DepartmentId,
                    (d, v) =>
                    {
                        var code = (v ?? string.Empty).Trim();

                        if (string.IsNullOrWhiteSpace(code))
                            throw new Exception("Department Code is required.");

                        if (ambiguousDepartmentCodes.Contains(code))
                            throw new Exception($"Department Code '{code}' matches more than one department - ask an admin to make department codes unique.");

                        if (!departmentLookup.TryGetValue(code, out var departmentId))
                            throw new Exception($"Department Code '{code}' was not found. See the Reference Data sheet for valid codes.");

                        d.DepartmentId = departmentId;
                    },
                    isRequired: true,
                    sampleValue: "IT"),

                new ExcelColumn<JobOpeningDto>(
                    "Designation Code*",
                    d => d.DesignationId,
                    (d, v) =>
                    {
                        var code = (v ?? string.Empty).Trim();

                        if (string.IsNullOrWhiteSpace(code))
                            throw new Exception("Designation Code is required.");

                        if (string.IsNullOrWhiteSpace(d.DepartmentId))
                            throw new Exception("Designation Code could not be resolved because the Department Code above is invalid.");

                        var key = (d.DepartmentId, code.ToUpperInvariant());

                        if (ambiguousDesignationCodes.Contains(key))
                            throw new Exception($"Designation Code '{code}' matches more than one designation within that department - ask an admin to make designation codes unique.");

                        if (!designationLookup.TryGetValue(key, out var designationId))
                            throw new Exception($"Designation Code '{code}' was not found for that department. See the Reference Data sheet for valid codes.");

                        d.DesignationId = designationId;
                    },
                    isRequired: true,
                    sampleValue: "SE"),

                new ExcelColumn<JobOpeningDto>(
                    "Vacancy Count",
                    d => d.VacancyCount,
                    (d, v) =>
                    {
                        if (string.IsNullOrWhiteSpace(v))
                        {
                            d.VacancyCount = 1;
                            return;
                        }

                        if (!int.TryParse(v.Trim(), out var count) || count < 1)
                            throw new Exception("Vacancy Count must be a whole number of at least 1.");

                        d.VacancyCount = count;
                    },
                    sampleValue: 1),

                new ExcelColumn<JobOpeningDto>(
                    "Min Salary",
                    d => d.MinSalary,
                    (d, v) =>
                    {
                        if (string.IsNullOrWhiteSpace(v))
                        {
                            d.MinSalary = null;
                            return;
                        }

                        if (!decimal.TryParse(v.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var minSalary) || minSalary < 0)
                            throw new Exception($"Invalid Min Salary '{v}'. Use a non-negative number.");

                        d.MinSalary = minSalary;
                    },
                    sampleValue: 40000),

                new ExcelColumn<JobOpeningDto>(
                    "Max Salary",
                    d => d.MaxSalary,
                    (d, v) =>
                    {
                        if (string.IsNullOrWhiteSpace(v))
                        {
                            d.MaxSalary = null;
                            return;
                        }

                        if (!decimal.TryParse(v.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var maxSalary) || maxSalary < 0)
                            throw new Exception($"Invalid Max Salary '{v}'. Use a non-negative number.");

                        d.MaxSalary = maxSalary;
                    },
                    sampleValue: 70000),

                new ExcelColumn<JobOpeningDto>(
                    "Job Description",
                    d => d.JobDescription,
                    (d, v) => d.JobDescription = string.IsNullOrWhiteSpace(v) ? null : v.Trim(),
                    sampleValue: ""),

                new ExcelColumn<JobOpeningDto>(
                    "Required Skills",
                    d => d.RequiredSkills,
                    (d, v) => d.RequiredSkills = string.IsNullOrWhiteSpace(v) ? null : v.Trim(),
                    sampleValue: "C#, ASP.NET Core, SQL Server"),

                new ExcelColumn<JobOpeningDto>(
                    "Status* (Open/Closed/OnHold)",
                    d => d.StatusText,
                    (d, v) =>
                    {
                        var text = (v ?? string.Empty).Trim();

                        d.Status = text.ToLowerInvariant() switch
                        {
                            "open" => 1,
                            "closed" => 2,
                            "onhold" or "on hold" or "on-hold" => 3,
                            _ => throw new Exception($"Invalid Status '{text}'. Use Open, Closed or OnHold.")
                        };
                    },
                    isRequired: true,
                    sampleValue: "Open"),

                new ExcelColumn<JobOpeningDto>(
                    "Posted Date",
                    d => d.PostedDate,
                    (d, v) =>
                    {
                        if (string.IsNullOrWhiteSpace(v))
                        {
                            d.PostedDate = DateTime.UtcNow.Date;
                            return;
                        }

                        if (!DateTime.TryParse(v.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                            throw new Exception($"Invalid Posted Date '{v}'. Use a valid date (e.g. 2026-07-19).");

                        d.PostedDate = date.Date;
                    },
                    sampleValue: ""),

                new ExcelColumn<JobOpeningDto>(
                    "Closing Date",
                    d => d.ClosingDate,
                    (d, v) =>
                    {
                        if (string.IsNullOrWhiteSpace(v))
                        {
                            d.ClosingDate = null;
                            return;
                        }

                        if (!DateTime.TryParse(v.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                            throw new Exception($"Invalid Closing Date '{v}'. Use a valid date (e.g. 2026-12-31).");

                        d.ClosingDate = date.Date;
                    },
                    sampleValue: ""),
            };
        }

        private static List<ExcelColumn<JobOpeningListDto>> GetExportColumns()
        {
            return new List<ExcelColumn<JobOpeningListDto>>
            {
                new ExcelColumn<JobOpeningListDto>("Job Title", d => d.Title, (d, v) => { }),
                new ExcelColumn<JobOpeningListDto>("Department", d => d.DepartmentName, (d, v) => { }),
                new ExcelColumn<JobOpeningListDto>("Designation", d => d.DesignationName, (d, v) => { }),
                new ExcelColumn<JobOpeningListDto>("Vacancy Count", d => d.VacancyCount, (d, v) => { }),
                new ExcelColumn<JobOpeningListDto>("Status", d => d.StatusText, (d, v) => { }),
                new ExcelColumn<JobOpeningListDto>("Posted Date", d => d.PostedDate, (d, v) => { }),
                new ExcelColumn<JobOpeningListDto>("Closing Date", d => d.ClosingDate, (d, v) => { }),
                new ExcelColumn<JobOpeningListDto>("Applications", d => d.ApplicationCount, (d, v) => { }),
            };
        }

        private static (Dictionary<string, string> Lookup, HashSet<string> Ambiguous) BuildDepartmentLookup(
            List<DepartmentListDto> departments)
        {
            var groups = (departments ?? new List<DepartmentListDto>())
                .Where(d => !string.IsNullOrWhiteSpace(d.Code))
                .GroupBy(d => d.Code.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToList();

            var ambiguous = groups.Where(g => g.Count() > 1).Select(g => g.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var lookup = groups.Where(g => g.Count() == 1)
                .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

            return (lookup, ambiguous);
        }

        // Designation lookup is keyed by (DepartmentId, upper-cased Code)
        // so a code shared by two designations in different departments
        // never collides.
        private static (Dictionary<(string, string), string> Lookup, HashSet<(string, string)> Ambiguous) BuildDesignationLookup(
            List<DesignationListDto> designations)
        {
            var groups = (designations ?? new List<DesignationListDto>())
                .Where(d => !string.IsNullOrWhiteSpace(d.Code) && !string.IsNullOrWhiteSpace(d.DepartmentId))
                .GroupBy(d => (DepartmentId: d.DepartmentId!, Code: d.Code.Trim().ToUpperInvariant()))
                .ToList();

            var ambiguous = groups.Where(g => g.Count() > 1).Select(g => g.Key).ToHashSet();

            var lookup = groups.Where(g => g.Count() == 1)
                .ToDictionary(g => g.Key, g => g.First().Id);

            return (lookup, ambiguous);
        }

        private static List<ExcelTemplateReferenceSheet> GetReferenceSheets(
            List<DepartmentListDto> departments,
            List<DesignationListDto> designations)
        {
            var orderedDepartments = (departments ?? new List<DepartmentListDto>()).OrderBy(d => d.Name).ToList();
            var orderedDesignations = (designations ?? new List<DesignationListDto>()).OrderBy(d => d.DepartmentName).ThenBy(d => d.Name).ToList();

            return new List<ExcelTemplateReferenceSheet>
            {
                new ExcelTemplateReferenceSheet(
                    "Reference Data",
                    new List<ExcelReferenceColumn>
                    {
                        new ExcelReferenceColumn("Department Code", orderedDepartments.Select(d => d.Code)),
                        new ExcelReferenceColumn("Department Name", orderedDepartments.Select(d => d.Name)),
                        new ExcelReferenceColumn("Designation Code", orderedDesignations.Select(d => d.Code)),
                        new ExcelReferenceColumn("Designation Name", orderedDesignations.Select(d => d.Name)),
                        new ExcelReferenceColumn("Designation's Department", orderedDesignations.Select(d => d.DepartmentName)),
                        new ExcelReferenceColumn("Status values", new[] { "Open", "Closed", "OnHold" }),
                    })
            };
        }

        [HttpGet]
        public IActionResult Import()
        {
            if (!_isAdmin)
                return Forbid();

            return View(new ExcelImportResult());
        }

        [HttpGet]
        public async Task<IActionResult> DownloadImportTemplate()
        {
            if (!_isAdmin)
                return Forbid();

            var departments = await _apiService.GetAsync<List<DepartmentListDto>>("department") ?? new();
            var designations = await _apiService.GetAsync<List<DesignationListDto>>("designation") ?? new();

            var bytes = _excelEngine.BuildTemplate(
                GetImportColumns(
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                    new Dictionary<(string, string), string>(),
                    new HashSet<(string, string)>()),
                sheetName: "Job Openings",
                referenceSheets: GetReferenceSheets(departments, designations));

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "JobOpening_Import_Template.xlsx");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (!_isAdmin)
                return Forbid();

            var result = new ExcelImportResult();

            var departments = await _apiService.GetAsync<List<DepartmentListDto>>("department") ?? new();
            var designations = await _apiService.GetAsync<List<DesignationListDto>>("designation") ?? new();

            var (departmentLookup, ambiguousDepartmentCodes) = BuildDepartmentLookup(departments);
            var (designationLookup, ambiguousDesignationCodes) = BuildDesignationLookup(designations);

            List<ExcelImportRow<JobOpeningDto>> rows;

            try
            {
                rows = _excelEngine.ReadRows(
                    file,
                    GetImportColumns(departmentLookup, ambiguousDepartmentCodes, designationLookup, ambiguousDesignationCodes));
            }
            catch (ExcelFileValidationException ex)
            {
                TempData["GlobalError"] = ex.Message;
                return View(result);
            }
            catch (Exception ex)
            {
                TempData["GlobalError"] = $"Unable to read the uploaded file: {ex.Message}";
                return View(result);
            }

            foreach (var row in rows)
            {
                var identifier = row.Item.Title;

                if (row.HasErrors)
                {
                    result.AddRow(row.RowNumber, identifier, false, string.Join(" ", row.ParseErrors));
                    continue;
                }

                try
                {
                    row.Item.TenantId = _tenantId;
                    row.Item.CreatedBy = _userId;

                    await _apiService.PostAsync<dynamic>("job-opening", row.Item);

                    result.AddRow(row.RowNumber, identifier, true, "Imported successfully.");
                }
                catch (ApiException apiEx)
                {
                    result.AddRow(row.RowNumber, identifier, false, GetErrorMessage(apiEx.ResponseContent));
                }
                catch (Exception ex)
                {
                    result.AddRow(row.RowNumber, identifier, false, ex.Message);
                }
            }

            if (result.TotalRows == 0)
                TempData["GlobalError"] = "The uploaded file didn't contain any job opening rows.";
            else if (result.FailureCount == 0)
                TempData["Success"] = $"All {result.SuccessCount} job opening(s) imported successfully.";
            else
                TempData["Info"] = $"{result.SuccessCount} of {result.TotalRows} job opening(s) imported. {result.FailureCount} failed - see details below.";

            return View(result);
        }

        private string GetErrorMessage(string json)
        {
            try
            {
                var obj = Newtonsoft.Json.Linq.JObject.Parse(json);

                if (obj["Message"] != null)
                    return obj["Message"]!.ToString();

                if (obj["Errors"] is Newtonsoft.Json.Linq.JArray errors && errors.Count > 0)
                    return errors[0]?.ToString();

                if (obj["errors"] is Newtonsoft.Json.Linq.JObject validationErrors)
                {
                    foreach (var property in validationErrors.Properties())
                    {
                        if (property.Value is Newtonsoft.Json.Linq.JArray arr && arr.Count > 0)
                            return arr[0]?.ToString();
                    }
                }
            }
            catch { }

            return "Import failed.";
        }

        #endregion

        #region Export

        [HttpGet]
        public async Task<IActionResult> Export()
        {
            if (!_isAdmin)
                return Forbid();

            var data = await _apiService.GetAsync<List<JobOpeningListDto>>("job-opening") ?? new();

            var bytes = _excelEngine.Export(data, GetExportColumns(), "Job Openings");

            var fileName = $"JobOpenings_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        #endregion
    }

    #endregion
}
