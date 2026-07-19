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
    #region Salary Structure Controller

    [JwtAuthorize]
    public class SalaryStructureController : Controller
    {
        private readonly IApiService _apiService;
        private readonly IExcelEngine _excelEngine;
        private string _tenantId;
        private string _userId;
        private readonly bool _isAdmin;

        public SalaryStructureController(IApiService apiService, IExcelEngine excelEngine)
        {
            _apiService = apiService;
            _excelEngine = excelEngine;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
            _isAdmin = SessionHelper.IsAdminRole();
        }

        public async Task<IActionResult> Index()
        {
            var data = await _apiService
                .GetAsync<List<SalaryStructureListDto>>("salary-structure");
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();
            return View(new SalaryStructureDto());
        }

        [HttpPost]
        public async Task<IActionResult> Create(SalaryStructureDto dto)
        {
            if (dto != null && dto.Details != null && dto.Details.Any(d => !string.IsNullOrEmpty(d.SalaryComponentId)))
            {
                dto.TenantId = _tenantId;
                dto.CreatedBy = _userId;

                await _apiService.PostAsync<dynamic>("salary-structure", dto);

                TempData["Success"] = "Salary structure saved successfully.";
                return RedirectToAction(nameof(Index));
            }

            TempData["Error"] = "Please add at least one salary component.";
            await LoadDropdowns();
            return View(dto);
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var data = await _apiService
                .GetAsync<SalaryStructureDto>($"salary-structure/{id}");
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var data = await _apiService
                .GetAsync<SalaryStructureDto>($"salary-structure/{id}");
            await LoadDropdowns();
            return View("Create", data);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string id, SalaryStructureDto dto)
        {
            if (dto != null && dto.Details != null && dto.Details.Any(d => !string.IsNullOrEmpty(d.SalaryComponentId)))
            {
                dto.TenantId = _tenantId;
                dto.ModifiedBy = _userId;
                dto.ModifiedOn = DateTime.UtcNow;

                await _apiService.PutAsync<dynamic>($"salary-structure/{id}", dto);

                TempData["Success"] = "Salary structure updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            TempData["Error"] = "Please add at least one salary component.";
            await LoadDropdowns();
            return View("Create", dto);
        }

        public async Task<IActionResult> Delete(string id)
        {
            await _apiService.DeleteAsync($"salary-structure/{id}");
            return RedirectToAction(nameof(Index));
        }

        #region Load Dropdowns

        private async Task LoadDropdowns()
        {
            var employees = await _apiService
                .GetAsync<List<DropdownDto>>("dropdown/employee");
            ViewBag.EmployeeList = new SelectList(employees, "Value", "Text");

            var components = await _apiService
                .GetAsync<List<DropdownDto>>("dropdown/salary-component");
            ViewBag.ComponentList = new SelectList(components, "Value", "Text");
        }

        #endregion

        #region Import

        // DESIGN DECISION - this is the structurally trickiest module in
        // this batch. A Salary Structure is a header (Employee +
        // EffectiveFrom) with many component/amount line rows underneath
        // it, and the existing Create endpoint (POST "salary-structure",
        // see Create() above) expects the WHOLE structure - header plus
        // every one of its Details lines - in a single call. It does not
        // offer a per-line "add one component to an existing structure"
        // endpoint.
        //
        // So each Excel row here represents one (Employee, EffectiveFrom,
        // Salary Component, Amount) tuple - a private flat row type,
        // SalaryStructureImportLine, is used for parsing instead of the
        // richer SalaryStructureDto - but rows are NOT posted one at a
        // time. They are grouped client-side by (EmployeeId,
        // EffectiveFrom.Date) first, and exactly ONE POST is made per
        // group with every matching line attached as Details. This is the
        // only way to use the existing endpoint safely: posting per-row
        // would either fail (missing Details) or silently create several
        // broken single-line structures for what should have been one
        // structure with multiple components.
        //
        // A consequence of grouping: the API call happens per GROUP, not
        // per Excel row, so every row in a group succeeds or fails
        // together (they share one result message) - this is called out
        // explicitly in the Import view instructions.
        public class SalaryStructureImportLine
        {
            public string EmployeeCode { get; set; } = string.Empty;
            public string EmployeeId { get; set; } = string.Empty;
            public DateTime EffectiveFrom { get; set; }
            public string ComponentCode { get; set; } = string.Empty;
            public string SalaryComponentId { get; set; } = string.Empty;
            public decimal Amount { get; set; }
        }

        private static List<ExcelColumn<SalaryStructureImportLine>> GetImportColumns(
            Dictionary<string, string> employeeLookup,
            HashSet<string> ambiguousEmployeeCodes,
            Dictionary<string, string> componentLookup,
            HashSet<string> ambiguousComponentCodes)
        {
            return new List<ExcelColumn<SalaryStructureImportLine>>
            {
                new ExcelColumn<SalaryStructureImportLine>(
                    "Employee Code*",
                    d => d.EmployeeCode,
                    (d, v) =>
                    {
                        var code = (v ?? string.Empty).Trim();

                        if (string.IsNullOrWhiteSpace(code))
                            throw new Exception("Employee Code is required.");

                        if (ambiguousEmployeeCodes.Contains(code))
                            throw new Exception($"Employee Code '{code}' matches more than one employee - ask an admin to make employee codes unique.");

                        if (!employeeLookup.TryGetValue(code, out var employeeId))
                            throw new Exception($"Employee Code '{code}' was not found. See the Reference Data sheet for valid codes.");

                        d.EmployeeCode = code;
                        d.EmployeeId = employeeId;
                    },
                    isRequired: true,
                    sampleValue: "EMP0001"),

                new ExcelColumn<SalaryStructureImportLine>(
                    "Effective From*",
                    d => d.EffectiveFrom,
                    (d, v) =>
                    {
                        if (string.IsNullOrWhiteSpace(v) ||
                            !DateTime.TryParse(v.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                            throw new Exception("Effective From is required and must be a valid date (e.g. 2026-04-01).");

                        d.EffectiveFrom = date.Date;
                    },
                    isRequired: true,
                    sampleValue: "2026-04-01"),

                new ExcelColumn<SalaryStructureImportLine>(
                    "Salary Component Code*",
                    d => d.ComponentCode,
                    (d, v) =>
                    {
                        var code = (v ?? string.Empty).Trim();

                        if (string.IsNullOrWhiteSpace(code))
                            throw new Exception("Salary Component Code is required.");

                        if (ambiguousComponentCodes.Contains(code))
                            throw new Exception($"Salary Component Code '{code}' matches more than one component - ask an admin to make component codes unique.");

                        if (!componentLookup.TryGetValue(code, out var componentId))
                            throw new Exception($"Salary Component Code '{code}' was not found. See the Reference Data sheet for valid codes.");

                        d.ComponentCode = code;
                        d.SalaryComponentId = componentId;
                    },
                    isRequired: true,
                    sampleValue: "HRA"),

                new ExcelColumn<SalaryStructureImportLine>(
                    "Amount*",
                    d => d.Amount,
                    (d, v) =>
                    {
                        if (string.IsNullOrWhiteSpace(v) ||
                            !decimal.TryParse(v.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) ||
                            amount < 0)
                            throw new Exception("Amount is required and must be a non-negative number.");

                        d.Amount = amount;
                    },
                    isRequired: true,
                    sampleValue: 15000),
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

        private static List<ExcelTemplateReferenceSheet> GetReferenceSheets(
            List<EmployeeListDto> employees,
            List<SalaryComponentListDto> components)
        {
            var orderedEmployees = (employees ?? new List<EmployeeListDto>())
                .OrderBy(e => e.EmployeeCode).ToList();
            var orderedComponents = (components ?? new List<SalaryComponentListDto>())
                .OrderBy(c => c.Code).ToList();

            return new List<ExcelTemplateReferenceSheet>
            {
                new ExcelTemplateReferenceSheet(
                    "Reference Data",
                    new List<ExcelReferenceColumn>
                    {
                        new ExcelReferenceColumn("Employee Code", orderedEmployees.Select(e => e.EmployeeCode)),
                        new ExcelReferenceColumn("Employee Name", orderedEmployees.Select(e => $"{e.FirstName} {e.LastName}".Trim())),
                        new ExcelReferenceColumn("Salary Component Code", orderedComponents.Select(c => c.Code)),
                        new ExcelReferenceColumn("Salary Component Name", orderedComponents.Select(c => c.Name)),
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

            var employees = await _apiService.GetAsync<List<EmployeeListDto>>("Employee/employee-list") ?? new();
            var components = await _apiService.GetAsync<List<SalaryComponentListDto>>("salary-component") ?? new();

            var bytes = _excelEngine.BuildTemplate(
                GetImportColumns(
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase)),
                sheetName: "Salary Structure Lines",
                referenceSheets: GetReferenceSheets(employees, components));

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "SalaryStructure_Import_Template.xlsx");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (!_isAdmin)
                return Forbid();

            var result = new ExcelImportResult();

            var employees = await _apiService.GetAsync<List<EmployeeListDto>>("Employee/employee-list") ?? new();
            var components = await _apiService.GetAsync<List<SalaryComponentListDto>>("salary-component") ?? new();

            var (employeeLookup, ambiguousEmployeeCodes) = BuildCodeLookup(
                employees.Select(e => (e.EmployeeCode, e.Id)));
            var (componentLookup, ambiguousComponentCodes) = BuildCodeLookup(
                components.Select(c => (c.Code, c.Id)));

            List<ExcelImportRow<SalaryStructureImportLine>> rows;

            try
            {
                rows = _excelEngine.ReadRows(
                    file,
                    GetImportColumns(employeeLookup, ambiguousEmployeeCodes, componentLookup, ambiguousComponentCodes));
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

            // Rows that failed to parse are reported individually - they
            // never make it into a group, since there's nothing usable to
            // group them by.
            foreach (var row in rows.Where(r => r.HasErrors))
            {
                var identifier = string.Join(
                    " - ",
                    new[] { row.Item.EmployeeCode, row.Item.ComponentCode }.Where(x => !string.IsNullOrWhiteSpace(x)));

                result.AddRow(row.RowNumber, identifier, false, string.Join(" ", row.ParseErrors));
            }

            // Every successfully-parsed row is grouped by (Employee,
            // EffectiveFrom date) - one API call per group, since the
            // existing endpoint only accepts a full structure at once.
            var groups = rows
                .Where(r => !r.HasErrors)
                .GroupBy(r => (r.Item.EmployeeId, EffectiveFrom: r.Item.EffectiveFrom.Date));

            foreach (var group in groups)
            {
                var groupRows = group.ToList();
                var rowNumbers = string.Join(", ", groupRows.Select(r => r.RowNumber));
                var employeeCode = groupRows.First().Item.EmployeeCode;
                var groupIdentifier = $"{employeeCode} @ {group.Key.EffectiveFrom:yyyy-MM-dd} (rows {rowNumbers})";

                // Guard against the same component appearing twice for the
                // same Employee + Effective Date in the file - posting that
                // as-is would create a structure with a duplicate line, so
                // the whole group is failed with a clear message instead.
                var duplicateComponents = groupRows
                    .GroupBy(r => r.Item.SalaryComponentId)
                    .Where(g => g.Count() > 1)
                    .Select(g => groupRows.First(r => r.Item.SalaryComponentId == g.Key).Item.ComponentCode)
                    .ToList();

                if (duplicateComponents.Any())
                {
                    var message = $"Salary Component(s) {string.Join(", ", duplicateComponents)} appear more than once for {groupIdentifier} - each component may only appear once per structure.";

                    foreach (var row in groupRows)
                        result.AddRow(row.RowNumber, $"{employeeCode} / {row.Item.ComponentCode}", false, message);

                    continue;
                }

                try
                {
                    var dto = new SalaryStructureDto
                    {
                        EmployeeId = group.Key.EmployeeId,
                        EffectiveFrom = group.Key.EffectiveFrom,
                        TenantId = _tenantId,
                        CreatedBy = _userId,
                        Details = groupRows.Select(r => new SalaryStructureLineDto
                        {
                            SalaryComponentId = r.Item.SalaryComponentId,
                            Amount = r.Item.Amount
                        }).ToList()
                    };

                    await _apiService.PostAsync<dynamic>("salary-structure", dto);

                    foreach (var row in groupRows)
                        result.AddRow(row.RowNumber, $"{employeeCode} / {row.Item.ComponentCode}", true, $"Imported as part of the structure effective {group.Key.EffectiveFrom:yyyy-MM-dd}.");
                }
                catch (ApiException apiEx)
                {
                    var message = GetErrorMessage(apiEx.ResponseContent);

                    foreach (var row in groupRows)
                        result.AddRow(row.RowNumber, $"{employeeCode} / {row.Item.ComponentCode}", false, message);
                }
                catch (Exception ex)
                {
                    foreach (var row in groupRows)
                        result.AddRow(row.RowNumber, $"{employeeCode} / {row.Item.ComponentCode}", false, ex.Message);
                }
            }

            if (result.TotalRows == 0)
                TempData["GlobalError"] = "The uploaded file didn't contain any salary structure line rows.";
            else if (result.FailureCount == 0)
                TempData["Success"] = $"All {result.SuccessCount} salary structure line(s) imported successfully.";
            else
                TempData["Info"] = $"{result.SuccessCount} of {result.TotalRows} salary structure line(s) imported. {result.FailureCount} failed - see details below.";

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

        // Flattens each structure's Details back to one row per component
        // line, matching the shape Import consumes, so an admin can
        // export, tweak in Excel and re-import. EmployeeCode isn't part of
        // SalaryStructureDto/SalaryStructureListDto, so Employee Name is
        // shown here instead (informational only - re-import still keys
        // off Employee Code from the Reference Data sheet).
        private class SalaryStructureExportRow
        {
            public string? EmployeeName { get; set; }
            public DateTime EffectiveFrom { get; set; }
            public string? ComponentName { get; set; }
            public decimal Amount { get; set; }
        }

        private static List<ExcelColumn<SalaryStructureExportRow>> GetExportColumns()
        {
            return new List<ExcelColumn<SalaryStructureExportRow>>
            {
                new ExcelColumn<SalaryStructureExportRow>("Employee", d => d.EmployeeName, (d, v) => { }),
                new ExcelColumn<SalaryStructureExportRow>("Effective From", d => d.EffectiveFrom, (d, v) => { }),
                new ExcelColumn<SalaryStructureExportRow>("Salary Component", d => d.ComponentName, (d, v) => { }),
                new ExcelColumn<SalaryStructureExportRow>("Amount", d => d.Amount, (d, v) => { }),
            };
        }

        [HttpGet]
        public async Task<IActionResult> Export()
        {
            if (!_isAdmin)
                return Forbid();

            var structures = await _apiService.GetAsync<List<SalaryStructureListDto>>("salary-structure") ?? new();

            var rows = new List<SalaryStructureExportRow>();

            foreach (var structure in structures)
            {
                var detail = await _apiService.GetAsync<SalaryStructureDto>($"salary-structure/{structure.Id}");

                if (detail?.Details == null)
                    continue;

                foreach (var line in detail.Details)
                {
                    rows.Add(new SalaryStructureExportRow
                    {
                        EmployeeName = detail.EmployeeName,
                        EffectiveFrom = detail.EffectiveFrom,
                        ComponentName = line.SalaryComponentName,
                        Amount = line.Amount
                    });
                }
            }

            var bytes = _excelEngine.Export(rows, GetExportColumns(), "Salary Structures");

            var fileName = $"SalaryStructures_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        #endregion
    }

    #endregion
}
