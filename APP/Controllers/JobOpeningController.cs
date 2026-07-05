using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace APP.Controllers
{
    #region Job Opening Controller

    [JwtAuthorize]
    public class JobOpeningController : Controller
    {
        private readonly IApiService _apiService;
        private string _tenantId;
        private string _userId;

        public JobOpeningController(IApiService apiService)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _apiService.GetAsync<List<JobOpeningListDto>>("job-opening");
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();
            return View(new JobOpeningDto());
        }

        [HttpPost]
        public async Task<IActionResult> Create(JobOpeningDto dto)
        {
            if (dto != null)
            {
                dto.TenantId = _tenantId;
                dto.CreatedBy = _userId;

                await _apiService.PostAsync<dynamic>("job-opening", dto);

                TempData["Success"] = "Job opening saved successfully.";
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdowns(dto.DepartmentId);
            return View(dto);
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var data = await _apiService.GetAsync<JobOpeningDto>($"job-opening/{id}");
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var data = await _apiService.GetAsync<JobOpeningDto>($"job-opening/{id}");
            await LoadDropdowns(data.DepartmentId);
            return View("Create", data);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string id, JobOpeningDto dto)
        {
            if (dto != null)
            {
                dto.TenantId = _tenantId;
                dto.ModifiedBy = _userId;
                dto.ModifiedOn = DateTime.UtcNow;

                await _apiService.PutAsync<dynamic>($"job-opening/{id}", dto);

                TempData["Success"] = "Job opening updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdowns(dto.DepartmentId);
            return View("Create", dto);
        }

        public async Task<IActionResult> Delete(string id)
        {
            await _apiService.DeleteAsync($"job-opening/{id}");
            return RedirectToAction(nameof(Index));
        }

        #region Load Dropdowns

        private async Task LoadDropdowns(string? departmentId = null)
        {
            var departments = await _apiService.GetAsync<List<DropdownDto>>("dropdown/department");
            ViewBag.DepartmentList = new SelectList(departments, "Value", "Text");

            List<DropdownDto> designations = new();
            if (!string.IsNullOrEmpty(departmentId))
            {
                designations = await _apiService
                    .GetAsync<List<DropdownDto>>($"dropdown/designation/{departmentId}");
            }
            ViewBag.DesignationList = new SelectList(designations, "Value", "Text");
        }

        #endregion

        [HttpGet]
        public async Task<JsonResult> GetDesignationByDepartment(string departmentId)
        {
            var data = await _apiService
                .GetAsync<List<DropdownDto>>($"dropdown/designation/{departmentId}");
            var result = data.Select(x => new { value = x.Value, text = x.Text });
            return Json(result);
        }
    }

    #endregion
}
