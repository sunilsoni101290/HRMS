using APP.Attributes;
using APP.Excel;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.Design;

namespace APP.Controllers
{
    [JwtAuthorize]
    public class LocationController : Controller
    {
        private readonly IApiService _apiService;
        private readonly IExcelEngine _excelEngine;
        private string _tenantId;
        private string _userId;
        private string _companyId;
        private readonly bool _isAdmin;

        public LocationController(IApiService apiService, IExcelEngine excelEngine)
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
            var data = await _apiService.GetAsync<List<LocationDto>>(
                "location"
            );

            return View(data);
        }
        // =====================================================
        // CREATE - GET
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();

            return View(new LocationDto());
        }

        // =====================================================
        // CREATE - POST
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LocationDto dto)
        {
            try
            {
                if (dto != null)
                {
                    await LoadDropdowns();

                    dto.CreatedBy = _userId;
                    dto.TenantId = _tenantId;

                    await _apiService.PostAsync<LocationDto>(
                        "location",
                        dto
                    );

                    TempData["Success"] = "Branch created successfully.";

                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;

                await LoadDropdowns();

                return RedirectToAction(nameof(Index));
            }
            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // EDIT - GET
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var data = await _apiService.GetAsync<LocationDto>(
                $"location/{id}"
            );

            if (data == null)
            {
                return NotFound();
            }

            await LoadDropdowns(data.CompanyId);

            return View("Create", data);
        }

        // =====================================================
        // EDIT - POST
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(LocationDto dto)
        {
            try
            {
                if (dto != null && !string.IsNullOrEmpty(dto.Id))
                {
                    await LoadDropdowns(dto.CompanyId);

                    dto.CreatedBy = _userId;
                    dto.TenantId = _tenantId;
                    dto.ModifiedOn = DateTime.UtcNow;
                    dto.ModifiedBy = _userId;

                    await _apiService.PutAsync<dynamic>($"location/{dto.Id}", dto);

                    TempData["Success"] = "Branch updated successfully.";

                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;

                await LoadDropdowns();

                return View("Create",dto);
            }
            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // DETAILS
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var data = await _apiService.GetAsync<LocationDto>(
                $"location/{id}"
            );

            return View(data);
        }

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
   
        #region Private method
        private async Task LoadDropdowns(string? selectedCompanyId = null)
        {

            // =========================
            // Company Dropdown
            // =========================
            var companies = await _apiService
                .GetAsync<List<DropdownDto>>($"dropdown/company");

            ViewBag.CompanyList = new SelectList(
                companies,
                "Value",
                "Text");

            // =========================
            // Brances Dropdown
            // =========================
            List<DropdownDto> branches = new();

            if (!string.IsNullOrEmpty(selectedCompanyId))
            {
                branches = await _apiService
                    .GetAsync<List<DropdownDto>>(
                        $"dropdown/branch/{selectedCompanyId}"
                    );
            }

            ViewBag.BranchList = branches.Select(x => new SelectListItem
            {
                Value = x.Value,
                Text = x.Text
            }).ToList();

        }
        #endregion

        #region Import

        // Location import needs to resolve a Branch Code to a BranchId.
        // Branch is scoped to the admin's active session company - the same
        // rule Department/Designation import used for their own Branch
        // lookups - so a Location import run can only attach locations to
        // branches that belong to the company currently selected in the
        // top bar. Company/Tenant are always taken from the session, never
        // from the uploaded file.
        private static List<ExcelColumn<LocationDto>> GetImportColumns(
            Dictionary<string, string> branchLookup,
            HashSet<string> ambiguousBranchCodes)
        {
            return new List<ExcelColumn<LocationDto>>
            {
                new ExcelColumn<LocationDto>(
                    "Location Name*",
                    d => d.LocationName,
                    (d, v) =>
                    {
                        if (string.IsNullOrWhiteSpace(v))
                            throw new Exception("Location Name is required.");
                        d.LocationName = v.Trim();
                    },
                    isRequired: true,
                    sampleValue: "Head Office - Reception"),

                new ExcelColumn<LocationDto>(
                    "Location Code",
                    d => d.LocationCode,
                    (d, v) => d.LocationCode = string.IsNullOrWhiteSpace(v) ? null : v.Trim(),
                    sampleValue: "LOC01"),

                new ExcelColumn<LocationDto>(
                    "Branch Code*",
                    d => d.BranchId,
                    (d, v) =>
                    {
                        var code = (v ?? string.Empty).Trim();

                        if (string.IsNullOrWhiteSpace(code))
                            throw new Exception("Branch Code is required.");

                        if (ambiguousBranchCodes.Contains(code))
                            throw new Exception($"Branch Code '{code}' matches more than one branch for the active company - ask an admin to make branch codes unique.");

                        if (!branchLookup.TryGetValue(code, out var branchId))
                            throw new Exception($"Branch Code '{code}' was not found for the active company. See the Reference Data sheet for valid codes.");

                        d.BranchId = branchId;
                    },
                    isRequired: true,
                    sampleValue: "MUM01"),

                new ExcelColumn<LocationDto>(
                    "Address",
                    d => d.Address,
                    (d, v) => d.Address = string.IsNullOrWhiteSpace(v) ? null : v.Trim(),
                    sampleValue: ""),

                new ExcelColumn<LocationDto>(
                    "Is Default (Yes/No)",
                    d => d.IsDefault ? "Yes" : "No",
                    (d, v) => d.IsDefault = ParseYesNo(v),
                    sampleValue: "No"),
            };
        }

        private static List<ExcelColumn<LocationDto>> GetExportColumns()
        {
            return new List<ExcelColumn<LocationDto>>
            {
                new ExcelColumn<LocationDto>("Location Name", d => d.LocationName, (d, v) => { }),
                new ExcelColumn<LocationDto>("Location Code", d => d.LocationCode, (d, v) => { }),
                new ExcelColumn<LocationDto>("Company", d => d.CompanyName, (d, v) => { }),
                new ExcelColumn<LocationDto>("Branch", d => d.BranchName, (d, v) => { }),
                new ExcelColumn<LocationDto>("Address", d => d.Address, (d, v) => { }),
                new ExcelColumn<LocationDto>("Is Default", d => d.IsDefault ? "Yes" : "No", (d, v) => { }),
            };
        }

        private static bool ParseYesNo(string? text, bool defaultValue = false)
        {
            if (string.IsNullOrWhiteSpace(text))
                return defaultValue;

            return text.Trim().ToLowerInvariant() switch
            {
                "yes" or "y" or "true" or "1" => true,
                "no" or "n" or "false" or "0" => false,
                _ => throw new Exception($"Invalid value '{text}'. Use Yes or No.")
            };
        }

        private static (Dictionary<string, string> Lookup, HashSet<string> Ambiguous) BuildCodeLookup(
            IEnumerable<(string? Code, string Id)> items)
        {
            var groups = items
                .Where(x => !string.IsNullOrWhiteSpace(x.Code))
                .GroupBy(x => x.Code!.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToList();

            var ambiguous = groups.Where(g => g.Count() > 1).Select(g => g.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var lookup = groups.Where(g => g.Count() == 1)
                .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

            return (lookup, ambiguous);
        }

        private static List<ExcelTemplateReferenceSheet> GetReferenceSheets(List<BranchDto> branches)
        {
            var ordered = (branches ?? new List<BranchDto>()).OrderBy(b => b.Name).ToList();

            return new List<ExcelTemplateReferenceSheet>
            {
                new ExcelTemplateReferenceSheet(
                    "Reference Data",
                    new List<ExcelReferenceColumn>
                    {
                        new ExcelReferenceColumn("Branch Code", ordered.Select(b => b.Code)),
                        new ExcelReferenceColumn("Branch Name", ordered.Select(b => b.Name)),
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

            var bytes = _excelEngine.BuildTemplate(
                GetImportColumns(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase), new HashSet<string>(StringComparer.OrdinalIgnoreCase)),
                sheetName: "Locations",
                referenceSheets: GetReferenceSheets(branches));

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Location_Import_Template.xlsx");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (!_isAdmin)
                return Forbid();

            var result = new ExcelImportResult();

            var branches = await GetActiveCompanyBranches();
            var (branchLookup, ambiguousBranchCodes) = BuildCodeLookup(branches.Select(b => (b.Code, b.Id!)));

            List<ExcelImportRow<LocationDto>> rows;

            try
            {
                rows = _excelEngine.ReadRows(file, GetImportColumns(branchLookup, ambiguousBranchCodes));
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
                    new[] { row.Item.LocationCode, row.Item.LocationName }.Where(x => !string.IsNullOrWhiteSpace(x)));

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

                    await _apiService.PostAsync<dynamic>("location", row.Item);

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
                TempData["GlobalError"] = "The uploaded file didn't contain any location rows.";
            else if (result.FailureCount == 0)
                TempData["Success"] = $"All {result.SuccessCount} location(s) imported successfully.";
            else
                TempData["Info"] = $"{result.SuccessCount} of {result.TotalRows} location(s) imported. {result.FailureCount} failed - see details below.";

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

            var data = await _apiService.GetAsync<List<LocationDto>>("location") ?? new();

            var bytes = _excelEngine.Export(data, GetExportColumns(), "Locations");

            var fileName = $"Locations_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        #endregion
    }
}
