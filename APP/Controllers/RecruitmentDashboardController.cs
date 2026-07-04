using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace APP.Controllers
{
    #region Recruitment Dashboard Controller

    [JwtAuthorize]
    public class RecruitmentDashboardController : Controller
    {
        private readonly IApiService _apiService;

        public RecruitmentDashboardController(IApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _apiService
                .GetAsync<RecruitmentDashboardDto>("recruitment-dashboard");
            return View(data);
        }
    }

    #endregion
}
