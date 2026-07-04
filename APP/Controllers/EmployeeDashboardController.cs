using APP.Attributes;
using APP.Helpers;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace APP.Controllers
{
    #region Employee Dashboard Controller

    [JwtAuthorize]
    public class EmployeeDashboardController : Controller
    {
        private readonly IApiService _apiService;
        private string _userId;

        public EmployeeDashboardController(IApiService apiService)
        {
            _apiService = apiService;
            _userId = SessionHelper.GetActiveUserId;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _apiService
                .GetAsync<Application.DTOs.Dashboard.EmployeeDashboardDto>(
                    $"employee-dashboard/{_userId}");

            return View(data);
        }
    }

    #endregion
}
