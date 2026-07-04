using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace APP.Controllers
{
    #region Candidate Controller

    [JwtAuthorize]
    public class CandidateController : Controller
    {
        private readonly IApiService _apiService;
        private string _tenantId;
        private string _userId;

        private static readonly string[] AllowedResumeExtensions =
            { ".pdf", ".doc", ".docx" };

        public CandidateController(IApiService apiService)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _apiService.GetAsync<List<CandidateListDto>>("candidate");
            return View(data);
        }

        [HttpGet]
        public IActionResult Create()
        {
            LoadStatusList();
            return View(new CandidateDto());
        }

        [HttpPost]
        public async Task<IActionResult> Create(CandidateDto dto, IFormFile? resumeFile)
        {
            if (dto != null)
            {
                var resumePath = await SaveResumeAsync(resumeFile);
                if (!string.IsNullOrEmpty(resumePath))
                    dto.ResumeUrl = resumePath;

                dto.TenantId = _tenantId;
                dto.CreatedBy = _userId;

                await _apiService.PostAsync<dynamic>("candidate", dto);

                TempData["Success"] = "Candidate saved successfully.";
                return RedirectToAction(nameof(Index));
            }

            LoadStatusList();
            return View(dto);
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var data = await _apiService.GetAsync<CandidateDto>($"candidate/{id}");
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var data = await _apiService.GetAsync<CandidateDto>($"candidate/{id}");
            LoadStatusList();
            return View("Create", data);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string id, CandidateDto dto, IFormFile? resumeFile)
        {
            if (dto != null)
            {
                var resumePath = await SaveResumeAsync(resumeFile);
                if (!string.IsNullOrEmpty(resumePath))
                    dto.ResumeUrl = resumePath;

                dto.TenantId = _tenantId;
                dto.ModifiedBy = _userId;
                dto.ModifiedOn = DateTime.UtcNow;

                await _apiService.PutAsync<dynamic>($"candidate/{id}", dto);

                TempData["Success"] = "Candidate updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            LoadStatusList();
            return View("Create", dto);
        }

        public async Task<IActionResult> Delete(string id)
        {
            await _apiService.DeleteAsync($"candidate/{id}");
            return RedirectToAction(nameof(Index));
        }

        #region Helpers

        private async Task<string?> SaveResumeAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return null;

            var extension = Path.GetExtension(file.FileName).ToLower();
            if (!AllowedResumeExtensions.Contains(extension))
            {
                TempData["Error"] = "Resume must be a PDF or Word document.";
                return null;
            }
            if (file.Length > (5 * 1024 * 1024))
            {
                TempData["Error"] = "Maximum resume size is 5 MB.";
                return null;
            }

            var folder = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot", "Uploads", "Resumes");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var fileName = Guid.NewGuid().ToString() + extension;
            var fullPath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return "/Uploads/Resumes/" + fileName;
        }

        private void LoadStatusList()
        {
            ViewBag.StatusList = new List<SelectListItem>
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
