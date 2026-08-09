using APP.Attributes;
using APP.Excel;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace APP.Controllers
{
    // Admin-only master CRUD for Advance Types - see
    // API/Controllers/AdvanceTypeController.cs (api/advancetype). Named
    // exactly "AdvanceType" to match AppFeatureConstants.ADVANCE_TYPE_CONTROLLER.
    [JwtAuthorize]
    public class AdvanceTypeController : Controller
    {
        private readonly IApiService _apiService;
        private readonly IExcelEngine _excelEngine;
        private readonly string? _tenantId;
        private readonly string? _userId;

        public AdvanceTypeController(IApiService apiService, IExcelEngine excelEngine)
        {
            _apiService = apiService;
            _excelEngine = excelEngine;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
        }

        [HttpGet]
        public async Task<IActionResult> Index(bool includeInactive = false)
        {
            ViewBag.IncludeInactive = includeInactive;

            try
            {
                var url =
                    $"advancetype" +
                    $"?includeInactive={includeInactive}" +
                    $"&tenantId={Uri.EscapeDataString(_tenantId ?? string.Empty)}" +
                    $"&actingUserId={Uri.EscapeDataString(_userId ?? string.Empty)}";

                var data = await _apiService.GetAsync<List<AdvanceTypeDto>>(url);
                
                return View(data ?? new List<AdvanceTypeDto>());
            }
            catch (UnauthorizedAccessException ex) when (ex.Message.StartsWith("Access Denied"))
            {
                TempData["GlobalError"] = "You are not authorized to view Advance Types.";
                return RedirectToAction("Index", "Dashboard");
            }
        }

        [HttpGet]
        public IActionResult Create() => View(new AdvanceTypeDto() { TenantId=_tenantId,ActingUserId =_userId,CreatedBy =_userId });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdvanceTypeDto model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                var result = await _apiService.PostAsync<AdvanceTypeDto, AdvanceTypeDto>("advancetype", model);

                TempData[result != null ? "Success" : "GlobalError"] = result != null
                    ? "Advance Type created successfully."
                    : "Unable to create Advance Type.";

                return RedirectToAction(nameof(Index));
            }
            catch (ApiException ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var data = await _apiService.GetAsync<AdvanceTypeDto>($"advancetype/{id}");
            if (data == null) return NotFound();
            
            data.TenantId= _tenantId;
            data.ActingUserId= _userId;

            return View("Create", data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, AdvanceTypeDto model)
        {
            if (!ModelState.IsValid) return View("Create", model);

            try
            {
                var result = await _apiService.PutAsync<AdvanceTypeDto>($"advancetype/{id}", model);

                TempData[result != null ? "Success" : "GlobalError"] = result != null
                    ? "Advance Type updated successfully."
                    : "Unable to update Advance Type.";

                return RedirectToAction(nameof(Index));
            }
            catch (ApiException ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
                return View("Create", model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var url =
                $"advancetype/{id}" +
                $"?tenantId={Uri.EscapeDataString(_tenantId ?? string.Empty)}" +
                $"&actingUserId={Uri.EscapeDataString(_userId ?? string.Empty)}";

                var result = await _apiService.DeleteAsync(url);

                TempData[result ? "Success" : "GlobalError"] = result
                    ? "Advance Type deleted successfully."
                    : "Unable to delete Advance Type.";
            }
            catch (ApiException ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
            }

            return RedirectToAction(nameof(Index));
        }

        #region Export / Import (Phase 13)

        private static List<ExcelColumn<AdvanceTypeDto>> GetExportColumns()
        {
            return new List<ExcelColumn<AdvanceTypeDto>>
            {
                new("Code", d => d.Code, (d, v) => { }),
                new("Name", d => d.Name, (d, v) => { }),
                new("Max Amount", d => d.MaxAmount, (d, v) => { }),
                new("Max Amount (Salary Multiplier)", d => d.MaxAmountSalaryMultiplier, (d, v) => { }),
                new("Max Installments", d => d.MaxInstallments, (d, v) => { }),
                new("Interest Free", d => d.IsInterestFree ? "Yes" : "No", (d, v) => { }),
                new("Active Advances", d => d.ActiveAdvanceCount, (d, v) => { }),
                new("Status", d => d.IsActive ? "Active" : "Inactive", (d, v) => { }),
            };
        }

        private static List<ExcelColumn<AdvanceTypeDto>> GetImportColumns()
        {
            return new List<ExcelColumn<AdvanceTypeDto>>
            {
                new("Code*", d => d.Code, (d, v) =>
                {
                    if (string.IsNullOrWhiteSpace(v)) throw new Exception("Code is required.");
                    d.Code = v.Trim();
                }, isRequired: true, sampleValue: "SAL"),

                new("Name*", d => d.Name, (d, v) =>
                {
                    if (string.IsNullOrWhiteSpace(v)) throw new Exception("Name is required.");
                    d.Name = v.Trim();
                }, isRequired: true, sampleValue: "Salary Advance"),

                new("Description", d => d.Description, (d, v) => d.Description = string.IsNullOrWhiteSpace(v) ? null : v.Trim()),

                new("Max Amount", d => d.MaxAmount, (d, v) =>
                {
                    if (string.IsNullOrWhiteSpace(v)) { d.MaxAmount = null; return; }
                    if (!decimal.TryParse(v, out var amt) || amt <= 0) throw new Exception("Max Amount must be a positive number, or left blank.");
                    d.MaxAmount = amt;
                }),

                new("Max Installments*", d => d.MaxInstallments, (d, v) =>
                {
                    if (!int.TryParse(v, out var count) || count <= 0)
                        throw new Exception("Max Installments must be a positive whole number.");
                    d.MaxInstallments = count;
                }, isRequired: true, sampleValue: "3"),

                new("Interest Free (Yes/No)", d => d.IsInterestFree ? "Yes" : "No", (d, v) =>
                    d.IsInterestFree = string.IsNullOrWhiteSpace(v) || (v ?? "").Trim().Equals("Yes", StringComparison.OrdinalIgnoreCase), sampleValue: "Yes"),
            };
        }

        [HttpGet]
        public async Task<IActionResult> Export()
        {
            var data = await _apiService.GetAsync<List<AdvanceTypeDto>>("advancetype?includeInactive=true") ?? new();
            var bytes = _excelEngine.Export(data, GetExportColumns(), "Advance Types");

            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"AdvanceTypes_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        }

        [HttpGet]
        public IActionResult Import() => View(new ExcelImportResult());

        [HttpGet]
        public IActionResult DownloadImportTemplate()
        {
            var bytes = _excelEngine.BuildTemplate(GetImportColumns(), sheetName: "Advance Types");
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "AdvanceType_Import_Template.xlsx");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile file)
        {
            var result = new ExcelImportResult();
            List<ExcelImportRow<AdvanceTypeDto>> rows;

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
                var identifier = string.Join(" - ", new[] { row.Item.Code, row.Item.Name }.Where(x => !string.IsNullOrWhiteSpace(x)));

                if (row.HasErrors)
                {
                    result.AddRow(row.RowNumber, identifier, false, string.Join(" ", row.ParseErrors));
                    continue;
                }

                try
                {
                    row.Item.TenantId = _tenantId;
                    row.Item.CreatedBy = _userId;

                    await _apiService.PostAsync<dynamic>("advancetype", row.Item);
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
                TempData["GlobalError"] = "The uploaded file didn't contain any Advance Type rows.";
            else if (result.FailureCount == 0)
                TempData["Success"] = $"All {result.SuccessCount} Advance Type(s) imported successfully.";
            else
                TempData["Info"] = $"{result.SuccessCount} of {result.TotalRows} Advance Type(s) imported. {result.FailureCount} failed - see details below.";

            return View(result);
        }

        #endregion

        private string GetErrorMessage(string raw)
        {
            try
            {
                var jsonStart = raw.IndexOf('{');
                var json = jsonStart >= 0 ? raw.Substring(jsonStart) : raw;
                var obj = Newtonsoft.Json.Linq.JObject.Parse(json);

                if (obj["Message"] != null) return obj["Message"]!.ToString();

                if (obj["errors"] is Newtonsoft.Json.Linq.JObject validationErrors)
                {
                    foreach (var property in validationErrors.Properties())
                    {
                        if (property.Value is Newtonsoft.Json.Linq.JArray arr && arr.Count > 0)
                            return arr[0]?.ToString();
                    }
                }

                return "Unable to process this Advance Type request.";
            }
            catch
            {
                return string.IsNullOrWhiteSpace(raw) ? "Unable to process this Advance Type request." : raw;
            }
        }
    }
}
