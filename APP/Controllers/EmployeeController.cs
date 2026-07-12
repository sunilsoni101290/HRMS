using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using ClosedXML.Excel;
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

        // Bulk import creates many employee accounts at once - restricted
        // to Admin/HR, same as the rest of employee management.
        private readonly bool _isAdmin;

        // The employee record linked to whoever is logged in - used so a
        // self-service user viewing/editing "My Profile" is always locked
        // to their own record, never one picked from the URL.
        private readonly string? _employeeId;

        public EmployeeController(IApiService apiService)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
            _isAdmin = SessionHelper.IsAdminRole();
            _employeeId = SessionHelper.GetActiveEmployeeId;
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

                #region Upload Image
                // Upload Folder
                string uploadPassportFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/passportDocument");

                // Create Folder if not exists
                if (!Directory.Exists(uploadPassportFolder))
                    Directory.CreateDirectory(uploadPassportFolder);

                // Upload Document
                if (dto.UploadPassport != null && dto.UploadPassport.Length > 0)
                {
                    
                    // Get File Extension
                    var extension = Path.GetExtension(dto.UploadPassport.FileName).ToLower();

                   
                    // Generate Unique File Name
                    string fileName = DateTime.Now.Hour + DateTime.Now.Minute + DateTime.Now.Second + DateTime.Now.Millisecond + extension;

                    // Full File Path
                    string filePath = Path.Combine(uploadPassportFolder, fileName);

                    // Save File
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await dto.UploadPassport.CopyToAsync(stream);
                    }

                    // Save Relative Path in DB
                    dto.PassportFilePath = "/passportDocument/" + fileName;
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
            // Self-service "Edit My Profile" must always target the
            // logged-in user's own employee record - never trust the id
            // segment of the URL for a non-admin caller.
            if (!_isAdmin)
            {
                if (string.IsNullOrEmpty(_employeeId))
                    return Forbid();

                id = _employeeId;
            }

            EmployeeDto data;

            try
            {
                data = await _apiService.GetAsync<EmployeeDto>($"Employee/get-employee-detail/{id}");
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }

            if (data == null)
                return NotFound();

            try
            {
                var response = await _apiService.GetAsync<ApiResponse<UserListDto>>
                (
                    $"auth/user-detailsby-emp/{id}"
                );

                if (response.Success && response.Data != null)
                {
                    data.UserId = response.Data.Id;
                    data.RoleId = response.Data.RoleId;
                    data.EmailConfirmed = response.Data.EmailConfirmed;
                    data.PhoneConfirmed = response.Data.PhoneConfirmed;
                }
            }
            catch (KeyNotFoundException)
            {
                // No linked login account for this employee - not fatal,
                // just leave the User/Role fields blank.
            }

            await LoadDropdowns(data.CompanyId, data.DepartmentId);

            // Drives the "only specific fields are editable" restriction in
            // the shared Create/Edit view - a self-service user can see
            // their whole profile but can only submit changes to their own
            // contact details. Enforced again server-side in the POST, not
            // just by disabling inputs here.
            ViewBag.IsAdmin = _isAdmin;

            return View("Create", data);
        }

        #endregion

        #region Edit POST

        // Fields a self-service user is allowed to change about their own
        // profile - everything else (role, company/branch/department/
        // designation, reporting manager, shift, employment dates, KYC,
        // passport, verification flags) is admin-only.
        private static void ApplySelfServiceEditableFields(EmployeeDto target, EmployeeDto posted)
        {
            target.Phone = posted.Phone;
            target.EmergencyContact = posted.EmergencyContact;
            target.Address = posted.Address;
            target.Pincode = posted.Pincode;
            target.UploadImage = posted.UploadImage;
        }

        [HttpPost]
        public async Task<IActionResult> Edit(EmployeeDto dto)
        {
            if (dto!=null)
            {
                // Self-service can only ever update their own record, and
                // only the whitelisted contact fields above - never trust
                // the posted Id, and never let the rest of a crafted
                // request (role, org placement, employment info, KYC,
                // passport, verification flags) through even if the UI
                // wouldn't normally show those fields to this user.
                if (!_isAdmin)
                {
                    if (string.IsNullOrEmpty(_employeeId))
                        return Forbid();

                    EmployeeDto existing;

                    try
                    {
                        existing = await _apiService.GetAsync<EmployeeDto>($"Employee/get-employee-detail/{_employeeId}");
                    }
                    catch (KeyNotFoundException)
                    {
                        return NotFound();
                    }

                    if (existing == null)
                        return NotFound();

                    ApplySelfServiceEditableFields(existing, dto);
                    dto = existing;
                    dto.Id = _employeeId;
                }

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

                #region upload Passport Document 

                // Upload Passport Folder
                string uploadPassporFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/passportDocument");

                // Create Folder if not exists
                if (!Directory.Exists(uploadPassporFolder))
                    Directory.CreateDirectory(uploadPassporFolder);

                // New Image Upload
                if (dto.UploadPassport != null && dto.UploadPassport.Length > 0)
                {
                    // Extension
                    var extension = Path.GetExtension(dto.UploadPassport.FileName).ToLower();

                    // Delete Old Image
                    if (!string.IsNullOrEmpty(dto.FilePath))
                    {
                        string oldFilePath = Path.Combine(Directory.GetCurrentDirectory(),"wwwroot",dto.FilePath.TrimStart('/'));

                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    // Generate New File Name
                    string fileName = DateTime.Now.Hour + "" + DateTime.Now.Minute + "" + DateTime.Now.Second + "" + DateTime.Now.Millisecond + extension;

                    // File Path
                    string filePath = Path.Combine(uploadPassporFolder, fileName);

                    // Save File
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await dto.UploadPassport.CopyToAsync(stream);
                    }

                    // Update Passport Path
                    dto.PassportFilePath = "/passportDocument/" + fileName;
                }
                else
                {
                    dto.PassportFilePath = dto.PassportFilePath;
                }
                #endregion

                var response = await _apiService.PutAsync<EmployeeDto, ApiResponse<EmployeeDto>>
                    (
                        $"Employee/update-employee",
                        dto
                    );

                ViewBag.IsAdmin = _isAdmin;

                if (response.Success)
                {
                    TempData["Success"] = "Employee updated successfully.";

                    return View("Create", dto);
                }

                TempData["Error"] = response.Message ?? "Unable to update employee.";

                if (!_isAdmin)
                    return View("Create", dto);
            }

            // The org-wide list is admin/HR only - a self-service failure
            // (or a null model, which shouldn't normally happen) must never
            // fall through to it.
            return _isAdmin
                ? RedirectToAction(nameof(Index))
                : RedirectToAction(nameof(Details), new { id = _employeeId });
        }

        #endregion

        #region Details

        public async Task<IActionResult> Details(string id)
        {
            // Self-service users only ever reach this as "My Profile" - a
            // plain employee must not be able to view a colleague's full
            // record just by editing the id in the URL.
            if (!_isAdmin && id != _employeeId)
                return Forbid();

            var data = await _apiService
                .GetAsync<EmployeeListDto>($"Employee/get-employee-detail/{id}");

            if (data == null)
                return NotFound();

            ViewBag.IsAdmin = _isAdmin;

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

        #region View and Download Passport
        public async Task<IActionResult> ViewPassport(string employeeId)
        {

            var employee = await _apiService
                .GetAsync<EmployeeDto>($"Employee/get-employee-detail/{employeeId}");

            if (employee == null || string.IsNullOrEmpty(employee.PassportFilePath))
                return NotFound();

            var filePath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "wwwroot",
            employee.PassportFilePath.TrimStart('/'));

            if (!System.IO.File.Exists(filePath))
                return NotFound();

            var extension = Path.GetExtension(filePath).ToLower();

            var contentType = extension switch
            {
                ".pdf" => "application/pdf",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };

            return PhysicalFile(filePath, contentType);
        }

        public async Task<IActionResult> DownloadPassport(string employeeId)
        {
            var employee = await _apiService
                .GetAsync<EmployeeDto>($"Employee/get-employee-detail/{employeeId}");

            if (employee == null || string.IsNullOrEmpty(employee.PassportFilePath))
                return NotFound();

            var filePath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "wwwroot",
            employee.PassportFilePath.TrimStart('/'));

            if (!System.IO.File.Exists(filePath))
                return NotFound();

            var bytes = await System.IO.File.ReadAllBytesAsync(filePath);

            var extension = Path.GetExtension(employee.PassportFilePath);

            var fullName = string.Join(" ",
            new[] { employee.FirstName, employee.LastName }
            .Where(x => !string.IsNullOrWhiteSpace(x)));

            var downloadFileName = $"{fullName}_Passport{extension}";

            return File(
                bytes,
                "application/octet-stream",
                downloadFileName);
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
                    $"dropdown/designation/{departmentId}"
                );

            var result = designations.Select(x => new
            {
                value = x.Value,
                text = x.Text
            });

            return Json(result);
        }


        #region Import

        [HttpGet]
        public IActionResult Import()
        {
            if (!_isAdmin)
                return Forbid();

            return View(new EmployeeImportResultDto());
        }

        // Builds a ready-to-fill .xlsx: an "Employees" sheet with headers +
        // one sample row, and a "Reference Data" sheet listing this tenant's
        // real Company/Department/Role names (and the fixed enum choices)
        // so the client knows exactly what text to type in each column.
        [HttpGet]
        public async Task<IActionResult> DownloadImportTemplate()
        {
            if (!_isAdmin)
                return Forbid();

            var companies = await _apiService.GetAsync<List<DropdownDto>>("dropdown/company") ?? new();
            var departments = await _apiService.GetAsync<List<DropdownDto>>("dropdown/department") ?? new();
            var designations = await _apiService.GetAsync<List<DesignationListDto>>("designation") ?? new();
            var roles = await _apiService.GetAsync<List<DropdownDto>>("dropdown/role") ?? new();

            using var workbook = new XLWorkbook();

            // ----- Employees sheet -----
            var sheet = workbook.Worksheets.Add("Employees");

            string[] headers =
            {
                "First Name*", "Last Name", "Employee Code*", "Email", "Phone*",
                "Gender*", "Marital Status*", "Date Of Birth (yyyy-mm-dd)",
                "Address*", "Pincode*", "Company*", "Branch", "Department*",
                "Designation*", "Role*", "Reporting Manager (Employee Code)",
                "Employment Type*", "Joining Date* (yyyy-mm-dd)",
                "PAN Number", "Aadhaar Number", "Emergency Contact"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = sheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1B2A4A");
            }

            var sample = new[]
            {
                "John", "Doe", "EMP1001", "john.doe@example.com", "9876543210",
                "Male", "Married", "1995-06-15",
                "12, MG Road, Pune", "411001",
                companies.FirstOrDefault()?.Text ?? "Company Name",
                "",
                departments.FirstOrDefault()?.Text ?? "Department Name",
                "Designation Name",
                roles.FirstOrDefault()?.Text ?? "Role Name",
                "", "Permanent", DateTime.Today.ToString("yyyy-MM-dd"),
                "ABCDE1234F", "123456789012", "9123456780"
            };

            for (int i = 0; i < sample.Length; i++)
                sheet.Cell(2, i + 1).Value = sample[i];

            sheet.SheetView.FreezeRows(1);
            sheet.Columns().AdjustToContents();

            // ----- Reference Data sheet -----
            var refSheet = workbook.Worksheets.Add("Reference Data");

            void WriteList(int col, string title, IEnumerable<string> values)
            {
                var header = refSheet.Cell(1, col);
                header.Value = title;
                header.Style.Font.Bold = true;

                int row = 2;

                foreach (var v in values)
                {
                    refSheet.Cell(row, col).Value = v;
                    row++;
                }
            }

            WriteList(1, "Company", companies.Select(x => x.Text));
            WriteList(2, "Department", departments.Select(x => x.Text));
            WriteList(3, "Role", roles.Select(x => x.Text));
            WriteList(4, "Gender", Enum.GetNames(typeof(EnumExtensions.Gender)));
            WriteList(5, "Marital Status", Enum.GetNames(typeof(EnumExtensions.MaritalStatus)));
            WriteList(6, "Employment Type", Enum.GetNames(typeof(EnumExtensions.EmploymentType)));
            WriteList(7, "Designation", designations.Select(x => $"{x.Name} ({x.DepartmentName})"));

            refSheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Employee_Import_Template.xlsx");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (!_isAdmin)
                return Forbid();

            var result = new EmployeeImportResultDto();

            if (file == null || file.Length == 0)
            {
                TempData["GlobalError"] = "Please choose an Excel file to import.";
                return View(result);
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (extension != ".xlsx" && extension != ".xls")
            {
                TempData["GlobalError"] = "Only .xlsx or .xls files are supported.";
                return View(result);
            }

            // Reference lookups fetched once - names typed in the sheet are
            // resolved case-insensitively against this tenant's real
            // records, so the client never has to know internal IDs.
            var companies = await _apiService.GetAsync<List<DropdownDto>>("dropdown/company") ?? new();
            var companyMap = companies.ToDictionary(x => x.Text.Trim(), x => x.Value, StringComparer.OrdinalIgnoreCase);

            var departments = await _apiService.GetAsync<List<DropdownDto>>("dropdown/department") ?? new();
            var departmentMap = departments.ToDictionary(x => x.Text.Trim(), x => x.Value, StringComparer.OrdinalIgnoreCase);

            var roles = await _apiService.GetAsync<List<DropdownDto>>("dropdown/role") ?? new();
            var roleMap = roles.ToDictionary(x => x.Text.Trim(), x => x.Value, StringComparer.OrdinalIgnoreCase);

            var existingEmployees = await _apiService.GetAsync<List<EmployeeListDto>>("Employee/employee-list") ?? new();
            var employeeCodeMap = existingEmployees
                .Where(x => !string.IsNullOrWhiteSpace(x.EmployeeCode))
                .GroupBy(x => x.EmployeeCode.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

            // Branch/Designation lists are scoped to a company/department,
            // so they're only fetched once per company/department actually
            // seen in the file, not once per row.
            var branchCache = new Dictionary<string, Dictionary<string, string>>();
            var designationCache = new Dictionary<string, Dictionary<string, string>>();

            try
            {
                using var stream = file.OpenReadStream();
                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheet(1);

                var dataRows = worksheet.RowsUsed().Skip(1).ToList();

                foreach (var row in dataRows)
                {
                    string Cell(int col) => row.Cell(col).GetString().Trim();

                    DateTime? CellDate(int col)
                    {
                        var c = row.Cell(col);

                        if (c.IsEmpty())
                            return null;

                        if (c.DataType == XLDataType.DateTime)
                            return c.GetDateTime();

                        var text = c.GetString().Trim();

                        return !string.IsNullOrWhiteSpace(text) && DateTime.TryParse(text, out var parsed)
                            ? parsed
                            : (DateTime?)null;
                    }

                    var firstName = Cell(1);
                    var employeeCode = Cell(3);

                    // Skip fully blank trailing rows
                    if (string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(employeeCode))
                        continue;

                    var rowResult = new EmployeeImportRowResult
                    {
                        RowNumber = row.RowNumber(),
                        EmployeeCode = employeeCode
                    };

                    try
                    {
                        if (string.IsNullOrWhiteSpace(firstName))
                            throw new Exception("First Name is required.");

                        if (string.IsNullOrWhiteSpace(employeeCode))
                            throw new Exception("Employee Code is required.");

                        var phone = Cell(5);
                        if (string.IsNullOrWhiteSpace(phone))
                            throw new Exception("Phone Number is required.");

                        var address = Cell(9);
                        if (string.IsNullOrWhiteSpace(address))
                            throw new Exception("Address is required.");

                        var pincode = Cell(10);
                        if (string.IsNullOrWhiteSpace(pincode))
                            throw new Exception("Pincode is required.");

                        var companyName = Cell(11);
                        if (!companyMap.TryGetValue(companyName, out var companyId))
                            throw new Exception($"Company '{companyName}' not found.");

                        string? branchId = null;
                        var branchName = Cell(12);

                        if (!string.IsNullOrWhiteSpace(branchName))
                        {
                            if (!branchCache.TryGetValue(companyId, out var branchMap))
                            {
                                var branches = await _apiService.GetAsync<List<DropdownDto>>($"dropdown/branch/{companyId}") ?? new();
                                branchMap = branches.ToDictionary(x => x.Text.Trim(), x => x.Value, StringComparer.OrdinalIgnoreCase);
                                branchCache[companyId] = branchMap;
                            }

                            if (!branchMap.TryGetValue(branchName, out branchId))
                                throw new Exception($"Branch '{branchName}' not found under company '{companyName}'.");
                        }

                        var departmentName = Cell(13);
                        if (!departmentMap.TryGetValue(departmentName, out var departmentId))
                            throw new Exception($"Department '{departmentName}' not found.");

                        var designationName = Cell(14);

                        if (!designationCache.TryGetValue(departmentId, out var designationMap))
                        {
                            var designations = await _apiService.GetAsync<List<DropdownDto>>($"dropdown/designation/{departmentId}") ?? new();
                            designationMap = designations.ToDictionary(x => x.Text.Trim(), x => x.Value, StringComparer.OrdinalIgnoreCase);
                            designationCache[departmentId] = designationMap;
                        }

                        if (!designationMap.TryGetValue(designationName, out var designationId))
                            throw new Exception($"Designation '{designationName}' not found under department '{departmentName}'.");

                        var roleName = Cell(15);
                        if (!roleMap.TryGetValue(roleName, out var roleId))
                            throw new Exception($"Role '{roleName}' not found.");

                        var genderText = Cell(6);
                        if (!Enum.TryParse<EnumExtensions.Gender>(genderText, true, out var gender))
                            throw new Exception($"Invalid Gender '{genderText}'. Use Male, Female or Other.");

                        var maritalStatusText = Cell(7);
                        if (!Enum.TryParse<EnumExtensions.MaritalStatus>(maritalStatusText, true, out var maritalStatus))
                            throw new Exception($"Invalid Marital Status '{maritalStatusText}'. Use Married, Unmarried or Divorced.");

                        var employmentTypeText = Cell(17);
                        if (!Enum.TryParse<EnumExtensions.EmploymentType>(employmentTypeText, true, out var employmentType))
                            throw new Exception($"Invalid Employment Type '{employmentTypeText}'.");

                        var joiningDate = CellDate(18);
                        if (joiningDate == null)
                            throw new Exception("Joining Date is required and must be a valid date.");

                        string? reportingManagerId = null;
                        var managerCode = Cell(16);

                        if (!string.IsNullOrWhiteSpace(managerCode))
                        {
                            if (!employeeCodeMap.TryGetValue(managerCode, out reportingManagerId))
                                throw new Exception($"Reporting Manager with Employee Code '{managerCode}' not found. The manager must already exist in the system.");
                        }

                        var dto = new EmployeeDto
                        {
                            FirstName = firstName,
                            LastName = Cell(2),
                            EmployeeCode = employeeCode,
                            Email = string.IsNullOrWhiteSpace(Cell(4)) ? null : Cell(4),
                            Phone = phone,
                            Gender = gender,
                            MaritalStatus = maritalStatus,
                            DateOfBirth = CellDate(8),
                            Address = address,
                            Pincode = pincode,
                            CompanyId = companyId,
                            BranchId = branchId,
                            DepartmentId = departmentId,
                            DesignationId = designationId,
                            RoleId = roleId,
                            ReportingManagerId = reportingManagerId,
                            EmploymentType = employmentType,
                            JoiningDate = joiningDate.Value,
                            PANNumber = string.IsNullOrWhiteSpace(Cell(19)) ? null : Cell(19),
                            AadharNumber = string.IsNullOrWhiteSpace(Cell(20)) ? null : Cell(20),
                            EmergencyContact = string.IsNullOrWhiteSpace(Cell(21)) ? null : Cell(21),
                            TenantId = _tenantId,
                            CreatedBy = _userId
                        };

                        rowResult.EmployeeName = string.Join(
                            " ",
                            new[] { dto.FirstName, dto.LastName }.Where(x => !string.IsNullOrWhiteSpace(x)));

                        var response = await _apiService
                            .PostAsync<EmployeeDto, ApiResponse<EmployeeDto>>("Employee/add-employee", dto);

                        if (response != null && response.Success)
                        {
                            rowResult.Success = true;
                            rowResult.Message = "Imported successfully.";
                        }
                        else
                        {
                            rowResult.Success = false;
                            rowResult.Message = response?.Message ?? "Import failed.";
                        }
                    }
                    catch (ApiException apiEx)
                    {
                        rowResult.Success = false;
                        rowResult.Message = GetErrorMessage(apiEx.ResponseContent);
                    }
                    catch (Exception ex)
                    {
                        rowResult.Success = false;
                        rowResult.Message = ex.Message;
                    }

                    result.Rows.Add(rowResult);
                }
            }
            catch (Exception ex)
            {
                TempData["GlobalError"] = $"Unable to read the uploaded file: {ex.Message}";
                return View(result);
            }

            result.TotalRows = result.Rows.Count;
            result.SuccessCount = result.Rows.Count(x => x.Success);
            result.FailureCount = result.TotalRows - result.SuccessCount;

            if (result.TotalRows == 0)
                TempData["GlobalError"] = "The uploaded file didn't contain any employee rows.";
            else if (result.FailureCount == 0)
                TempData["Success"] = $"All {result.SuccessCount} employee(s) imported successfully.";
            else
                TempData["Info"] = $"{result.SuccessCount} of {result.TotalRows} employee(s) imported. {result.FailureCount} failed - see details below.";

            return View(result);
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

                return "Import failed.";
            }
            catch
            {
                return "Import failed.";
            }
        }

        #endregion

        #region LoadDropdowns

        private async Task LoadDropdowns(string? companyId = null,string? deptId = null)
        {
            var countries = await _apiService.GetAsync<List<DropdownDto>>($"dropdown/country");

            ViewBag.CountryList = countries.Select(x => new SelectListItem
            {
                Value = x.Value,
                Text = x.Text
            }).ToList();

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
