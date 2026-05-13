using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace APP.Controllers
{
    #region Country Controller

    public class CountryController : Controller
    {
        private readonly IApiService _apiService;

        public CountryController(IApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _apiService
                .GetAsync<List<CountryListDto>>("country");

            return View(data);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(CountryDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            await _apiService.PostAsync<dynamic>("country", dto);

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var data = await _apiService
                .GetAsync<CountryDto>($"country/{id}");

            return View(data);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string id, CountryDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            await _apiService
                .PutAsync<dynamic>($"country/{id}", dto);

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(string id)
        {
            await _apiService.DeleteAsync($"country/{id}");

            return RedirectToAction(nameof(Index));
        }
    }

    #endregion
}
