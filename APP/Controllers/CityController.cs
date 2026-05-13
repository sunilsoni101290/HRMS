using APP.Attributes;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace APP.Controllers
{
    #region City Controller
    [JwtAuthorize]
    public class CityController : Controller
    {
        private readonly IApiService _apiService;

        public CityController(IApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _apiService
                .GetAsync<List<CityListDto>>("city");

            return View(data);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(CityDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            await _apiService.PostAsync<dynamic>("city", dto);

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var data = await _apiService
                .GetAsync<CityDto>($"city/{id}");

            return View(data);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string id, CityDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            await _apiService
                .PutAsync<dynamic>($"city/{id}", dto);

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(string id)
        {
            await _apiService.DeleteAsync($"city/{id}");

            return RedirectToAction(nameof(Index));
        }
    }

    #endregion
}
