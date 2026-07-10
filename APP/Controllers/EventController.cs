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
        private string _companyId;

        // Creating/editing/deleting events is an Admin/HR function only -
        // employees can browse them but not manage them, enforced
        // server-side here, not just by hiding buttons in the view.
        private readonly bool _isAdmin;

        public EventController(IApiService apiService)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
            _companyId = SessionHelper.GetActiveCompanyId;
            _isAdmin = SessionHelper.IsAdminRole();
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.IsAdmin = _isAdmin;
            var data = await _apiService.GetAsync<List<EventListDto>>("event");
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (!_isAdmin)
            {
                TempData["GlobalError"] = "You don't have permission to create events.";
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdowns();
            return View(new EventDto());
        }

        [HttpPost]
        public async Task<IActionResult> Create(EventDto dto)
        {
            if (!_isAdmin)
            {
                TempData["GlobalError"] = "You don't have permission to create events.";
                return RedirectToAction(nameof(Index));
            }

            if (dto != null)
            {
                dto.TenantId = _tenantId;
                dto.CreatedBy = _userId;
                dto.CompanyId = _companyId;

                var result = await _apiService.PostAsync<dynamic>("event", dto);

                if (string.IsNullOrEmpty(result))
                {
                    TempData["Error"] = "Failed to create Event.";
                }
                else
                {
                    TempData["Success"] = "Event created successfully.";
                }
                
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdowns();
            return View(dto);
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            ViewBag.IsAdmin = _isAdmin;
            var data = await _apiService.GetAsync<EventDto>($"event/{id}");
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (!_isAdmin)
            {
                TempData["GlobalError"] = "You don't have permission to edit events.";
                return RedirectToAction(nameof(Index));
            }

            var data = await _apiService.GetAsync<EventDto>($"event/{id}");
            await LoadDropdowns();
            return View("Create", data);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string id, EventDto dto)
        {
            if (!_isAdmin)
            {
                TempData["GlobalError"] = "You don't have permission to edit events.";
                return RedirectToAction(nameof(Index));
            }

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
            if (!_isAdmin)
            {
                TempData["GlobalError"] = "You don't have permission to delete events.";
                return RedirectToAction(nameof(Index));
            }

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
