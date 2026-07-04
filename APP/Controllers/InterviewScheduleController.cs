using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace APP.Controllers
{
    #region Interview Schedule Controller

    [JwtAuthorize]
    public class InterviewScheduleController : Controller
    {
        private readonly IApiService _apiService;
        private string _tenantId;
        private string _userId;

        public InterviewScheduleController(IApiService apiService)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _apiService
                .GetAsync<List<InterviewScheduleListDto>>("interview-schedule");
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();
            return View(new InterviewScheduleDto());
        }

        [HttpPost]
        public async Task<IActionResult> Create(InterviewScheduleDto dto)
        {
            if (dto != null)
            {
                dto.TenantId = _tenantId;
                dto.CreatedBy = _userId;

                await _apiService.PostAsync<dynamic>("interview-schedule", dto);

                TempData["Success"] = "Interview scheduled successfully.";
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdowns();
            return View(dto);
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var data = await _apiService
                .GetAsync<InterviewScheduleDto>($"interview-schedule/{id}");
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var data = await _apiService
                .GetAsync<InterviewScheduleDto>($"interview-schedule/{id}");
            await LoadDropdowns();
            return View("Create", data);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string id, InterviewScheduleDto dto)
        {
            if (dto != null)
            {
                dto.TenantId = _tenantId;
                dto.ModifiedBy = _userId;
                dto.ModifiedOn = DateTime.UtcNow;

                await _apiService.PutAsync<dynamic>($"interview-schedule/{id}", dto);

                TempData["Success"] = "Interview updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdowns();
            return View("Create", dto);
        }

        [HttpPost]
        public async Task<IActionResult> Complete(string id, string? feedback)
        {
            await _apiService.PutAsync<dynamic>(
                $"interview-schedule/status/{id}?status=2&feedback={Uri.EscapeDataString(feedback ?? "")}&userId={_userId}",
                new { });
            TempData["Success"] = "Interview marked as completed.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Cancel(string id)
        {
            await _apiService.PutAsync<dynamic>(
                $"interview-schedule/status/{id}?status=3&userId={_userId}", new { });
            TempData["Success"] = "Interview cancelled.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(string id)
        {
            await _apiService.DeleteAsync($"interview-schedule/{id}");
            return RedirectToAction(nameof(Index));
        }

        #region Load Dropdowns

        private async Task LoadDropdowns()
        {
            var applications = await _apiService.GetAsync<List<DropdownDto>>("dropdown/application");
            ViewBag.ApplicationList = new SelectList(applications, "Value", "Text");

            var employees = await _apiService.GetAsync<List<DropdownDto>>("dropdown/employee");
            ViewBag.InterviewerList = new SelectList(employees, "Value", "Text");

            ViewBag.ModeList = new List<SelectListItem>
            {
                new() { Value = "1", Text = "Online" },
                new() { Value = "2", Text = "Offline" }
            };

            ViewBag.StatusList = new List<SelectListItem>
            {
                new() { Value = "1", Text = "Scheduled" },
                new() { Value = "2", Text = "Completed" },
                new() { Value = "3", Text = "Cancelled" },
                new() { Value = "4", Text = "Expired" }
            };
        }

        #endregion
    }

    #endregion
}
