using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;

namespace APP.Controllers
{
    #region Salary Component Controller

    [JwtAuthorize]
    public class SalaryComponentController : Controller
    {
        private readonly IApiService _apiService;
        private string _tenantId;
        private string _userId;

        // Import/Export touches every component org-wide - same admin-only
        // convention as the Employee bulk import feature.
        private readonly bool _isAdmin;

        public SalaryComponentController(IApiService apiService)
        {
            _apiService = apiService;
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

            return View(new SalaryComponentImportResultDto());
        }

        // A ready-to-fill .xlsx: headers + one sample row, plus a
        // "Reference Data" sheet listing the exact Component Type text the
        // parser accepts, so nobody has to guess.
        [HttpGet]
        public IActionResult DownloadImportTemplate()
        {
            if (!_isAdmin)
                return Forbid();

            using var workbook = new XLWorkbook();

            var sheet = workbook.Worksheets.Add("Salary Components");

            string[] headers =
            {
                "Component Name*", "Code*", "Component Type* (Earning/Deduction)",
                "Taxable (Yes/No)", "PF Applicable (Yes/No)", "ESIC Applicable (Yes/No)"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = sheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1B2A4A");
            }

            var sample = new[] { "House Rent Allowance", "HRA", "Earning", "Yes", "No", "No" };

            for (int i = 0; i < sample.Length; i++)
                sheet.Cell(2, i + 1).Value = sample[i];

            sheet.SheetView.FreezeRows(1);
            sheet.Columns().AdjustToContents();

            var refSheet = workbook.Worksheets.Add("Reference Data");
            refSheet.Cell(1, 1).Value = "Component Type";
            refSheet.Cell(1, 1).Style.Font.Bold = true;
            refSheet.Cell(2, 1).Value = "Earning";
            refSheet.Cell(3, 1).Value = "Deduction";

            refSheet.Cell(1, 2).Value = "Yes / No columns";
            refSheet.Cell(1, 2).Style.Font.Bold = true;
            refSheet.Cell(2, 2).Value = "Yes";
            refSheet.Cell(3, 2).Value = "No";

            refSheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "SalaryComponent_Import_Template.xlsx");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (!_isAdmin)
                return Forbid();

            var result = new SalaryComponentImportResultDto();

            if (file == null || file.Length == 0)
            {
                TempData["GlobalError"] = "Please choose an Excel file to import.";
                return View(result);
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (extension != ".xlsx" && extension != ".xls")
            {
                TempData["GlobalError"] = "Only .xlsx or .xls files are supported.";
                return View(result);
            }

            try
            {
                using var stream = file.OpenReadStream();
                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheet(1);

                var dataRows = worksheet.RowsUsed().Skip(1).ToList();

                foreach (var row in dataRows)
                {
                    string Cell(int col) => row.Cell(col).GetString().Trim();

                    var name = Cell(1);
                    var code = Cell(2);

                    // Skip fully blank trailing rows
                    if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(code))
                        continue;

                    var rowResult = new SalaryComponentImportRowResult
                    {
                        RowNumber = row.RowNumber(),
                        Code = code,
                        Name = name
                    };

                    try
                    {
                        if (string.IsNullOrWhiteSpace(name))
                            throw new Exception("Component Name is required.");

                        if (string.IsNullOrWhiteSpace(code))
                            throw new Exception("Code is required.");

                        var componentTypeText = Cell(3);
                        int componentType;

                        if (string.Equals(componentTypeText, "Earning", StringComparison.OrdinalIgnoreCase))
                            componentType = 1;
                        else if (string.Equals(componentTypeText, "Deduction", StringComparison.OrdinalIgnoreCase))
                            componentType = 2;
                        else
                            throw new Exception($"Invalid Component Type '{componentTypeText}'. Use Earning or Deduction.");

                        bool ParseYesNo(string text, bool defaultValue = false)
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

                        var dto = new SalaryComponentDto
                        {
                            Name = name,
                            Code = code,
                            ComponentType = componentType,
                            IsTaxable = ParseYesNo(Cell(4)),
                            IsPFApplicable = ParseYesNo(Cell(5)),
                            IsESICApplicable = ParseYesNo(Cell(6)),
                            TenantId = _tenantId,
                            CreatedBy = _userId
                        };

                        await _apiService.PostAsync<dynamic>("salary-component", dto);

                        rowResult.Success = true;
                        rowResult.Message = "Imported successfully.";
                    }
                    catch (ApiException apiEx)
                    {
                        rowResult.Success = false;
                        rowResult.Message = GetErrorMessage(apiEx.ResponseContent);
                    }
                    catch (Exception ex)
                    {
                        rowResult.Success = false;
                        rowResult.Message = ex.Message;
                    }

                    result.Rows.Add(rowResult);
                }
            }
            catch (Exception ex)
            {
                TempData["GlobalError"] = $"Unable to read the uploaded file: {ex.Message}";
                return View(result);
            }

            result.TotalRows = result.Rows.Count;
            result.SuccessCount = result.Rows.Count(x => x.Success);
            result.FailureCount = result.TotalRows - result.SuccessCount;

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

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Salary Components");

            string[] headers = { "Component Name", "Code", "Component Type", "Taxable", "PF Applicable", "ESIC Applicable" };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = sheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1B2A4A");
            }

            int r = 2;

            foreach (var item in data)
            {
                sheet.Cell(r, 1).Value = item.Name;
                sheet.Cell(r, 2).Value = item.Code;
                sheet.Cell(r, 3).Value = item.ComponentType == 1 ? "Earning" : "Deduction";
                sheet.Cell(r, 4).Value = item.IsTaxable ? "Yes" : "No";
                sheet.Cell(r, 5).Value = item.IsPFApplicable ? "Yes" : "No";
                sheet.Cell(r, 6).Value = item.IsESICApplicable ? "Yes" : "No";
                r++;
            }

            sheet.SheetView.FreezeRows(1);
            sheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            var fileName = $"SalaryComponents_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        #endregion
    }

    #endregion
}
