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

        #region Navbar bell (AJAX - polled by the shared layout partial)

        // Lightweight JSON endpoints backing the bell-icon dropdown in both
        // the admin/HR (_LayoutMain) and ESS (_EmployeeNavbar) navbars.
        // Deliberately separate from Index/MarkRead/MarkAllRead above (which
        // render a full page / redirect) - these return JSON only, polled
        // every 45s by _NotificationBell.cshtml, so the badge count and
        // dropdown list can refresh without a full page reload.

        [HttpGet]
        public async Task<JsonResult> UnreadCount()
        {
            if (string.IsNullOrEmpty(_userId))
                return Json(new { count = 0 });

            try
            {
                // The API returns { Count = n } (see API's NotificationController.GetUnreadCount),
                // not a bare int - deserialize into a matching shape rather
                // than a raw int.
                var response = await _apiService.GetAsync<CountResponseDto>($"notification/unread-count/{_userId}");
                return Json(new { count = response?.Count ?? 0 });
            }
            catch
            {
                // The bell must never break page load/polling just because
                // the API call failed for any reason - fail quiet at 0.
                return Json(new { count = 0 });
            }
        }

        [HttpGet]
        public async Task<JsonResult> Recent()
        {
            if (string.IsNullOrEmpty(_userId))
                return Json(new List<MyNotificationDto>());

            try
            {
                var data = await _apiService.GetAsync<List<MyNotificationDto>>($"notification/my/{_userId}");

                // Projected into an explicit lowercase-keyed shape rather
                // than serializing MyNotificationDto directly - the global
                // JsonOptions for this project sets PropertyNamingPolicy to
                // null (i.e. PascalCase is preserved, not camelCased), which
                // would otherwise silently mismatch the camelCase field
                // names _NotificationBell.cshtml's JS reads (item.title,
                // item.isRead, etc.).
                var recent = (data ?? new List<MyNotificationDto>())
                    .OrderByDescending(x => x.CreatedOn)
                    .Take(8)
                    .Select(x => new
                    {
                        recipientId = x.RecipientId,
                        notificationId = x.NotificationId,
                        title = x.Title,
                        message = x.Message,
                        notificationType = x.NotificationType,
                        priority = x.Priority,
                        redirectUrl = x.RedirectUrl,
                        isRead = x.IsRead,
                        readDate = x.ReadDate,
                        createdOn = x.CreatedOn
                    })
                    .ToList();

                return Json(recent);
            }
            catch
            {
                return Json(new List<object>());
            }
        }

        [HttpPost]
        public async Task<JsonResult> MarkReadAjax(string recipientId)
        {
            if (string.IsNullOrEmpty(recipientId) || string.IsNullOrEmpty(_userId))
                return Json(new { success = false });

            try
            {
                await _apiService.PutAsync<dynamic>(
                    $"notification/read/{recipientId}?userId={_userId}", new { });

                return Json(new { success = true });
            }
            catch
            {
                return Json(new { success = false });
            }
        }

        private class CountResponseDto
        {
            public int Count { get; set; }
        }

        #endregion
    }

    #endregion
}
