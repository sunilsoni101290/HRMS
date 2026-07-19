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
    [JwtAuthorize]
    public class HolidayGroupController : Controller
    {
        private readonly IApiService _apiService;
        private readonly IExcelEngine _excelEngine;
        private string _tenantId;
        private string _userId;
        private readonly bool _isAdmin;

        public HolidayGroupController(IApiService apiService, IExcelEngine excelEngine)
        {
            _apiService = apiService;
            _excelEngine = excelEngine;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
            _isAdmin = SessionHelper.IsAdminRole();
        }


        // =====================================================
        // HOLIDAY GROUP INDEX
        // =====================================================
        public async Task<IActionResult> Index()
        {
            var data = await _apiService.GetAsync<List<HolidayGroupDto>>("holidaygroup/groups");

            return View(data);
        }

        // =====================================================
        // CREATE HOLIDAY GROUP - GET
        // =====================================================
        [HttpGet]
        public IActionResult Create()
        {
            return View(new HolidayGroupDto());
        }

        // =====================================================
        // CREATE HOLIDAY GROUP - POST
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(HolidayGroupDto dto)
        {
            try
            {
               if(dto!=null)
                {
                    dto.CreatedBy = _userId;
                    dto.TenantId = _tenantId;

                    await _apiService.PostAsync<HolidayGroupDto>("holidaygroup/groups", dto);

                    AlertHelper.Success(TempData, "Holiday Group created successfully.");
                    return View(dto);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return View(dto);
            }
            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // EDIT HOLIDAY GROUP - GET
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var data = await _apiService.GetAsync<HolidayGroupDto>(
                $"holidaygroup/groups/{id}"
            );

            if (data == null)
            {
                return NotFound();
            }

            return View("Create", data);
        }

        // =====================================================
        // EDIT HOLIDAY GROUP - POST
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(HolidayGroupDto dto)
        {
            try
            {
                if (!string.IsNullOrEmpty(dto.Id) && dto != null)
                {

                    dto.ModifiedOn = DateTime.UtcNow;
                    dto.ModifiedBy = _userId;
                    dto.CreatedBy = _userId;
                    dto.TenantId = _tenantId;

                    await _apiService.PutAsync<dynamic>($"holidaygroup/groups/{dto.Id}", dto);

                    AlertHelper.Success(TempData, "Holiday Group updated successfully.");

                    return View("Create",dto);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
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

            var data = await _apiService.GetAsync<HolidayGroupDto>(
                $"holidaygroup/groups/{id}"
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
                await _apiService.DeleteAsync(
                    $"holidaygroup/groups/{id}"
                );

                TempData["Success"] = "Holiday Group deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // HOLIDAY DETAILS LIST
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> HolidayList(string holidayGroupId)
        {
            ViewBag.HolidayGroupId = holidayGroupId;

            var data = await _apiService.GetAsync<List<HolidayGroupDetailDto>>(
                $"holidaygroup/details/group/{holidayGroupId}"
            );

            return View(data);
        }

        // =====================================================
        // CREATE HOLIDAY DETAIL - GET
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> CreateHoliday(string holidayGroupId)
        {
            await LoadHolidayGroupDropdown();

            return View(new HolidayGroupDetailDto
            {
                HolidayGroupId = holidayGroupId,
                HolidayDate = DateTime.Today
            });
        }

        // =====================================================
        // CREATE HOLIDAY DETAIL - POST
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateHoliday(HolidayGroupDetailDto dto)
        {
            try
            {
                if (!string.IsNullOrEmpty(dto.HolidayGroupId) && dto != null)
                {
                    await LoadHolidayGroupDropdown();

                    dto.CreatedBy = _userId;
                    dto.TenantId = _tenantId;

                    await _apiService.PostAsync<HolidayGroupDetailDto>("holidaygroup/details/",dto);

                    AlertHelper.Success(TempData, "Holiday added successfully.");

                    return View(dto);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;

                await LoadHolidayGroupDropdown();

                return View(dto);
            }
            return RedirectToAction(nameof(HolidayList), new { holidayGroupId = dto.HolidayGroupId });
        }

        // =====================================================
        // EDIT HOLIDAY DETAIL - GET
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> EditHoliday(string id)
        {
            var data = await _apiService.GetAsync<HolidayGroupDetailDto>(
                $"holidaygroup/details/{id}"
            );

            if (data == null)
            {
                return NotFound();
            }

            await LoadHolidayGroupDropdown();

            return View("CreateHoliday", data);
        }

        // =====================================================
        // EDIT HOLIDAY DETAIL - POST
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditHoliday(HolidayGroupDetailDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    await LoadHolidayGroupDropdown();

                    return View("CreateHoliday", dto);
                }

                dto.ModifiedOn = DateTime.UtcNow;
                dto.ModifiedBy = _userId;
                dto.CreatedBy = _userId;
                dto.TenantId = _tenantId;

                await _apiService.PutAsync<dynamic>($"holidaygroup/details/{dto.Id}", dto);

                AlertHelper.Success(TempData, "Holiday updated successfully.");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;

                await LoadHolidayGroupDropdown();

                return View("CreateHoliday", dto);
            }
            return RedirectToAction(nameof(HolidayList), new { holidayGroupId = dto.HolidayGroupId });
        }

        // =====================================================
        // DELETE HOLIDAY DETAIL
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteHoliday(
            string id,
            string holidayGroupId
        )
        {
            try
            {
                await _apiService.DeleteAsync(
                    $"holidaygroup/details/{id}"
                );

                TempData["Success"] = "Holiday deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(
                nameof(HolidayList),
                new { holidayGroupId }
            );
        }

        // =====================================================
        // LOAD HOLIDAY GROUP DROPDOWN
        // =====================================================
        private async Task LoadHolidayGroupDropdown()
        {
            var groups = await _apiService.GetAsync<List<DropdownDto>>(
                "dropdown/holidaygroup"
            );

            ViewBag.HolidayGroupList = groups.Select(x => new SelectListItem
            {
                Value = x.Value,
                Text = x.Text
            }).ToList();
        }

        #region Import

        // DESIGN DECISION: a Holiday Group is a header (Name/Description)
        // with many holiday rows underneath it, and there's no separate
        // top-level "Holiday" module - HolidayGroupDetail rows are always
        // created against an existing group (see CreateHoliday above,
        // which requires a HolidayGroupId chosen from a dropdown of
        // already-created groups).
        //
        // The realistic bulk-load use case here is "load a year's public
        // holidays into an existing group", not "create groups and their
        // holidays in one shot" - so this Import targets holiday ROWS only,
        // resolved against an EXISTING Holiday Group by Name (there is no
        // Code column on HolidayGroup - see Domain.Entities.HolidayGroup).
        // Creating the group header itself stays on the existing
        // Create/Edit pages; adding a second "create groups" import here
        // would double the surface area for very little benefit, since a
        // handful of groups (e.g. "India Holidays 2026") are typically
        // created once and reused for years of holiday rows.
        //
        // Each Excel row = one HolidayGroupDetailDto, posted individually
        // to the existing "holidaygroup/details/" endpoint (the same one
        // CreateHoliday POST already uses) - there's no header/child
        // grouping problem here the way there is for Salary Structure,
        // since every row already stands alone as one holiday.
        private static List<ExcelColumn<HolidayGroupDetailDto>> GetImportColumns(
            Dictionary<string, string> groupLookup,
            HashSet<string> ambiguousGroupNames)
        {
            return new List<ExcelColumn<HolidayGroupDetailDto>>
            {
                new ExcelColumn<HolidayGroupDetailDto>(
                    "Holiday Group Name*",
                    d => d.HolidayGroupName,
                    (d, v) =>
                    {
                        var name = (v ?? string.Empty).Trim();

                        if (string.IsNullOrWhiteSpace(name))
                            throw new Exception("Holiday Group Name is required.");

                        if (ambiguousGroupNames.Contains(name))
                            throw new Exception($"Holiday Group Name '{name}' matches more than one holiday group - ask an admin to make group names unique.");

                        if (!groupLookup.TryGetValue(name, out var groupId))
                            throw new Exception($"Holiday Group Name '{name}' was not found. Create the group first, or see the Reference Data sheet for valid names.");

                        d.HolidayGroupId = groupId;
                    },
                    isRequired: true,
                    sampleValue: "India Holidays 2026"),

                new ExcelColumn<HolidayGroupDetailDto>(
                    "Holiday Date*",
                    d => d.HolidayDate,
                    (d, v) =>
                    {
                        if (string.IsNullOrWhiteSpace(v) ||
                            !DateTime.TryParse(v.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                            throw new Exception("Holiday Date is required and must be a valid date (e.g. 2026-01-26).");

                        d.HolidayDate = date.Date;
                    },
                    isRequired: true,
                    sampleValue: "2026-01-26"),

                new ExcelColumn<HolidayGroupDetailDto>(
                    "Holiday Name*",
                    d => d.HolidayName,
                    (d, v) =>
                    {
                        if (string.IsNullOrWhiteSpace(v))
                            throw new Exception("Holiday Name is required.");
                        d.HolidayName = v.Trim();
                    },
                    isRequired: true,
                    sampleValue: "Republic Day"),

                new ExcelColumn<HolidayGroupDetailDto>(
                    "Remarks",
                    d => d.Remarks,
                    (d, v) => d.Remarks = string.IsNullOrWhiteSpace(v) ? null : v.Trim(),
                    sampleValue: ""),

                new ExcelColumn<HolidayGroupDetailDto>(
                    "Is Optional (Yes/No)",
                    d => d.IsOptional ? "Yes" : "No",
                    (d, v) => d.IsOptional = ParseYesNo(v),
                    sampleValue: "No"),
            };
        }

        private static List<ExcelColumn<HolidayGroupDetailDto>> GetExportColumns()
        {
            return new List<ExcelColumn<HolidayGroupDetailDto>>
            {
                new ExcelColumn<HolidayGroupDetailDto>("Holiday Group Name", d => d.HolidayGroupName, (d, v) => { }),
                new ExcelColumn<HolidayGroupDetailDto>("Holiday Date", d => d.HolidayDate, (d, v) => { }),
                new ExcelColumn<HolidayGroupDetailDto>("Holiday Name", d => d.HolidayName, (d, v) => { }),
                new ExcelColumn<HolidayGroupDetailDto>("Remarks", d => d.Remarks, (d, v) => { }),
                new ExcelColumn<HolidayGroupDetailDto>("Is Optional", d => d.IsOptional ? "Yes" : "No", (d, v) => { }),
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

        private static (Dictionary<string, string> Lookup, HashSet<string> Ambiguous) BuildGroupLookup(
            List<HolidayGroupDto> groups)
        {
            var grouped = (groups ?? new List<HolidayGroupDto>())
                .Where(g => !string.IsNullOrWhiteSpace(g.Name))
                .GroupBy(g => g.Name.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToList();

            var ambiguous = grouped.Where(g => g.Count() > 1).Select(g => g.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var lookup = grouped.Where(g => g.Count() == 1)
                .ToDictionary(g => g.Key, g => g.First().Id!, StringComparer.OrdinalIgnoreCase);

            return (lookup, ambiguous);
        }

        private static List<ExcelTemplateReferenceSheet> GetReferenceSheets(List<HolidayGroupDto> groups)
        {
            var ordered = (groups ?? new List<HolidayGroupDto>()).OrderBy(g => g.Name).ToList();

            return new List<ExcelTemplateReferenceSheet>
            {
                new ExcelTemplateReferenceSheet(
                    "Reference Data",
                    new List<ExcelReferenceColumn>
                    {
                        new ExcelReferenceColumn("Holiday Group Name", ordered.Select(g => g.Name)),
                        new ExcelReferenceColumn("Description", ordered.Select(g => g.Description ?? string.Empty)),
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

            var groups = await _apiService.GetAsync<List<HolidayGroupDto>>("holidaygroup/groups") ?? new();

            var bytes = _excelEngine.BuildTemplate(
                GetImportColumns(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase), new HashSet<string>(StringComparer.OrdinalIgnoreCase)),
                sheetName: "Holidays",
                referenceSheets: GetReferenceSheets(groups));

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "HolidayGroup_Import_Template.xlsx");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (!_isAdmin)
                return Forbid();

            var result = new ExcelImportResult();

            var groups = await _apiService.GetAsync<List<HolidayGroupDto>>("holidaygroup/groups") ?? new();
            var (groupLookup, ambiguousGroupNames) = BuildGroupLookup(groups);

            List<ExcelImportRow<HolidayGroupDetailDto>> rows;

            try
            {
                rows = _excelEngine.ReadRows(file, GetImportColumns(groupLookup, ambiguousGroupNames));
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
                    new[] { row.Item.HolidayGroupName, row.Item.HolidayName }.Where(x => !string.IsNullOrWhiteSpace(x)));

                if (row.HasErrors)
                {
                    result.AddRow(row.RowNumber, identifier, false, string.Join(" ", row.ParseErrors));
                    continue;
                }

                try
                {
                    row.Item.TenantId = _tenantId;
                    row.Item.CreatedBy = _userId;

                    await _apiService.PostAsync<HolidayGroupDetailDto>("holidaygroup/details/", row.Item);

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
                TempData["GlobalError"] = "The uploaded file didn't contain any holiday rows.";
            else if (result.FailureCount == 0)
                TempData["Success"] = $"All {result.SuccessCount} holiday(s) imported successfully.";
            else
                TempData["Info"] = $"{result.SuccessCount} of {result.TotalRows} holiday(s) imported. {result.FailureCount} failed - see details below.";

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

        // Exports the flat list of holiday rows across every group (not
        // the group headers) - the same granularity the Import above
        // consumes, so an admin can export, tweak in Excel and re-import.
        [HttpGet]
        public async Task<IActionResult> Export()
        {
            if (!_isAdmin)
                return Forbid();

            var data = await _apiService.GetAsync<List<HolidayGroupDetailDto>>("holidaygroup/details") ?? new();

            var bytes = _excelEngine.Export(data, GetExportColumns(), "Holidays");

            var fileName = $"Holidays_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        #endregion
    }
}
