using APP.Models.Auth;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace APP.Controllers
{
    public class AuthController : Controller
    {
        private readonly IApiService _apiService;

        public AuthController(IApiService apiService)
        {
            _apiService = apiService;
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
            var refreshToken =
                HttpContext.Session.GetString("RefreshToken");

            if (!string.IsNullOrEmpty(refreshToken))
            {
                await _apiService.PostAsync<bool>(
                    "auth/logout",
                    refreshToken);
            }

            HttpContext.Session.Clear();

            return RedirectToAction("Login");
        }
    }
}
