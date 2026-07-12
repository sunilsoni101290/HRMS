using APP.Attributes;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace APP.Controllers
{
    #region Login History Controller

    [JwtAuthorize]
    public class LoginHistoryController : Controller
    {
        private readonly IApiService _apiService;

        public LoginHistoryController(IApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _apiService.GetAsync<List<LoginHistoryListDto>>("loginhistory");
            return View(data ?? new List<LoginHistoryListDto>());
        }
    }

    #endregion
}
