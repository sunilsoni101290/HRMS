using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace APP.Controllers
{
    /// <summary>
    /// Standalone "Sync Biometric Attendance (Month Wise)" admin screen - a
    /// brand-new, COMPLETELY SEPARATE controller/view from EsslAttendance and
    /// HistoricalAttendanceSync (neither is modified or referenced here).
    /// One button opens a modal (Year/Month/Tenant/Company/Shift/Branch/
    /// CreatedBy/Status), which posts straight to
    /// api/MonthWiseBiometricSync/execute and shows the SP's summary via
    /// SweetAlert - no preview step, no background job/polling, since this
    /// SP is a single bounded set-based/cursor-driven run over one tenant/
    /// company/shift/month. Admin/HR-only via EssRestrictionAttribute's
    /// AdminOnlyControllers list (same gating style as EsslAttendance/
    /// HistoricalAttendanceSync).
    /// </summary>
    [JwtAuthorize]
    public class MonthWiseBiometricSyncController : Controller
    {
        private readonly IApiService _apiService;

        public MonthWiseBiometricSyncController(IApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<IActionResult> Index()
        {
            await LoadDropdowns();
            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Execute([FromBody] MonthWiseSyncRequestDto model)
        {
            try
            {
                var response = await _apiService.PostAsync<MonthWiseSyncRequestDto, ApiResponse<MonthWiseSyncResultDto>>(
                    "MonthWiseBiometricSync/execute", model ?? new MonthWiseSyncRequestDto());

                return Json(new
                {
                    success = response?.Success ?? false,
                    message = response?.Message ?? "Unable to reach the ERP API.",
                    data = response?.Data
                });
            }
            catch (ApiException ex)
            {
                return Json(new { success = false, message = GetErrorMessage(ex.ResponseContent) });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = GetErrorMessage(ex.Message) });
            }
        }

        // Company -> Branch cascade, same "dropdown/branch/{companyId}" proxy
        // pattern as BiometricDeviceController.GetBranchByCompanyId.
        [HttpGet]
        public async Task<JsonResult> GetBranchByCompanyId(string companyId)
        {
            var branches = string.IsNullOrWhiteSpace(companyId)
                ? new List<DropdownDto>()
                : await _apiService.GetAsync<List<DropdownDto>>($"dropdown/branch/{companyId}")
                    ?? new List<DropdownDto>();

            var result = branches.Select(x => new { value = x.Value, text = x.Text });

            return Json(result);
        }

        #region LoadDropdowns

        private async Task LoadDropdowns()
        {
            // Tenant - GET /api/tenant (TenantController.GetAll), the only
            // real tenant-listing endpoint this codebase has; every other
            // admin screen derives TenantId implicitly from the logged-in
            // session (ITenantService.GetTenantId()) instead of showing a
            // Tenant picker, so this dropdown's selection is informational
            // for the operator - the API call always uses the session's own
            // TenantId as authoritative (see
            // MonthWiseBiometricSyncController.Execute in API).
            var tenants = await _apiService.GetAsync<List<TenantDto>>("tenant")
                ?? new List<TenantDto>();

            ViewBag.TenantList = new SelectList(
                tenants.Where(t => t.IsActive),
                "Id",
                "Name",
                SessionHelper.GetActiveTenantId);

            // Company - GET /api/dropdown/company, same endpoint every other
            // admin screen (BiometricDevice, EmployeeShiftMapping, etc.) uses.
            var companies = await _apiService.GetAsync<List<DropdownDto>>("dropdown/company")
                ?? new List<DropdownDto>();

            ViewBag.CompanyList = new SelectList(
                companies,
                "Value",
                "Text",
                SessionHelper.GetActiveCompanyId);

            // Shift - GET /api/dropdown/shift.
            var shifts = await _apiService.GetAsync<List<DropdownDto>>("dropdown/shift")
                ?? new List<DropdownDto>();

            ViewBag.ShiftList = new SelectList(shifts, "Value", "Text");

            // Branch - loaded empty here; populated client-side via
            // GetBranchByCompanyId once a Company is chosen (optional field).
            ViewBag.BranchList = new SelectList(new List<DropdownDto>(), "Value", "Text");

            ViewBag.DefaultCreatedBy = string.IsNullOrWhiteSpace(SessionHelper.GetActiveFullName)
                ? "SYSTEM"
                : SessionHelper.GetActiveFullName;
        }

        #endregion

        // Same shape as the GetErrorMessage helper used across the rest of
        // the APP controllers (see EsslAttendanceController/
        // HistoricalAttendanceSyncController's identical private helper,
        // copied verbatim per this codebase's convention).
        private static string GetErrorMessage(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return "The ERP API returned an error.";

            try
            {
                var jsonStart = raw.IndexOf('{');
                var json = jsonStart >= 0 ? raw.Substring(jsonStart) : raw;

                var obj = Newtonsoft.Json.Linq.JObject.Parse(json);

                if (obj["Message"] != null) return obj["Message"]!.ToString();
                if (obj["message"] != null) return obj["message"]!.ToString();

                if (obj["errors"] is Newtonsoft.Json.Linq.JObject errors)
                {
                    foreach (var prop in errors.Properties())
                    {
                        if (prop.Value is Newtonsoft.Json.Linq.JArray arr && arr.Count > 0)
                            return arr[0]!.ToString();
                    }
                }

                return raw;
            }
            catch
            {
                return raw;
            }
        }
    }
}
