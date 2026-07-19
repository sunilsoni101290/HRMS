using APP.Attributes;
using APP.Excel;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace APP.Controllers
{
    #region State Controller
    [JwtAuthorize]

    public class StateController : Controller
    {
        private readonly IApiService _apiService;
        private readonly IExcelEngine _excelEngine;
        private string _tenantId;
        private string _userId;
        private readonly bool _isAdmin;

        public StateController(IApiService apiService, IExcelEngine excelEngine)
        {
            _apiService = apiService;
            _excelEngine = excelEngine;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
            _isAdmin = SessionHelper.IsAdminRole();
        }

        // =====================================================
        // INDEX
        // =====================================================
        public async Task<IActionResult> Index()
        {
            var data = await _apiService.GetAsync<List<StateDto>>("state");

            return View(data);
        }

        // =====================================================
        // CREATE - GET
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadCountryDropdown();

            return View(new StateDto());
        }

        // =====================================================
        // CREATE - POST
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StateDto dto)
        {
            try
            {
                if (dto != null)
                {
                    await LoadCountryDropdown();

                    dto.CreatedBy = _userId;
                    dto.TenantId = _tenantId;

                    await _apiService.PostAsync<StateDto>(
                        "state",
                        dto
                    );

                    TempData["Success"] = "State created successfully.";

                    return View("Create",dto);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;

                await LoadCountryDropdown();

                return View(dto);
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

            var data = await _apiService.GetAsync<StateDto>(
                $"state/{id}"
            );

            if (data == null)
            {
                return NotFound();
            }

            await LoadCountryDropdown();

           return View("Create", data);
        }

        // =====================================================
        // EDIT - POST
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(StateDto dto)
        {
            try
            {
                if (dto!=null && !string.IsNullOrEmpty(dto.Id))
                {
                    await LoadCountryDropdown();

                    dto.ModifiedOn = DateTime.UtcNow;
                    dto.ModifiedBy = _userId;
                    dto.CreatedBy = _userId;

                    await _apiService.PutAsync<dynamic>(
                        $"state/{dto.Id}",
                        dto
                    );

                    TempData["Success"] = "State updated successfully.";
                    return View("Create", dto);
                }
                
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;

                await LoadCountryDropdown();

                return View("Create", dto);
            }
            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // DETAILS
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var data = await _apiService.GetAsync<StateDto>(
                $"state/{id}"
            );

            if (data == null)
            {
                return NotFound();
            }

            return View(data);
        }

        // =====================================================
        // DELETE
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return NotFound();
                }

                await _apiService.DeleteAsync(
                    $"state/{id}"
                );

                TempData["Success"] = "State deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // LOAD COUNTRY DROPDOWN
        // =====================================================
        private async Task LoadCountryDropdown()
        {
            var countries = await _apiService.GetAsync<List<DropdownDto>>($"dropdown/country");

            ViewBag.CountryList = countries.Select(x => new SelectListItem
            {
                Value = x.Value,
                Text = x.Text
            }).ToList();
        }

        #region Import

        // State import needs to resolve a "Country Code" cell to a
        // CountryId server-side. Country Code is used rather than Country
        // Name because it's the shorter, less ambiguous identifier the app
        // already treats as the stable key for a country (matches the
        // "Country Code*" label shown in the Country module itself).
        //
        // countryLookup: Country Code (trimmed, case-insensitive) -> CountryId.
        // ambiguousCodes: codes that matched more than one country - any row
        // using one of these fails clearly instead of silently picking the
        // wrong country.
        private static List<ExcelColumn<StateDto>> GetImportColumns(
            Dictionary<string, string> countryLookup,
            HashSet<string> ambiguousCodes)
        {
            return new List<ExcelColumn<StateDto>>
            {
                new ExcelColumn<StateDto>(
                    "State Name*",
                    d => d.Name,
                    (d, v) =>
                    {
                        if (string.IsNullOrWhiteSpace(v))
                            throw new Exception("State Name is required.");

                        d.Name = v.Trim();
                    },
                    isRequired: true,
                    sampleValue: "Maharashtra"),

                new ExcelColumn<StateDto>(
                    "State Code*",
                    d => d.Code,
                    (d, v) =>
                    {
                        if (string.IsNullOrWhiteSpace(v))
                            throw new Exception("State Code is required.");

                        d.Code = v.Trim();
                    },
                    isRequired: true,
                    sampleValue: "MH"),

                new ExcelColumn<StateDto>(
                    "Country Code*",
                    d => d.CountryId,
                    (d, v) =>
                    {
                        var code = (v ?? string.Empty).Trim();

                        if (string.IsNullOrWhiteSpace(code))
                            throw new Exception("Country Code is required.");

                        if (ambiguousCodes.Contains(code))
                            throw new Exception(
                                $"Country Code '{code}' matches more than one country - ask an admin to make country codes unique.");

                        if (!countryLookup.TryGetValue(code, out var countryId))
                            throw new Exception(
                                $"Country Code '{code}' was not found. See the Reference Data sheet for valid codes.");

                        d.CountryId = countryId;
                    },
                    isRequired: true,
                    sampleValue: "IN"),

                new ExcelColumn<StateDto>(
                    "GST State Code*",
                    d => d.GSTStateCode,
                    (d, v) =>
                    {
                        if (string.IsNullOrWhiteSpace(v))
                            throw new Exception("GST State Code is required.");

                        d.GSTStateCode = v.Trim();
                    },
                    isRequired: true,
                    sampleValue: "27"),
            };
        }

        private static List<ExcelColumn<StateDto>> GetExportColumns()
        {
            return new List<ExcelColumn<StateDto>>
            {
                new ExcelColumn<StateDto>("State Name", d => d.Name, (d, v) => d.Name = v ?? string.Empty),
                new ExcelColumn<StateDto>("State Code", d => d.Code, (d, v) => d.Code = v ?? string.Empty),
                new ExcelColumn<StateDto>("Country", d => d.CountryName, (d, v) => { }),
                new ExcelColumn<StateDto>("GST State Code", d => d.GSTStateCode, (d, v) => d.GSTStateCode = v ?? string.Empty),
            };
        }

        // Builds the Country Code -> CountryId lookup used by the "Country
        // Code*" column's Setter, plus the set of any codes that turned out
        // to be ambiguous (shared by two or more countries) so those rows
        // can be failed with a clear message instead of resolved to the
        // wrong country.
        private static (Dictionary<string, string> Lookup, HashSet<string> Ambiguous) BuildCountryLookup(
            List<CountryDto> countries)
        {
            var groups = (countries ?? new List<CountryDto>())
                .Where(c => !string.IsNullOrWhiteSpace(c.Code))
                .GroupBy(c => c.Code.Trim(), StringComparer.OrdinalIgnoreCase)
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

        // "Reference Data" sheet listing every valid Country Code side by
        // side with its Country Name, so the person filling in the template
        // knows exactly what to type in the "Country Code*" column.
        private static List<ExcelTemplateReferenceSheet> GetReferenceSheets(List<CountryDto> countries)
        {
            var ordered = (countries ?? new List<CountryDto>())
                .OrderBy(c => c.Name)
                .ToList();

            return new List<ExcelTemplateReferenceSheet>
            {
                new ExcelTemplateReferenceSheet(
                    "Reference Data",
                    new List<ExcelReferenceColumn>
                    {
                        new ExcelReferenceColumn("Country Code", ordered.Select(c => c.Code)),
                        new ExcelReferenceColumn("Country Name", ordered.Select(c => c.Name)),
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

            var countries = await _apiService.GetAsync<List<CountryDto>>("Country") ?? new();

            var bytes = _excelEngine.BuildTemplate(
                GetImportColumns(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase), new HashSet<string>(StringComparer.OrdinalIgnoreCase)),
                sheetName: "States",
                referenceSheets: GetReferenceSheets(countries));

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "State_Import_Template.xlsx");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (!_isAdmin)
                return Forbid();

            var result = new ExcelImportResult();

            // Fetched once up front - every row's Country Code is resolved
            // against this same in-memory lookup instead of hitting the API
            // per row.
            var countries = await _apiService.GetAsync<List<CountryDto>>("Country") ?? new();
            var (countryLookup, ambiguousCodes) = BuildCountryLookup(countries);

            List<ExcelImportRow<StateDto>> rows;

            try
            {
                rows = _excelEngine.ReadRows(file, GetImportColumns(countryLookup, ambiguousCodes));
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
                    row.Item.CreatedBy = _userId;

                    await _apiService.PostAsync<dynamic>("state", row.Item);

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
                TempData["GlobalError"] = "The uploaded file didn't contain any state rows.";
            else if (result.FailureCount == 0)
                TempData["Success"] = $"All {result.SuccessCount} state(s) imported successfully.";
            else
                TempData["Info"] = $"{result.SuccessCount} of {result.TotalRows} state(s) imported. {result.FailureCount} failed - see details below.";

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

            var data = await _apiService.GetAsync<List<StateDto>>("state") ?? new();

            var bytes = _excelEngine.Export(data, GetExportColumns(), "States");

            var fileName = $"States_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        #endregion
    }

    #endregion
}
