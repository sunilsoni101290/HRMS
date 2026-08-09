using APP.Attributes;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace APP.Controllers
{
    // Phase 16 - standalone "Loan & Advance Audit Log" browse page. Named
    // exactly "LoanAdvanceAuditLog" to match
    // AppFeatureConstants.LOAN_ADVANCE_AUDIT_LOG_CONTROLLER (seeded menu
    // entry, Phase 10). Read-only - all writes happen automatically via
    // Infrastructure/Interceptors/LoanAdvanceAuditInterceptor.cs (Phase 16),
    // never through this controller.
    [JwtAuthorize]
    public class LoanAdvanceAuditLogController : Controller
    {
        private readonly IApiService _apiService;

        public LoanAdvanceAuditLogController(IApiService apiService)
        {
            _apiService = apiService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? entityType)
        {
            ViewBag.EntityType = entityType;

            var url = $"loanadvanceauditlog/recent?entityType={Uri.EscapeDataString(entityType ?? string.Empty)}&take=200";
            var data = await _apiService.GetAsync<List<LoanAdvanceAuditLogDto>>(url) ?? new();

            return View(data);
        }

        // Per-record drill-down, linked from EmployeeLoan/EmployeeAdvance
        // Details views ("View Audit Trail").
        [HttpGet]
        public async Task<IActionResult> ForEntity(string entityType, string entityId)
        {
            ViewBag.EntityType = entityType;
            ViewBag.EntityId = entityId;

            var url = $"loanadvanceauditlog?entityType={Uri.EscapeDataString(entityType)}&entityId={Uri.EscapeDataString(entityId)}";
            var data = await _apiService.GetAsync<List<LoanAdvanceAuditLogDto>>(url) ?? new();

            return View("ForEntity", data);
        }
    }
}
