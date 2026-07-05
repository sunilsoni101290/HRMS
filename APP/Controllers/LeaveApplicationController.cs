using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace APP.Controllers
{
    [JwtAuthorize]
    public class LeaveApplicationController : Controller
    {
        private readonly IApiService _apiService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private string _tenantId;
        private string _userId;
        private string _companyId;
        private string _branchId;
        public LeaveApplicationController(IApiService apiService, IHttpContextAccessor httpContextAccessor)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
            _companyId = SessionHelper.GetActiveCompanyId;
            _branchId = SessionHelper.GetActiveBranchId;
            _httpContextAccessor = httpContextAccessor;
        }


        #region CRUD

        public async Task<IActionResult> Index()
        {
            await LoadDropdowns();
            var data = await _apiService.GetAsync<List<LeaveApplicationDto>>("LeaveApplication");

            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();

            return View(new ApplyLeaveRequestDto
            {
                FromDate = DateTime.Today,
                ToDate = DateTime.Today
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            ApplyLeaveRequestDto model)
        {
            if (model==null)
            {
                await LoadDropdowns();
                return View(model);
            }

            model.TenantId = _tenantId;
            model.CreatedBy = _userId;
            model.CompanyId = _companyId;
            model.BranchId = _branchId;

            #region Upload Image
            // Upload Folder
            string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/LeaveDocument");

            // Create Folder if not exists
            if (!Directory.Exists(uploadFolder))
                Directory.CreateDirectory(uploadFolder);

            // Upload Image
            if (model.UploadDocument != null && model.UploadDocument.Length > 0)
            {
                
                // Get File Extension
                var extension = Path.GetExtension(model.UploadDocument.FileName).ToLower();

               
                // Generate Unique File Name
                string fileName = DateTime.Now.Hour + DateTime.Now.Minute + DateTime.Now.Second + DateTime.Now.Millisecond + extension;

                // Full File Path
                string filePath = Path.Combine(uploadFolder, fileName);

                // Save File
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.UploadDocument.CopyToAsync(stream);
                }

                // Save Relative Path in DB
                model.DocumentUrl = "/LeaveDocument/" + fileName;
            }

            #endregion

            var result = await _apiService.PostAsync<ApplyLeaveRequestDto,LeaveApplicationDto>("LeaveApplication",model);

            TempData["Success"] = "Leave applied successfully.";
            
            await LoadDropdowns();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var data =
                await _apiService.GetAsync<LeaveApplicationDto>(
                    $"LeaveApplication/{id}");

            await LoadDropdowns();

            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var data =
                await _apiService.GetAsync<ApplyLeaveRequestDto>(
                    $"LeaveApplication/{id}");

            await LoadDropdowns();

            return View(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            string id,
            ApplyLeaveRequestDto model)
        {
            if (model==null)
            {
                await LoadDropdowns();
                return View(model);
            }

            model.TenantId = _tenantId;
            model.CreatedBy = _userId;
            model.ModifiedBy = _tenantId;
            model.ModifiedOn = DateTime.Now;
            model.CompanyId = _companyId;
            model.BranchId = _branchId;

            var result =
                await _apiService.PutAsync<
                    ApplyLeaveRequestDto,
                    bool>(
                    $"LeaveApplication/{id}",
                    model);

            if (result)
            {
                TempData["Success"] = "Leave updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            TempData["Error"] = "Unable to update leave.";

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var result =
                await _apiService.DeleteAsync(
                    $"LeaveApplication/{id}");

            return Json(new
            {
                success = result
            });
        }

        #endregion

        #region Workflow

        [HttpPost]
        public async Task<IActionResult> Approve(
            ApproveLeaveRequestDto model)
        {
            if (model == null)
            {
                return RedirectToAction(nameof(Index));
            }

            model.CreatedBy = _userId;
            model.ApprovedBy = _userId;
            model.TenantId = _tenantId;

            var result =
                await _apiService.PostAsync<
                    ApproveLeaveRequestDto,
                    bool>(
                    "LeaveApplication/approve",
                    model);

            if (result)
            {
                TempData["Success"] = "Leave application approved successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Reject(
            RejectLeaveRequestDto model)
        {
            model.RejectedBy = _userId;
            model.TenantId = _tenantId;

            var result =
                await _apiService.PostAsync<
                    RejectLeaveRequestDto,
                    bool>(
                    "LeaveApplication/reject",
                    model);

            if (result)
            {
                TempData["Error"] = "Leave application rejected successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Cancel(
            CancelLeaveRequestDto model)
        {
            model.CancelledBy = _userId;
            model.TenantId = _tenantId;

            var result =
                await _apiService.PostAsync<
                    CancelLeaveRequestDto,
                    bool>(
                    "LeaveApplication/cancel",
                    model);

            return Json(new
            {
                success = result
            });
        }

        #endregion

        #region Queries

        [HttpGet]
        public async Task<IActionResult> Pending()
        {
            var data =
                await _apiService.GetAsync<List<LeaveApplicationDto>>(
                    "LeaveApplication/pending");

            return View("Index", data);
        }

        [HttpGet]
        public async Task<IActionResult> Approved()
        {
            var data =
                await _apiService.GetAsync<List<LeaveApplicationDto>>(
                    "LeaveApplication/approved");

            return View("Index", data);
        }

        [HttpGet]
        public async Task<IActionResult> Rejected()
        {
            var data =
                await _apiService.GetAsync<List<LeaveApplicationDto>>(
                    "LeaveApplication/rejected");

            return View("Index", data);
        }

        [HttpGet]
        public async Task<IActionResult> Cancelled()
        {
            var data =
                await _apiService.GetAsync<List<LeaveApplicationDto>>(
                    "LeaveApplication/cancelled");

            return View("Index", data);
        }

        [HttpGet]
        public async Task<IActionResult> EmployeeLeaves(
            string employeeId)
        {
            var data =
                await _apiService.GetAsync<List<LeaveApplicationDto>>(
                    $"LeaveApplication/employee/{employeeId}");

            return View("Index", data);
        }

        [HttpPost]
        public async Task<IActionResult> Filter(
            LeaveApplicationFilterRequestDto model)
        {
            var data =
                await _apiService.PostAsync<
                    LeaveApplicationFilterRequestDto,
                    List<LeaveApplicationDto>>(
                    "LeaveApplication/filter",
                    model);

            return PartialView("_LeaveList", data);
        }

        #endregion

        #region Dashboard

        [HttpGet]
        public async Task<JsonResult> GetCounts()
        {
            var pending =
                await _apiService.GetAsync<int>(
                    "LeaveApplication/count/pending");

            var approved =
                await _apiService.GetAsync<int>(
                    "LeaveApplication/count/approved");

            var today =
                await _apiService.GetAsync<int>(
                    "LeaveApplication/count/today");

            return Json(new
            {
                Pending = pending,
                Approved = approved,
                Today = today
            });
        }

        #endregion

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
