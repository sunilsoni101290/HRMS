using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json.Linq;

namespace APP.Controllers
{
    #region User Controller

    [JwtAuthorize]
    public class UserController : Controller
    {
        private readonly IApiService _apiService;
        private string _tenantId;
        private string _userId;

        public UserController(IApiService apiService)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _apiService.GetAsync<List<UserListDto>>("user");
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();
            return View(new UserDto());
        }

        [HttpPost]
        public async Task<IActionResult> Create(UserDto dto)
        {
            if (dto != null)
            {
                dto.TenantId = _tenantId;
                dto.CreatedBy = _userId;

                await _apiService.PostAsync<dynamic>("user", dto);

                TempData["Success"] = "User created successfully.";
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdowns(dto.CompanyId);
            return View(dto);
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var data = await _apiService.GetAsync<UserDto>($"user/{id}");
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var data = await _apiService.GetAsync<UserDto>($"user/{id}");
            await LoadDropdowns(data.CompanyId);
            return View("Create", data);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string id, UserDto dto)
        {
            if (dto != null)
            {
                dto.TenantId = _tenantId;
                dto.ModifiedBy = _userId;
                dto.ModifiedOn = DateTime.UtcNow;

                await _apiService.PutAsync<dynamic>($"user/{id}", dto);

                TempData["Success"] = "User updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdowns(dto.CompanyId);
            return View("Create", dto);
        }

        // newPassword comes from the admin-typed field in the Reset
        // Password modal. If left blank, the API falls back to generating
        // a random one server-side. Either way the password is shown here
        // exactly once via TempData, which is cleared after this single
        // redirect renders - it is never persisted or retrievable again.
        [HttpPost]
        public async Task<IActionResult> ResetPassword(string id, string newPassword)
        {
            try
            {
                dynamic result = await _apiService.PutAsync<dynamic>(
                    $"user/reset-password/{id}",
                    new ResetPasswordRequest { UserId = id, NewPassword = newPassword });

                string resultPassword = result?.newPassword ?? result?.NewPassword;

                TempData["Success"] = "Password reset successfully.";
                TempData["NewPassword"] = resultPassword;
            }
            catch (ApiException ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
            }
            catch (ApplicationException ex)
            {
                // ApiService.PutAsync -> HandleResponse throws
                // ApplicationException($"Bad Request (400): {json}") for a
                // 400 response - e.g. the "password must be at least 6
                // characters" validation error. Strip the prefix so
                // GetErrorMessage gets clean JSON to parse.
                const string prefix = "Bad Request (400): ";
                var json = ex.Message.StartsWith(prefix)
                    ? ex.Message.Substring(prefix.Length)
                    : ex.Message;

                TempData["GlobalError"] = GetErrorMessage(json);
            }
            catch (Exception)
            {
                TempData["GlobalError"] = "Failed to reset password.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleActive(string id)
        {
            await _apiService.PutAsync<dynamic>($"user/toggle-active/{id}", new { });
            TempData["Success"] = "User status updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleLock(string id)
        {
            await _apiService.PutAsync<dynamic>($"user/toggle-lock/{id}", new { });
            TempData["Success"] = "User lock updated.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(string id)
        {
            await _apiService.DeleteAsync($"user/{id}");
            TempData["Success"] = "User deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        #region Load Dropdowns

        private async Task LoadDropdowns(string? companyId = null)
        {
            var roles = await _apiService.GetAsync<List<DropdownDto>>("dropdown/role");
            ViewBag.RoleList = new SelectList(roles, "Value", "Text");

            var companies = await _apiService.GetAsync<List<DropdownDto>>("dropdown/company");
            ViewBag.CompanyList = new SelectList(companies, "Value", "Text");

            var employees = await _apiService.GetAsync<List<DropdownDto>>("dropdown/employee");
            ViewBag.EmployeeList = new SelectList(employees, "Value", "Text");

            List<DropdownDto> branches = new();
            if (!string.IsNullOrEmpty(companyId))
                branches = await _apiService.GetAsync<List<DropdownDto>>($"dropdown/branch/{companyId}");

            ViewBag.BranchList = branches.Select(x => new SelectListItem
            {
                Value = x.Value,
                Text = x.Text
            }).ToList();
        }

        #endregion

        [HttpGet]
        public async Task<JsonResult> GetBranchByCompanyId(string companyId)
        {
            var branches = await _apiService.GetAsync<List<DropdownDto>>($"dropdown/branch/{companyId}");
            var result = branches.Select(x => new { value = x.Value, text = x.Text });
            return Json(result);
        }

        // Pulls the "Message" (or first Errors entry) out of the API's
        // error body - e.g. "New password must be at least 6 characters."
        // from ResetPassword - instead of showing the generic
        // "Failed to reset password." for validation failures the admin
        // actually needs to see and correct.
        private string GetErrorMessage(string json)
        {
            try
            {
                var obj = JObject.Parse(json);

                if (obj["Message"] != null)
                    return obj["Message"]!.ToString();

                if (obj["Errors"] is JArray errors && errors.Count > 0)
                    return errors[0]?.ToString();

                return "An error occurred.";
            }
            catch
            {
                return "An error occurred.";
            }
        }
    }

    #endregion
}
