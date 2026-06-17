using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using static APP.Helpers.EnumExtensions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace APP.Controllers
{
    [JwtAuthorize]
    public class CompanyController : Controller
    {
        private readonly IApiService _apiService;
        private string _tenantId;
        private string _userId;

        public CompanyController(IApiService apiService)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
        }

        // =====================================================
        // INDEX
        // =====================================================
        public async Task<IActionResult> Index()
        {
            var data = await _apiService.GetAsync<List<CompanyDto>>(
                "company"
            );

            return View(data);
        }

        // =====================================================
        // CREATE - GET
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();

            return View(new CompanyDto());
        }

        // =====================================================
        // CREATE - POST
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CompanyDto dto)
        {
            try
            {
                if (dto!=null)
                {
                    await LoadDropdowns();

                    #region Upload Image
                    // Upload Folder
                    string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/companylogo");

                    // Create Folder if not exists
                    if (!Directory.Exists(uploadFolder))
                        Directory.CreateDirectory(uploadFolder);

                    // Upload Image
                    if (dto.LogoFile != null && dto.LogoFile.Length > 0)
                    {
                        // Allowed Extensions
                        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };

                        // Get File Extension
                        var extension = Path.GetExtension(dto.LogoFile.FileName).ToLower();

                        // Validate Extension
                        if (!allowedExtensions.Contains(extension))
                        {
                            return BadRequest("Only JPG, JPEG and PNG files are allowed.");
                        }

                        
                        // Generate Unique File Name
                        string fileName = DateTime.Now.Hour + DateTime.Now.Minute+ DateTime.Now.Second+ DateTime.Now.Millisecond + extension;

                        // Full File Path
                        string filePath = Path.Combine(uploadFolder, fileName);

                        // Save File
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await dto.LogoFile.CopyToAsync(stream);
                        }

                        // Save Relative Path in DB
                        dto.Logo = "/companylogo/" + fileName;
                    }

                    #endregion

                    dto.CreatedBy = _userId;
                    dto.TenantId = _tenantId;

                    await _apiService.PostAsync<CompanyDto>(
                        "company",
                        dto
                    );

                    TempData["Success"] = "Company created successfully.";

                    return View(dto);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;

                await LoadDropdowns();

                return View(dto);
            }
            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // EDIT - GET
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var data = await _apiService.GetAsync<CompanyDto>(
                $"company/{id}"
            );

            if (data == null)
            {
                return NotFound();
            }

            await LoadDropdowns(data.CountryId,data.StateId);

            return View("Create",data);
        }

        // =====================================================
        // EDIT - POST
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CompanyDto dto)
        {
            try
            {
                if (dto != null && !string.IsNullOrEmpty(dto.Id))
                {
                    await LoadDropdowns(dto.CountryId, dto.StateId);

                    #region upload file image 

                    // Upload Folder
                    string uploadFolder = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot/companylogo");

                    // Create Folder if not exists
                    if (!Directory.Exists(uploadFolder))
                        Directory.CreateDirectory(uploadFolder);

                    // New Image Upload
                    if (dto.LogoFile != null && dto.LogoFile.Length > 0)
                    {
                        // Allowed Extensions
                        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };

                        // Extension
                        var extension = Path.GetExtension(dto.LogoFile.FileName).ToLower();

                        // Validate Extension
                        if (!allowedExtensions.Contains(extension))
                        {
                            return BadRequest("Only JPG, JPEG and PNG files are allowed.");
                        }

                       
                        // Delete Old Image
                        if (!string.IsNullOrEmpty(dto.Logo))
                        {
                            string oldFilePath = Path.Combine(
                                Directory.GetCurrentDirectory(),
                                "wwwroot",
                                dto.Logo.TrimStart('/'));

                            if (System.IO.File.Exists(oldFilePath))
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                        }

                        // Generate New File Name
                        string fileName = DateTime.Now.Hour + "" + DateTime.Now.Minute + "" + DateTime.Now.Second + "" + DateTime.Now.Millisecond + extension;

                        // File Path
                        string filePath = Path.Combine(uploadFolder, fileName);

                        // Save File
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await dto.LogoFile.CopyToAsync(stream);
                        }

                        // Update Logo Path
                        dto.Logo = "/companylogo/" + fileName;
                    }
                    else
                    {
                        dto.Logo = dto.Logo;
                    }
                    #endregion

                    dto.CreatedBy = _userId;
                    dto.TenantId = _tenantId;
                    dto.ModifiedOn = DateTime.UtcNow;
                    dto.ModifiedBy = _userId;

                    await _apiService.PutAsync<dynamic>($"company/{dto.Id}",dto);

                    TempData["Success"] = "Company updated successfully.";
                    return View("Create",dto);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;

                await LoadDropdowns();

                return View(dto);
            }
            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // DETAILS
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var data = await _apiService.GetAsync<CompanyDto>(
                $"company/{id}"
            );

            return View(data);
        }

        // =====================================================
        // DELETE
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _apiService.DeleteAsync(
                    $"company/{id}"
                );

                TempData["Success"] = "Company deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }


        [HttpGet]
        public async Task<JsonResult> GetStatesByCountry(string countryId)
        {
            var states = await _apiService
                .GetAsync<List<DropdownDto>>(
                    $"dropdown/state/{countryId}"
                );

            var result = states.Select(x => new
            {
                value = x.Value,
                text = x.Text
            });

            return Json(result);
        }

        [HttpGet]
        public async Task<JsonResult> GetCityByState(string stateId)
        {
            var states = await _apiService
                .GetAsync<List<DropdownDto>>(
                    $"dropdown/city/{stateId}"
                );

            var result = states.Select(x => new
            {
                value = x.Value,
                text = x.Text
            });

            return Json(result);
        }


        // =====================================================
        // LOAD DROPDOWNS
        // =====================================================
        private async Task LoadDropdowns(string? selectedCountryId = null, string? selectedStatedId = null)
        {
            // =========================
            // Country Dropdown
            // =========================
            var countries = await _apiService
                .GetAsync<List<DropdownDto>>("dropdown/country");

            ViewBag.CountryList = countries.Select(x => new SelectListItem
            {
                Value = x.Value,
                Text = x.Text
            }).ToList();

            // =========================
            // State Dropdown
            // =========================
            List<DropdownDto> states = new();

            if (!string.IsNullOrEmpty(selectedCountryId))
            {
                states = await _apiService
                    .GetAsync<List<DropdownDto>>(
                        $"dropdown/state/{selectedCountryId}"
                    );
            }

            ViewBag.StateList = states.Select(x => new SelectListItem
            {
                Value = x.Value,
                Text = x.Text
            }).ToList();


            // ==============================
            // CITY
            // ==============================
            List<DropdownDto> cities = new();
            if (!string.IsNullOrEmpty(selectedStatedId))
            {
                cities = await _apiService
                    .GetAsync<List<DropdownDto>>(
                        $"dropdown/city/{selectedStatedId}"
                    );
            }

            ViewBag.CityList = cities.Select(x => new SelectListItem
            {
                Value = x.Value,
                Text = x.Text
            }).ToList();

            // ==============================
            // OWNERSHIP TYPE
            // ==============================
            ViewBag.OwnershipTypeList =
                Enum.GetValues(typeof(BusinessOwnershipType))
                .Cast<BusinessOwnershipType>()
                .Select(x => new SelectListItem
                {
                    Value = ((int)x).ToString(),
                    Text = x.ToString()
                }).ToList();

            // ==============================
            // BUSINESS CATEGORY
            // ==============================
            ViewBag.BusinessCategoryList =
                Enum.GetValues(typeof(BusinessCategory))
                .Cast<BusinessCategory>()
                .Select(x => new SelectListItem
                {
                    Value = ((int)x).ToString(),
                    Text = x.ToString()
                }).ToList();
        }
    }
}
