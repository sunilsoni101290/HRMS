using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace APP.Controllers
{
    #region Event Controller

    [JwtAuthorize]
    public class EventController : Controller
    {
        private readonly IApiService _apiService;
        private string _tenantId;
        private string _userId;

        public EventController(IApiService apiService)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _apiService.GetAsync<List<EventListDto>>("event");
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();
            return View(new EventDto());
        }

        [HttpPost]
        public async Task<IActionResult> Create(EventDto dto)
        {
            if (dto != null)
            {
                dto.TenantId = _tenantId;
                dto.CreatedBy = _userId;

                await _apiService.PostAsync<dynamic>("event", dto);

                TempData["Success"] = "Event created successfully.";
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdowns();
            return View(dto);
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var data = await _apiService.GetAsync<EventDto>($"event/{id}");
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var data = await _apiService.GetAsync<EventDto>($"event/{id}");
            await LoadDropdowns();
            return View("Create", data);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string id, EventDto dto)
        {
            if (dto != null)
            {
                dto.TenantId = _tenantId;
                dto.ModifiedBy = _userId;
                dto.ModifiedOn = DateTime.UtcNow;

                await _apiService.PutAsync<dynamic>($"event/{id}", dto);

                TempData["Success"] = "Event updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdowns();
            return View("Create", dto);
        }

        public async Task<IActionResult> Delete(string id)
        {
            await _apiService.DeleteAsync($"event/{id}");
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
