using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using static APP.Helpers.EnumExtensions;

namespace APP.Controllers
{
    #region Employee Document Controller

    [JwtAuthorize]
    public class EmployeeDocumentController : Controller
    {
        private readonly IApiService _apiService;
        private string _tenantId;
        private string _userId;
        private readonly string? _employeeId;

        // Uploading, editing, deleting, and verifying documents is an
        // Admin/HR function only. A plain employee may view and download
        // their own documents but nothing more - enforced server-side here,
        // not just by hiding buttons in the view.
        private readonly bool _isAdmin;

        private static readonly string[] AllowedExtensions =
            { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx", ".xls", ".xlsx" };

        public EmployeeDocumentController(IApiService apiService)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
            _employeeId = SessionHelper.GetActiveEmployeeId;
            _isAdmin = SessionHelper.IsAdminRole();
        }

        #region Index / Details

        public async Task<IActionResult> Index()
        {
            ViewBag.IsAdmin = _isAdmin;
            var model = await _apiService
                .GetAsync<List<EmployeeDocumentDto>>("EmployeeDocument");
            return View(model);
        }

        /// <summary>
        /// Self-service "my documents" list - reuses the Index view but
        /// filters server-side to the logged-in user's own employee record,
        /// so one employee can never browse another's uploaded documents.
        /// </summary>
        public async Task<IActionResult> MyDocuments()
        {
            ViewBag.IsAdmin = false;

            if (string.IsNullOrEmpty(_employeeId))
            {
                ViewBag.NoEmployeeProfile = true;
                return View("Index", new List<EmployeeDocumentDto>());
            }

            var model = await _apiService
                .GetAsync<List<EmployeeDocumentDto>>("EmployeeDocument");

            var mine = (model ?? new List<EmployeeDocumentDto>())
                .Where(d => d.EmployeeId == _employeeId)
                .ToList();

            ViewBag.ListTitle = "My Documents";
            return View("Index", mine);
        }

        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return RedirectToAction(nameof(Index));

            var model = await _apiService
                .GetAsync<EmployeeDocumentDto>($"EmployeeDocument/{id}");

            if (model == null) return NotFound();

            // An employee may only view details of their own document.
            if (!_isAdmin && model.EmployeeId != _employeeId)
                return Forbid();

            ViewBag.IsAdmin = _isAdmin;
            return View(model);
        }

        #endregion

        #region Create

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (!_isAdmin)
            {
                TempData["GlobalError"] = "You don't have permission to upload documents.";
                return RedirectToAction(nameof(MyDocuments));
            }

