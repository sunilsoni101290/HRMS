using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace APP.Controllers
{
    #region Announcement Controller

    [JwtAuthorize]
    public class AnnouncementController : Controller
    {
        private readonly IApiService _apiService;
        private string _tenantId;
        private string _companyId;
        private string _userId;

        public AnnouncementController(IApiService apiService)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _companyId = SessionHelper.GetActiveCompanyId;
            _userId = SessionHelper.GetActiveUserId;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _apiService.GetAsync<List<AnnouncementListDto>>("announcement");
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();
            return View(new AnnouncementDto());
        }

        [HttpPost]
        public async Task<IActionResult> Create(AnnouncementDto dto)
        {
            if (dto != null)
            {
                dto.TenantId = _tenantId;
                dto.CompanyId = _companyId;
                dto.CreatedBy = _userId;

                await _apiService.PostAsync<dynamic>("announcement", dto);

                TempData["Success"] = "Announcement published successfully.";
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdowns();
            return View(dto);
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var data = await _apiService.GetAsync<AnnouncementDto>($"announcement/{id}");
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var data = await _apiService.GetAsync<AnnouncementDto>($"announcement/{id}");
            await LoadDropdowns();
            return View("Create", data);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string id, AnnouncementDto dto)
        {
            if (dto != null)
            {
                dto.TenantId = _tenantId;
                dto.CompanyId = _companyId;
                dto.ModifiedBy = _userId;
                dto.ModifiedOn = DateTime.UtcNow;

                await _apiService.PutAsync<dynamic>($"announcement/{id}", dto);

                TempData["Success"] = "Announcement updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdowns();
            return View("Create", dto);
        }

        public async Task<IActionResult> Delete(string id)
        {
            await _apiService.DeleteAsync($"announcement/{id}");
            return RedirectToAction(nameof(Index));
        }

        #region Load Dropdowns

        private async Task LoadDropdowns()
        {
            var departments = await _apiService.GetAsync<List<DropdownDto>>("dropdown/department");
            ViewBag.DepartmentList = new SelectList(departments, "Value", "Text");

            var roles = await _apiService.GetAsync<List<DropdownDto>>("dropdown/role");
            ViewBag.RoleList = new SelectList(roles, "Value", "Text");
        }

        #endregion
    }

    #endregion
}
