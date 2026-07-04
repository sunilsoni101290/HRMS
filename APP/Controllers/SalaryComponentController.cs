using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace APP.Controllers
{
    #region Salary Component Controller

    [JwtAuthorize]
    public class SalaryComponentController : Controller
    {
        private readonly IApiService _apiService;
        private string _tenantId;
        private string _userId;

        public SalaryComponentController(IApiService apiService)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _apiService
                .GetAsync<List<SalaryComponentListDto>>("salary-component");
            return View(data);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new SalaryComponentDto());
        }

        [HttpPost]
        public async Task<IActionResult> Create(SalaryComponentDto dto)
        {
            if (dto != null)
            {
                dto.TenantId = _tenantId;
                dto.CreatedBy = _userId;

                await _apiService.PostAsync<dynamic>("salary-component", dto);

                TempData["Success"] = "Record saved successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(dto);
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var data = await _apiService
                .GetAsync<SalaryComponentDto>($"salary-component/{id}");
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var data = await _apiService
                .GetAsync<SalaryComponentDto>($"salary-component/{id}");
            return View("Create", data);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string id, SalaryComponentDto dto)
        {
            if (dto != null)
            {
                dto.TenantId = _tenantId;
                dto.ModifiedBy = _userId;
                dto.ModifiedOn = DateTime.UtcNow;

                await _apiService.PutAsync<dynamic>($"salary-component/{id}", dto);

                TempData["Success"] = "Record updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View("Create", dto);
        }

        public async Task<IActionResult> Delete(string id)
        {
            await _apiService.DeleteAsync($"salary-component/{id}");
            return RedirectToAction(nameof(Index));
        }
    }

    #endregion
}
