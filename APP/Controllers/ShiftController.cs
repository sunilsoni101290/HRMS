using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace APP.Controllers
{
    [JwtAuthorize]
    public class ShiftController : Controller
    {
        private readonly IApiService _apiService;
        private string _tenantId;
        private string _userId;
        public ShiftController(IApiService apiService)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _apiService
                .GetAsync<List<ShiftDto>>("shift");

            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ShiftDto shift = new ShiftDto
            {
                Name = string.Empty,
                StartTime = TimeSpan.Zero,
                EndTime = TimeSpan.Zero,
                HalfDayMinutes = 0,
                FullDayMinutes = 0,
                MinimumWorkingMinutes = 0,
                MaximumWorkingMinutes = 0
            };

            return View(shift);
        }

        [HttpPost]
        public async Task<IActionResult> Create(ShiftDto dto)
        {
            dto.TenantId = _tenantId;
            dto.CreatedBy = _userId;

            if (dto!=null)
            {
                await _apiService.PostAsync<dynamic>($"shift/add-shift", dto);

                TempData["Success"] = "Record saved successfully.";

                return View("Create", dto);
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var data = await _apiService
                .GetAsync<ShiftDto>($"shift/{id}");
            return View("Create", data);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string id, ShiftDto dto)
        {
            if (dto!=null)
            {
                dto.TenantId = _tenantId;
                dto.CreatedBy = _userId;

                await _apiService
                    .PutAsync<dynamic>($"shift/{id}", dto);
                TempData["Success"] = "Record updated successfully.";
                return View("Create", dto);
            }

            return RedirectToAction(nameof(Index));
        }

        #region Details

        public async Task<IActionResult> Details(string id)
        {
            var data = await _apiService
                .GetAsync<ShiftDto>($"shift/{id}");

            if (data == null)
                return NotFound();

            return View(data);
        }

        #endregion
    }
}
