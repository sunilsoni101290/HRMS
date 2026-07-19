using APP.Attributes;
using APP.Excel;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace APP.Controllers
{
    #region Salary Component Controller

    [JwtAuthorize]
    public class SalaryComponentController : Controller
    {
        private readonly IApiService _apiService;
        private readonly IExcelEngine _excelEngine;
        private string _tenantId;
        private string _userId;

        // Import/Export touches every component org-wide - same admin-only
        // convention as the Employee bulk import feature.
        private readonly bool _isAdmin;

        public SalaryComponentController(IApiService apiService, IExcelEngine excelEngine)
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
                .GetAsync<List<SalaryComponentListDto>>("salary-component");
            return View(data);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new SalaryComponentDto());
        }

        [HttpPost]
        public async Task<IActionResult> Create(SalaryComponentDto dto)
        {
            if (dto != null)
            {
                dto.TenantId = _tenantId;
                dto.CreatedBy = _userId;

                await _apiService.PostAsync<dynamic>("salary-component", dto);

                TempData["Success"] = "Record saved successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(dto);
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var data = await _apiService
                .GetAsync<SalaryComponentDto>($"salary-component/{id}");
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var data = await _apiService
                .GetAsync<SalaryComponentDto>($"salary-component/{id}");
            return View("Create", data);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string id, SalaryComponentDto dto)
        {
            if (dto != null)
            {
                dto.TenantId = _tenantId;
                dto.ModifiedBy = _userId;
                dto.ModifiedOn = DateTime.UtcNow;

                await _apiService.PutAsync<dynamic>($"salary-component/{id}", dto);

                TempData["Success"] = "Record updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View("Create", dto);
        }

        public async Task<IActionResult> Delete(string id)
        {
            await _apiService.DeleteAsync($"salary-component/{id}");
            return RedirectToAction(nameof(Index));
        }

        #region Import

        [HttpGet]
        public IActionResult Import()
        {
            if (!_isAdmin)
                return Forbid();

            return View(new ExcelImportResult());
        }

        // Column mapping shared by DownloadImportTemplate and Import(POST) -
        // the single source of truth for what each Excel column means and
        // how it maps onto SalaryComponentDto. Headers carry the "*"/hint
        // text shown in the template; each Setter owns its own parsing so
        // there's no separate ParseYesNo/Cell(int) local helper duplicated
        // here anymore (that logic now lives once in APP.Excel.ExcelEngine
        // and per-column below).
        private static List<ExcelColumn<SalaryComponentDto>> GetImportColumns()
        {
            return new List<ExcelColumn<SalaryComponentDto>>
            {
                new ExcelColumn<SalaryComponentDto>(
                    "Component Name*",
                    d => d.Name,
                    (d, v) =>
                    {
                        if (string.IsNullOrWhiteSpace(v))
                            throw new Exception("Component Name is required.");

                        d.Name = v.Trim();
                    },
                    isRequired: true,
                    sampleValue: "House Rent Allowance"),

                new ExcelColumn<SalaryComponentDto>(
                    "Code*",
                    d => d.Code,
                    (d, v) =>
                    {
                        if (string.IsNullOrWhiteSpace(v))
                            throw new Exception("Code is required.");

                        d.Code = v.Trim();
                    },
                    isRequired: true,
                    sampleValue: "HRA"),

                new ExcelColumn<SalaryComponentDto>(
                    "Component Type* (Earning/Deduction)",
                    d => d.ComponentType == 1 ? "Earning" : "Deduction",
                    (d, v) =>
                    {
                        var text = (v ?? string.Empty).Trim();

                        if (string.Equals(text, "Earning", StringComparison.OrdinalIgnoreCase))
                            d.ComponentType = 1;
                        else if (string.Equals(text, "Deduction", StringComparison.OrdinalIgnoreCase))
                            d.ComponentType = 2;
                        else
                            throw new Exception($"Invalid Component Type '{text}'. Use Earning or Deduction.");
                    },
                    isRequired: true,
                    sampleValue: "Earning"),

                new ExcelColumn<SalaryComponentDto>(
                    "Taxable (Yes/No)",
                    d => d.IsTaxable ? "Yes" : "No",
                    (d, v) => d.IsTaxable = ParseYesNo(v),
                    sampleValue: "Yes"),

                new ExcelColumn<SalaryComponentDto>(
                    "PF Applicable (Yes/No)",
                    d => d.IsPFApplicable ? "Yes" : "No",
                    (d, v) => d.IsPFApplicable = ParseYesNo(v),
                    sampleValue: "No"),

                new ExcelColumn<SalaryComponentDto>(
                    "ESIC Applicable (Yes/No)",
                    d => d.IsESICApplicable ? "Yes" : "No",
                    (d, v) => d.IsESICApplicable = ParseYesNo(v),
                    sampleValue: "No"),
            };
        }

        private static List<ExcelColumn<SalaryComponentListDto>> GetExportColumns()
        {
            return new List<ExcelColumn<SalaryComponentListDto>>
            {
                new ExcelColumn<SalaryComponentListDto>("Component Name", d => d.Name, (d, v) => d.Name = v ?? string.Empty),
                new ExcelColumn<SalaryComponentListDto>("Code", d => d.Code, (d, v) => d.Code = v ?? string.Empty),
                new ExcelColumn<SalaryComponentListDto>("Component Type", d => d.ComponentType == 1 ? "Earning" : "Deduction", (d, v) => { }),
                new ExcelColumn<SalaryComponentListDto>("Taxable", d => d.IsTaxable ? "Yes" : "No", (d, v) => { }),
                new ExcelColumn<SalaryComponentListDto>("PF Applicable", d => d.IsPFApplicable ? "Yes" : "No", (d, v) => { }),
                new ExcelColumn<SalaryComponentListDto>("ESIC Applicable", d => d.IsESICApplicable ? "Yes" : "No", (d, v) => { }),
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

        // Same "Reference Data" sheet as before - the exact Component Type
        // text and Yes/No text the parser accepts - now expressed as data
        // the engine renders rather than hand-written ClosedXML cell calls.
        private static List<ExcelTemplateReferenceSheet> GetReferenceSheets()
        {
            return new List<ExcelTemplateReferenceSheet>
            {
                new ExcelTemplateReferenceSheet(
                    "Reference Data",
                    new List<ExcelReferenceColumn>
                    {
                        new ExcelReferenceColumn("Component Type", new[] { "Earning", "Deduction" }),
                        new ExcelReferenceColumn("Yes / No columns", new[] { "Yes", "No" }),
                    })
            };
        }

        // A ready-to-fill .xlsx: headers + one sample row, plus a
        // "Reference Data" sheet listing the exact Component Type text the
        // parser accepts, so nobody has to guess.
        [HttpGet]
        public IActionResult DownloadImportTemplate()
        {
            if (!_isAdmin)
                return Forbid();

            var sampleRow = new SalaryComponentDto
            {
                Name = "House Rent Allowance",
                Code = "HRA",
                ComponentType = 1,
                IsTaxable = true,
                IsPFApplicable = false,
                IsESICApplicable = false
            };

            var bytes = _excelEngine.BuildTemplate(
                GetImportColumns(),
                sheetName: "Salary Components",
                referenceSheets: GetReferenceSheets(),
                sampleRow: sampleRow);

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "SalaryComponent_Import_Template.xlsx");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (!_isAdmin)
                return Forbid();

            var result = new ExcelImportResult();

            List<ExcelImportRow<SalaryComponentDto>> rows;

            try
            {
                rows = _excelEngine.ReadRows(file, GetImportColumns());
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

                    await _apiService.PostAsync<dynamic>("salary-component", row.Item);

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
                TempData["GlobalError"] = "The uploaded file didn't contain any component rows.";
            else if (result.FailureCount == 0)
                TempData["Success"] = $"All {result.SuccessCount} salary component(s) imported successfully.";
            else
                TempData["Info"] = $"{result.SuccessCount} of {result.TotalRows} salary component(s) imported. {result.FailureCount} failed - see details below.";

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

            var data = await _apiService
                .GetAsync<List<SalaryComponentListDto>>("salary-component") ?? new();

            var bytes = _excelEngine.Export(data, GetExportColumns(), "Salary Components");

            var fileName = $"SalaryComponents_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        #endregion
    }

    #endregion
}
