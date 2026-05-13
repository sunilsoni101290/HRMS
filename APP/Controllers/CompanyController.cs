using APP.Attributes;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace APP.Controllers
{
    [JwtAuthorize]
    public class CompanyController : Controller
    {
        private readonly IApiService _apiService;

        public CompanyController(IApiService apiService)
        {
            _apiService = apiService;
        }

        #region Index

        public async Task<IActionResult> Index()
        {
            var data = await _apiService
                .GetAsync<List<CompanyListDto>>("company");

            return View(data);
        }

        #endregion

        #region Create GET

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        #endregion

        #region Create POST

        [HttpPost]
        public async Task<IActionResult> Create(CompanyDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            var result = await _apiService
                .PostAsync<dynamic>("company", dto);

            return RedirectToAction("Index");
        }

        #endregion

        #region Edit GET

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var data = await _apiService
                .GetAsync<CompanyDto>($"company/{id}");

            return View(data);
        }

        #endregion

        #region Edit POST

        [HttpPost]
        public async Task<IActionResult> Edit(string id, CompanyDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            await _apiService
                .PutAsync<dynamic>($"company/{id}", dto);

            return RedirectToAction("Index");
        }

        #endregion

        #region Delete

        public async Task<IActionResult> Delete(string id)
        {
            await _apiService
                .DeleteAsync($"company/{id}");

            return RedirectToAction("Index");
        }

        #endregion
    }
}
