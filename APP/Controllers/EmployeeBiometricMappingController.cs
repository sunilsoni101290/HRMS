using APP.Attributes;
using APP.Excel;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace APP.Controllers
{
    /// <summary>
    /// Lets an admin link an Employee to the code their biometric device
    /// reports (BiometricAttendanceLog.EmployeeCode / BiometricEmployeeCode)
    /// through a form, instead of writing directly to the database. Required
    /// before any punch - real or test - can turn into an Attendance record.
    ///
    /// The Bulk/SaveBulk/AutoMap/Export/Import actions below add a
    /// bulk-mapping workflow entirely at this APP layer, by combining three
    /// already-existing endpoints (Employee/employee-list,
    /// EmployeeBiometricMapping, esslattendance/unmapped-employees) and
    /// looping the existing single-row Create/Update API calls - no new
    /// Application/API-layer code was needed, and no schema changes.
    /// </summary>
    [JwtAuthorize]
    public class EmployeeBiometricMappingController : Controller
    {
        private readonly IApiService _apiService;
        private readonly IExcelEngine _excelEngine;
        private string _userId;
        private string _tenantId;

        public EmployeeBiometricMappingController(IApiService apiService, IExcelEngine excelEngine)
        {
            _apiService = apiService;
            _excelEngine = excelEngine;
            _userId = SessionHelper.GetActiveUserId;
            _tenantId = SessionHelper.GetActiveTenantId;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _apiService
                .GetAsync<List<EmployeeBiometricMappingDto>>("EmployeeBiometricMapping");

            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();
            return View(new EmployeeBiometricMappingDto());
        }

        [HttpPost]
        public async Task<ActionResult> Create(EmployeeBiometricMappingDto model)
        {
            try
            {
                model.CreatedBy = _userId;
                model.TenantId = _tenantId;
                var response =
                    await _apiService.PostAsync<EmployeeBiometricMappingDto, ApiResponse<EmployeeBiometricMappingDto>>
                    (
                        "EmployeeBiometricMapping",
                        model
                    );

                if (response.Success)
                {
                    TempData["Success"] = response.Message;

                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError("", response.Message);
            }
            catch (ApiException ex)
            {
                // PostAsync<TRequest,TResponse> always throws ApiException on
                // a non-success response (see ApiService.PostAsync) - unwrap
                // ex.ResponseContent instead of showing the generic
                // "API Error" message, same pattern as EmployeeBankController.
                ModelState.AddModelError("", GetErrorMessage(ex.ResponseContent));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
            }

            await LoadDropdowns();

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {

            var data = await _apiService
                .GetAsync<EmployeeBiometricMappingDto>($"EmployeeBiometricMapping/{id}");

            data.CreatedBy = _userId;
            data.TenantId = _tenantId;

            if (data == null)
                return NotFound();

            await LoadDropdowns();

            return View("Create", data);
        }

        [HttpPost]
        public async Task<ActionResult> Edit(EmployeeBiometricMappingDto model)
        {
            try
            {
                var response =
                    await _apiService.PutAsync<EmployeeBiometricMappingDto, ApiResponse<object>>
                    (
                        "EmployeeBiometricMapping",
                        model
                    );

                if (response.Success)
                {
                    TempData["Success"] = response.Message;

                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError("", response.Message);
            }
            catch (Exception ex)
            {
                // PutAsync<TRequest,TResponse> (unlike PostAsync) doesn't
                // wrap a non-success response in ApiException - HandleResponse
                // throws a plain Exception whose Message is
                // "Bad Request (400): {json}", so pull the embedded json
                // back out to get at the real validation/business message.
                ModelState.AddModelError("", GetErrorMessage(ex.Message));
            }

            await LoadDropdowns();

            return View("Create", model);
        }

        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _apiService.DeleteAsync($"EmployeeBiometricMapping/{id}");

                AlertHelper.Success(TempData, "Mapping removed successfully.");
            }
            catch (ApiException ex)
            {
                AlertHelper.Error(TempData, GetErrorMessage(ex.ResponseContent));
            }
            catch (Exception ex)
            {
                AlertHelper.Error(TempData, GetErrorMessage(ex.Message));
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadDropdowns()
        {
            var employees = await _apiService
                .GetAsync<List<DropdownDto>>("dropdown/employee");

            ViewBag.EmployeeList = new SelectList(employees, "Value", "Text");
        }

        // ==================================================================
        // BULK MAPPING
        // ==================================================================

        [HttpGet]
        public async Task<IActionResult> Bulk()
        {
            var vm = await BuildBulkViewModelAsync();
            return View(vm);
        }

        [HttpPost]
        public async Task<JsonResult> SaveBulk([FromBody] List<BulkMappingSaveRowDto> rows)
        {
            var result = await SaveRowsAsync(rows ?? new List<BulkMappingSaveRowDto>());
            return Json(result);
        }

        // Auto-creates a mapping wherever an eSSL-reported biometric code
        // exactly equals an employee's own EmployeeCode (case-insensitive) -
        // the common case where the device was enrolled using the same
        // codes as the HRMS. Anything that doesn't match exactly is left for
        // manual mapping in the "Unmapped From Device" section/grid.
        [HttpPost]
        public async Task<JsonResult> AutoMap()
        {
            List<EsslUnmappedEmployeeDto> unmapped;

            try
            {
                unmapped = await _apiService
                    .GetAsync<List<EsslUnmappedEmployeeDto>>("esslattendance/unmapped-employees")
                    ?? new List<EsslUnmappedEmployeeDto>();
            }
            catch
            {
                return Json(new { success = false, createdCount = 0, remaining = new List<EsslUnmappedEmployeeDto>(), message = "eSSL integration isn't configured for this tenant." });
            }

            if (unmapped.Count == 0)
                return Json(new { success = true, createdCount = 0, remaining = new List<EsslUnmappedEmployeeDto>() });

            var employees = await _apiService
                .GetAsync<List<EmployeeListDto>>("Employee/employee-list")
                ?? new List<EmployeeListDto>();

            var byCode = employees
                .Where(e => e.RelievingDate == null && !string.IsNullOrWhiteSpace(e.EmployeeCode))
                .GroupBy(e => e.EmployeeCode.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var rowsToSave = new List<BulkMappingSaveRowDto>();
            var remaining = new List<EsslUnmappedEmployeeDto>();

            foreach (var u in unmapped)
            {
                if (byCode.TryGetValue(u.BiometricEmployeeCode?.Trim() ?? "", out var emp))
                {
                    rowsToSave.Add(new BulkMappingSaveRowDto
                    {
                        EmployeeId = emp.Id,
                        MappingId = null,
                        BiometricEmployeeCode = u.BiometricEmployeeCode,
                        CardNumber = null,
                        IsActive = true
                    });
                }
                else
                {
                    remaining.Add(u);
                }
            }

            if (rowsToSave.Count == 0)
                return Json(new { success = true, createdCount = 0, remaining });

            var saveResult = await SaveRowsAsync(rowsToSave);

            // A row can still fail here (e.g. that employee was already
            // mapped to a different code moments earlier) - put failed rows
            // back into "remaining" instead of silently dropping them.
            var failedCodes = saveResult.Results
                .Where(r => !r.Success)
                .Select(r => r.EmployeeCode)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (failedCodes.Count > 0)
            {
                remaining.AddRange(unmapped.Where(u =>
                    byCode.TryGetValue(u.BiometricEmployeeCode?.Trim() ?? "", out var emp) &&
                    failedCodes.Contains(emp.EmployeeCode)));
            }

            return Json(new { success = true, createdCount = saveResult.SuccessCount, remaining });
        }

        [HttpGet]
        public IActionResult DownloadImportTemplate()
        {
            var sampleRow = new BulkMappingImportRowDto
            {
                EmployeeCode = "EMP001",
                BiometricEmployeeCode = "EMP001",
                CardNumber = "",
                IsActive = true
            };

            var bytes = _excelEngine.BuildTemplate(
                GetImportColumns(),
                sheetName: "Biometric Mapping",
                sampleRow: sampleRow);

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "EmployeeBiometricMapping_Import_Template.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> Export()
        {
            var vm = await BuildBulkViewModelAsync();

            var bytes = _excelEngine.Export(vm.Rows, GetExportColumns(), "Biometric Mapping");

            var fileName = $"EmployeeBiometricMapping_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        [HttpGet]
        public IActionResult Import()
        {
            return View(new ExcelImportResult());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile file)
        {
            var result = new ExcelImportResult();

            List<ExcelImportRow<BulkMappingImportRowDto>> rows;

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

            var employees = await _apiService
                .GetAsync<List<EmployeeListDto>>("Employee/employee-list")
                ?? new List<EmployeeListDto>();

            var byCode = employees
                .Where(e => !string.IsNullOrWhiteSpace(e.EmployeeCode))
                .GroupBy(e => e.EmployeeCode.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var existingMappings = await _apiService
                .GetAsync<List<EmployeeBiometricMappingDto>>("EmployeeBiometricMapping")
                ?? new List<EmployeeBiometricMappingDto>();

            var mappingByEmployeeId = existingMappings
                .GroupBy(m => m.EmployeeId)
                .ToDictionary(g => g.Key, g => g.First());

            foreach (var row in rows)
            {
                var identifier = row.Item.EmployeeCode;

                if (row.HasErrors)
                {
                    result.AddRow(row.RowNumber, identifier, false, string.Join(" ", row.ParseErrors));
                    continue;
                }

                if (!byCode.TryGetValue(row.Item.EmployeeCode.Trim(), out var emp))
                {
                    result.AddRow(row.RowNumber, identifier, false, $"Employee code '{row.Item.EmployeeCode}' was not found.");
                    continue;
                }

                try
                {
                    var dto = new EmployeeBiometricMappingDto
                    {
                        EmployeeId = emp.Id,
                        BiometricEmployeeCode = row.Item.BiometricEmployeeCode,
                        CardNumber = row.Item.CardNumber,
                        IsActive = row.Item.IsActive,
                        CreatedBy = _userId,
                        TenantId = _tenantId
                    };

                    if (mappingByEmployeeId.TryGetValue(emp.Id, out var existing))
                    {
                        dto.Id = existing.Id;
                        var response = await _apiService.PutAsync<EmployeeBiometricMappingDto, ApiResponse<object>>(
                            "EmployeeBiometricMapping", dto);

                        result.AddRow(row.RowNumber, identifier, response?.Success ?? false,
                            response?.Success == true ? "Updated successfully." : (response?.Message ?? "Update failed."));
                    }
                    else
                    {
                        var response = await _apiService.PostAsync<EmployeeBiometricMappingDto, ApiResponse<EmployeeBiometricMappingDto>>(
                            "EmployeeBiometricMapping", dto);

                        result.AddRow(row.RowNumber, identifier, response?.Success ?? false,
                            response?.Success == true ? "Created successfully." : (response?.Message ?? "Create failed."));
                    }
                }
                catch (ApiException apiEx)
                {
                    result.AddRow(row.RowNumber, identifier, false, GetErrorMessage(apiEx.ResponseContent));
                }
                catch (Exception ex)
                {
                    result.AddRow(row.RowNumber, identifier, false, GetErrorMessage(ex.Message));
                }
            }

            if (result.TotalRows == 0)
                TempData["GlobalError"] = "The uploaded file didn't contain any mapping rows.";
            else if (result.FailureCount == 0)
                TempData["Success"] = $"All {result.SuccessCount} mapping(s) imported successfully.";
            else
                TempData["Info"] = $"{result.SuccessCount} of {result.TotalRows} mapping(s) imported. {result.FailureCount} failed - see details below.";

            return View(result);
        }

        private async Task<BulkMappingViewModel> BuildBulkViewModelAsync()
        {
            var employees = await _apiService
                .GetAsync<List<EmployeeListDto>>("Employee/employee-list")
                ?? new List<EmployeeListDto>();

            var mappings = await _apiService
                .GetAsync<List<EmployeeBiometricMappingDto>>("EmployeeBiometricMapping")
                ?? new List<EmployeeBiometricMappingDto>();

            var mappingByEmployeeId = mappings
                .GroupBy(m => m.EmployeeId)
                .ToDictionary(g => g.Key, g => g.First());

            var rows = employees
                .Where(e => e.RelievingDate == null)
                .OrderBy(e => e.FirstName)
                .Select(e =>
                {
                    mappingByEmployeeId.TryGetValue(e.Id, out var mapping);

                    return new BulkMappingRowDto
                    {
                        EmployeeId = e.Id,
                        EmployeeCode = e.EmployeeCode,
                        EmployeeName = (e.FirstName + " " + e.LastName).Trim(),
                        Department = e.DepartmentName,
                        Designation = e.DesignationName,
                        MappingId = mapping?.Id,
                        BiometricEmployeeCode = mapping?.BiometricEmployeeCode,
                        CardNumber = mapping?.CardNumber,
                        IsActive = mapping?.IsActive ?? true
                    };
                })
                .ToList();

            List<EsslUnmappedEmployeeDto> unmapped;

            try
            {
                unmapped = await _apiService
                    .GetAsync<List<EsslUnmappedEmployeeDto>>("esslattendance/unmapped-employees")
                    ?? new List<EsslUnmappedEmployeeDto>();
            }
            catch
            {
                // eSSL integration may not be configured for this tenant at
                // all - the bulk grid itself must still load.
                unmapped = new List<EsslUnmappedEmployeeDto>();
            }

            return new BulkMappingViewModel { Rows = rows, Unmapped = unmapped };
        }

        // Applies each row via the existing single-row Create/Update/Delete
        // endpoints (never a new bulk API), so every business rule already
        // enforced there - employee existence, duplicate-code check,
        // tenant/CreatedBy stamping - runs unchanged for every row.
        private async Task<BulkMappingSaveResult> SaveRowsAsync(List<BulkMappingSaveRowDto> rows)
        {
            var result = new BulkMappingSaveResult { TotalRows = rows.Count };

            // Employee display names for the result panel only.
            var employees = await _apiService
                .GetAsync<List<EmployeeListDto>>("Employee/employee-list")
                ?? new List<EmployeeListDto>();
            var employeeById = employees.ToDictionary(e => e.Id, e => e);

            foreach (var row in rows)
            {
                employeeById.TryGetValue(row.EmployeeId, out var emp);
                var code = emp?.EmployeeCode ?? row.EmployeeId;
                var name = emp != null ? (emp.FirstName + " " + emp.LastName).Trim() : "";

                try
                {
                    if (string.IsNullOrWhiteSpace(row.BiometricEmployeeCode))
                    {
                        if (!string.IsNullOrEmpty(row.MappingId))
                        {
                            await _apiService.DeleteAsync($"EmployeeBiometricMapping/{row.MappingId}");
                            result.Results.Add(new BulkMappingRowResult { EmployeeCode = code, EmployeeName = name, Success = true, Message = "Mapping removed." });
                            result.SuccessCount++;
                        }
                        // Blank and never mapped - nothing to do, not an error.
                        continue;
                    }

                    var dto = new EmployeeBiometricMappingDto
                    {
                        Id = row.MappingId,
                        EmployeeId = row.EmployeeId,
                        BiometricEmployeeCode = row.BiometricEmployeeCode.Trim(),
                        CardNumber = row.CardNumber,
                        IsActive = row.IsActive,
                        CreatedBy = _userId,
                        TenantId = _tenantId
                    };

                    if (string.IsNullOrEmpty(row.MappingId))
                    {
                        var response = await _apiService.PostAsync<EmployeeBiometricMappingDto, ApiResponse<EmployeeBiometricMappingDto>>(
                            "EmployeeBiometricMapping", dto);

                        result.Results.Add(new BulkMappingRowResult
                        {
                            EmployeeCode = code,
                            EmployeeName = name,
                            Success = response?.Success ?? false,
                            Message = response?.Success == true ? "Mapped successfully." : (response?.Message ?? "Mapping failed.")
                        });
                        if (response?.Success == true) result.SuccessCount++; else result.FailureCount++;
                    }
                    else
                    {
                        var response = await _apiService.PutAsync<EmployeeBiometricMappingDto, ApiResponse<object>>(
                            "EmployeeBiometricMapping", dto);

                        result.Results.Add(new BulkMappingRowResult
                        {
                            EmployeeCode = code,
                            EmployeeName = name,
                            Success = response?.Success ?? false,
                            Message = response?.Success == true ? "Updated successfully." : (response?.Message ?? "Update failed.")
                        });
                        if (response?.Success == true) result.SuccessCount++; else result.FailureCount++;
                    }
                }
                catch (ApiException apiEx)
                {
                    result.Results.Add(new BulkMappingRowResult { EmployeeCode = code, EmployeeName = name, Success = false, Message = GetErrorMessage(apiEx.ResponseContent) });
                    result.FailureCount++;
                }
                catch (Exception ex)
                {
                    result.Results.Add(new BulkMappingRowResult { EmployeeCode = code, EmployeeName = name, Success = false, Message = GetErrorMessage(ex.Message) });
                    result.FailureCount++;
                }
            }

            return result;
        }

        private static List<ExcelColumn<BulkMappingImportRowDto>> GetImportColumns()
        {
            return new List<ExcelColumn<BulkMappingImportRowDto>>
            {
                new ExcelColumn<BulkMappingImportRowDto>(
                    "Employee Code*",
                    d => d.EmployeeCode,
                    (d, v) =>
                    {
                        if (string.IsNullOrWhiteSpace(v))
                            throw new Exception("Employee Code is required.");
                        d.EmployeeCode = v.Trim();
                    },
                    isRequired: true,
                    sampleValue: "EMP001"),

                new ExcelColumn<BulkMappingImportRowDto>(
                    "Biometric Employee Code*",
                    d => d.BiometricEmployeeCode,
                    (d, v) =>
                    {
                        if (string.IsNullOrWhiteSpace(v))
                            throw new Exception("Biometric Employee Code is required.");
                        d.BiometricEmployeeCode = v.Trim();
                    },
                    isRequired: true,
                    sampleValue: "EMP001"),

                new ExcelColumn<BulkMappingImportRowDto>(
                    "Card Number",
                    d => d.CardNumber,
                    (d, v) => d.CardNumber = string.IsNullOrWhiteSpace(v) ? null : v.Trim(),
                    sampleValue: ""),

                new ExcelColumn<BulkMappingImportRowDto>(
                    "Active (Yes/No)",
                    d => d.IsActive ? "Yes" : "No",
                    (d, v) => d.IsActive = ParseYesNo(v, true),
                    sampleValue: "Yes"),
            };
        }

        private static List<ExcelColumn<BulkMappingRowDto>> GetExportColumns()
        {
            return new List<ExcelColumn<BulkMappingRowDto>>
            {
                new ExcelColumn<BulkMappingRowDto>("Employee Code", d => d.EmployeeCode, (d, v) => { }),
                new ExcelColumn<BulkMappingRowDto>("Employee Name", d => d.EmployeeName, (d, v) => { }),
                new ExcelColumn<BulkMappingRowDto>("Department", d => d.Department, (d, v) => { }),
                new ExcelColumn<BulkMappingRowDto>("Biometric Employee Code", d => d.BiometricEmployeeCode, (d, v) => { }),
                new ExcelColumn<BulkMappingRowDto>("Card Number", d => d.CardNumber, (d, v) => { }),
                new ExcelColumn<BulkMappingRowDto>("Active", d => d.IsActive ? "Yes" : "No", (d, v) => { }),
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

        // Same shape as ApiService.HandleResponse's error body (ApiResponse<T>
        // serialized as JSON, occasionally prefixed with plain text like
        // "Bad Request (400): {...}") - mirrors the GetErrorMessage helper
        // used across the rest of the APP controllers (e.g. EmployeeBankController).
        private static string GetErrorMessage(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return "The ERP API returned an error.";

            try
            {
                var jsonStart = raw.IndexOf('{');
                var json = jsonStart >= 0 ? raw.Substring(jsonStart) : raw;

                var obj = Newtonsoft.Json.Linq.JObject.Parse(json);

                if (obj["Message"] != null)
                    return obj["Message"]!.ToString();

                if (obj["message"] != null)
                    return obj["message"]!.ToString();

                if (obj["Errors"] is Newtonsoft.Json.Linq.JArray errors && errors.Count > 0)
                    return errors[0]?.ToString() ?? "The ERP API returned an error.";

                return raw;
            }
            catch
            {
                return raw;
            }
        }
    }
}
