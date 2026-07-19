using APP.Attributes;
using APP.Excel;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace APP.Controllers
{
    [JwtAuthorize]
    public class LeaveTypeController : Controller
    {
        private readonly IApiService _apiService;
        private readonly IExcelEngine _excelEngine;
        private string _tenantId;
        private string _userId;
        private readonly bool _isAdmin;

        public LeaveTypeController(IApiService apiService, IExcelEngine excelEngine)
        {
            _apiService = apiService;
            _excelEngine = excelEngine;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
            _isAdmin = SessionHelper.IsAdminRole();
        }

        #region Index

        public async Task<IActionResult> Index()
        {
            try
            {
                var leaveTypes = await _apiService.GetAsync<List<LeaveTypeDto>>("LeaveType");

                return View(leaveTypes);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return View(new List<LeaveTypeDto>());
            }
        }

        #endregion

        #region Details

        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var leaveType = await _apiService.GetAsync<LeaveTypeDto>(
                $"LeaveType/{id}");

            if (leaveType == null)
                return NotFound();

            return View(leaveType);
        }

        #endregion

        #region Create

        [HttpGet]
        public IActionResult Create()
        {
            return View(new LeaveTypeDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LeaveTypeDto model)
        {

            try
            {
                model.TenantId = _tenantId;
                model.CreatedBy = _userId;

                var response =
                    await _apiService.PostAsync<LeaveTypeDto, ApiResponse<LeaveTypeDto>>
                    (
                        $"LeaveType",
                        model
                    );

                if (response.Success)
                {
                    AlertHelper.Success(TempData, "Leave Type created successfully.");

                    return RedirectToAction(nameof(Index));
                }

                AlertHelper.Error(TempData, "Unable to create Leave Type.");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    "",
                    ex.Message);
            }

            return View(model);
        }

        #endregion

        #region Edit

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var leaveType = await _apiService.GetAsync<LeaveTypeDto>(
                $"LeaveType/{id}");

            if (leaveType == null)
                return NotFound();

            return View("Create",leaveType);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(LeaveTypeDto model)
        {
            if (model==null)
                return View(model);

            model.TenantId = _tenantId;
            model.CreatedBy = _userId;
            model.ModifiedBy = _userId;
            model.ModifiedOn = DateTime.Now;

            var response =
                    await _apiService.PutAsync<LeaveTypeDto, ApiResponse<LeaveTypeDto>>
                    (
                        $"LeaveType/{model.Id}",
                        model
                    );

            if (response.Success)
            {
                AlertHelper.Success(TempData, "Leave Type updated successfully.");

                return RedirectToAction(nameof(Index));
            }

            AlertHelper.Error(TempData, "Unable to update Leave Type.");

            return View("Create",model);
        }

        #endregion

        #region Delete

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return Json(new
                {
                    success = false,
                    message = "Invalid Id."
                });
            }

            var result = await _apiService.DeleteAsync(
                $"LeaveType/{id}");

            return Json(new
            {
                success = result,
                message = result
                    ? "Leave Type deleted successfully."
                    : "Unable to delete Leave Type."
            });
        }

        #endregion

        #region Import

        // Leave Type has no cross-entity lookups, so this is a simple
        // column-by-column import - same shape as Salary Component's.
        //
        // Design decision on the AllowCarryForward / MaxCarryForwardDays
        // dependency: rather than silently accepting 0/blank when carry
        // forward is off (which would let a stray number sit in the file
        // unnoticed) or silently ignoring a filled-in number when it's on,
        // this validates the dependency explicitly at import time:
        //   - AllowCarryForward = Yes  -> Max Carry Forward Days must be a
        //     positive whole number, or the row fails with a clear message.
        //   - AllowCarryForward = No   -> Max Carry Forward Days is forced
        //     to null regardless of what's typed in the cell, since it's
        //     not meaningful without carry forward enabled.
        private static List<ExcelColumn<LeaveTypeDto>> GetImportColumns()
        {
            return new List<ExcelColumn<LeaveTypeDto>>
            {
                new ExcelColumn<LeaveTypeDto>(
                    "Leave Type Name*",
                    d => d.Name,
                    (d, v) =>
                    {
                        if (string.IsNullOrWhiteSpace(v))
                            throw new Exception("Leave Type Name is required.");
                        d.Name = v.Trim();
                    },
                    isRequired: true,
                    sampleValue: "Earned Leave"),

                new ExcelColumn<LeaveTypeDto>(
                    "Max Days Per Year*",
                    d => d.MaxDaysPerYear,
                    (d, v) =>
                    {
                        if (string.IsNullOrWhiteSpace(v) || !int.TryParse(v.Trim(), out var days) || days < 0)
                            throw new Exception("Max Days Per Year is required and must be a non-negative whole number.");
                        d.MaxDaysPerYear = days;
                    },
                    isRequired: true,
                    sampleValue: 18),

                new ExcelColumn<LeaveTypeDto>(
                    "Paid Leave (Yes/No)",
                    d => d.IsPaid ? "Yes" : "No",
                    (d, v) => d.IsPaid = ParseYesNo(v, defaultValue: true),
                    sampleValue: "Yes"),

                new ExcelColumn<LeaveTypeDto>(
                    "Allow Carry Forward (Yes/No)",
                    d => d.AllowCarryForward ? "Yes" : "No",
                    (d, v) => d.AllowCarryForward = ParseYesNo(v),
                    sampleValue: "No"),

                new ExcelColumn<LeaveTypeDto>(
                    "Max Carry Forward Days",
                    d => d.MaxCarryForwardDays,
                    (d, v) =>
                    {
                        if (!d.AllowCarryForward)
                        {
                            // Not meaningful without carry forward enabled -
                            // ignore whatever was typed here.
                            d.MaxCarryForwardDays = null;
                            return;
                        }

                        if (string.IsNullOrWhiteSpace(v) || !int.TryParse(v.Trim(), out var days) || days <= 0)
                            throw new Exception("Max Carry Forward Days is required and must be a positive whole number when Allow Carry Forward is Yes.");

                        d.MaxCarryForwardDays = days;
                    },
                    sampleValue: ""),

                new ExcelColumn<LeaveTypeDto>(
                    "Allow Half Day (Yes/No)",
                    d => d.AllowHalfDay ? "Yes" : "No",
                    (d, v) => d.AllowHalfDay = ParseYesNo(v),
                    sampleValue: "Yes"),
            };
        }

        private static List<ExcelColumn<LeaveTypeDto>> GetExportColumns()
        {
            return new List<ExcelColumn<LeaveTypeDto>>
            {
                new ExcelColumn<LeaveTypeDto>("Leave Type Name", d => d.Name, (d, v) => { }),
                new ExcelColumn<LeaveTypeDto>("Max Days Per Year", d => d.MaxDaysPerYear, (d, v) => { }),
                new ExcelColumn<LeaveTypeDto>("Paid Leave", d => d.IsPaid ? "Yes" : "No", (d, v) => { }),
                new ExcelColumn<LeaveTypeDto>("Allow Carry Forward", d => d.AllowCarryForward ? "Yes" : "No", (d, v) => { }),
                new ExcelColumn<LeaveTypeDto>("Max Carry Forward Days", d => d.MaxCarryForwardDays, (d, v) => { }),
                new ExcelColumn<LeaveTypeDto>("Allow Half Day", d => d.AllowHalfDay ? "Yes" : "No", (d, v) => { }),
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

            var sampleRow = new LeaveTypeDto
            {
                Name = "Earned Leave",
                MaxDaysPerYear = 18,
                IsPaid = true,
                AllowCarryForward = true,
                MaxCarryForwardDays = 10,
                AllowHalfDay = true
            };

            var bytes = _excelEngine.BuildTemplate(
                GetImportColumns(),
                sheetName: "Leave Types",
                sampleRow: sampleRow);

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "LeaveType_Import_Template.xlsx");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (!_isAdmin)
                return Forbid();

            var result = new ExcelImportResult();

            List<ExcelImportRow<LeaveTypeDto>> rows;

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

                    await _apiService.PostAsync<dynamic>("LeaveType", row.Item);

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
                TempData["GlobalError"] = "The uploaded file didn't contain any leave type rows.";
            else if (result.FailureCount == 0)
                TempData["Success"] = $"All {result.SuccessCount} leave type(s) imported successfully.";
            else
                TempData["Info"] = $"{result.SuccessCount} of {result.TotalRows} leave type(s) imported. {result.FailureCount} failed - see details below.";

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

            var data = await _apiService.GetAsync<List<LeaveTypeDto>>("LeaveType") ?? new();

            var bytes = _excelEngine.Export(data, GetExportColumns(), "Leave Types");

            var fileName = $"LeaveTypes_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        #endregion
    }
 }
