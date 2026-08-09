using APP.Attributes;
using APP.Excel;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using static APP.Helpers.EnumExtensions;

namespace APP.Controllers
{
    // Full EmployeeAdvance lifecycle UI - see
    // API/Controllers/EmployeeAdvanceController.cs (api/employeeadvance).
    // Named exactly "EmployeeAdvance" to match
    // AppFeatureConstants.EMPLOYEE_ADVANCE_CONTROLLER. Mirrors
    // EmployeeLoanController's shape minus EMI/pre-closure (advances don't
    // amortize) - see EmployeeAdvanceService for the single-level approval
    // model (Reporting Manager or Finance override).
    [JwtAuthorize]
    public class EmployeeAdvanceController : Controller
    {
        private readonly IApiService _apiService;
        private readonly IExcelEngine _excelEngine;
        private readonly string? _employeeId;
        private readonly bool _isAdmin;

        public EmployeeAdvanceController(IApiService apiService, IExcelEngine excelEngine)
        {
            _apiService = apiService;
            _excelEngine = excelEngine;
            _employeeId = SessionHelper.GetActiveEmployeeId;
            _isAdmin = SessionHelper.IsAdminRole();
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? status, string? employeeId)
        {
            ViewBag.Status = status;
            ViewBag.StatusList = EnumHelper.GetEnumList<AdvanceStatus>();

            var url = $"employeeadvance?status={Uri.EscapeDataString(status ?? string.Empty)}" +
                      $"&employeeId={Uri.EscapeDataString(employeeId ?? string.Empty)}";

            try
            {
                var data = await _apiService.GetAsync<List<EmployeeAdvanceListDto>>(url);
                return View(data ?? new List<EmployeeAdvanceListDto>());
            }
            catch (UnauthorizedAccessException ex) when (ex.Message.StartsWith("Access Denied"))
            {
                TempData["GlobalError"] = "You are not authorized to view Employee Advances.";
                return RedirectToAction("Index", "Dashboard");
            }
        }

