using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace APP.Controllers
{
    #region Candidate Application Controller

    [JwtAuthorize]
    public class CandidateApplicationController : Controller
    {
        private readonly IApiService _apiService;
        private string _tenantId;
        private string _userId;

        public CandidateApplicationController(IApiService apiService)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _apiService
                .GetAsync<List<CandidateApplicationListDto>>("candidate-application");
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();
            return View(new CandidateApplicationDto());
        }

        [HttpPost]
        public async Task<IActionResult> Create(CandidateApplicationDto dto)
        {
            if (dto != null)
            {
                dto.TenantId = _tenantId;
                dto.CreatedBy = _userId;

                await _apiService.PostAsync<dynamic>("candidate-application", dto);

                TempData["Success"] = "Application saved successfully.";
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdowns();
            return View(dto);
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var data = await _apiService
                .GetAsync<CandidateApplicationListDto>($"candidate-application/{id}");
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var data = await _apiService
                .GetAsync<CandidateApplicationDto>($"candidate-application/edit/{id}");
            await LoadDropdowns();
            return View("Create", data);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string id, CandidateApplicationDto dto)
        {
            if (dto != null)
            {
                dto.TenantId = _tenantId;
                dto.ModifiedBy = _userId;
                dto.ModifiedOn = DateTime.UtcNow;

                await _apiService.PutAsync<dynamic>($"candidate-application/{id}", dto);

                TempData["Success"] = "Application updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdowns();
            return View("Create", dto);
        }

        [HttpPost]
        public async Task<IActionResult> ChangeStage(string id, int status)
        {
            await _apiService.PutAsync<dynamic>(
                $"candidate-application/stage/{id}?status={status}&userId={_userId}", new { });
            TempData["Success"] = "Stage updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(string id)
        {
            await _apiService.DeleteAsync($"candidate-application/{id}");
            return RedirectToAction(nameof(Index));
        }

        #region Load Dropdowns

        private async Task LoadDropdowns()
        {
            var candidates = await _apiService.GetAsync<List<DropdownDto>>("dropdown/candidate");
            ViewBag.CandidateList = new SelectList(candidates, "Value", "Text");

            var jobs = await _apiService.GetAsync<List<DropdownDto>>("dropdown/job-opening");
            ViewBag.JobList = new SelectList(jobs, "Value", "Text");

            ViewBag.StageList = new List<SelectListItem>
            {
                new() { Value = "1", Text = "Applied" },
                new() { Value = "2", Text = "Shortlisted" },
                new() { Value = "3", Text = "Interview" },
                new() { Value = "4", Text = "Selected" },
                new() { Value = "5", Text = "Rejected" }
            };
        }

        #endregion
    }

    #endregion
}
