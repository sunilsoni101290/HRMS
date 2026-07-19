using APP.Attributes;
using APP.Excel;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace APP.Controllers
{
    #region City Controller
    [JwtAuthorize]
    public class CityController : Controller
    {
        private readonly IApiService _apiService;
        private readonly IExcelEngine _excelEngine;
        private string _tenantId;
        private string _userId;
        private readonly bool _isAdmin;

        public CityController(IApiService apiService, IExcelEngine excelEngine)
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
            var data = await _apiService.GetAsync<List<CityDto>>(
                "city"
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

            return View(new CityDto());
        }

        // =====================================================
        // CREATE - POST
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CityDto dto)
        {
            try
            {
                if (dto!=null)
                {
                    await LoadDropdowns();


                    dto.CreatedBy = _userId;
                    dto.TenantId = _tenantId;

                    await _apiService.PostAsync<CityDto>(
                        "city",
                        dto
                    );
                    AlertHelper.Success(TempData, "City created successfully.");
                    return View(dto);
                }

            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;

                await LoadDropdowns();

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

            var data = await _apiService.GetAsync<CityDto>(
                $"city/{id}"
            );

            if (data == null)
            {
                return NotFound();
            }

            await LoadDropdowns(data.CountryId);

            return View("Create",data);
        }

        // =====================================================
        // EDIT - POST
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CityDto dto)
        {
            try
            {
                if (!string.IsNullOrEmpty(dto.Id) && dto!=null)
                {
                    await LoadDropdowns();

                    dto.CreatedBy = _userId;
                    dto.TenantId = _tenantId;
                    dto.ModifiedOn = DateTime.UtcNow;
                    dto.ModifiedBy = _userId;

                    await _apiService.PutAsync<dynamic>(
                        $"city/{dto.Id}",
                        dto
                    );

                    AlertHelper.Success(TempData, "City updated successfully.");

                    return View("Create", dto);
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
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var data = await _apiService.GetAsync<CityDto>(
                $"city/{id}"
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
                    $"city/{id}"
                );

                TempData["Success"] = "City deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<JsonResult> GetStatesByCountry(string countryId)
        {
            var states = await _apiService
                .GetAsync<List<DropdownDto>>(
                    $"dropdown/state/{countryId}"
                );

            var result = states.Select(x => new
            {
                value = x.Value,
                text = x.Text
            });

            return Json(result);
        }

        // =====================================================
        // LOAD STATE DROPDOWN
        // =====================================================
        private async Task LoadDropdowns(string? selectedCountryId = null)
        {
            // =========================
            // Country Dropdown
            // =========================
            var countries = await _apiService
                .GetAsync<List<DropdownDto>>("dropdown/country");

            ViewBag.CountryList = countries.Select(x => new SelectListItem
            {
                Value = x.Value,
                Text = x.Text
            }).ToList();

            // =========================
            // State Dropdown
            // =========================
            List<DropdownDto> states = new();

            if (!string.IsNullOrEmpty(selectedCountryId))
            {
                states = await _apiService
                    .GetAsync<List<DropdownDto>>(
                        $"dropdown/state/{selectedCountryId}"
                    );
            }

            ViewBag.StateList = states.Select(x => new SelectListItem
            {
                Value = x.Value,
                Text = x.Text
            }).ToList();
        }

        #region Import

        // City import needs to resolve a State cell to a StateId. State
        // Name is NOT used for this lookup: State.Name/Code have no unique
        // index anywhere in the schema (see ApplicationDbContext), so two
        // states in different countries could plausibly share the same
        // name (or even the same short code). We require "State Code"
        // rather than "State Name" since Code is the shorter, more
        // deliberate identifier - and we still defend against a Code
        // collision explicitly below rather than assuming Codes are unique
        // either.
        //
        // stateLookup: State Code (trimmed, case-insensitive) -> StateId.
        // ambiguousCodes: codes shared by more than one state - any row
        // using one of these fails clearly instead of resolving to the
        // wrong state.
        private static List<ExcelColumn<CityDto>> GetImportColumns(
            Dictionary<string, string> stateLookup,
            HashSet<string> ambiguousCodes)
        {
            return new List<ExcelColumn<CityDto>>
            {
                new ExcelColumn<CityDto>(
                    "City Name*",
                    d => d.Name,
                    (d, v) =>
                    {
                        if (string.IsNullOrWhiteSpace(v))
                            throw new Exception("City Name is required.");

                        d.Name = v.Trim();
                    },
                    isRequired: true,
                    sampleValue: "Mumbai"),

                new ExcelColumn<CityDto>(
                    "State Code*",
                    d => d.StateId,
                    (d, v) =>
                    {
                        var code = (v ?? string.Empty).Trim();

                        if (string.IsNullOrWhiteSpace(code))
                            throw new Exception("State Code is required.");

                        if (ambiguousCodes.Contains(code))
                            throw new Exception(
                                $"State Code '{code}' matches more than one state (in different countries) - ask an admin to make state codes unique.");

                        if (!stateLookup.TryGetValue(code, out var stateId))
                            throw new Exception(
                                $"State Code '{code}' was not found. See the Reference Data sheet for valid codes.");

                        d.StateId = stateId;
                    },
                    isRequired: true,
                    sampleValue: "MH"),
            };
        }

        private static List<ExcelColumn<CityDto>> GetExportColumns()
        {
            return new List<ExcelColumn<CityDto>>
            {
                new ExcelColumn<CityDto>("City Name", d => d.Name, (d, v) => d.Name = v ?? string.Empty),
                new ExcelColumn<CityDto>("State", d => d.StateName, (d, v) => { }),
                new ExcelColumn<CityDto>("Country", d => d.CountryName, (d, v) => { }),
            };
        }

        private static (Dictionary<string, string> Lookup, HashSet<string> Ambiguous) BuildStateLookup(
            List<StateDto> states)
        {
            var groups = (states ?? new List<StateDto>())
                .Where(s => !string.IsNullOrWhiteSpace(s.Code))
                .GroupBy(s => s.Code.Trim(), StringComparer.OrdinalIgnoreCase)
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

        // "Reference Data" sheet listing every valid State Code alongside
        // its State Name and Country, so the person filling in the
        // template can tell same-named/-coded states in different
        // countries apart.
        private static List<ExcelTemplateReferenceSheet> GetReferenceSheets(List<StateDto> states)
        {
            var ordered = (states ?? new List<StateDto>())
                .OrderBy(s => s.CountryName)
                .ThenBy(s => s.Name)
                .ToList();

            return new List<ExcelTemplateReferenceSheet>
            {
                new ExcelTemplateReferenceSheet(
                    "Reference Data",
                    new List<ExcelReferenceColumn>
                    {
                        new ExcelReferenceColumn("State Code", ordered.Select(s => s.Code)),
                        new ExcelReferenceColumn("State Name", ordered.Select(s => s.Name)),
                        new ExcelReferenceColumn("Country", ordered.Select(s => s.CountryName)),
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

            var states = await _apiService.GetAsync<List<StateDto>>("state") ?? new();

            var bytes = _excelEngine.BuildTemplate(
                GetImportColumns(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase), new HashSet<string>(StringComparer.OrdinalIgnoreCase)),
                sheetName: "Cities",
                referenceSheets: GetReferenceSheets(states));

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "City_Import_Template.xlsx");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (!_isAdmin)
                return Forbid();

            var result = new ExcelImportResult();

            var states = await _apiService.GetAsync<List<StateDto>>("state") ?? new();
            var (stateLookup, ambiguousCodes) = BuildStateLookup(states);

            List<ExcelImportRow<CityDto>> rows;

            try
            {
                rows = _excelEngine.ReadRows(file, GetImportColumns(stateLookup, ambiguousCodes));
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
                var identifier = row.Item.Name;

                if (row.HasErrors)
                {
                    result.AddRow(row.RowNumber, identifier, false, string.Join(" ", row.ParseErrors));
                    continue;
                }

                try
                {
                    row.Item.TenantId = _tenantId;
                    row.Item.CreatedBy = _userId;

                    await _apiService.PostAsync<dynamic>("city", row.Item);

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
                TempData["GlobalError"] = "The uploaded file didn't contain any city rows.";
            else if (result.FailureCount == 0)
                TempData["Success"] = $"All {result.SuccessCount} cit{(result.SuccessCount == 1 ? "y" : "ies")} imported successfully.";
            else
                TempData["Info"] = $"{result.SuccessCount} of {result.TotalRows} cities imported. {result.FailureCount} failed - see details below.";

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

            var data = await _apiService.GetAsync<List<CityDto>>("city") ?? new();

            var bytes = _excelEngine.Export(data, GetExportColumns(), "Cities");

            var fileName = $"Cities_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        #endregion
    }

    #endregion
}
