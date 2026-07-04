using APP.Attributes;
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
        private string _tenantId;
        private string _userId;
        public LeaveTypeController(IApiService apiService)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
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
    }
 }
