using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace APP.Controllers
{
    public class TenantController : Controller
    {
        private readonly IApiService _apiService;
        private string _userId;

        public TenantController(IApiService apiService)
        {
            _apiService = apiService;
            _userId = SessionHelper.GetActiveUserId;
        }
        #region Index

        public async Task<IActionResult> Index()
        {
            var data = await _apiService
                .GetAsync<List<TenantDto>>("Tenant");

            return View(data);
        }

        #endregion

        #region Details

        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var data = await _apiService
                .GetAsync<TenantDto>($"Tenant/{id}");

            return View(data);
        }

        #endregion

        #region Create

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();

            return View(new TenantDto
            {
                SubscriptionStartDate = DateTime.Today,
                SubscriptionEndDate = DateTime.Today.AddYears(1),
                IsActive = true
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TenantDto model)
        {
            if (model==null)
            {
                await LoadDropdowns(
                    model.CountryId,
                    model.StateId);

                return View(model);
            }

            model.CreatedBy = _userId;

            #region upload file image 

            // Upload Folder
            string uploadFolder = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot/Tenantlogo");

            // Create Folder if not exists
            if (!Directory.Exists(uploadFolder))
                Directory.CreateDirectory(uploadFolder);

            // New Image Upload
            if (model.LogoFile != null && model.LogoFile.Length > 0)
            {
                // Allowed Extensions
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };

                // Extension
                var extension = Path.GetExtension(model.LogoFile.FileName).ToLower();

                // Validate Extension
                if (!allowedExtensions.Contains(extension))
                {
                    return BadRequest("Only JPG, JPEG and PNG files are allowed.");
                }


                // Delete Old Image
                if (!string.IsNullOrEmpty(model.Logo))
                {
                    string oldFilePath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        model.Logo.TrimStart('/'));

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
                    await model.LogoFile.CopyToAsync(stream);
                }

                // Update Logo Path
                model.Logo = "/Tenantlogo/" + fileName;
            }
            #endregion

            var result =
                await _apiService.PostAsync<TenantDto>(
                    "Tenant",
                    model);

            if (!string.IsNullOrEmpty(result.Id))
            {
                TempData["Success"] =
                    "Tenant created successfully.";

                return RedirectToAction(nameof(Index));
            }

            TempData["Error"] =
                "Unable to create tenant.";

            await LoadDropdowns(
                model.CountryId,
                model.StateId);

            return View(model);
        }

        #endregion

        #region Edit

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var data =
                await _apiService.GetAsync<TenantDto>(
                    $"Tenant/{id}");

            await LoadDropdowns(
                data.CountryId,
                data.StateId);

            return View("Create",data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            string id,
            TenantDto model)
        {
            if (model == null)
            {
                await LoadDropdowns(
                    model.CountryId,
                    model.StateId);

                return View(model);
            }

            model.ModifiedBy = _userId;
            model.ModifiedOn = DateTime.Now;

            #region upload file image 

            // Upload Folder
            string uploadFolder = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot/Tenantlogo");

            // Create Folder if not exists
            if (!Directory.Exists(uploadFolder))
                Directory.CreateDirectory(uploadFolder);

            // New Image Upload
            if (model.LogoFile != null && model.LogoFile.Length > 0)
            {
                // Allowed Extensions
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };

                // Extension
                var extension = Path.GetExtension(model.LogoFile.FileName).ToLower();

                // Validate Extension
                if (!allowedExtensions.Contains(extension))
                {
                    return BadRequest("Only JPG, JPEG and PNG files are allowed.");
                }


                // Delete Old Image
                if (!string.IsNullOrEmpty(model.Logo))
                {
                    string oldFilePath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        model.Logo.TrimStart('/'));

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
                    await model.LogoFile.CopyToAsync(stream);
                }

                // Update Logo Path
                model.Logo = "/Tenantlogo/" + fileName;
            }
            else
            {
                model.Logo = model.Logo;
            }
            #endregion

            var result =
                await _apiService.PutAsync<TenantDto>(
                    $"Tenant/{id}",
                    model);

            if (!string.IsNullOrEmpty(result.Id))
            {
                TempData["Success"] =
                    "Tenant updated successfully.";

                return RedirectToAction(nameof(Index));
            }

            TempData["Error"] =
                "Unable to update tenant.";

            await LoadDropdowns(
                model.CountryId,
                model.StateId);

            return View(model);
        }

        #endregion

        #region Delete

        [HttpGet]
        public async Task<IActionResult> Delete(string id)
        {
            var result =
                await _apiService.DeleteAsync(
                    $"Tenant/{id}");

            if (result)
            {
                TempData["Success"] =
                "Tenant deleted successfully.";
            }

            TempData["Error"] =
               "Unable to delete tenant.";

            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Activate / Deactivate

        [HttpPost]
        public async Task<IActionResult> Activate(string id)
        {
            var result =
                await _apiService.PostAsync<bool>(
                    $"Tenant/activate/{id}",
                    new { });

            return Json(result);
        }

        [HttpPost]
        public async Task<IActionResult> Deactivate(string id)
        {
            var result =
                await _apiService.PostAsync<bool>(
                    $"Tenant/deactivate/{id}",
                    new { });

            return Json(result);
        }

        #endregion

        #region Cascading Dropdowns

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

        #endregion


        #region Load Dropdowns

        private async Task LoadDropdowns(string? countryId = null, string? stateId = null)
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

            if (!string.IsNullOrEmpty(countryId))
            {
                states = await _apiService
                    .GetAsync<List<DropdownDto>>(
                        $"dropdown/state/{countryId}"
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
            if (!string.IsNullOrEmpty(stateId))
            {
                cities = await _apiService
                    .GetAsync<List<DropdownDto>>(
                        $"dropdown/city/{stateId}"
                    );
            }

            ViewBag.CityList = cities.Select(x => new SelectListItem
            {
                Value = x.Value,
                Text = x.Text
            }).ToList();

        }
        #endregion
    }
}