        [HttpGet]
        public async Task<IActionResult> PendingOnMe()
        {
            var data = await _apiService.GetAsync<List<EmployeeAdvanceListDto>>("employeeadvance/pending-on-me")
                ?? new List<EmployeeAdvanceListDto>();

            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await BindAdvanceTypeDropdown();
            await BindEmployeeDropdown();

            return View(new AdvanceSubmitDto { EmployeeId = _isAdmin ? string.Empty : (_employeeId ?? string.Empty) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdvanceSubmitDto model)
        {
            if (!ModelState.IsValid)
            {
                await BindAdvanceTypeDropdown();
                await BindEmployeeDropdown();
                return View(model);
            }

            try
            {
                var result = await _apiService.PostAsync<AdvanceSubmitDto, EmployeeAdvanceDto>("employeeadvance/submit", model);

                if (result == null)
                {
                    TempData["GlobalError"] = "Unable to submit the advance request.";
                    return RedirectToAction(nameof(Create));
                }

                TempData["Success"] = "Advance request submitted successfully.";
                return RedirectToAction(nameof(Details), new { id = result.Id });
            }
            catch (ApiException ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
                await BindAdvanceTypeDropdown();
                await BindEmployeeDropdown();
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            EmployeeAdvanceDto? data;

            try
            {
                data = await _apiService.GetAsync<EmployeeAdvanceDto>($"employeeadvance/{id}");
            }
            catch (UnauthorizedAccessException ex) when (ex.Message.StartsWith("Access Denied"))
            {
                TempData["GlobalError"] = GetErrorMessage(ex.Message);
                return RedirectToAction(nameof(Index));
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }

            if (data == null) return NotFound();
            return View(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(string id, decimal? approvedAmount, string? remarks)
        {
            try
            {
                var result = await _apiService.PutAsync<EmployeeAdvanceDto>("employeeadvance/approve", new AdvanceApprovalActionDto
                {
                    EmployeeAdvanceId = id,
                    Decision = 1,
                    ApprovedAmount = approvedAmount,
                    Remarks = remarks
                });

                TempData[result != null ? "Success" : "GlobalError"] = result != null
                    ? "Advance request approved successfully."
                    : "Unable to approve this advance request.";
            }
            catch (Exception ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex is ApiException apiEx ? apiEx.ResponseContent : ex.Message);
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(string id, string remarks)
        {
            try
            {
                var result = await _apiService.PutAsync<EmployeeAdvanceDto>("employeeadvance/reject", new AdvanceApprovalActionDto
                {
                    EmployeeAdvanceId = id,
                    Decision = 2,
                    Remarks = remarks
                });

                TempData[result != null ? "Success" : "GlobalError"] = result != null
                    ? "Advance request rejected."
                    : "Unable to reject this advance request.";
            }
            catch (Exception ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex is ApiException apiEx ? apiEx.ResponseContent : ex.Message);
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Disburse(AdvanceDisbursementDto model)
        {
            try
            {
                var result = await _apiService.PostAsync<AdvanceDisbursementDto, EmployeeAdvanceDto>("employeeadvance/disburse", model);

                TempData[result != null ? "Success" : "GlobalError"] = result != null
                    ? "Advance disbursed and installment schedule generated successfully."
                    : "Unable to disburse this advance.";
            }
            catch (ApiException ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
            }

            return RedirectToAction(nameof(Details), new { id = model.EmployeeAdvanceId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settle(AdvanceSettlementDto model)
        {
            try
            {
                var result = await _apiService.PostAsync<AdvanceSettlementDto, EmployeeAdvanceDto>("employeeadvance/settle", model);

                TempData[result != null ? "Success" : "GlobalError"] = result != null
                    ? "Advance settled successfully."
                    : "Unable to settle this advance.";
            }
            catch (ApiException ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
            }

            return RedirectToAction(nameof(Details), new { id = model.EmployeeAdvanceId });
        }

        #region Export (Phase 13)

        private static List<ExcelColumn<EmployeeAdvanceListDto>> GetExportColumns()
        {
            return new List<ExcelColumn<EmployeeAdvanceListDto>>
            {
                new("Employee", d => d.EmployeeName, (d, v) => { }),
                new("Employee Code", d => d.EmployeeCode, (d, v) => { }),
                new("Advance Type", d => d.AdvanceTypeName, (d, v) => { }),
                new("Requested Amount", d => d.RequestedAmount, (d, v) => { }),
                new("Approved Amount", d => d.ApprovedAmount, (d, v) => { }),
                new("Outstanding Amount", d => d.OutstandingAmount, (d, v) => { }),
                new("Approval Level", d => d.CurrentApprovalLevel, (d, v) => { }),
                new("Status", d => d.StatusName, (d, v) => { }),
                new("Disbursed On", d => d.DisbursedOn?.ToString("dd-MMM-yyyy"), (d, v) => { }),
                new("Submitted On", d => d.CreatedOn.ToString("dd-MMM-yyyy"), (d, v) => { }),
            };
        }

        [HttpGet]
        public async Task<IActionResult> Export(string? status, string? employeeId)
        {
            var url = $"employeeadvance?status={Uri.EscapeDataString(status ?? string.Empty)}" +
                      $"&employeeId={Uri.EscapeDataString(employeeId ?? string.Empty)}";

            var data = await _apiService.GetAsync<List<EmployeeAdvanceListDto>>(url) ?? new();
            var bytes = _excelEngine.Export(data, GetExportColumns(), "Employee Advances");

            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"EmployeeAdvances_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        }

        #endregion

        private async Task BindAdvanceTypeDropdown()
        {
            var types = await _apiService.GetAsync<List<AdvanceTypeDto>>("advancetype")
                ?? new List<AdvanceTypeDto>();

            var options = types.Where(x => x.IsActive).Select(x => new { Value = x.Id, Text = x.Name }).ToList();
            ViewBag.AdvanceTypeList = new SelectList(options, "Value", "Text");
        }

        private async Task BindEmployeeDropdown()
        {
            if (!_isAdmin) return;

            var employees = await _apiService.GetAsync<List<DropdownDto>>("dropdown/employee")
                ?? new List<DropdownDto>();

            ViewBag.EmployeeList = new SelectList(employees, "Value", "Text");
        }

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

                return "Unable to process this Advance request.";
            }
            catch
            {
                return string.IsNullOrWhiteSpace(raw) ? "Unable to process this Advance request." : raw;
            }
        }
    }
}
