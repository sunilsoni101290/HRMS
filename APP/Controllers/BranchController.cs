using APP.Attributes;
using APP.Excel;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Globalization;
using static APP.Helpers.EnumExtensions;

namespace APP.Controllers
{
    [JwtAuthorize]
    public class BranchController : Controller
    {
        private readonly IApiService _apiService;
        private readonly IExcelEngine _excelEngine;
        private string _tenantId;
        private string _userId;
        private string _companyId;
        private readonly bool _isAdmin;

        public BranchController(IApiService apiService, IExcelEngine excelEngine)
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
            var data =  await _apiService.GetAsync<List<BranchDto>>(
                "branch"
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

            return View(new BranchDto());
        }

        // =====================================================
        // CREATE - POST
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BranchDto dto)
        {
            try
            {
                if (dto != null)
                {
                    await LoadDropdowns();

                    dto.CreatedBy = _userId;
                    dto.TenantId = _tenantId;

                    await _apiService.PostAsync<BranchDto>(
                        "branch",
                        dto
                    );

                    TempData["Success"] = "Branch created successfully.";

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

            var data = await _apiService.GetAsync<BranchDto>(
                $"branch/{id}"
            );

            if (data == null)
            {
                return NotFound();
            }

            await LoadDropdowns(data.CountryId, data.StateId);

            return View("Create", data);
        }

        // =====================================================
        // EDIT - POST
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(BranchDto dto)
        {
            try
            {
                if (dto != null && !string.IsNullOrEmpty(dto.Id))
                {
                    await LoadDropdowns(dto.CountryId, dto.StateId);

                    dto.CreatedBy = _userId;
                    dto.TenantId = _tenantId;
                    dto.ModifiedOn = DateTime.UtcNow;
                    dto.ModifiedBy = _userId;

                    await _apiService.PutAsync<dynamic>($"branch/{dto.Id}", dto);

                    TempData["Success"] = "Branch updated successfully.";
                    return View("Create", dto);
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
        // DETAILS
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var data = await _apiService.GetAsync<BranchDto>(
                $"branch/{id}"
            );

            return View(data);
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

        [HttpGet]
        public async Task<JsonResult> GetCityByState(string stateId)
        {
            var states = await _apiService
                .GetAsync<List<DropdownDto>>(
                    $"dropdown/city/{stateId}"
                );

            var result = states.Select(x => new
            {
                value = x.Value,
                text = x.Text
            });

            return Json(result);
        }


        #region Private method
        private async Task LoadDropdowns(string? selectedCountryId = null, string? selectedStatedId = null)
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


            // ==============================
            // CITY
            // ==============================
            List<DropdownDto> cities = new();
            if (!string.IsNullOrEmpty(selectedStatedId))
            {
                cities = await _apiService
                    .GetAsync<List<DropdownDto>>(
                        $"dropdown/city/{selectedStatedId}"
                    );
            }

            ViewBag.CityList = cities.Select(x => new SelectListItem
            {
                Value = x.Value,
                Text = x.Text
            }).ToList();


        }
        #endregion

        #region Import

        // Branch import resolves a 3-level cascading location: Country Code
        // -> State Code -> City Name. Company/Tenant are ALWAYS the admin's
        // active session company/tenant, never trusted from the uploaded
        // file - an import run always creates branches under whatever
        // company is currently selected in the top bar.
        //
        // State Code is scoped to the resolved Country (ambiguity is
        // checked per-country, not globally) because the Country column is
        // already resolved earlier in the same row - two states sharing a
        // code under different countries is fine here, since Country
        // disambiguates it. City has no Code column anywhere in this schema
        // (see Domain.Entities.City - Name only), so City is matched by
        // Name, scoped to the resolved State for the same reason.
        private static List<ExcelColumn<BranchDto>> GetImportColumns(
            Dictionary<string, string> countryLookup,
            HashSet<string> ambiguousCountryCodes,
            Dictionary<(string CountryId, string Code), string> stateLookup,
            HashSet<(string CountryId, string Code)> ambiguousStateCodes,
            Dictionary<(string StateId, string Name), string> cityLookup,
            HashSet<(string StateId, string Name)> ambiguousCityNames)
        {
            return new List<ExcelColumn<BranchDto>>
            {
                new ExcelColumn<BranchDto>(
                    "Branch Name*",
                    d => d.Name,
                    (d, v) =>
                    {
                        if (string.IsNullOrWhiteSpace(v))
                            throw new Exception("Branch Name is required.");
                        d.Name = v.Trim();
                    },
                    isRequired: true,
                    sampleValue: "Mumbai Branch"),

                new ExcelColumn<BranchDto>(
                    "Branch Code*",
                    d => d.Code,
                    (d, v) =>
                    {
                        if (string.IsNullOrWhiteSpace(v))
                            throw new Exception("Branch Code is required.");
                        d.Code = v.Trim();
                    },
                    isRequired: true,
                    sampleValue: "MUM01"),

                new ExcelColumn<BranchDto>(
                    "Country Code*",
                    d => d.CountryId,
                    (d, v) =>
                    {
                        var code = (v ?? string.Empty).Trim();

                        if (string.IsNullOrWhiteSpace(code))
                            throw new Exception("Country Code is required.");

                        if (ambiguousCountryCodes.Contains(code))
                            throw new Exception($"Country Code '{code}' matches more than one country - ask an admin to make country codes unique.");

                        if (!countryLookup.TryGetValue(code, out var countryId))
                            throw new Exception($"Country Code '{code}' was not found. See the Reference Data sheet for valid codes.");

                        d.CountryId = countryId;
                    },
                    isRequired: true,
                    sampleValue: "IN"),

                new ExcelColumn<BranchDto>(
                    "State Code*",
                    d => d.StateId,
                    (d, v) =>
                    {
                        var code = (v ?? string.Empty).Trim();

                        if (string.IsNullOrWhiteSpace(code))
                            throw new Exception("State Code is required.");

                        if (string.IsNullOrWhiteSpace(d.CountryId))
                            throw new Exception("State Code could not be resolved because the Country Code above is invalid.");

                        var key = (d.CountryId, code.ToUpperInvariant());

                        if (ambiguousStateCodes.Contains(key))
                            throw new Exception($"State Code '{code}' matches more than one state within that country - ask an admin to make state codes unique.");

                        if (!stateLookup.TryGetValue(key, out var stateId))
                            throw new Exception($"State Code '{code}' was not found for that country. See the Reference Data sheet for valid codes.");

                        d.StateId = stateId;
                    },
                    isRequired: true,
                    sampleValue: "MH"),

                new ExcelColumn<BranchDto>(
                    "City Name*",
                    d => d.CityId,
                    (d, v) =>
                    {
                        var name = (v ?? string.Empty).Trim();

                        if (string.IsNullOrWhiteSpace(name))
                            throw new Exception("City Name is required.");

                        if (string.IsNullOrWhiteSpace(d.StateId))
                            throw new Exception("City Name could not be resolved because the State Code above is invalid.");

                        var key = (d.StateId, name.ToUpperInvariant());

                        if (ambiguousCityNames.Contains(key))
                            throw new Exception($"City Name '{name}' matches more than one city within that state - ask an admin to make city names unique within a state.");

                        if (!cityLookup.TryGetValue(key, out var cityId))
                            throw new Exception($"City Name '{name}' was not found for that state. See the Reference Data sheet for valid names.");

                        d.CityId = cityId;
                    },
                    isRequired: true,
                    sampleValue: "Mumbai"),

                new ExcelColumn<BranchDto>(
                    "Email",
                    d => d.Email,
                    (d, v) => d.Email = string.IsNullOrWhiteSpace(v) ? null : v.Trim(),
                    sampleValue: ""),

                new ExcelColumn<BranchDto>(
                    "Phone*",
                    d => d.Phone,
                    (d, v) =>
                    {
                        if (string.IsNullOrWhiteSpace(v))
                            throw new Exception("Phone Number is required.");
                        d.Phone = v.Trim();
                    },
                    isRequired: true,
                    sampleValue: "9876543210"),

                new ExcelColumn<BranchDto>(
                    "Alternate Phone",
                    d => d.AlternatePhone,
                    (d, v) => d.AlternatePhone = string.IsNullOrWhiteSpace(v) ? null : v.Trim(),
                    sampleValue: ""),

                new ExcelColumn<BranchDto>(
                    "Address*",
                    d => d.Address,
                    (d, v) =>
                    {
                        if (string.IsNullOrWhiteSpace(v))
                            throw new Exception("Address is required.");
                        d.Address = v.Trim();
                    },
                    isRequired: true,
                    sampleValue: "123 Business Park"),

                new ExcelColumn<BranchDto>(
                    "Pincode*",
                    d => d.Pincode,
                    (d, v) =>
                    {
                        if (string.IsNullOrWhiteSpace(v))
                            throw new Exception("Pincode is required.");
                        d.Pincode = v.Trim();
                    },
                    isRequired: true,
                    sampleValue: "400001"),

                new ExcelColumn<BranchDto>(
                    "GST Number",
                    d => d.GSTNumber,
                    (d, v) => d.GSTNumber = string.IsNullOrWhiteSpace(v) ? null : v.Trim(),
                    sampleValue: ""),

                new ExcelColumn<BranchDto>(
                    "CIN No",
                    d => d.CINNo,
                    (d, v) => d.CINNo = string.IsNullOrWhiteSpace(v) ? null : v.Trim(),
                    sampleValue: ""),

                new ExcelColumn<BranchDto>(
                    "Is Head Office (Yes/No)",
                    d => d.IsHeadOffice ? "Yes" : "No",
                    (d, v) => d.IsHeadOffice = ParseYesNo(v),
                    sampleValue: "No"),
            };
        }

        private static List<ExcelColumn<BranchDto>> GetExportColumns()
        {
            return new List<ExcelColumn<BranchDto>>
            {
                new ExcelColumn<BranchDto>("Branch Name", d => d.Name, (d, v) => { }),
                new ExcelColumn<BranchDto>("Branch Code", d => d.Code, (d, v) => { }),
                new ExcelColumn<BranchDto>("Company", d => d.CompanyName, (d, v) => { }),
                new ExcelColumn<BranchDto>("Country", d => d.CountryName, (d, v) => { }),
                new ExcelColumn<BranchDto>("State", d => d.StateName, (d, v) => { }),
                new ExcelColumn<BranchDto>("City", d => d.CityName, (d, v) => { }),
                new ExcelColumn<BranchDto>("Phone", d => d.Phone, (d, v) => { }),
                new ExcelColumn<BranchDto>("Email", d => d.Email, (d, v) => { }),
                new ExcelColumn<BranchDto>("Address", d => d.Address, (d, v) => { }),
                new ExcelColumn<BranchDto>("Pincode", d => d.Pincode, (d, v) => { }),
                new ExcelColumn<BranchDto>("GST Number", d => d.GSTNumber, (d, v) => { }),
                new ExcelColumn<BranchDto>("CIN No", d => d.CINNo, (d, v) => { }),
                new ExcelColumn<BranchDto>("Is Head Office", d => d.IsHeadOffice ? "Yes" : "No", (d, v) => { }),
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

        private static (Dictionary<string, string> Lookup, HashSet<string> Ambiguous) BuildCountryLookup(
            List<CountryDto> countries)
        {
            var groups = (countries ?? new List<CountryDto>())
                .Where(c => !string.IsNullOrWhiteSpace(c.Code))
                .GroupBy(c => c.Code.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToList();

            var ambiguous = groups.Where(g => g.Count() > 1).Select(g => g.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var lookup = groups.Where(g => g.Count() == 1)
                .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

            return (lookup, ambiguous);
        }

        // State lookup is keyed by (CountryId, upper-cased Code) so the same
        // code under two different countries never collides.
        private static (Dictionary<(string, string), string> Lookup, HashSet<(string, string)> Ambiguous) BuildStateLookup(
            List<StateDto> states)
        {
            var groups = (states ?? new List<StateDto>())
                .Where(s => !string.IsNullOrWhiteSpace(s.Code) && !string.IsNullOrWhiteSpace(s.CountryId))
                .GroupBy(s => (CountryId: s.CountryId, Code: s.Code.Trim().ToUpperInvariant()))
                .ToList();

            var ambiguous = groups.Where(g => g.Count() > 1).Select(g => g.Key).ToHashSet();

            var lookup = groups.Where(g => g.Count() == 1)
                .ToDictionary(g => g.Key, g => g.First().Id);

            return (lookup, ambiguous);
        }

        // City has no Code column in this schema, so it's keyed by
        // (StateId, upper-cased Name) instead.
        private static (Dictionary<(string, string), string> Lookup, HashSet<(string, string)> Ambiguous) BuildCityLookup(
            List<CityDto> cities)
        {
            var groups = (cities ?? new List<CityDto>())
                .Where(c => !string.IsNullOrWhiteSpace(c.Name) && !string.IsNullOrWhiteSpace(c.StateId))
                .GroupBy(c => (StateId: c.StateId, Name: c.Name.Trim().ToUpperInvariant()))
                .ToList();

            var ambiguous = groups.Where(g => g.Count() > 1).Select(g => g.Key).ToHashSet();

            var lookup = groups.Where(g => g.Count() == 1)
                .ToDictionary(g => g.Key, g => g.First().Id);

            return (lookup, ambiguous);
        }

        private static List<ExcelTemplateReferenceSheet> GetReferenceSheets(
            List<CountryDto> countries,
            List<StateDto> states,
            List<CityDto> cities)
        {
            var orderedCountries = (countries ?? new List<CountryDto>()).OrderBy(c => c.Name).ToList();
            var orderedStates = (states ?? new List<StateDto>()).OrderBy(s => s.CountryName).ThenBy(s => s.Name).ToList();
            var orderedCities = (cities ?? new List<CityDto>()).OrderBy(c => c.StateName).ThenBy(c => c.Name).ToList();

            return new List<ExcelTemplateReferenceSheet>
            {
                new ExcelTemplateReferenceSheet(
                    "Reference Data",
                    new List<ExcelReferenceColumn>
                    {
                        new ExcelReferenceColumn("Country Code", orderedCountries.Select(c => c.Code)),
                        new ExcelReferenceColumn("Country Name", orderedCountries.Select(c => c.Name)),
                        new ExcelReferenceColumn("State Code", orderedStates.Select(s => s.Code)),
                        new ExcelReferenceColumn("State Name", orderedStates.Select(s => s.Name)),
                        new ExcelReferenceColumn("State's Country", orderedStates.Select(s => s.CountryName)),
                        new ExcelReferenceColumn("City Name", orderedCities.Select(c => c.Name)),
                        new ExcelReferenceColumn("City's State", orderedCities.Select(c => c.StateName)),
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
            var states = await _apiService.GetAsync<List<StateDto>>("state") ?? new();
            var cities = await _apiService.GetAsync<List<CityDto>>("city") ?? new();

            var bytes = _excelEngine.BuildTemplate(
                GetImportColumns(
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                    new Dictionary<(string, string), string>(),
                    new HashSet<(string, string)>(),
                    new Dictionary<(string, string), string>(),
                    new HashSet<(string, string)>()),
                sheetName: "Branches",
                referenceSheets: GetReferenceSheets(countries, states, cities));

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Branch_Import_Template.xlsx");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (!_isAdmin)
                return Forbid();

            var result = new ExcelImportResult();

            var countries = await _apiService.GetAsync<List<CountryDto>>("Country") ?? new();
            var states = await _apiService.GetAsync<List<StateDto>>("state") ?? new();
            var cities = await _apiService.GetAsync<List<CityDto>>("city") ?? new();

            var (countryLookup, ambiguousCountryCodes) = BuildCountryLookup(countries);
            var (stateLookup, ambiguousStateCodes) = BuildStateLookup(states);
            var (cityLookup, ambiguousCityNames) = BuildCityLookup(cities);

            List<ExcelImportRow<BranchDto>> rows;

            try
            {
                rows = _excelEngine.ReadRows(
                    file,
                    GetImportColumns(
                        countryLookup, ambiguousCountryCodes,
                        stateLookup, ambiguousStateCodes,
                        cityLookup, ambiguousCityNames));
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
                    row.Item.CompanyId = _companyId;
                    row.Item.CreatedBy = _userId;

                    await _apiService.PostAsync<dynamic>("branch", row.Item);

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
                TempData["GlobalError"] = "The uploaded file didn't contain any branch rows.";
            else if (result.FailureCount == 0)
                TempData["Success"] = $"All {result.SuccessCount} branch(es) imported successfully.";
            else
                TempData["Info"] = $"{result.SuccessCount} of {result.TotalRows} branch(es) imported. {result.FailureCount} failed - see details below.";

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

            var data = await _apiService.GetAsync<List<BranchDto>>("branch") ?? new();

            var bytes = _excelEngine.Export(data, GetExportColumns(), "Branches");

            var fileName = $"Branches_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        #endregion
    }
}
