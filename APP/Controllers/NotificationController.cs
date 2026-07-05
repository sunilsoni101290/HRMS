using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace APP.Controllers
{
    #region Notification Controller

    [JwtAuthorize]
    public class NotificationController : Controller
    {
        private readonly IApiService _apiService;
        private string _tenantId;
        private string _userId;

        public NotificationController(IApiService apiService)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
        }

        // Per-user inbox
        public async Task<IActionResult> Index()
        {
            var data = await _apiService
                .GetAsync<List<MyNotificationDto>>($"notification/my/{_userId}");
            return View(data);
        }

        // Admin list of broadcasts
        public async Task<IActionResult> Manage()
        {
            var data = await _apiService.GetAsync<List<NotificationListDto>>("notification");
            return View(data);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new NotificationCreateDto());
        }

        [HttpPost]
        public async Task<IActionResult> Create(NotificationCreateDto dto)
        {
            if (dto != null)
            {
                dto.TenantId = _tenantId;
                dto.CreatedBy = _userId;

                await _apiService.PostAsync<dynamic>("notification/broadcast", dto);

                TempData["Success"] = "Notification sent to all users.";
                return RedirectToAction(nameof(Manage));
            }

            return View(dto);
        }

        [HttpPost]
        public async Task<IActionResult> MarkRead(string recipientId)
        {
            await _apiService.PutAsync<dynamic>(
                $"notification/read/{recipientId}?userId={_userId}", new { });
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllRead()
        {
            await _apiService.PutAsync<dynamic>(
                $"notification/read-all/{_userId}", new { });
            TempData["Success"] = "All notifications marked as read.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(string id)
        {
            await _apiService.DeleteAsync($"notification/{id}");
            return RedirectToAction(nameof(Manage));
        }
    }

    #endregion
}
