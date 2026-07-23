using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using static APP.Helpers.EnumExtensions;

namespace APP.Controllers
{
    // Separate from OnboardingController on purpose - the menu seeding in
    // Infrastructure/Data/DbSeeder.cs (AppFeatureConstants.ONBOARDING_TEMPLATE)
    // points at ControllerName = "OnboardingTemplate" / ActionName = "Index",
    // which the menu API turns into the literal Url "/OnboardingTemplate/Index"
    // (see Application/Services/Masters/AppFeatureService.cs). The class name
    // here has to match that exactly for the sidebar link to resolve.
    [JwtAuthorize]
    public class OnboardingTemplateController : Controller
    {
        private readonly IApiService _apiService;
        private readonly string _tenantId;
        private readonly string _userId;

        public OnboardingTemplateController(IApiService apiService)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var data = await _apiService.GetAsync<List<OnboardingChecklistTemplateItemDto>>("onboarding/templates")
                ?? new();

            return View(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UpsertOnboardingTemplateItemDto model)
        {
            try
            {
                await _apiService.PostAsync<UpsertOnboardingTemplateItemDto, OnboardingChecklistTemplateItemDto>(
                    "onboarding/templates", model);

                TempData["Success"] = "Checklist template item added successfully.";
            }
            catch (ApiException ex)
            {
                TempData["Error"] = GetErrorMessage(ex.ResponseContent);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, UpsertOnboardingTemplateItemDto model)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            try
            {
                await _apiService.PutAsync<UpsertOnboardingTemplateItemDto, OnboardingChecklistTemplateItemDto>(
                    $"onboarding/templates/{id}", model);

                TempData["Success"] = "Checklist template item updated successfully.";
            }
            catch (ApiException ex)
            {
                TempData["Error"] = GetErrorMessage(ex.ResponseContent);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            if (!string.IsNullOrWhiteSpace(id))
            {
                var result = await _apiService.DeleteAsync($"onboarding/templates/{id}");

                TempData[result ? "Success" : "Error"] = result
                    ? "Checklist template item deleted successfully."
                    : "Unable to delete checklist template item.";
            }

            return RedirectToAction(nameof(Index));
        }

        private string GetErrorMessage(string json)
        {
            try
            {
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

                return "Unable to save checklist template item.";
            }
            catch
            {
                return "Unable to save checklist template item.";
            }
        }
    }
}
