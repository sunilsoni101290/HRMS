using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using static APP.Helpers.EnumExtensions;

namespace APP.Controllers
{
    // NOTE: named exactly "AttendancePolicy" to match the menu-seeding string
    // in Domain/Helper/AppFeatureConstants.cs
    // (ATTENDANCE_POLICY_CONTROLLER = "AttendancePolicy") and the API route
    // (api/attendancepolicy). Admin/HR only - see EssRestrictionAttribute.
    [JwtAuthorize]
    public class AttendancePolicyController : Controller
    {
        private readonly IApiService _apiService;
        private string _tenantId;
        private string _userId;

        public AttendancePolicyController(IApiService apiService)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
        }

        #region Index

        public async Task<IActionResult> Index()
        {
            var data = await _apiService.GetAsync<List<AttendancePolicyDto>>("attendancepolicy");
            return View(data ?? new List<AttendancePolicyDto>());
        }

        #endregion

        #region Create

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await BindDropdowns();

            return View(new AttendancePolicyDto
            {
                EffectiveFrom = DateTime.Today,
                IsActive = true,
                LateMarkPenaltyType = (int)LateMarkPenaltyType.None
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AttendancePolicyDto model)
        {
            if (!ModelState.IsValid)
            {
                await BindDropdowns();
                return View(model);
            }

            model.TenantId = _tenantId;
            model.CreatedBy = _userId;

            try
            {
                await _apiService.PostAsync<dynamic>("attendancepolicy", model);

                TempData["Success"] = "Attendance policy created successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (ApiException ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
                await BindDropdowns();
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["GlobalError"] = ex.Message;
                await BindDropdowns();
                return View(model);
            }
        }

        #endregion

        #region Edit

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return RedirectToAction(nameof(Index));

            var model = await _apiService.GetAsync<AttendancePolicyDto>($"attendancepolicy/{id}");

            if (model == null) return NotFound();

            await BindDropdowns();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, AttendancePolicyDto model)
        {
            if (!ModelState.IsValid)
            {
                await BindDropdowns();
                return View(model);
            }

            model.TenantId = _tenantId;
            model.ModifiedBy = _userId;
            model.ModifiedOn = DateTime.UtcNow;

            try
            {
                await _apiService.PutAsync<dynamic>($"attendancepolicy/{id}", model);

                TempData["Success"] = "Attendance policy updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (ApiException ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
                await BindDropdowns();
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.Message);
                await BindDropdowns();
                return View(model);
            }
        }

        #endregion

        #region Delete

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _apiService.DeleteAsync($"attendancepolicy/{id}");
                TempData["Success"] = "Attendance policy deleted successfully.";
            }
            catch (ApiException ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
            }
            catch (Exception ex)
            {
                TempData["GlobalError"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Helpers

        private async Task BindDropdowns()
        {
            var companies = await _apiService.GetAsync<List<DropdownDto>>("dropdown/company");
            ViewBag.CompanyList = new SelectList(companies, "Value", "Text");

            ViewBag.LateMarkPenaltyTypes = Enum.GetValues(typeof(LateMarkPenaltyType))
                .Cast<LateMarkPenaltyType>()
                .Select(x => new SelectListItem
                {
                    Text = x.ToString(),
                    Value = ((int)x).ToString()
                })
                .ToList();
        }

        private string GetErrorMessage(string raw)
        {
            try
            {
                var jsonStart = raw.IndexOf('{');
                var json = jsonStart >= 0 ? raw.Substring(jsonStart) : raw;

                var obj = Newtonsoft.Json.Linq.JObject.Parse(json);

                if (obj["Message"] != null)
                    return obj["Message"]!.ToString();

                if (obj["Errors"] is Newtonsoft.Json.Linq.JArray errors && errors.Count > 0)
                    return errors[0]?.ToString();

                if (obj["errors"] is Newtonsoft.Json.Linq.JObject validationErrors)
                {
                    foreach (var property in validationErrors.Properties())
                    {
                        if (property.Value is Newtonsoft.Json.Linq.JArray arr && arr.Count > 0)
                            return arr[0]?.ToString();
                    }
                }

                return "Unable to save attendance policy.";
            }
            catch
            {
                return string.IsNullOrWhiteSpace(raw) ? "Unable to save attendance policy." : raw;
            }
        }

        #endregion
    }
}
