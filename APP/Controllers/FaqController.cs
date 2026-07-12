using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace APP.Controllers
{
    #region FAQ / Knowledge Base Controller

    // Browsing published FAQs is open to everyone. Managing the knowledge
    // base (create/edit/delete/publish) is Admin/HR only - enforced here
    // server-side, not just by hiding buttons in the view.
    [JwtAuthorize]
    public class FaqController : Controller
    {
        private readonly IApiService _apiService;
        private string _tenantId;
        private string _userId;
        private readonly bool _isAdmin;

        public FaqController(IApiService apiService)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
            _isAdmin = SessionHelper.IsAdminRole();
        }

        // Admin/HR only - manage every FAQ item, including unpublished ones.
        public async Task<IActionResult> Index()
        {
            if (!_isAdmin)
            {
                TempData["GlobalError"] = "You don't have permission to manage the FAQ knowledge base.";
                return RedirectToAction(nameof(Browse));
            }

            var data = await _apiService.GetAsync<List<FaqItemDto>>("faq");
            return View(data ?? new List<FaqItemDto>());
        }

        // Everyone - published FAQs only, grouped by category in the view.
        public async Task<IActionResult> Browse()
        {
            ViewBag.IsAdmin = _isAdmin;

            var data = await _apiService.GetAsync<List<FaqItemDto>>("faq/active");
            return View(data ?? new List<FaqItemDto>());
        }

        [HttpGet]
        public IActionResult Create()
        {
            if (!_isAdmin)
            {
                TempData["GlobalError"] = "You don't have permission to add FAQ items.";
                return RedirectToAction(nameof(Browse));
            }

            return View(new FaqItemDto());
        }

        [HttpPost]
        public async Task<IActionResult> Create(FaqItemDto dto)
        {
            if (!_isAdmin)
            {
                TempData["GlobalError"] = "You don't have permission to add FAQ items.";
                return RedirectToAction(nameof(Browse));
            }

            if (dto != null)
            {
                dto.TenantId = _tenantId;
                dto.CreatedBy = _userId;

                await _apiService.PostAsync<dynamic>("faq", dto);

                TempData["Success"] = "FAQ item added successfully.";
                return RedirectToAction(nameof(Index));
            }

            return View(dto);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (!_isAdmin)
            {
                TempData["GlobalError"] = "You don't have permission to edit FAQ items.";
                return RedirectToAction(nameof(Browse));
            }

            var data = await _apiService.GetAsync<FaqItemDto>($"faq/{id}");
            return View("Create", data);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string id, FaqItemDto dto)
        {
            if (!_isAdmin)
            {
                TempData["GlobalError"] = "You don't have permission to edit FAQ items.";
                return RedirectToAction(nameof(Browse));
            }

            if (dto != null)
            {
                dto.TenantId = _tenantId;
                dto.ModifiedBy = _userId;
                dto.ModifiedOn = DateTime.UtcNow;

                await _apiService.PutAsync<dynamic>($"faq/{id}", dto);

                TempData["Success"] = "FAQ item updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            return View("Create", dto);
        }

        public async Task<IActionResult> Delete(string id)
        {
            if (!_isAdmin)
            {
                TempData["GlobalError"] = "You don't have permission to delete FAQ items.";
                return RedirectToAction(nameof(Browse));
            }

            await _apiService.DeleteAsync($"faq/{id}");
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ToggleActive(string id)
        {
            if (!_isAdmin)
            {
                TempData["GlobalError"] = "You don't have permission to publish/unpublish FAQ items.";
                return RedirectToAction(nameof(Browse));
            }

            await _apiService.PostAsync<dynamic>($"faq/{id}/toggle-active", new { });
            return RedirectToAction(nameof(Index));
        }
    }

    #endregion
}
