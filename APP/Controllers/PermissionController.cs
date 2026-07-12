using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace APP.Controllers
{
    #region Permission Controller

    [JwtAuthorize]
    public class PermissionController : Controller
    {
        private readonly IApiService _apiService;
        private string _userId;

        // Fixed catalogs - these mirror Domain.Helper.Modules / Actions.
        // APP doesn't reference the Domain project directly (HTTP-only
        // boundary to the API), so the small fixed lists are kept in sync
        // here by hand.
        private static readonly string[] ModuleOptions = { "HRMS", "Billing", "Inventory", "CRM", "Admin" };
        private static readonly string[] ActionOptions =
        {
            "Create", "View", "Edit", "Delete", "Approve", "Reject", "Submit", "Cancel", "Export", "Import"
        };

        public PermissionController(IApiService apiService)
        {
            _apiService = apiService;
            _userId = SessionHelper.GetActiveUserId;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _apiService.GetAsync<List<PermissionListDto>>("permission");
            return View(data ?? new List<PermissionListDto>());
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();
            return View(new PermissionDto());
        }

        [HttpPost]
        public async Task<IActionResult> Create(PermissionDto dto)
        {
            if (dto != null)
            {
                dto.CreatedBy = _userId;

                try
                {
                    await _apiService.PostAsync<dynamic>("permission", dto);
                    TempData["Success"] = "Permission created successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception)
                {
                    TempData["GlobalError"] = "A permission with this code already exists, or the request was invalid.";
                }
            }

            await LoadDropdowns();
            return View(dto);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var data = await _apiService.GetAsync<PermissionDto>($"permission/{id}");
            if (data == null) return NotFound();

            await LoadDropdowns();
            return View("Create", data);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string id, PermissionDto dto)
        {
            if (dto != null)
            {
                dto.ModifiedBy = _userId;
                dto.ModifiedOn = DateTime.UtcNow;

                await _apiService.PutAsync<dynamic>($"permission/{id}", dto);

                TempData["Success"] = "Permission updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdowns();
            return View("Create", dto);
        }

        public async Task<IActionResult> Delete(string id)
        {
            await _apiService.DeleteAsync($"permission/{id}");
            TempData["Success"] = "Permission deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        #region Load Dropdowns

        private async Task LoadDropdowns()
        {
            ViewBag.ModuleList = new SelectList(ModuleOptions);
            ViewBag.ActionList = new SelectList(ActionOptions);

            var features = await _apiService.GetAsync<List<DropdownDto>>("dropdown/appfeature");
            ViewBag.FeatureList = new SelectList(features ?? new List<DropdownDto>(), "Value", "Text");
        }

        #endregion
    }

    #endregion
}
