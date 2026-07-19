using APP.Attributes;
using APP.Excel;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace APP.Controllers
{
    #region Country Controller

    [JwtAuthorize]
    public class CountryController : Controller
    {
        private readonly IApiService _apiService;
        private readonly IExcelEngine _excelEngine;
        private string _tenantId;
        private string _userId;

        // Country is admin/master data - the whole controller is already
        // blocked for self-service users by EssRestrictionAttribute
        // (AdminOnlyControllers), but Import/Export also get their own
        // explicit _isAdmin check here to match the SalaryComponent
        // reference pattern exactly.
        private readonly bool _isAdmin;

        public CountryController(IApiService apiService, IExcelEngine excelEngine)
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
            var data = await _apiService.GetAsync<List<CountryDto>>("country");

            return View(data);
        }

        // =====================================================
        // CREATE - GET
        // =====================================================
        [HttpGet]
        public IActionResult Create()
        {
            return View(new CountryDto());
        }

        // =====================================================
        // CREATE - POST
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CountryDto dto)
        {
            try
            {
                if (dto!=null)
                {

                    dto.CreatedBy = _userId;
                    dto.TenantId = _tenantId;

                    var response = await _apiService.PostAsync<CountryDto>("Country", dto);

                    TempData["Success"] = "Country created successfully.";
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
        // EDIT - GET
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var data = await _apiService.GetAsync<CountryDto>($"Country/{id}");

            if (data == null)
            {
                return NotFound();
            }

            return View("Create",data);
        }

        // =====================================================
        // EDIT - POST
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CountryDto dto)
        {
            try
            {
                if (dto != null)
                {

                    dto.ModifiedOn = DateTime.UtcNow;
                    dto.ModifiedBy = _userId;
                    dto.CreatedBy = _userId;

                    await _apiService.PutAsync<dynamic>($"Country/{dto.Id}", dto);

                    TempData["Success"] = "Record updated successfully.";

                    return View("Create", dto);
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
        // DETAILS
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var data = await _apiService.GetAsync<CountryDto>($"Country/{id}");

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

                await _apiService.DeleteAsync($"Country/{id}");

                TempData["Success"] = "Country deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        #region Import

        // Column mapping shared by DownloadImportTemplate and Import(POST).
        // Country sits at the top of the location hierarchy - it has no
        // lookup dependencies, unlike State/City/Department/Designation.
        private static List<ExcelColumn<CountryDto>> GetImportColumns()
        {
            return new List<ExcelColumn<CountryDto>>
            {
                new ExcelColumn<CountryDto>(
                    "Country Name*",
                    d => d.Name,
                    (d, v) =>
                    {
                        if (string.IsNullOrWhiteSpace(v))
                            throw new Exception("Country Name is required.");

                        d.Name = v.Trim();
                    },
                    isRequired: true,
                    sampleValue: "India"),

                new ExcelColumn<CountryDto>(
                    "Country Code*",
                    d => d.Code,
                    (d, v) =>
                    {
                        if (string.IsNullOrWhiteSpace(v))
                            throw new Exception("Country Code is required.");

                        d.Code = v.Trim();
                    },
                    isRequired: true,
                    sampleValue: "IN"),

                new ExcelColumn<CountryDto>(
                    "Phone Code*",
                    d => d.PhoneCode,
                    (d, v) =>
                    {
                        if (string.IsNullOrWhiteSpace(v))
                            throw new Exception("Phone Code is required.");

                        d.PhoneCode = v.Trim();
                    },
                    isRequired: true,
                    sampleValue: "+91"),
            };
        }

        private static List<ExcelColumn<CountryDto>> GetExportColumns()
        {
            return new List<ExcelColumn<CountryDto>>
            {
                new ExcelColumn<CountryDto>("Country Name", d => d.Name, (d, v) => d.Name = v ?? string.Empty),
                new ExcelColumn<CountryDto>("Country Code", d => d.Code, (d, v) => d.Code = v ?? string.Empty),
                new ExcelColumn<CountryDto>("Phone Code", d => d.PhoneCode, (d, v) => d.PhoneCode = v ?? string.Empty),
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
        public IActionResult DownloadImportTemplate()
        {
            if (!_isAdmin)
                return Forbid();

            var bytes = _excelEngine.BuildTemplate(
                GetImportColumns(),
                sheetName: "Countries");

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Country_Import_Template.xlsx");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (!_isAdmin)
                return Forbid();

            var result = new ExcelImportResult();

            List<ExcelImportRow<CountryDto>> rows;

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

                    await _apiService.PostAsync<dynamic>("Country", row.Item);

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
                TempData["GlobalError"] = "The uploaded file didn't contain any country rows.";
            else if (result.FailureCount == 0)
                TempData["Success"] = $"All {result.SuccessCount} countr{(result.SuccessCount == 1 ? "y" : "ies")} imported successfully.";
            else
                TempData["Info"] = $"{result.SuccessCount} of {result.TotalRows} countries imported. {result.FailureCount} failed - see details below.";

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

            var data = await _apiService.GetAsync<List<CountryDto>>("Country") ?? new();

            var bytes = _excelEngine.Export(data, GetExportColumns(), "Countries");

            var fileName = $"Countries_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        #endregion
    }

    #endregion
}
