using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace APP.Controllers
{
    #region Payslip Verification Controller

    // The landing page a payslip's "Scan to Verify" QR code opens - reached
    // by anyone with a phone camera, logged in or not, so this controller
    // deliberately carries NO [JwtAuthorize]. The token itself (an
    // HMAC-signed, storage-free credential - see
    // Domain.Helper.PayslipVerificationHelper) is what proves the payslip
    // is genuine; nothing here trusts a database id from the query string.
    public class PayslipVerificationController : Controller
    {
        private readonly IApiService _apiService;

        public PayslipVerificationController(IApiService apiService)
        {
            _apiService = apiService;
        }

        [HttpGet]
        public async Task<IActionResult> Verify(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return View(new PayslipVerificationResultDto { IsValid = false });

            try
            {
                var data = await _apiService.GetAsync<PayslipVerificationResultDto>(
                    $"payroll/verify-payslip?token={Uri.EscapeDataString(token)}");

                return View(data ?? new PayslipVerificationResultDto { IsValid = false });
            }
            catch (ApiException)
            {
                return View(new PayslipVerificationResultDto { IsValid = false });
            }
        }
    }

    #endregion
}
