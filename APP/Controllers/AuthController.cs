using APP.Attributes;
using APP.Helpers;
using APP.Models.Auth;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using System.Net;
using System.Net.Sockets;
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
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                string localIP = Dns.GetHostEntry(Dns.GetHostName())
                    .AddressList
                    .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)?
                    .ToString();

                model.IpAddress = localIP;

                var response = await _apiService
                    .PostAsync<LoginDto, ApiResponse<AuthResponse>>(
                        "auth/login",
                        model);

                if (response != null && response.Success)
                {
                    HttpContext.Session.SetString("AccessToken", response.Data.AccessToken);
                    HttpContext.Session.SetString("RefreshToken", response.Data.RefreshToken);
                    HttpContext.Session.SetString("FullName", response.Data.FullName ?? "");
                    HttpContext.Session.SetString("UserId", response.Data.UserId);
                    HttpContext.Session.SetString("EmployeeId", response.Data.EmployeeId ?? "");
                    HttpContext.Session.SetString("TenantId", response.Data.TenantId);
                    HttpContext.Session.SetString("Designation", response.Data.Designation ?? "");
                    HttpContext.Session.SetString("CompanyName", response.Data.CompanyName ?? "");
                    HttpContext.Session.SetString("CompanyId", response.Data.CompanyId ?? "");
                    HttpContext.Session.SetString("BranchId", response.Data.BranchId ?? "");
                    HttpContext.Session.SetString("RoleName", response.Data.RoleName ?? "");

                    // Role-based landing page
                    return SessionHelper.IsAdminRole(response.Data.RoleName)
                        ? RedirectToAction("Index", "Dashboard")
                        : RedirectToAction("Index", "EmployeeDashboard");
                }

                TempData["GlobalError"] = response?.Message ?? "Login failed.";
            }
            catch (ApiException ex)
            {
                var errorMessage = GetErrorMessage(ex.ResponseContent);

                TempData["GlobalError"] = errorMessage;
            }
            catch (Exception ex)
            {
                TempData["GlobalError"] = ex.Message;
            }

            return View(model);
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

        #region Change Password

        // Available to every logged-in user - admin, HR, or self-service
        // employee alike - to change their own password. The API endpoint
        // always resolves "whose password" from the caller's own session
        // token, never from anything posted here.

        [JwtAuthorize]
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordDto());
        }

        [JwtAuthorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var response = await _apiService
                    .PostAsync<ChangePasswordDto, ApiResponse<object>>(
                        "auth/change-password",
                        model);

                if (response != null && response.Success)
                {
                    TempData["Success"] = response.Message ?? "Password changed successfully.";
                    return RedirectToAction(nameof(ChangePassword));
                }

                ModelState.AddModelError("", response?.Message ?? "Unable to change password.");
            }
            catch (ApiException ex)
            {
                ModelState.AddModelError("", GetErrorMessage(ex.ResponseContent));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
            }

            return View(model);
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

        private string GetErrorMessage(string json)
        {
            try
            {
                var obj = Newtonsoft.Json.Linq.JObject.Parse(json);

                // Custom API Response
                if (obj["Message"] != null)
                    return obj["Message"]!.ToString();

                // Errors Array
                if (obj["Errors"] is Newtonsoft.Json.Linq.JArray errors &&
                    errors.Count > 0)
                {
                    return errors[0]?.ToString();
                }

                // ASP.NET Core Validation Errors
                if (obj["errors"] is Newtonsoft.Json.Linq.JObject validationErrors)
                {
                    foreach (var property in validationErrors.Properties())
                    {
                        if (property.Value is Newtonsoft.Json.Linq.JArray arr &&
                            arr.Count > 0)
                        {
                            return arr[0]?.ToString();
                        }
                    }
                }

                return "An error occurred.";
            }
            catch
            {
                return "An error occurred.";
            }
        }
    }
}
