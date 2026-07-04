using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace APP.Controllers
{
    public class LeaveApprovalHistoryController : Controller
    {
        private readonly IApiService _apiService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public LeaveApprovalHistoryController(IApiService apiService, IHttpContextAccessor httpContextAccessor)
        {
            _apiService = apiService;
            _httpContextAccessor = httpContextAccessor;
        }
        public async Task<IActionResult> Index()
        {
            var data = await _apiService
                .GetAsync<List<LeaveApprovalHistoryDetailDto>>
                (
                    $"LeaveApplication/approval-history"
                );

            return View(data);
        }

        public async Task<IActionResult> Details(string id)
        {
            var data = await _apiService
                .GetAsync<List<LeaveApprovalHistoryDetailDto>>
                (
                    $"LeaveApplication/approval-history/{id}"
                );

            return View(data);
        }

        public async Task<IActionResult>ApprovalHistory(string leaveApplicationId)
        {
            var data =
                await _apiService
                .GetAsync<List<LeaveApprovalHistoryDetailDto>>
                (
                    $"LeaveApplication/{leaveApplicationId}/approval-history"
                );

            return View(data);
        }

    }
}
