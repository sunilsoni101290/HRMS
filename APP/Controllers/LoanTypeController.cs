using APP.Attributes;
using APP.Excel;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.AspNetCore.Mvc;

namespace APP.Controllers
{
    // Admin-only master CRUD for Loan Types - see
    // API/Controllers/LoanTypeController.cs (api/loantype). Named exactly
    // "LoanType" to match AppFeatureConstants.LOAN_TYPE_CONTROLLER.
    [JwtAuthorize]
    public class LoanTypeController : Controller
    {
        private readonly IApiService _apiService;
        private readonly IExcelEngine _excelEngine;
        private readonly string? _tenantId;
        private readonly string? _userId;

        public LoanTypeController(IApiService apiService, IExcelEngine excelEngine)
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
                $"loantype?tenantId={Uri.EscapeDataString(_tenantId ?? string.Empty)}" +
                $"&actingUserId={Uri.EscapeDataString(_userId ?? string.Empty)}"+
                $"&includeInactive={includeInactive}";

                var data = await _apiService.GetAsync<List<LoanTypeDto>>(url);
                
                return View(data ?? new List<LoanTypeDto>());
            }
            catch (UnauthorizedAccessException ex) when (ex.Message.StartsWith("Access Denied"))
            {
                TempData["GlobalError"] = "You are not authorized to view Loan Types.";
                return RedirectToAction("Index", "Dashboard");
            }
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.InterestMethodList = EnumHelper.GetEnumList<EnumExtensions.LoanInterestMethod>();
            return View(new LoanTypeDto() { TenantId=_tenantId,ActingUserId=_userId});
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LoanTypeDto model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.InterestMethodList = EnumHelper.GetEnumList<EnumExtensions.LoanInterestMethod>();
                return View(model);
            }

            try
            {
                var result = await _apiService.PostAsync<LoanTypeDto, LoanTypeDto>("loantype", model);

                TempData[result != null ? "Success" : "GlobalError"] = result != null
                    ? "Loan Type created successfully."
                    : "Unable to create Loan Type.";

                return RedirectToAction(nameof(Index));
            }
            catch (ApiException ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
                ViewBag.InterestMethodList = EnumHelper.GetEnumList<EnumExtensions.LoanInterestMethod>();
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var data = await _apiService.GetAsync<LoanTypeDto>($"loantype/{id}");
            
            data.TenantId = _tenantId; data.ActingUserId=_userId;

            if (data == null) return NotFound();

            ViewBag.InterestMethodList = EnumHelper.GetEnumList<EnumExtensions.LoanInterestMethod>();
            return View("Create", data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, LoanTypeDto model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.InterestMethodList = EnumHelper.GetEnumList<EnumExtensions.LoanInterestMethod>();
                return View("Create", model);
            }

            try
            {
                var result = await _apiService.PutAsync<LoanTypeDto>($"loantype/{id}", model);

                TempData[result != null ? "Success" : "GlobalError"] = result != null
                    ? "Loan Type updated successfully."
                    : "Unable to update Loan Type.";

                return RedirectToAction(nameof(Index));
            }
            catch (ApiException ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
                ViewBag.InterestMethodList = EnumHelper.GetEnumList<EnumExtensions.LoanInterestMethod>();
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
            $"loantype/{Uri.EscapeDataString(id ?? string.Empty)}" +
            $"?tenantId={Uri.EscapeDataString(_tenantId ?? string.Empty)}" +
            $"&actingUserId={Uri.EscapeDataString(_userId ?? string.Empty)}";


                var result = await _apiService.DeleteAsync(url);
                TempData[result ? "Success" : "GlobalError"] = result
                    ? "Loan Type deleted successfully."
                    : "Unable to delete Loan Type.";
            }
            catch (ApiException ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
            }

            return RedirectToAction(nameof(Index));
        }

        #region Export / Import (Phase 13)

        private static List<ExcelColumn<LoanTypeDto>> GetExportColumns()
        {
            return new List<ExcelColumn<LoanTypeDto>>
            {
                new("Code", d => d.Code, (d, v) => { }),
                new("Name", d => d.Name, (d, v) => { }),
                new("Interest Method", d => d.InterestMethodName, (d, v) => { }),
                new("Default Interest Rate (%)", d => d.DefaultInterestRatePercent, (d, v) => { }),
                new("Max Tenure (Months)", d => d.MaxTenureMonths, (d, v) => { }),
                new("Requires Guarantor", d => d.RequiresGuarantor ? "Yes" : "No", (d, v) => { }),
                new("Requires Collateral", d => d.RequiresCollateral ? "Yes" : "No", (d, v) => { }),
                new("Active Loans", d => d.ActiveLoanCount, (d, v) => { }),
                new("Status", d => d.IsActive ? "Active" : "Inactive", (d, v) => { }),
            };
        }

        private static List<ExcelColumn<LoanTypeDto>> GetImportColumns()
        {
            return new List<ExcelColumn<LoanTypeDto>>
            {
                new("Code*", d => d.Code, (d, v) =>
                {
                    if (string.IsNullOrWhiteSpace(v)) throw new Exception("Code is required.");
                    d.Code = v.Trim();
                }, isRequired: true, sampleValue: "PL"),

                new("Name*", d => d.Name, (d, v) =>
                {
                    if (string.IsNullOrWhiteSpace(v)) throw new Exception("Name is required.");
                    d.Name = v.Trim();
                }, isRequired: true, sampleValue: "Personal Loan"),

                new("Description", d => d.Description, (d, v) => d.Description = string.IsNullOrWhiteSpace(v) ? null : v.Trim()),

                new("Interest Method (Reducing/Flat)*", d => d.InterestMethodName, (d, v) =>
                {
                    d.InterestMethod = (v ?? "").Trim().Equals("Flat", StringComparison.OrdinalIgnoreCase) ? 2 : 1;
                }, isRequired: true, sampleValue: "Reducing"),

                new("Default Interest Rate (%)*", d => d.DefaultInterestRatePercent, (d, v) =>
                {
                    if (!decimal.TryParse(v, out var rate) || rate < 0 || rate > 100)
                        throw new Exception("Default Interest Rate must be a number between 0 and 100.");
                    d.DefaultInterestRatePercent = rate;
                }, isRequired: true, sampleValue: "12"),

                new("Max Tenure (Months)*", d => d.MaxTenureMonths, (d, v) =>
                {
                    if (!int.TryParse(v, out var months) || months <= 0)
                        throw new Exception("Max Tenure Months must be a positive whole number.");
                    d.MaxTenureMonths = months;
                }, isRequired: true, sampleValue: "60"),

                new("Requires Guarantor (Yes/No)", d => d.RequiresGuarantor ? "Yes" : "No", (d, v) =>
                    d.RequiresGuarantor = (v ?? "").Trim().Equals("Yes", StringComparison.OrdinalIgnoreCase), sampleValue: "No"),

                new("Requires Collateral (Yes/No)", d => d.RequiresCollateral ? "Yes" : "No", (d, v) =>
                    d.RequiresCollateral = (v ?? "").Trim().Equals("Yes", StringComparison.OrdinalIgnoreCase), sampleValue: "No"),
            };
        }

        [HttpGet]
        public async Task<IActionResult> Export()
        {
            var data = await _apiService.GetAsync<List<LoanTypeDto>>("loantype?includeInactive=true") ?? new();
            var bytes = _excelEngine.Export(data, GetExportColumns(), "Loan Types");

            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"LoanTypes_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        }

        [HttpGet]
        public IActionResult Import() => View(new ExcelImportResult());

        [HttpGet]
        public IActionResult DownloadImportTemplate()
        {
            var bytes = _excelEngine.BuildTemplate(GetImportColumns(), sheetName: "Loan Types");
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "LoanType_Import_Template.xlsx");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile file)
        {
            var result = new ExcelImportResult();
            List<ExcelImportRow<LoanTypeDto>> rows;

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

                    await _apiService.PostAsync<dynamic>("loantype", row.Item);
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
                TempData["GlobalError"] = "The uploaded file didn't contain any Loan Type rows.";
            else if (result.FailureCount == 0)
                TempData["Success"] = $"All {result.SuccessCount} Loan Type(s) imported successfully.";
            else
                TempData["Info"] = $"{result.SuccessCount} of {result.TotalRows} Loan Type(s) imported. {result.FailureCount} failed - see details below.";

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

                return "Unable to process this Loan Type request.";
            }
            catch
            {
                return string.IsNullOrWhiteSpace(raw) ? "Unable to process this Loan Type request." : raw;
            }
        }
    }
}