            await BindDropdowns();
            return View(new EmployeeDocumentDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmployeeDocumentDto model)
        {
            if (!_isAdmin)
            {
                TempData["GlobalError"] = "You don't have permission to upload documents.";
                return RedirectToAction(nameof(MyDocuments));
            }

            if (model.UploadFile == null || model.UploadFile.Length == 0)
                ModelState.AddModelError(nameof(model.UploadFile), "Please select a file to upload.");

            if (!ModelState.IsValid)
            {
                await BindDropdowns();
                return View(model);
            }

            var saved = await SaveFileAsync(model.UploadFile);
            if (saved == null)
            {
                ModelState.AddModelError(nameof(model.UploadFile), "Invalid file type or size (max 5 MB).");
                await BindDropdowns();
                return View(model);
            }

            model.FileName = saved.Value.fileName;
            model.FilePath = saved.Value.filePath;
            model.FileExtension = saved.Value.extension;
            model.FileSize = saved.Value.size;

            model.CreatedBy = _userId;
            model.TenantId = _tenantId;
            model.UploadFile = null; // don't serialize the stream to the API

            await _apiService.PostAsync<EmployeeDocumentDto>("EmployeeDocument", model);

            TempData["Success"] = "Employee document created successfully.";
            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Edit

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (!_isAdmin)
            {
                TempData["GlobalError"] = "You don't have permission to edit documents.";
                return RedirectToAction(nameof(MyDocuments));
            }

            if (string.IsNullOrWhiteSpace(id))
                return RedirectToAction(nameof(Index));

            var model = await _apiService
                .GetAsync<EmployeeDocumentDto>($"EmployeeDocument/{id}");

            if (model == null) return NotFound();

            await BindDropdowns();
            return View("Create", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, EmployeeDocumentDto model)
        {
            if (!_isAdmin)
            {
                TempData["GlobalError"] = "You don't have permission to edit documents.";
                return RedirectToAction(nameof(MyDocuments));
            }

            if (!ModelState.IsValid)
            {
                await BindDropdowns();
                return View("Create", model);
            }

            // Replace the file only if a new one was uploaded
            if (model.UploadFile != null && model.UploadFile.Length > 0)
            {
                var saved = await SaveFileAsync(model.UploadFile);
                if (saved == null)
                {
                    ModelState.AddModelError(nameof(model.UploadFile), "Invalid file type or size (max 5 MB).");
                    await BindDropdowns();
                    return View("Create", model);
                }
                model.FileName = saved.Value.fileName;
                model.FilePath = saved.Value.filePath;
                model.FileExtension = saved.Value.extension;
                model.FileSize = saved.Value.size;
            }
            else
            {
                // Leave FilePath empty so the service keeps the existing file
                model.FilePath = null;
            }

            model.TenantId = _tenantId;
            model.ModifiedBy = _userId;
            model.ModifiedOn = DateTime.UtcNow;
            model.UploadFile = null;

            await _apiService.PutAsync<EmployeeDocumentDto>($"EmployeeDocument/{id}", model);

            TempData["Success"] = "Employee document updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Delete

        public async Task<IActionResult> Delete(string id)
        {
            if (!_isAdmin)
            {
                TempData["GlobalError"] = "You don't have permission to delete documents.";
                return RedirectToAction(nameof(MyDocuments));
            }

            await _apiService.DeleteAsync($"EmployeeDocument/{id}");
            TempData["Success"] = "Employee document deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Verify / UnVerify

        [HttpPost]
        public async Task<IActionResult> Verify(string id, string? remarks)
        {
            if (!_isAdmin)
            {
                TempData["GlobalError"] = "You don't have permission to verify documents.";
                return RedirectToAction(nameof(MyDocuments));
            }

            await _apiService.PostAsync<object>(
                $"EmployeeDocument/{id}/verify?verifiedBy={_userId}&remarks={Uri.EscapeDataString(remarks ?? "")}",
                new { });
            TempData["Success"] = "Document verified successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> UnVerify(string id)
        {
            if (!_isAdmin)
            {
                TempData["GlobalError"] = "You don't have permission to unverify documents.";
                return RedirectToAction(nameof(MyDocuments));
            }

            await _apiService.PostAsync<object>(
                $"EmployeeDocument/{id}/unverify", new { });
            TempData["Success"] = "Document unverified.";
            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Download

        public async Task<IActionResult> Download(string id)
        {
            var dto = await _apiService
                .GetAsync<EmployeeDocumentDto>($"EmployeeDocument/{id}");

            if (dto == null || string.IsNullOrWhiteSpace(dto.FilePath))
                return NotFound();

            // An employee may only download their own document.
            if (!_isAdmin && dto.EmployeeId != _employeeId)
                return Forbid();

            var file = Path.Combine(
                Directory.GetCurrentDirectory(), "wwwroot",
                dto.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

            if (!System.IO.File.Exists(file))
                return NotFound();

            return PhysicalFile(file, "application/octet-stream", dto.FileName ?? "document");
        }

        #endregion

        #region Expiry views

        public async Task<IActionResult> Expired()
        {
            if (!_isAdmin)
                return RedirectToAction(nameof(MyDocuments));

            ViewBag.IsAdmin = true;
            var model = await _apiService
                .GetAsync<List<EmployeeDocumentDto>>("EmployeeDocument/expired");
            ViewBag.ListTitle = "Expired Documents";
            return View("Index", model);
        }

        public async Task<IActionResult> Expiring(int days = 30)
        {
            if (!_isAdmin)
                return RedirectToAction(nameof(MyDocuments));

            ViewBag.IsAdmin = true;
            var model = await _apiService
                .GetAsync<List<EmployeeDocumentDto>>($"EmployeeDocument/expiring/{days}");
            ViewBag.ListTitle = $"Documents Expiring in {days} Days";
            return View("Index", model);
        }

        #endregion

        #region Helpers

        private async Task<(string fileName, string filePath, string extension, long size)?> SaveFileAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0) return null;

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension)) return null;
            if (file.Length > 5 * 1024 * 1024) return null;

            var folder = Path.Combine(
                Directory.GetCurrentDirectory(), "wwwroot", "Uploads", "EmployeeDocuments");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var stored = Guid.NewGuid().ToString() + extension;
            var fullPath = Path.Combine(folder, stored);

            using (var stream = new FileStream(fullPath, FileMode.Create))
                await file.CopyToAsync(stream);

            return (file.FileName, "/Uploads/EmployeeDocuments/" + stored, extension, file.Length);
        }

        private async Task BindDropdowns()
        {
            var employees = await _apiService
                .GetAsync<List<DropdownDto>>("dropdown/employee");

            ViewBag.EmployeeList = new SelectList(employees, "Value", "Text");

            ViewBag.DocumentTypes = Enum.GetValues(typeof(EmployeeDocumentType))
                .Cast<EmployeeDocumentType>()
                .Select(x => new SelectListItem
                {
                    Text = x.ToString(),
                    Value = ((int)x).ToString()
                })
                .ToList();
        }

        #endregion
    }

    #endregion
}
