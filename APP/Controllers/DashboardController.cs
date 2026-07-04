using APP.Attributes;
using APP.Helpers;
using APP.Models;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace APP.Controllers
{
    [JwtAuthorize]
    public class DashboardController : Controller
    {
        private readonly IApiService _apiService;
        private string _tenantId;
        private string _userId;

        public DashboardController(IApiService apiService)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
        }



        public async Task<IActionResult> Index()
        {
            // Non-admins (Employees) see their self-service dashboard
            if (!SessionHelper.IsAdminRole())
                return RedirectToAction("Index", "EmployeeDashboard");

            var data = await _apiService.GetAsync<DashboardKpiDto>(
                $"dashboard/{_tenantId}"
            );

            return View(data);
        }
    }
}
