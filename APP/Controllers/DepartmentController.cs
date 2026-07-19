using APP.Attributes;
using APP.Excel;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace APP.Controllers
{
    #region Department Controller
    [JwtAuthorize]
    public class DepartmentController : Controller
    {
        private readonly IApiService _apiService;
        private readonly IExcelEngine _excelEngine;
        private string _tenantId;
        private string _userId;
        private string _companyId;
        private readonly bool _isAdmin;

        public DepartmentController(IApiService apiService, IExcelEngine excelEngine)
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
                .GetAsync<List<DepartmentListDto>>("department");

            await LoadDropdowns();

            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
             await LoadDropdowns();
            return View(new DepartmentDto());
        }

        [HttpPost]
        public async Task<IActionResult> Create(DepartmentDto dto)
        {
            if (!ModelState.IsValid || dto == null)
            {
                await LoadDropdowns();
                return View("Create", dto);
            }

            dto.TenantId = _tenantId;
            dto.CreatedBy = _userId;

            await _apiService.PostAsync<dynamic>("department", dto);

            TempData["Success"] = "Record saved successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var data = await _apiService
                .GetAsync<DepartmentDto>($"department/{id}");
            await LoadDropdowns(data.ParentDepartmentId, data.CompanyId);
            return View("Create",data);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string id, DepartmentDto dto)
        {
            if (!ModelState.IsValid)
            {
                await LoadDropdowns();
                return View("Create", dto);
            }

            dto.TenantId = _tenantId;
            dto.CreatedBy = _userId;
            dto.ModifiedBy = _userId;
            dto.ModifiedOn = DateTime.UtcNow;

            await _apiService
                .PutAsync<dynamic>($"department/{id}", dto);
            TempData["Success"] = "Record updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var data = await _apiService
                .GetAsync<DepartmentDto>($"department/{id}");
            await LoadDropdowns(data.ParentDepartmentId, data.CompanyId);
            return View(data);
        }

        public async Task<IActionResult> Delete(string id)
        {
            await _apiService.DeleteAsync($"department/{id}");

            return RedirectToAction(nameof(Index));
        }

        #region Load Dropdowns

        private async Task LoadDropdowns(string? deptId = null, string? companyId = null)
        {
            // Department
            var departments = await _apiService
                .GetAsync<List<DropdownDto>>($"dropdown/parent-department?tenantId={_tenantId}&departmentId={deptId}");

            ViewBag.ParentDepartmentList = new SelectList(
                departments,
                "Value",
                "Text");

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

        // TenantId and CompanyId are NEVER read from the uploaded file, even
        // though a column could exist for them - they're always resolved
        // from the logged-in admin's session (SessionHelper.GetActiveTenantId
        // / GetActiveCompanyId), exactly like every other admin action in
        // this controller (Create/Edit above). This keeps a bulk import
        // from being able to plant a department under a different
        // tenant/company than the one the importing admin is actually
        // working in.
        //
        // Branch and Parent Department are optional lookups, matched by
        // Code (not Name) for the same robustness reason as
        // Country/State/City above - Code is the short, regex-validated
        // identifier this app already treats as unique-ish per module,
        // rather than a free-text Name that's easier to mistype.
        private static List<ExcelColumn<DepartmentDto>> GetImportColumns(
            Dictionary<string, string> branchLookup,
            HashSet<string> ambiguousBranchCodes,
            Dictionary<string, string> departmentLookup,
            HashSet<string> ambiguousDepartmentCodes)
        {
            return new List<ExcelColumn<DepartmentDto>>
            {
                new ExcelColumn<DepartmentDto>(
                    "Department Name*",
                    d => d.Name,
                    (d, v) =>
                    {
                        if (string.IsNullOrWhiteSpace(v))
                            throw new Exception("Department Name is required.");

                        d.Name = v.Trim();
                    },
                    isRequired: true,
                    sampleValue: "Human Resources"),

                new ExcelColumn<DepartmentDto>(
                    "Department Code*",
                    d => d.Code,
                    (d, v) =>
                    {
                        if (string.IsNullOrWhiteSpace(v))
                            throw new Exception("Department Code is required.");

                        d.Code = v.Trim();
                    },
                    isRequired: true,
                    sampleValue: "HR"),

                new ExcelColumn<DepartmentDto>(
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

                new ExcelColumn<DepartmentDto>(
                    "Parent Department Code",
                    d => d.ParentDepartmentId,
                    (d, v) =>
                    {
                        var code = (v ?? string.Empty).Trim();

                        if (string.IsNullOrWhiteSpace(code))
                        {
                            d.ParentDepartmentId = null;
                            return;
                        }

                        if (ambiguousDepartmentCodes.Contains(code))
                            throw new Exception($"Parent Department Code '{code}' matches more than one existing department - ask an admin to make department codes unique.");

                        if (!departmentLookup.TryGetValue(code, out var parentId))
                            throw new Exception($"Parent Department Code '{code}' was not found. See the Reference Data sheet, or leave this blank for a root department.");

                        d.ParentDepartmentId = parentId;
                    },
                    sampleValue: ""),
            };
        }

        private static List<ExcelColumn<DepartmentListDto>> GetExportColumns()
        {
            return new List<ExcelColumn<DepartmentListDto>>
            {
                new ExcelColumn<DepartmentListDto>("Department Name", d => d.Name, (d, v) => d.Name = v ?? string.Empty),
                new ExcelColumn<DepartmentListDto>("Department Code", d => d.Code, (d, v) => d.Code = v ?? string.Empty),
                new ExcelColumn<DepartmentListDto>("Company", d => d.CompanyName, (d, v) => { }),
                new ExcelColumn<DepartmentListDto>("Branch", d => d.BranchName, (d, v) => { }),
                new ExcelColumn<DepartmentListDto>("Parent Department", d => d.ParentDepartmentName, (d, v) => { }),
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

        // "Reference Data" sheet listing the Branch Codes valid for the
        // admin's active company, plus every existing Department Code that
        // can be used as a Parent Department.
        private static List<ExcelTemplateReferenceSheet> GetReferenceSheets(
            List<BranchDto> branches,
            List<DepartmentListDto> departments)
        {
            var orderedBranches = (branches ?? new List<BranchDto>()).OrderBy(b => b.Name).ToList();
            var orderedDepartments = (departments ?? new List<DepartmentListDto>()).OrderBy(d => d.Name).ToList();

            return new List<ExcelTemplateReferenceSheet>
            {
                new ExcelTemplateReferenceSheet(
                    "Reference Data",
                    new List<ExcelReferenceColumn>
                    {
                        new ExcelReferenceColumn("Branch Code", orderedBranches.Select(b => b.Code)),
                        new ExcelReferenceColumn("Branch Name", orderedBranches.Select(b => b.Name)),
                        new ExcelReferenceColumn("Department Code", orderedDepartments.Select(d => d.Code)),
                        new ExcelReferenceColumn("Department Name", orderedDepartments.Select(d => d.Name)),
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

            var branches = await GetActiveCompanyBranches();
            var departments = await _apiService.GetAsync<List<DepartmentListDto>>("department") ?? new();

            var bytes = _excelEngine.BuildTemplate(
                GetImportColumns(
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase)),
                sheetName: "Departments",
                referenceSheets: GetReferenceSheets(branches, departments));

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Department_Import_Template.xlsx");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (!_isAdmin)
                return Forbid();

            var result = new ExcelImportResult();

            var branches = await GetActiveCompanyBranches();
            var departments = await _apiService.GetAsync<List<DepartmentListDto>>("department") ?? new();

            var (branchLookup, ambiguousBranchCodes) = BuildCodeLookup(
                branches.Select(b => (b.Code, b.Id!)));

            var (departmentLookup, ambiguousDepartmentCodes) = BuildCodeLookup(
                departments.Select(d => (d.Code, d.Id)));

            List<ExcelImportRow<DepartmentDto>> rows;

            try
            {
                rows = _excelEngine.ReadRows(
                    file,
                    GetImportColumns(branchLookup, ambiguousBranchCodes, departmentLookup, ambiguousDepartmentCodes));
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

                    await _apiService.PostAsync<dynamic>("department", row.Item);

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
                TempData["GlobalError"] = "The uploaded file didn't contain any department rows.";
            else if (result.FailureCount == 0)
                TempData["Success"] = $"All {result.SuccessCount} department(s) imported successfully.";
            else
                TempData["Info"] = $"{result.SuccessCount} of {result.TotalRows} department(s) imported. {result.FailureCount} failed - see details below.";

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

            var data = await _apiService.GetAsync<List<DepartmentListDto>>("department") ?? new();

            var bytes = _excelEngine.Export(data, GetExportColumns(), "Departments");

            var fileName = $"Departments_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        #endregion

    }

    #endregion
}
