using APP.Attributes;
using APP.Excel;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Globalization;

namespace APP.Controllers
{
    #region Designation Controller

    [JwtAuthorize]
    public class DesignationController : Controller
    {
        private readonly IApiService _apiService;
        private readonly IExcelEngine _excelEngine;
        private string _tenantId;
        private string _userId;
        private string _companyId;
        private readonly bool _isAdmin;

        public DesignationController(IApiService apiService, IExcelEngine excelEngine)
        {
            _apiService = apiService;
            _excelEngine = excelEngine;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
            _companyId = SessionHelper.GetActiveCompanyId;
            _isAdmin = SessionHelper.IsAdminRole();
        }

        public async Task<IActionResult> Index()
        {
            var data = await _apiService
                .GetAsync<List<DesignationListDto>>("designation");

            // Department
            var departments = await _apiService
                .GetAsync<List<DropdownDto>>($"dropdown/department");

            ViewBag.DepartmentNames = departments.ToDictionary(x => x.Value, x => x.Text);

            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();
            return View(new DesignationDto());
        }

        [HttpPost]
        public async Task<IActionResult> Create(DesignationDto dto)
        {
            if (dto == null)
            {
                await LoadDropdowns();
                return View(dto);
            }

            dto.TenantId = _tenantId;
            dto.CreatedBy = _userId;

            await _apiService.PostAsync<dynamic>("designation", dto);

            TempData["Success"] = "Record saved successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var data = await _apiService
                .GetAsync<DesignationDto>($"designation/{id}");
            await LoadDropdowns(data.ParentDesignationId, data.CompanyId);
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var data = await _apiService
                .GetAsync<DesignationDto>($"designation/{id}");
            await LoadDropdowns(data.ParentDesignationId,data.CompanyId);
            return View("Create",data);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string id, DesignationDto dto)
        {
            if (!ModelState.IsValid)
            {
                await LoadDropdowns(dto.ParentDesignationId, dto.CompanyId);
                return View("Create", dto);
            }

            dto.TenantId = _tenantId;
            dto.CreatedBy = _userId;
            dto.ModifiedBy = _userId;
            dto.ModifiedOn = DateTime.UtcNow;

            await _apiService
                .PutAsync<dynamic>($"designation/{id}", dto);

            TempData["Success"] = "Record updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(string id)
        {
            await _apiService.DeleteAsync($"designation/{id}");

            return RedirectToAction(nameof(Index));
        }

        #region Load Dropdowns

        private async Task LoadDropdowns(string? designationId = null, string? companyId = null)
        {
            // Department
            var parentDesignations = await _apiService
                .GetAsync<List<DropdownDto>>($"dropdown/parent-designation?tenantId={_tenantId}&designationId={designationId}");

            ViewBag.ParentDesignationList = new SelectList(
                parentDesignations,
                "Value",
                "Text");

            ViewBag.ParentDesignations = parentDesignations.ToDictionary(x => x.Value, x => x.Text);

            // Department
            var departments = await _apiService
                .GetAsync<List<DropdownDto>>($"dropdown/department");

            ViewBag.DepartmentList = new SelectList(departments,"Value","Text");

            ViewBag.ParentDepartments = departments.ToDictionary(x => x.Value, x => x.Text);

            // =========================
            // Company Dropdown
            // =========================
            var companies = await _apiService
                .GetAsync<List<DropdownDto>>($"dropdown/company");

            ViewBag.CompanyList = new SelectList(
                companies,
                "Value",
                "Text");

            ViewBag.CompanyNames = companies.ToDictionary(x => x.Value, x => x.Text);

            // =========================
            // Brances Dropdown
            // =========================
            List<DropdownDto> branches = new();

            if (!string.IsNullOrEmpty(companyId))
            {
                branches = await _apiService
                    .GetAsync<List<DropdownDto>>(
                        $"dropdown/branch/{companyId}"
                    );
            }

            ViewBag.BranchList = branches.Select(x => new SelectListItem
            {
                Value = x.Value,
                Text = x.Text
            }).ToList();

            ViewBag.BranchNames = branches.ToDictionary(x => x.Value, x => x.Text);
        }

        #endregion

        [HttpGet]
        public async Task<JsonResult> GetBranchByCompanyId(string companyId)
        {
            var states = await _apiService
                .GetAsync<List<DropdownDto>>(
                    $"dropdown/branch/{companyId}"
                );

            var result = states.Select(x => new
            {
                value = x.Value,
                text = x.Text
            });

            return Json(result);
        }

        #region Import

        // Department is a required lookup here (every designation must
        // belong to one); Branch and Parent Designation are optional.
        // TenantId/CompanyId are always resolved from the admin's session,
        // never trusted from the uploaded file - same rule as Department
        // import above. All lookups match by Code, not Name, for the same
        // robustness reason used throughout this batch of modules.
        private static List<ExcelColumn<DesignationDto>> GetImportColumns(
            Dictionary<string, string> departmentLookup,
            HashSet<string> ambiguousDepartmentCodes,
            Dictionary<string, string> branchLookup,
            HashSet<string> ambiguousBranchCodes,
            Dictionary<(string DepartmentId, string Code), string> designationLookup,
            HashSet<(string DepartmentId, string Code)> ambiguousDesignationCodes)
        {
            return new List<ExcelColumn<DesignationDto>>
            {
                new ExcelColumn<DesignationDto>(
                    "Designation Name*",
                    d => d.Name,
                    (d, v) =>
                    {
                        if (string.IsNullOrWhiteSpace(v))
                            throw new Exception("Designation Name is required.");

                        d.Name = v.Trim();
                    },
                    isRequired: true,
                    sampleValue: "Software Engineer"),

                new ExcelColumn<DesignationDto>(
                    "Designation Code*",
                    d => d.Code,
                    (d, v) =>
                    {
                        if (string.IsNullOrWhiteSpace(v))
                            throw new Exception("Designation Code is required.");

                        d.Code = v.Trim();
                    },
                    isRequired: true,
                    sampleValue: "SE"),

                new ExcelColumn<DesignationDto>(
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

                new ExcelColumn<DesignationDto>(
                    "Branch Code",
                    d => d.BranchId,
                    (d, v) =>
                    {
                        var code = (v ?? string.Empty).Trim();

                        if (string.IsNullOrWhiteSpace(code))
                        {
                            d.BranchId = null;
                            return;
                        }

                        if (ambiguousBranchCodes.Contains(code))
                            throw new Exception($"Branch Code '{code}' matches more than one branch - ask an admin to make branch codes unique.");

                        if (!branchLookup.TryGetValue(code, out var branchId))
                            throw new Exception($"Branch Code '{code}' was not found for the active company. See the Reference Data sheet, or leave this blank.");

                        d.BranchId = branchId;
                    },
                    sampleValue: ""),

                new ExcelColumn<DesignationDto>(
                    "Parent Designation Code",
                    d => d.ParentDesignationId,
                    (d, v) =>
                    {
                        var code = (v ?? string.Empty).Trim();

                        if (string.IsNullOrWhiteSpace(code))
                        {
                            d.ParentDesignationId = null;
                            return;
                        }

                        // Scoped to this row's own (already-resolved) Department -
                        // the Department Code column above is applied first, so
                        // d.DepartmentId is populated by the time this runs. A
                        // parent designation is expected to belong to the same
                        // department; the same Code may legitimately exist under
                        // a different department without being ambiguous here.
                        if (string.IsNullOrWhiteSpace(d.DepartmentId))
                            throw new Exception($"Parent Designation Code '{code}' can't be resolved because the Department Code for this row is missing or invalid.");

                        var key = (d.DepartmentId, code);

                        if (ambiguousDesignationCodes.Contains(key))
                            throw new Exception($"Parent Designation Code '{code}' matches more than one existing designation in this department - ask an admin to make designation codes unique per department.");

                        if (!designationLookup.TryGetValue(key, out var parentId))
                            throw new Exception($"Parent Designation Code '{code}' was not found in this row's department. See the Reference Data sheet, or leave this blank.");

                        d.ParentDesignationId = parentId;
                    },
                    sampleValue: ""),

                new ExcelColumn<DesignationDto>(
                    "Level*",
                    d => d.Level,
                    (d, v) =>
                    {
                        if (string.IsNullOrWhiteSpace(v) ||
                            !int.TryParse(v.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var level) ||
                            level < 1 || level > 100)
                            throw new Exception("Level is required and must be a whole number between 1 and 100.");

                        d.Level = level;
                    },
                    isRequired: true,
                    sampleValue: 3),

                new ExcelColumn<DesignationDto>(
                    "Min Salary",
                    d => d.MinSalary,
                    (d, v) =>
                    {
                        if (string.IsNullOrWhiteSpace(v))
                        {
                            d.MinSalary = 0m;
                            return;
                        }

                        if (!decimal.TryParse(v.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var minSalary) || minSalary < 0)
                            throw new Exception($"Invalid Min Salary '{v}'. Use a non-negative number.");

                        d.MinSalary = minSalary;
                    },
                    sampleValue: 25000),

                new ExcelColumn<DesignationDto>(
                    "Max Salary",
                    d => d.MaxSalary,
                    (d, v) =>
                    {
                        if (string.IsNullOrWhiteSpace(v))
                        {
                            d.MaxSalary = 0m;
                            return;
                        }

                        if (!decimal.TryParse(v.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var maxSalary) || maxSalary < 0)
                            throw new Exception($"Invalid Max Salary '{v}'. Use a non-negative number.");

                        d.MaxSalary = maxSalary;
                    },
                    sampleValue: 45000),
            };
        }

        private static List<ExcelColumn<DesignationListDto>> GetExportColumns()
        {
            return new List<ExcelColumn<DesignationListDto>>
            {
                new ExcelColumn<DesignationListDto>("Designation Name", d => d.Name, (d, v) => d.Name = v ?? string.Empty),
                new ExcelColumn<DesignationListDto>("Designation Code", d => d.Code, (d, v) => d.Code = v ?? string.Empty),
                new ExcelColumn<DesignationListDto>("Department", d => d.DepartmentName, (d, v) => { }),
                new ExcelColumn<DesignationListDto>("Company", d => d.CompanyName, (d, v) => { }),
                new ExcelColumn<DesignationListDto>("Branch", d => d.BranchName, (d, v) => { }),
                new ExcelColumn<DesignationListDto>("Parent Designation", d => d.ParentDesignationName, (d, v) => { }),
                new ExcelColumn<DesignationListDto>("Level", d => d.Level, (d, v) => { }),
                new ExcelColumn<DesignationListDto>("Min Salary", d => d.MinSalary, (d, v) => { }),
                new ExcelColumn<DesignationListDto>("Max Salary", d => d.MaxSalary, (d, v) => { }),
            };
        }

        private static (Dictionary<string, string> Lookup, HashSet<string> Ambiguous) BuildCodeLookup(
            IEnumerable<(string? Code, string Id)> items)
        {
            var groups = items
                .Where(x => !string.IsNullOrWhiteSpace(x.Code))
                .GroupBy(x => x.Code!.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToList();

            var ambiguous = groups
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var lookup = groups
                .Where(g => g.Count() == 1)
                .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

            return (lookup, ambiguous);
        }

        // Parent Designation Code is looked up scoped to (DepartmentId, Code)
        // rather than by Code alone - a designation code is only required to
        // be unique within its own department (mirrors JobOpeningController's
        // BuildDesignationLookup), so e.g. "MGR" existing in both Sales and
        // Engineering is expected, not ambiguous.
        private static (Dictionary<(string DepartmentId, string Code), string> Lookup, HashSet<(string DepartmentId, string Code)> Ambiguous) BuildDepartmentScopedDesignationLookup(
            List<DesignationListDto> designations)
        {
            var groups = (designations ?? new List<DesignationListDto>())
                .Where(d => !string.IsNullOrWhiteSpace(d.Code) && !string.IsNullOrWhiteSpace(d.DepartmentId))
                .GroupBy(d => (DepartmentId: d.DepartmentId!, Code: d.Code!.Trim()), TupleComparer.Instance)
                .ToList();

            var ambiguous = groups
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToHashSet(TupleComparer.Instance);

            var lookup = groups
                .Where(g => g.Count() == 1)
                .ToDictionary(g => g.Key, g => g.First().Id, TupleComparer.Instance);

            return (lookup, ambiguous);
        }

        // Case-insensitive comparer for the (DepartmentId, Code) tuple key -
        // DepartmentId is an exact-match id (case doesn't matter either way)
        // but Code should match the same OrdinalIgnoreCase behavior used
        // everywhere else in this file.
        private sealed class TupleComparer : IEqualityComparer<(string DepartmentId, string Code)>
        {
            public static readonly TupleComparer Instance = new();

            public bool Equals((string DepartmentId, string Code) x, (string DepartmentId, string Code) y) =>
                string.Equals(x.DepartmentId, y.DepartmentId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.Code, y.Code, StringComparison.OrdinalIgnoreCase);

            public int GetHashCode((string DepartmentId, string Code) obj) =>
                HashCode.Combine(
                    obj.DepartmentId?.ToUpperInvariant(),
                    obj.Code?.ToUpperInvariant());
        }

        // "Reference Data" sheet listing valid Department Codes, Branch
        // Codes (for the active company) and existing Designation Codes
        // (for the Parent Designation column).
        private static List<ExcelTemplateReferenceSheet> GetReferenceSheets(
            List<DepartmentListDto> departments,
            List<BranchDto> branches,
            List<DesignationListDto> designations)
        {
            var orderedDepartments = (departments ?? new List<DepartmentListDto>()).OrderBy(d => d.Name).ToList();
            var orderedBranches = (branches ?? new List<BranchDto>()).OrderBy(b => b.Name).ToList();
            var orderedDesignations = (designations ?? new List<DesignationListDto>()).OrderBy(d => d.DepartmentName).ThenBy(d => d.Name).ToList();

            return new List<ExcelTemplateReferenceSheet>
            {
                new ExcelTemplateReferenceSheet(
                    "Reference Data",
                    new List<ExcelReferenceColumn>
                    {
                        new ExcelReferenceColumn("Department Code", orderedDepartments.Select(d => d.Code)),
                        new ExcelReferenceColumn("Department Name", orderedDepartments.Select(d => d.Name)),
                        new ExcelReferenceColumn("Branch Code", orderedBranches.Select(b => b.Code)),
                        new ExcelReferenceColumn("Branch Name", orderedBranches.Select(b => b.Name)),
                        new ExcelReferenceColumn("Designation Code", orderedDesignations.Select(d => d.Code)),
                        new ExcelReferenceColumn("Designation Name", orderedDesignations.Select(d => d.Name)),
                        new ExcelReferenceColumn("Designation's Department", orderedDesignations.Select(d => d.DepartmentName)),
                    })
            };
        }

        private async Task<List<BranchDto>> GetActiveCompanyBranches()
        {
            if (string.IsNullOrWhiteSpace(_companyId))
                return new List<BranchDto>();

            return await _apiService.GetAsync<List<BranchDto>>($"branch/company/{_companyId}") ?? new();
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
            var branches = await GetActiveCompanyBranches();
            var designations = await _apiService.GetAsync<List<DesignationListDto>>("designation") ?? new();

            var bytes = _excelEngine.BuildTemplate(
                GetImportColumns(
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                    new Dictionary<(string, string), string>(TupleComparer.Instance),
                    new HashSet<(string, string)>(TupleComparer.Instance)),
                sheetName: "Designations",
                referenceSheets: GetReferenceSheets(departments, branches, designations));

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Designation_Import_Template.xlsx");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (!_isAdmin)
                return Forbid();

            var result = new ExcelImportResult();

            var departments = await _apiService.GetAsync<List<DepartmentListDto>>("department") ?? new();
            var branches = await GetActiveCompanyBranches();
            var designations = await _apiService.GetAsync<List<DesignationListDto>>("designation") ?? new();

            var (departmentLookup, ambiguousDepartmentCodes) = BuildCodeLookup(
                departments.Select(d => (d.Code, d.Id)));

            var (branchLookup, ambiguousBranchCodes) = BuildCodeLookup(
                branches.Select(b => (b.Code, b.Id!)));

            var (designationLookup, ambiguousDesignationCodes) = BuildDepartmentScopedDesignationLookup(designations);

            List<ExcelImportRow<DesignationDto>> rows;

            try
            {
                rows = _excelEngine.ReadRows(
                    file,
                    GetImportColumns(
                        departmentLookup, ambiguousDepartmentCodes,
                        branchLookup, ambiguousBranchCodes,
                        designationLookup, ambiguousDesignationCodes));
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
                var identifier = string.Join(
                    " - ",
                    new[] { row.Item.Code, row.Item.Name }.Where(x => !string.IsNullOrWhiteSpace(x)));

                if (row.HasErrors)
                {
                    result.AddRow(row.RowNumber, identifier, false, string.Join(" ", row.ParseErrors));
                    continue;
                }

                try
                {
                    row.Item.TenantId = _tenantId;
                    row.Item.CompanyId = string.IsNullOrWhiteSpace(_companyId) ? null : _companyId;
                    row.Item.CreatedBy = _userId;

                    await _apiService.PostAsync<dynamic>("designation", row.Item);

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
                TempData["GlobalError"] = "The uploaded file didn't contain any designation rows.";
            else if (result.FailureCount == 0)
                TempData["Success"] = $"All {result.SuccessCount} designation(s) imported successfully.";
            else
                TempData["Info"] = $"{result.SuccessCount} of {result.TotalRows} designation(s) imported. {result.FailureCount} failed - see details below.";

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

            var data = await _apiService.GetAsync<List<DesignationListDto>>("designation") ?? new();

            var bytes = _excelEngine.Export(data, GetExportColumns(), "Designations");

            var fileName = $"Designations_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        #endregion
    }

    #endregion
}
