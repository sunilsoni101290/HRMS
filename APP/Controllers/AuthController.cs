using APP.Attributes;
using APP.Helpers;
using APP.Models.Auth;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Humanizer;
using Microsoft.AspNetCore.Http;
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

        // How long a "Remember Me" login stays silently renewable for. Must
        // stay in sync with the RefreshToken.ExpiryDate window set when the
        // token is issued in AuthService.GenerateAuthResponse (7 days) -
        // there's no point remembering a refresh token on the client for
        // longer than the server will actually honor it.
        private static readonly TimeSpan RememberMeDuration = TimeSpan.FromDays(7);
        private const string RememberedUsernameCookie = "RememberedUsername";
        private const string RememberMeTokenCookie = "RememberMeToken";

        // =========================
        // LOGIN PAGE
        // =========================
        [HttpGet]
        public IActionResult Login()
        {
            var rememberedUsername = Request.Cookies[RememberedUsernameCookie];
            
            // Get Client IP
            string localIP = Dns.GetHostEntry(Dns.GetHostName())
                .AddressList
                .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)?
                .ToString();

            return View(new LoginDto
            {
                IpAddress=localIP,
                Username = rememberedUsername ?? string.Empty,
                RememberMe = !string.IsNullOrEmpty(rememberedUsername)
            });
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
                // Enrich with request-derived info for LoginHistory - none
                // of this is user input, so it's set here rather than bound
                // from the posted form.

                model.IpAddress = model.IpAddress;

                var userAgent = Request.Headers["User-Agent"].ToString();
                var (browser, os, deviceInfo) = UserAgentHelper.Parse(userAgent);
                model.Browser = browser;
                model.OS = os;
                model.DeviceInfo = deviceInfo;

                var response = await _apiService.PostAsync<LoginDto, ApiResponse<AuthResponse>>(
                    "auth/login",
                    model);

                // API did not return anything
                if (response == null)
                {
                    TempData["GlobalError"] = "Unable to connect to the server.";
                    return View(model);
                }

                // Login Failed
                if (!response.Success)
                {
                    TempData["GlobalError"] = response.Message;

                    if (response.Errors != null && response.Errors.Any())
                    {
                        foreach (var error in response.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error);
                        }
                    }

                    return View(model);
                }

                // Safety check
                if (response.Data == null)
                {
                    TempData["GlobalError"] = "Login response is invalid.";
                    return View(model);
                }

                // Store Session
                HttpContext.Session.SetString("AccessToken", response.Data.AccessToken ?? "");
                HttpContext.Session.SetString("RefreshToken", response.Data.RefreshToken ?? "");
                HttpContext.Session.SetString("FullName", response.Data.FullName ?? "");
                HttpContext.Session.SetString("UserId", response.Data.UserId ?? "");
                HttpContext.Session.SetString("EmployeeId", response.Data.EmployeeId ?? "");
                HttpContext.Session.SetString("TenantId", response.Data.TenantId ?? "");
                HttpContext.Session.SetString("Designation", response.Data.Designation ?? "");
                HttpContext.Session.SetString("CompanyName", response.Data.CompanyName ?? "");
                HttpContext.Session.SetString("CompanyId", response.Data.CompanyId ?? "");
                HttpContext.Session.SetString("BranchId", response.Data.BranchId ?? "");
                HttpContext.Session.SetString("RoleName", response.Data.RoleName ?? "");

                // Remember Me
                ApplyRememberMeCookies(
                    model.Username,
                    response.Data.RefreshToken ?? "",
                    model.RememberMe);

                // Redirect based on role
                if (SessionHelper.IsAdminRole(response.Data.RoleName))
                    return RedirectToAction("Index", "Dashboard");

                return RedirectToAction("Index", "EmployeeDashboard");
            }
            catch (ApiException ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
            }
            catch (HttpRequestException)
            {
                TempData["GlobalError"] = "Unable to connect to the API server.";
            }
            catch (TaskCanceledException)
            {
                TempData["GlobalError"] = "The request timed out. Please try again.";
            }
            catch (Exception ex)
            {
                // Log ex here
                TempData["GlobalError"] = "An unexpected error occurred. Please try again.";
            }

            return View(model);
        }
        //[HttpPost]
        //public async Task<IActionResult> Login(LoginDto model)
        //{
        //    if (model==null)
        //        return View(model);

        //    try
        //    {
        //        string localIP = Dns.GetHostEntry(Dns.GetHostName())
        //            .AddressList
        //            .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)?
        //            .ToString();

        //        model.IpAddress = localIP;

        //        var response = await _apiService
        //            .PostAsync<LoginDto, ApiResponse<AuthResponse>>(
        //                "auth/login",
        //                model);

        //        if (response != null && response.Success)
        //        {
        //            HttpContext.Session.SetString("AccessToken", response.Data.AccessToken);
        //            HttpContext.Session.SetString("RefreshToken", response.Data.RefreshToken);
        //            HttpContext.Session.SetString("FullName", response.Data.FullName ?? "");
        //            HttpContext.Session.SetString("UserId", response.Data.UserId);
        //            HttpContext.Session.SetString("EmployeeId", response.Data.EmployeeId ?? "");
        //            HttpContext.Session.SetString("TenantId", response.Data.TenantId);
        //            HttpContext.Session.SetString("Designation", response.Data.Designation ?? "");
        //            HttpContext.Session.SetString("CompanyName", response.Data.CompanyName ?? "");
        //            HttpContext.Session.SetString("CompanyId", response.Data.CompanyId ?? "");
        //            HttpContext.Session.SetString("BranchId", response.Data.BranchId ?? "");
        //            HttpContext.Session.SetString("RoleName", response.Data.RoleName ?? "");

        //            ApplyRememberMeCookies(model.Username, response.Data.RefreshToken, model.RememberMe);

        //            // Role-based landing page
        //            return SessionHelper.IsAdminRole(response.Data.RoleName)
        //                ? RedirectToAction("Index", "Dashboard")
        //                : RedirectToAction("Index", "EmployeeDashboard");
        //        }

        //        TempData["GlobalError"] = response?.Message ?? "Login failed.";
        //    }
        //    catch (ApiException ex)
        //    {
        //        var errorMessage = GetErrorMessage(ex.ResponseContent);

        //        TempData["GlobalError"] = errorMessage;
        //    }
        //    catch (Exception ex)
        //    {
        //        TempData["GlobalError"] = ex.Message;
        //    }

        //    return View(model);
        //}

        // Sets or clears the two "Remember Me" cookies:
        //  - RememberedUsername: plain, non-sensitive, only used to
        //    pre-fill the Username field on the next visit.
        //  - RememberMeToken: HttpOnly copy of the refresh token, used by
        //    JwtAuthorizeAttribute to silently restore the session if the
        //    server-side Session has expired (e.g. the browser was closed)
        //    but the refresh token itself is still valid. Never readable
        //    from client-side script, and never sent over plain HTTP.
        private void ApplyRememberMeCookies(string username, string refreshToken, bool rememberMe)
        {
            if (rememberMe)
            {
                Response.Cookies.Append(RememberedUsernameCookie, username ?? string.Empty, new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.Add(RememberMeDuration),
                    HttpOnly = false,
                    Secure = Request.IsHttps,
                    SameSite = SameSiteMode.Lax
                });

                Response.Cookies.Append(RememberMeTokenCookie, refreshToken ?? string.Empty, new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.Add(RememberMeDuration),
                    HttpOnly = true,
                    Secure = Request.IsHttps,
                    SameSite = SameSiteMode.Lax
                });
            }
            else
            {
                Response.Cookies.Delete(RememberedUsernameCookie);
                Response.Cookies.Delete(RememberMeTokenCookie);
            }
        }

        // =========================
        // LOGOUT
        // =========================
        public async Task<IActionResult> Logout()
        {
            // An explicit logout should kill the silent-relogin token even
            // for a "remembered" login - otherwise JwtAuthorizeAttribute
            // would just log the user straight back in on their next
            // request. The username cookie is left alone as a convenience
            // so it still pre-fills next time.
            Response.Cookies.Delete(RememberMeTokenCookie);

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

        // =========================
        // FORGOT PASSWORD
        // =========================
        // No email/SMS infrastructure exists in this system yet, so this is
        // an identity-verified self-service reset: the user must know both
        // their Username and the Email already on file for that account.
        // The API re-verifies that match server-side before touching
        // anything - this page never sends a raw user id.
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var response = await _apiService
                    .PostAsync<ForgotPasswordDto, ApiResponse<object>>(
                        "auth/forgot-password",
                        model);

                if (response != null && response.Success)
                {
                    TempData["Success"] = response.Message ?? "Password reset successfully. Please log in with your new password.";
                    return RedirectToAction(nameof(Login));
                }

                ModelState.AddModelError("", response?.Message ?? "Unable to reset password.");
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

            return View(new ChangePasswordDto() { UserId =_userId });
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
