using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace APP.Controllers
{
    [JwtAuthorize]
    public class EmployeeController : Controller
    {
        private readonly IApiService _apiService;
        private  string _tenantId;
        private  string _userId;

        public EmployeeController(IApiService apiService)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
        }

        #region Index

        public async Task<IActionResult> Index()
        {
            var data = await _apiService
                .GetAsync<List<EmployeeListDto>>($"Employee/employee-list");

            return View(data);
        }

        #endregion

        #region Create GET

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();

            return View(new EmployeeDto
            {
                JoiningDate = DateTime.UtcNow
            });
        }

        #endregion

        #region Create POST

        [HttpPost]
        public async Task<IActionResult> Create(EmployeeDto dto)
        {
            try
            {
                await LoadDropdowns();

                dto.TenantId = _tenantId;
                dto.CreatedBy = _userId;

                #region Upload Image
                // Upload Folder
                string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/employee");

                // Create Folder if not exists
                if (!Directory.Exists(uploadFolder))
                    Directory.CreateDirectory(uploadFolder);

                // Upload Image
                if (dto.UploadImage != null && dto.UploadImage.Length > 0)
                {
                    // Allowed Extensions
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };

                    // Get File Extension
                    var extension = Path.GetExtension(dto.UploadImage.FileName).ToLower();

                    // Validate Extension
                    if (!allowedExtensions.Contains(extension))
                    {
                        return BadRequest("Only JPG, JPEG and PNG files are allowed.");
                    }


                    // Generate Unique File Name
                    string fileName = DateTime.Now.Hour + DateTime.Now.Minute + DateTime.Now.Second + DateTime.Now.Millisecond + extension;

                    // Full File Path
                    string filePath = Path.Combine(uploadFolder, fileName);

                    // Save File
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await dto.UploadImage.CopyToAsync(stream);
                    }

                    // Save Relative Path in DB
                    dto.FilePath = "/employee/" + fileName;
                }

                #endregion

                var response =
                    await _apiService.PostAsync<EmployeeDto, ApiResponse<EmployeeDto>>
                    (
                        $"Employee/add-employee",
                        dto
                    );

                if (response.Success)
                {
                    TempData["Success"] = response.Message;

                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError(
                    "",
                    response.Message);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    "",
                    ex.Message);
            }

            return View(dto);

            //if (!ModelState.IsValid)
            //{
            //    await LoadDropdowns();
               
            //    dto.TenantId = _tenantId;
            //    dto.CreatedBy = _userId;

            //    await _apiService
            //        .PostAsync<dynamic>($"Employee/add-employee", dto);

            //    TempData["Success"] = "Record created successfully.";

            //    /*
            //        TempData["Warning"] = "Please verify details.";
            //        TempData["Info"] = "New update available.";
            //        TempData["Error"] = "Something went wrong.";
            //     */

            //    return View(dto);
            //}

            //return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Edit GET

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var data = await _apiService
                .GetAsync<EmployeeDto>($"Employee/get-employee-detail/{id}");

            var userData = await _apiService.GetAsync<UserListDto>($"auth/user-detailsby-emp/{id}");

            data.UserId=userData.Id;
            data.RoleId=userData.RoleId;
            data.EmailConfirmed=userData.EmailConfirmed;
            data.PhoneConfirmed =userData.PhoneConfirmed;

            if (data == null)
                return NotFound();

            await LoadDropdowns(data.CompanyId,data.DepartmentId);

            return View("Create", data);
        }

        #endregion

        #region Edit POST

        [HttpPost]
        public async Task<IActionResult> Edit(EmployeeDto dto)
        {
            if (dto!=null)
            {
                dto.TenantId = _tenantId;
                dto.CreatedBy = _userId;
                dto.ModifiedBy = _userId;
                dto.ModifiedOn = DateTime.UtcNow;

                await LoadDropdowns(dto.CompanyId, dto.DepartmentId);

                #region upload file image 

                // Upload Folder
                string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(),"wwwroot/employee");

                // Create Folder if not exists
                if (!Directory.Exists(uploadFolder))
                    Directory.CreateDirectory(uploadFolder);

                // New Image Upload
                if (dto.UploadImage != null && dto.UploadImage.Length > 0)
                {
                    // Allowed Extensions
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };

                    // Extension
                    var extension = Path.GetExtension(dto.UploadImage.FileName).ToLower();

                    // Validate Extension
                    if (!allowedExtensions.Contains(extension))
                    {
                        return BadRequest("Only JPG, JPEG and PNG files are allowed.");
                    }


                    // Delete Old Image
                    if (!string.IsNullOrEmpty(dto.FilePath))
                    {
                        string oldFilePath = Path.Combine(
                            Directory.GetCurrentDirectory(),
                            "wwwroot",
                            dto.FilePath.TrimStart('/'));

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
                        await dto.UploadImage.CopyToAsync(stream);
                    }

                    // Update Logo Path
                    dto.FilePath = "/employee/" + fileName;
                }
                else
                {
                    dto.FilePath = dto.FilePath;
                }
                #endregion

                var response = await _apiService.PutAsync<EmployeeDto, ApiResponse<EmployeeDto>>
                    (
                        $"Employee/update-employee",
                        dto
                    );

                if (response.Success)
                {
                    TempData["Success"] = "Employee updated successfully.";

                    return View("Create", dto);
                }
            }
            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Details

        public async Task<IActionResult> Details(string id)
        {
            var data = await _apiService
                .GetAsync<EmployeeListDto>($"Employee/get-employee-detail/{id}");

            if (data == null)
                return NotFound();

            return View(data);
        }

        #endregion

        #region Delete

        public async Task<IActionResult> Delete(string id)
        {
            await _apiService
                .DeleteAsync($"Employee/employee/{id}");

            return RedirectToAction(nameof(Index));
        }

        #endregion

        [HttpGet]
        public async Task<JsonResult> GetBranchByCompanyId(string companyId)
        {
            var branches = await _apiService
                .GetAsync<List<DropdownDto>>(
                    $"dropdown/branch/{companyId}"
                );

            var result = branches.Select(x => new
            {
                value = x.Value,
                text = x.Text
            });

            return Json(result);
        }

        [HttpGet]
        public async Task<JsonResult> GetDesignationByDepartmentId(string departmentId)
        {
            var designations = await _apiService
                .GetAsync<List<DropdownDto>>(
                    $"dropdown/designantion/{departmentId}"
                );

            var result = designations.Select(x => new
            {
                value = x.Value,
                text = x.Text
            });

            return Json(result);
        }


        #region LoadDropdowns

        private async Task LoadDropdowns(
            string? companyId = null,
            string? deptId = null)
        {
            // Company
            var companies = await _apiService
                .GetAsync<List<DropdownDto>>("dropdown/company");

            ViewBag.CompanyList = new SelectList(
                companies,
                "Value",
                "Text"
            );

            // Branch
            List<DropdownDto> branches = new();

            if (!string.IsNullOrEmpty(companyId))
            {
                branches = await _apiService
                    .GetAsync<List<DropdownDto>>
                    ($"dropdown/branch/{companyId}");
            }

            ViewBag.BranchList = new SelectList(
                branches,
                "Value",
                "Text"
            );

            // Department
            var departments = await _apiService
                .GetAsync<List<DropdownDto>>("dropdown/department");

            ViewBag.DepartmentList = new SelectList(
                departments,
                "Value",
                "Text"
            );

            // Designation
            List<DropdownDto> designations = new();

            if (!string.IsNullOrEmpty(deptId))
            {
                designations = await _apiService
                    .GetAsync<List<DropdownDto>>
                    ($"dropdown/designation/{deptId}");
            }

            ViewBag.DesignationList = new SelectList(
                designations,
                "Value",
                "Text"
            );

            // Department
            var reportingManagers = await _apiService
                .GetAsync<List<DropdownDto>>("dropdown/employee");

            ViewBag.ReportingManagerList = new SelectList(
                reportingManagers,
                "Value",
                "Text"
            );

            // Department
            var roles = await _apiService
                .GetAsync<List<DropdownDto>>("dropdown/role");

            ViewBag.RoleList = new SelectList(
                roles,
                "Value",
                "Text"
            );

            // Shift
            var shifts = await _apiService
                .GetAsync<List<DropdownDto>>("dropdown/default-shift");

            ViewBag.ShiftList = new SelectList(
                shifts,
                "Value",
                "Text"
            ); ;
        }

        #endregion
    }
}
