using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace APP.Controllers
{
    #region Role Controller

    // Role Management is System Configurator ONLY (requirement: lock down
    // Role Management same as Error Log / Database Management).
    // [SystemConfiguratorOnly] is the "don't even show the page"
    // convenience gate for the whole controller; the real, data-driven
    // check is still IRoleService.EnsurePermissionAsync on the API side,
    // so actingUserId is appended to every call below.
    [JwtAuthorize]
    [SystemConfiguratorOnly]
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
            var data = await _apiService.GetAsync<List<RoleListDto>>($"role?actingUserId={Uri.EscapeDataString(_userId ?? "")}");
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
                    await _apiService.PostAsync<dynamic>($"role?actingUserId={Uri.EscapeDataString(_userId ?? "")}", dto);
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
            var data = await _apiService.GetAsync<RoleDto>($"role/{id}?actingUserId={Uri.EscapeDataString(_userId ?? "")}");
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

                await _apiService.PutAsync<dynamic>($"role/{id}?actingUserId={Uri.EscapeDataString(_userId ?? "")}", dto);

                TempData["Success"] = "Role updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            return View("Create", dto);
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var data = await _apiService.GetAsync<RoleDetailDto>($"role/{id}/detail?actingUserId={Uri.EscapeDataString(_userId ?? "")}");
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
            await _apiService.PutAsync<dynamic>($"role/toggle-active/{id}?actingUserId={Uri.EscapeDataString(_userId ?? "")}", new { });
            TempData["Success"] = "Role status updated.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _apiService.DeleteAsync($"role/{id}?actingUserId={Uri.EscapeDataString(_userId ?? "")}");
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
