using APP.Attributes;
using APP.Models;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace APP.Controllers
{
    [JwtAuthorize]
    public class DashboardController : Controller
    {
        private readonly IApiService _apiService;
        private readonly IConfiguration _configuration;
        public DashboardController(IApiService apiService, IConfiguration configuration)
        {
            _apiService = apiService;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {
            // API Base URL
            var baseUrl = _configuration["ApiSettings:BaseUrl"];

            var tenantId = HttpContext
                .Session
                .GetString("TenantId");

            // API Call
            var response = await _apiService
                    .GetAsync<DashboardViewModel>($"{baseUrl}dashboard");

            return View(response);
        }
    }
}
