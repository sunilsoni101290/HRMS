using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace APP.Controllers
{
    [JwtAuthorize]
    public class LeaveBalanceController : Controller
    {
        private readonly IApiService _apiService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private string _tenantId;
        private string _userId;
        public LeaveBalanceController(IApiService apiService, IHttpContextAccessor httpContextAccessor)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
            _httpContextAccessor = httpContextAccessor;
        }

        #region List

        public async Task<IActionResult> Index()
        {
            var data = await _apiService.GetAsync<List<LeaveBalanceDto>>("LeaveBalance");

            return View(data);
        }

        #endregion

        #region Create

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();  
            return View(new LeaveBalanceDto
            {
                Year = DateTime.Now.Year
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LeaveBalanceDto model)
        {
            if (model==null)
                return View(model);

            model.TenantId = _tenantId;
            model.CreatedBy = _userId;

            var result =
                await _apiService.PostAsync<LeaveBalanceDto, LeaveBalanceDto>(
                    "LeaveBalance",
                    model);

            TempData["SuccessMessage"] =
                "Leave balance saved successfully.";

            await LoadDropdowns();
            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Edit

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var model =
                await _apiService.GetAsync<LeaveBalanceDto>(
                    $"LeaveBalance/{id}");

            await LoadDropdowns();

            if (model == null)
                return NotFound();

            return View("Create",model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id,LeaveBalanceDto model)
        {
            if (model==null)
                return View(model);

            model.TenantId = _tenantId;
            model.CreatedBy = _userId;
            model.ModifiedBy = _tenantId;
            model.ModifiedOn = DateTime.Now;

            await _apiService.PutAsync<LeaveBalanceDto, LeaveBalanceDto>($"LeaveBalance/{id}",model);

            TempData["SuccessMessage"] =
                "Leave balance updated successfully.";

            await LoadDropdowns();

            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Details

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var model =
                await _apiService.GetAsync<LeaveBalanceDto>(
                    $"LeaveBalance/{id}");

            if (model == null)
                return NotFound();

            return View(model);
        }

        #endregion

        #region Delete

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            await _apiService.DeleteAsync(
                $"LeaveBalance/{id}");

            return Json(new
            {
                success = true,
                message = "Deleted successfully."
            });
        }

        #endregion

        #region Employee Balance

        [HttpGet]
        public async Task<IActionResult> EmployeeBalance(string employeeId,string leaveTypeId)
        {
            var request = new EmployeeTransactionRequestDto
            {
                EmployeeId = employeeId,
                LeaveTypeId = leaveTypeId,
                Year = DateTime.Now.Year
            };

            var balance = await _apiService.GetAsync<
                EmployeeTransactionRequestDto,
                LeaveBalanceDto>(
                "LeaveBalance/employee-balance",
                request);

            return Json(new
            {
                success = true,
                balance = balance?.Balance ?? 0
            });
        }

        #endregion

        #region Allocate Leave

        [HttpPost]
        public async Task<IActionResult> AllocateLeave(
            string employeeId,
            int year)
        {
            var request = new AllocateLeaveRequestDto
            {
                EmployeeId = employeeId,
                Year = year
            };

            var result = await _apiService.PostAsync<AllocateLeaveRequestDto, bool>(
                "LeaveBalance/allocate",
                request);

            return Json(result);
        }

        #endregion

        #region Credit Leave        
        [HttpPost]
        public async Task<IActionResult> CreditLeave(
            string employeeId,
            string leaveTypeId,
            decimal days)
        {
            var request = new LeaveAdjustmentRequestDto
            {
                EmployeeId = employeeId,
                LeaveTypeId = leaveTypeId,
                Days = days
            };

            var result = await _apiService.PostAsync<LeaveAdjustmentRequestDto, bool>(
                "LeaveBalance/credit",
                request);

            return Json(result);
        }

        #endregion

        #region Deduct Leave

        [HttpPost]
        public async Task<IActionResult> DeductLeave(
            string employeeId,
            string leaveTypeId,
            decimal days)
        {
            var request = new LeaveAdjustmentRequestDto
            {
                EmployeeId = employeeId,
                LeaveTypeId = leaveTypeId,
                Days = days
            };

            var result = await _apiService.PostAsync<LeaveAdjustmentRequestDto, bool>(
                "LeaveBalance/deduct",
                request);

            return Json(result);
        }

        #endregion

        #region Carry Forward

        [HttpPost]
        public async Task<IActionResult> CarryForward(
            string employeeId,
            int fromYear,
            int toYear)
        {
            var request = new CarryForwardLeaveRequestDto
            {
                EmployeeId = employeeId,
                FromYear = fromYear,
                ToYear = toYear
            };

            var result = await _apiService.PostAsync<CarryForwardLeaveRequestDto, bool>(
                "LeaveBalance/carry-forward",
                request);

            return Json(result);
        }

        #endregion

        #region Transactions

        [HttpGet]
        public async Task<IActionResult> Transactions(
            string employeeId,
            string leaveTypeId,
            int year)
        {
            var request = new LeaveTransactionFilterRequestDto
            {
                EmployeeId = employeeId,
                LeaveTypeId = leaveTypeId,
                Year = year
            };

            var data =
                await _apiService.PostAsync<LeaveTransactionFilterRequestDto,List<LeaveBalanceTransactionDto>>("LeaveBalance/transactions",request);

            return View(data);
        }

       
        [HttpGet]
        public async Task<IActionResult> EmployeeTransactions(string employeeId,string leaveTypeId)
        {
            var request = new EmployeeTransactionRequestDto
            {
                EmployeeId = employeeId,
                LeaveTypeId= leaveTypeId
            };

            var data =
                await _apiService.PostAsync<
                    EmployeeTransactionRequestDto,
                    List<LeaveBalanceTransactionDto>>(
                        "LeaveBalance/transactions/employee",
                        request);

            return View("Ledger", data);
        }

        [HttpGet]
        public async Task<IActionResult> TransactionsByDateRange(
            DateTime fromDate,
            DateTime toDate)
        {
            var request = new TransactionDateRangeRequestDto
            {
                FromDate = fromDate,
                ToDate = toDate
            };

            var data =
                await _apiService.PostAsync<
                    TransactionDateRangeRequestDto,
                    List<LeaveBalanceTransactionDto>>(
                        "LeaveBalance/transactions/date-range",
                        request);

            return View("Transactions", data);
        }

        #endregion

        [HttpGet]
        public async Task<IActionResult> GetEmployees()
        {
            var employees = await _apiService
                .GetAsync<List<DropdownDto>>($"dropdown/employee");

            return Json(employees.Select(x => new
            {
                x.Value,
                x.Text
            }));
        }

        [HttpGet]
        public async Task<IActionResult> GetLeaveTypes()
        {
            var leaveTypes = await _apiService
                .GetAsync<List<DropdownDto>>($"dropdown/leave-type");

            return Json(leaveTypes.Select(x => new
            {
                x.Text,
                x.Value
            }));
        }

        #region Dropdowns

        private async Task LoadDropdowns()
        {
            // Employee
            var employees = await _apiService
                .GetAsync<List<DropdownDto>>($"dropdown/employee");

            ViewBag.EmployeeList = new SelectList(
                employees,
                "Value",
                "Text");

            ViewBag.EmployeeNames = employees.ToDictionary(x => x.Value, x => x.Text);

            // Leave Type
            var leaveTypes = await _apiService
                .GetAsync<List<DropdownDto>>($"dropdown/leave-type");

            ViewBag.LeaveTypeList = new SelectList(
                leaveTypes,
                "Value",
                "Text");

            ViewBag.LeaveTypeNames = leaveTypes.ToDictionary(x => x.Value, x => x.Text);
        }

        #endregion
    }
}
