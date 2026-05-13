using APP.Attributes;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace APP.Controllers
{
    #region State Controller
    [JwtAuthorize]

    public class StateController : Controller
    {
        private readonly IApiService _apiService;

        public StateController(IApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _apiService
                .GetAsync<List<StateListDto>>("state");

            return View(data);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(StateDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            await _apiService.PostAsync<dynamic>("state", dto);

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var data = await _apiService
                .GetAsync<StateDto>($"state/{id}");

            return View(data);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string id, StateDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            await _apiService
                .PutAsync<dynamic>($"state/{id}", dto);

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(string id)
        {
            await _apiService.DeleteAsync($"state/{id}");

            return RedirectToAction(nameof(Index));
        }
    }

    #endregion
}
