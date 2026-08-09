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
    // Full EmployeeLoan lifecycle UI - see
    // API/Controllers/EmployeeLoanController.cs (api/employeeloan). Named
    // exactly "EmployeeLoan" to match AppFeatureConstants.EMPLOYEE_LOAN_CONTROLLER.
    // Dual-audience: an Employee submits/views their OWN requests
    // (self-service, EmployeeId auto-resolved from the session), while
    // HR/Finance additionally act tenant-wide - the API is the real
    // authority on both, this controller's own checks are cosmetic UX only.
    [JwtAuthorize]
    public class EmployeeLoanController : Controller
    {
        private readonly IApiService _apiService;
        private readonly IExcelEngine _excelEngine;
        private readonly string? _employeeId;
        private readonly string _tenantId;
        private readonly string _userId;
        private readonly bool _isAdmin;

        public EmployeeLoanController(IApiService apiService, IExcelEngine excelEngine)
        {
            _apiService = apiService;
            _excelEngine = excelEngine;
            _employeeId = SessionHelper.GetActiveEmployeeId;
            _isAdmin = SessionHelper.IsAdminRole();
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? status, string? employeeId)
        {
            ViewBag.Status = status;
            ViewBag.StatusList = EnumHelper.GetEnumList<LoanStatus>();

            var url =
                $"employeeloan" +
                $"?status={Uri.EscapeDataString(status ?? string.Empty)}" +
                $"&employeeId={Uri.EscapeDataString(employeeId ?? string.Empty)}" +
                $"&tenantId={Uri.EscapeDataString(_tenantId ?? string.Empty)}" +
                $"&actingUserId={Uri.EscapeDataString(_userId ?? string.Empty)}";

            try
            {
                var data = await _apiService.GetAsync<List<EmployeeLoanListDto>>(url);

                return View(data ?? new List<EmployeeLoanListDto>());
            }
            catch (UnauthorizedAccessException ex) when (ex.Message.StartsWith("Access Denied"))
            {
                TempData["GlobalError"] = "You are not authorized to view Employee Loans.";
                return RedirectToAction("Index", "Dashboard");
            }
        }

        [HttpGet]
        public async Task<IActionResult> PendingOnMe()
        {
            try
            {
                var url =
                    $"employeeloan/pending-on-me" +
                    $"?tenantId={Uri.EscapeDataString(_tenantId ?? string.Empty)}" +
                    $"&actingUserId={Uri.EscapeDataString(_userId ?? string.Empty)}";

                var data = await _apiService.GetAsync<List<EmployeeLoanListDto>>(url)
                           ?? new List<EmployeeLoanListDto>();

                return View(data);
            }
            catch (UnauthorizedAccessException ex) when (ex.Message.StartsWith("Access Denied"))
            {
                TempData["GlobalError"] = "You are not authorized to view pending Employee Loans.";
                return RedirectToAction("Index", "Dashboard");
            }
            catch (Exception)
            {
                TempData["GlobalError"] = "An error occurred while loading pending Employee Loans.";
                return RedirectToAction("Index", "Dashboard");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await BindLoanTypeDropdown();
            await BindEmployeeDropdown();

            return View(new LoanSubmitDto { EmployeeId = _isAdmin ? string.Empty : (_employeeId ?? string.Empty),TenantId=_tenantId,ActingUserId=_userId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LoanSubmitDto model)
        {
            if (!ModelState.IsValid)
            {
                await BindLoanTypeDropdown();
                await BindEmployeeDropdown();
                return View(model);
            }

            try
            {
                var result = await _apiService.PostAsync<LoanSubmitDto, EmployeeLoanDto>("employeeloan/submit", model);

                if (result == null)
                {
                    TempData["GlobalError"] = "Unable to submit the loan request.";
                    return RedirectToAction(nameof(Create));
                }

                TempData["Success"] = "Loan request submitted successfully.";
                return RedirectToAction(nameof(Details), new { id = result.Id });
            }
            catch (ApiException ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
                await BindLoanTypeDropdown();
                await BindEmployeeDropdown();
                return View(model);
            }
        }

        // AJAX - live eligibility preview on the Create form.
        [HttpGet]
        public async Task<IActionResult> CheckEligibility(
        string employeeId,
        string loanTypeId,
        decimal amount,
        int tenureMonths)
        {
            try
            {
                var url =
                    $"employeeloan/eligibility" +
                    $"?employeeId={Uri.EscapeDataString(employeeId ?? string.Empty)}" +
                    $"&loanTypeId={Uri.EscapeDataString(loanTypeId ?? string.Empty)}" +
                    $"&amount={amount}" +
                    $"&tenureMonths={tenureMonths}" +
                    $"&tenantId={Uri.EscapeDataString(_tenantId ?? string.Empty)}" +
                    $"&actingUserId={Uri.EscapeDataString(_userId ?? string.Empty)}";

                var result = await _apiService.GetAsync<LoanEligibilityDto>(url);

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    isEligible = false,
                    reasonIfNotEligible = GetErrorMessage(ex.Message)
                });
            }
        }

        // AJAX - live EMI amortization preview on the Create form.
        [HttpPost]
        public async Task<IActionResult> PreviewEmi([FromBody] EmiPreviewRequestDto dto)
        {
            try
            {
                dto.TenantId = _tenantId;
                dto.ActingUserId = _userId;

                var result = await _apiService.PostAsync<EmiPreviewRequestDto, EmiPreviewResponseDto>("employeeloan/preview-emi", dto);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { error = GetErrorMessage(ex.Message) });
            }
        }

        [HttpGet]
        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            EmployeeLoanDto? data;

            try
            {
                var url =
                    $"employeeloan/{Uri.EscapeDataString(id ?? string.Empty)}" +
                    $"?tenantId={Uri.EscapeDataString(_tenantId ?? string.Empty)}" +
                    $"&actingUserId={Uri.EscapeDataString(_userId ?? string.Empty)}";

                data = await _apiService.GetAsync<EmployeeLoanDto>(url);
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

            if (data == null)
                return NotFound();

            if (data.Status == (int)LoanStatus.Active ||
                data.Status == (int)LoanStatus.PreClosureRequested)
            {
                try
                {
                    var quoteUrl =
                        $"employeeloan/{Uri.EscapeDataString(id ?? string.Empty)}/pre-closure-quote" +
                        $"?tenantId={Uri.EscapeDataString(_tenantId ?? string.Empty)}" +
                        $"&actingUserId={Uri.EscapeDataString(_userId ?? string.Empty)}";

                    ViewBag.PreClosureQuote =
                        await _apiService.GetAsync<LoanPreClosureQuoteDto>(quoteUrl);
                }
                catch
                {
                    // Quote is optional; don't block Details page.
                }
            }

            return View(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(string id, decimal? approvedAmount, string? remarks)
        {
            try
            {
                var result = await _apiService.PutAsync<EmployeeLoanDto>("employeeloan/approve", new LoanApprovalActionDto
                {
                    EmployeeLoanId = id,
                    Decision = 1,
                    ApprovedAmount = approvedAmount,
                    TenantId = _tenantId,
                    ActingUserId=_userId,
                    Remarks = remarks
                });


                TempData[result != null ? "Success" : "GlobalError"] = result != null
                    ? "Loan request approved successfully."
                    : "Unable to approve this loan request.";
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
                var result = await _apiService.PutAsync<EmployeeLoanDto>("employeeloan/reject", new LoanApprovalActionDto
                {
                    EmployeeLoanId = id,
                    Decision = 2,
                    TenantId = _tenantId,
                    ActingUserId = _userId,
                    Remarks = remarks
                });

                TempData[result != null ? "Success" : "GlobalError"] = result != null
                    ? "Loan request rejected."
                    : "Unable to reject this loan request.";
            }
            catch (Exception ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex is ApiException apiEx ? apiEx.ResponseContent : ex.Message);
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Disburse(LoanDisbursementDto model)
        {
            try
            {
                model.TenantId=_tenantId;
                model.ActingUserId=_userId;

                var result = await _apiService.PostAsync<LoanDisbursementDto, EmployeeLoanDto>("employeeloan/disburse", model);

                TempData[result != null ? "Success" : "GlobalError"] = result != null
                    ? "Loan disbursed and EMI schedule generated successfully."
                    : "Unable to disburse this loan.";
            }
            catch (ApiException ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
            }

            return RedirectToAction(nameof(Details), new { id = model.EmployeeLoanId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> RequestPreClosure(string id)
        {
            try
            {
                var url =
                    $"employeeloan/{Uri.EscapeDataString(id ?? string.Empty)}/request-pre-closure" +
                    $"?tenantId={Uri.EscapeDataString(_tenantId ?? string.Empty)}" +
                    $"&actingUserId={Uri.EscapeDataString(_userId ?? string.Empty)}";

                var result = await _apiService.PostAsync<EmployeeLoanDto>(url, new { });

                TempData[result != null ? "Success" : "GlobalError"] =
                    result != null
                        ? "Pre-closure requested successfully."
                        : "Unable to request pre-closure.";
            }
            catch (ApiException ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settle(LoanSettlementDto model)
        {
            try
            {
                model.TenantId= _tenantId;
                model.ActingUserId=_userId;

                var result = await _apiService.PostAsync<LoanSettlementDto, EmployeeLoanDto>("employeeloan/settle", model);

                TempData[result != null ? "Success" : "GlobalError"] = result != null
                    ? "Loan settled and closed successfully."
                    : "Unable to settle this loan.";
            }
            catch (ApiException ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
            }

            return RedirectToAction(nameof(Details), new { id = model.EmployeeLoanId });
        }

        #region Export / Print (Phase 13)

        private static List<ExcelColumn<EmployeeLoanListDto>> GetExportColumns()
        {
            return new List<ExcelColumn<EmployeeLoanListDto>>
            {
                new("Employee", d => d.EmployeeName, (d, v) => { }),
                new("Employee Code", d => d.EmployeeCode, (d, v) => { }),
                new("Loan Type", d => d.LoanTypeName, (d, v) => { }),
                new("Requested Amount", d => d.RequestedAmount, (d, v) => { }),
                new("Approved Amount", d => d.ApprovedAmount, (d, v) => { }),
                new("Outstanding Principal", d => d.OutstandingPrincipal, (d, v) => { }),
                new("Approval Level", d => d.CurrentApprovalLevel, (d, v) => { }),
                new("Status", d => d.StatusName, (d, v) => { }),
                new("Disbursed On", d => d.DisbursedOn?.ToString("dd-MMM-yyyy"), (d, v) => { }),
                new("Submitted On", d => d.CreatedOn.ToString("dd-MMM-yyyy"), (d, v) => { }),
            };
        }

        // GET employeeloan/export?status=&employeeId= - same filters as Index.
        [HttpGet]
        public async Task<IActionResult> Export(string? status, string? employeeId)
        {
            var url = $"employeeloan?status={Uri.EscapeDataString(status ?? string.Empty)}" +
                      $"&employeeId={Uri.EscapeDataString(employeeId ?? string.Empty)}";

            var data = await _apiService.GetAsync<List<EmployeeLoanListDto>>(url) ?? new();
            var bytes = _excelEngine.Export(data, GetExportColumns(), "Employee Loans");

            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"EmployeeLoans_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        }

        // Print-friendly loan sanction letter - "PDF" via the browser's own
        // Print / Save-as-PDF (no server-side PDF library in this codebase -
        // see the Phase 13 note in Views/EmployeeLoan/SanctionLetter.cshtml).
        [HttpGet]
        public async Task<IActionResult> SanctionLetter(string id)
        {
            var data = await _apiService.GetAsync<EmployeeLoanDto>($"employeeloan/{id}");
            if (data == null) return NotFound();

            return View(data);
        }

        // Print-friendly full statement - EMI schedule + payment history.
        [HttpGet]
        public async Task<IActionResult> Statement(string id)
        {
            var data = await _apiService.GetAsync<EmployeeLoanDto>($"employeeloan/{id}");
            if (data == null) return NotFound();

            return View(data);
        }

        #endregion

        private async Task BindLoanTypeDropdown()
        {
            var types = await _apiService.GetAsync<List<LoanTypeDto>>("loantype")
                ?? new List<LoanTypeDto>();

            var options = types.Where(x => x.IsActive).Select(x => new { Value = x.Id, Text = x.Name }).ToList();
            ViewBag.LoanTypeList = new SelectList(options, "Value", "Text");
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

                return "Unable to process this Loan request.";
            }
            catch
            {
                return string.IsNullOrWhiteSpace(raw) ? "Unable to process this Loan request." : raw;
            }
        }
    }
}
