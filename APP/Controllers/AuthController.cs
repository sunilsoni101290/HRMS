using APP.Helpers;
using APP.Models.Auth;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Runtime.InteropServices;

namespace APP.Controllers
{
    public class AuthController : Controller
    {
        private readonly IApiService _apiService;
        private string _tenantId;
        private string _userId;

        public AuthController(IApiService apiService)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;

        }

        public async Task<IActionResult> Index()
        {
            var data = await _apiService
                .GetAsync<List<UserListDto>>("auth/user-list");

            await LoadDropdowns();

            return View(data);
        }

        // =========================
        // LOGIN PAGE
        // =========================
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // =========================
        // LOGIN
        // =========================
        [HttpPost]
        public async Task<IActionResult> Login(LoginDto model)
        {
            try
            {
                var response = await _apiService
                    .PostAsync<AuthResponse>(
                        "auth/login",
                        model);

                if (response != null)
                {
                    HttpContext.Session.SetString(
                        "AccessToken",
                        response.AccessToken);

                    HttpContext.Session.SetString(
                        "RefreshToken",
                        response.RefreshToken);

                    HttpContext.Session.SetString(
                        "FullName",
                        response.FullName ?? "");

                    HttpContext.Session.SetString(
                        "UserId",
                        response.UserId);

                    HttpContext.Session.SetString(
                        "TenantId",
                        response.TenantId);

                    HttpContext.Session.SetString(
                        "Designation",
                        response.Designation ?? ""
                    );

                    HttpContext.Session.SetString(
                        "RoleName",
                        response.RoleName ?? ""
                    );
                    return RedirectToAction("Index","Dashboard");
                }

                ViewBag.Error = "Invalid Username or Password";

                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;

                return View(model);
            }
        }

        // =========================
        // LOGOUT
        // =========================
        public async Task<IActionResult> Logout()
        {
            try
            {
                // Get refresh token from session
                var refreshToken = HttpContext.Session.GetString("RefreshToken");

                // Check null
                if (string.IsNullOrEmpty(refreshToken))
                {
                    // Clear session anyway
                    HttpContext.Session.Clear();

                    return RedirectToAction("Login");
                }

                // Call API
                await _apiService.PostAsync<object>("api/auth/logout", refreshToken);

                // Clear session
                HttpContext.Session.Clear();

                return RedirectToAction("Login");
            }
            catch
            {
                HttpContext.Session.Clear();

                return RedirectToAction("Login");
            }
        }

        public async Task<IActionResult> Register()
        {
            await LoadDropdowns(); 
            return View(new RegisterDto());
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            if (!ModelState.IsValid)
            {
                dto.TenantId = _tenantId;
                dto.CreatedBy = _userId;
                await LoadDropdowns();
                await _apiService.PostAsync<dynamic>($"auth/register", dto);

                TempData["Success"] = "Record saved successfully.";

                return View("Register", dto);
            }

            return RedirectToAction(nameof(Index));
        }

        #region Details

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var data = await _apiService.GetAsync<UserListDto>($"auth/get-user-details/{id}");

            if (data == null)
                return NotFound();

            return View(data);
        }

        #endregion

        #region Load Dropdowns

        private async Task LoadDropdowns()
        {
            // Role
            var roles = await _apiService
                .GetAsync<List<DropdownDto>>($"dropdown/role");

            ViewBag.RoleList = new SelectList(
                roles,
                "Value",
                "Text");

            ViewBag.RoleNames = roles.ToDictionary(x => x.Value, x => x.Text);

            // Company
            var companies = await _apiService
                .GetAsync<List<DropdownDto>>($"dropdown/company");

            ViewBag.CompanyList = new SelectList(
                companies,
                "Value",
                "Text");

            ViewBag.CompanyNames = companies.ToDictionary(x => x.Value, x => x.Text);

            // Employee
            var employees = await _apiService
                .GetAsync<List<DropdownDto>>($"dropdown/employee");

            ViewBag.EmployeeList = new SelectList(
                employees,
                "Value",
                "Text");

            ViewBag.EmployeeNames = employees.ToDictionary(x => x.Value, x => x.Text);

           
            // Branch
            var branches = await _apiService
                .GetAsync<List<DropdownDto>>($"dropdown/branch");

            ViewBag.BranchList = new SelectList(
                branches,
                "Value",
                "Text");

            ViewBag.BranchNames = branches.ToDictionary(x => x.Value, x => x.Text);
        }

        #endregion
    }
}
