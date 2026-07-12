using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace APP.Controllers
{
    #region Role Controller

    [JwtAuthorize]
    public class RoleController : Controller
    {
        private readonly IApiService _apiService;
        private string _tenantId;
        private string _userId;

        public RoleController(IApiService apiService)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _apiService.GetAsync<List<RoleListDto>>("role");
            return View(data ?? new List<RoleListDto>());
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new RoleDto());
        }

        [HttpPost]
        public async Task<IActionResult> Create(RoleDto dto)
        {
            if (dto != null)
            {
                dto.TenantId = _tenantId;
                dto.CreatedBy = _userId;

                try
                {
                    await _apiService.PostAsync<dynamic>("role", dto);
                    TempData["Success"] = "Role created successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception)
                {
                    TempData["GlobalError"] = "A role with this name already exists, or the request was invalid.";
                }
            }

            return View(dto);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var data = await _apiService.GetAsync<RoleDto>($"role/{id}");
            if (data == null) return NotFound();
            return View("Create", data);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string id, RoleDto dto)
        {
            if (dto != null)
            {
                dto.ModifiedBy = _userId;
                dto.ModifiedOn = DateTime.UtcNow;

                await _apiService.PutAsync<dynamic>($"role/{id}", dto);

                TempData["Success"] = "Role updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            return View("Create", dto);
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var data = await _apiService.GetAsync<RoleDetailDto>($"role/{id}/detail");
            if (data == null) return NotFound();
            return View(data);
        }

        [HttpPost]
        public async Task<IActionResult> AssignPermissions(string roleId, List<string> permissionIds)
        {
            var request = new AssignRolePermissionsRequestDto
            {
                RoleId = roleId,
                PermissionIds = permissionIds ?? new List<string>(),
                ModifiedBy = _userId
            };

            await _apiService.PostAsync<dynamic>("role/assign-permissions", request);

            TempData["Success"] = "Permissions updated successfully.";
            return RedirectToAction(nameof(Details), new { id = roleId });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleActive(string id)
        {
            await _apiService.PutAsync<dynamic>($"role/toggle-active/{id}", new { });
            TempData["Success"] = "Role status updated.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _apiService.DeleteAsync($"role/{id}");
                TempData["Success"] = "Role deleted successfully.";
            }
            catch (Exception)
            {
                TempData["GlobalError"] = "This role still has users assigned to it - reassign them first.";
            }

            return RedirectToAction(nameof(Index));
        }
    }

    #endregion
}
