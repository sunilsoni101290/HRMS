using APP.Attributes;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace APP.Controllers
{
    #region Designation Controller

    [JwtAuthorize]
    public class DesignationController : Controller
    {
        private readonly IApiService _apiService;

        public DesignationController(IApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _apiService
                .GetAsync<List<DesignationListDto>>("designation");

            return View(data);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(DesignationDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            await _apiService.PostAsync<dynamic>("designation", dto);

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var data = await _apiService
                .GetAsync<DesignationDto>($"designation/{id}");

            return View(data);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string id, DesignationDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            await _apiService
                .PutAsync<dynamic>($"designation/{id}", dto);

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(string id)
        {
            await _apiService.DeleteAsync($"designation/{id}");

            return RedirectToAction(nameof(Index));
        }
    }

    #endregion
}
