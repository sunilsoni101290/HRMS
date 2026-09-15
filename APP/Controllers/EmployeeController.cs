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

        // Optional querystring params let the Recruitment "Create Employee &
        // Start Onboarding" bridge (see Candidate/Index.cshtml,
        // Candidate/Details.cshtml) preselect/prefill this form from a
        // selected Candidate. candidateId is what actually matters
        // functionally - it flows through to CandidateId below and, once
        // posted, triggers EmployeeService.CreateAsync's automatic
        // OnboardingCase creation server-side.
        [HttpGet]
        public async Task<IActionResult> Create(string? candidateId, string? name, string? email, string? phone)
        {
            await LoadDropdowns();

            var model = new EmployeeDto
            {
                JoiningDate = DateTime.UtcNow,
                CandidateId = candidateId,

                // Requirement: "General Shift" (identified via the Shift
                // master's IsDefaultShift flag, never a hard-coded Id/name -
                // see GetDefaultShiftIdAsync) pre-selected for a brand-new
                // employee. The user can still change it before saving; this
                // only sets the initial selection. Falls back to empty (so
                // the dropdown shows its blank "Select Shift" option and
                // [Required] correctly blocks submission) only if no shift
                // is flagged default yet - never left null now that ShiftId
                // is a non-nullable, mandatory field.
                ShiftId = await GetDefaultShiftIdAsync() ?? string.Empty
            };

            if (!string.IsNullOrWhiteSpace(name))
            {
                var parts = name.Trim().Split(' ', 2);
                model.FirstName = parts[0];
                model.LastName = parts.Length > 1 ? parts[1] : null;
            }

            if (!string.IsNullOrWhiteSpace(email))
                model.Email = email;

            if (!string.IsNullOrWhiteSpace(phone))
                model.Phone = phone;

            ViewBag.FromCandidate = !string.IsNullOrWhiteSpace(candidateId);

            return View(model);
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

                    // If this Employee was created from a Candidate,
                    // EmployeeService.CreateAsync (Application layer) will
                    // have already auto-started an OnboardingCase for them -
                    // send HR straight there instead of the plain employee
                    // list so the "onboarding will start automatically"
                    // promise on the form is visibly kept. Best-effort only:
                    // any failure here just falls back to the normal
                    // redirect, the case is still reachable from the
                    // Onboarding tracker either way.
                    if (!string.IsNullOrWhiteSpace(dto.CandidateId) && response.Data != null && !string.IsNullOrWhiteSpace(response.Data.Id))
                    {
                        try
                        {
                            var onboardingCase = await _apiService
                                .GetAsync<OnboardingCaseDto>($"onboarding/employee/{response.Data.Id}");

                            if (onboardingCase != null && !string.IsNullOrWhiteSpace(onboardingCase.Id))
                                return RedirectToAction("Details", "Onboarding", new { id = onboardingCase.Id });
                        }
                        catch
                        {
                            // No onboarding case found (or the lookup
                            // failed) - fall through to the normal redirect.
                        }
                    }

                    return RedirectToAction(nameof(Index));
                }

                // Create.cshtml's SweetAlert popup only ever reads
                // TempData["Success"] / TempData["Error"] (see its Scripts
                // section) - it has no asp-validation-summary and never
                // reads ModelState at all. Every failure here used to call
                // ModelState.AddModelError only, so the page silently
                // re-rendered with no visible error whatsoever ("Add
                // Employee" appearing to do nothing). Now sets
                // TempData["Error"] too, matching every other action in
                // this app, so the same failure a user sees today (a
                // duplicate Employee Code, a rejected field, etc.) is
                // finally shown instead of hidden.
                TempData["Error"] = response.Message;

                ModelState.AddModelError(
                    "",
                    response.Message);
            }
            catch (ApiException apiEx)
            {
                // PostAsync<TRequest,TResponse> throws ApiException with a
                // generic "API Error" Message and the real server message
                // buried in ResponseContent (see ApiService.PostAsync) - a
                // duplicate Employee Code (or the existing phone/email
                // duplicate checks) would previously have shown the literal
                // text "API Error" here instead of the actual reason.
                var friendly = GetFriendlyErrorMessage(apiEx.ResponseContent, "Unable to create employee.");

                TempData["Error"] = friendly;

                ModelState.AddModelError("", friendly);
            }
            catch (Exception ex)
            {
                var friendly = GetFriendlyErrorMessage(ex.Message, "Unable to create employee.");

                TempData["Error"] = friendly;

                ModelState.AddModelError("", friendly);
            }

            ViewBag.FromCandidate = !string.IsNullOrWhiteSpace(dto.CandidateId);

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

                ViewBag.IsAdmin = _isAdmin;

                try
                {
                    // PutAsync<TRequest,TResponse> doesn't throw ApiException
                    // on a non-success response the way PostAsync does - a
                    // BadRequest from the API (e.g. duplicate Employee Code,
                    // now that EmployeeController.Update wraps UpdateAsync in
                    // try/catch) surfaces here as a plain Exception whose
                    // Message is "Bad Request (400): {json}" (see
                    // ApiService.HandleResponse) - GetFriendlyErrorMessage
                    // strips that prefix and pulls out the real message.
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

                    TempData["Error"] = response.Message ?? "Unable to update employee.";
                }
                catch (ApiException apiEx)
                {
                    TempData["Error"] = GetFriendlyErrorMessage(apiEx.ResponseContent, "Unable to update employee.");
                }
                catch (Exception ex)
                {
                    TempData["Error"] = GetFriendlyErrorMessage(ex.Message, "Unable to update employee.");
                }

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

        #region Upload Profile Photo

        // Powers the "Upload Profile Image" modal on My Profile. Scoped so
        // a self-service employee can only ever replace their OWN photo -
        // id is taken from the logged-in session, never trusted from the
        // form - and routes through the narrow update-photo API endpoint
        // rather than the full employee update, so nothing else about the
        // record can change through this action.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadProfilePhoto(IFormFile profilePhoto)
        {
            var targetEmployeeId = _isAdmin
                ? Request.Form["employeeId"].ToString()
                : _employeeId;

            if (string.IsNullOrWhiteSpace(targetEmployeeId))
            {
                TempData["GlobalError"] = "Unable to determine which employee to update.";
                return RedirectToAction(nameof(Details), new { id = _employeeId });
            }

            if (profilePhoto == null || profilePhoto.Length == 0)
            {
                TempData["GlobalError"] = "Please choose an image to upload.";
                return RedirectToAction(nameof(Details), new { id = targetEmployeeId });
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(profilePhoto.FileName).ToLower();

            if (!allowedExtensions.Contains(extension))
            {
                TempData["GlobalError"] = "Only JPG, JPEG and PNG files are allowed.";
                return RedirectToAction(nameof(Details), new { id = targetEmployeeId });
            }

            // 2 MB cap - a profile photo has no business being larger than
            // this, and it keeps the upload modal snappy.
            const long maxSizeBytes = 2 * 1024 * 1024;

            if (profilePhoto.Length > maxSizeBytes)
            {
                TempData["GlobalError"] = "Image is too large. Maximum size is 2 MB.";
                return RedirectToAction(nameof(Details), new { id = targetEmployeeId });
            }

            try
            {
                string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/employee");

                if (!Directory.Exists(uploadFolder))
                    Directory.CreateDirectory(uploadFolder);

                // Best-effort cleanup of the old file - fetch the current
                // record first so we know what to delete.
                var current = await _apiService
                    .GetAsync<EmployeeListDto>($"Employee/get-employee-detail/{targetEmployeeId}");

                // Save the NEW file and persist it to the DB first - only
                // delete the OLD file once both of those have actually
                // succeeded, so a failed upload never leaves the employee
                // with no photo at all.
                var fileName =
                    $"{DateTime.Now:yyyyMMddHHmmssfff}{extension}";

                var filePath = Path.Combine(uploadFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await profilePhoto.CopyToAsync(stream);
                }

                var relativePath = "/employee/" + fileName;

                // API always answers 200 with { Success, Message } here
                // (never a non-2xx status for a normal "not found"/"failed"
                // outcome) - PutAsync won't throw for that, so the actual
                // Success flag has to be checked explicitly. Skipping this
                // check was the bug: it always showed "updated
                // successfully" even when the DB write failed.
                var response = await _apiService.PutAsync<ApiResponse<object>>(
                    $"Employee/update-photo/{targetEmployeeId}",
                    new { FilePath = relativePath });

                if (response == null || !response.Success)
                {
                    // Roll back the file we just wrote - the DB was never
                    // updated to point at it.
                    if (System.IO.File.Exists(filePath))
                        System.IO.File.Delete(filePath);

                    TempData["GlobalError"] = response?.Message ?? "Unable to update the profile photo.";
                    return RedirectToAction(nameof(Details), new { id = targetEmployeeId });
                }

                // Now safe to remove the old photo.
                if (current != null && !string.IsNullOrEmpty(current.FilePath))
                {
                    var oldFilePath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        current.FilePath.TrimStart('/'));

                    if (System.IO.File.Exists(oldFilePath))
                        System.IO.File.Delete(oldFilePath);
                }

                TempData["Success"] = "Profile photo updated successfully.";
            }
            catch (Exception)
            {
                TempData["GlobalError"] = "Unable to upload the image. Please try again.";
            }

            return RedirectToAction(nameof(Details), new { id = targetEmployeeId });
        }

        #endregion

        #region Delete

        public async Task<IActionResult> Delete(string id)
        {
            // The API only exposes a bulk-delete endpoint
            // (POST api/Employee/delete, List<string> ids) - there is no
            // single-employee DELETE route. This used to call a URL/verb
            // combination ("DELETE Employee/employee/{id}") that never
            // existed on the API, so every delete failed with an unhandled
            // ApiException. Wired to the existing bulk endpoint with a
            // one-item list instead, wrapped in a try/catch (matching the
            // pattern used elsewhere in this controller, e.g.
            // UploadProfilePhoto) so a failure - including a legitimate
            // "can't delete, has related records" case - shows a friendly
            // message instead of the generic error page.
            try
            {
                dynamic result = await _apiService
                    .PostAsync<dynamic>("Employee/delete", new List<string> { id });

                bool success = result?.Success == true;

                TempData[success ? "Success" : "GlobalError"] =
                    (string)(result?.Message)
                    ?? (success
                        ? "Employee deleted successfully."
                        : "Unable to delete the employee.");
            }
            catch (UnauthorizedAccessException)
            {
                // Let ApiSessionExpiredFilter turn this into a clean
                // redirect to Login instead of swallowing it here.
                throw;
            }
            catch (Exception)
            {
                TempData["GlobalError"] =
                    "Unable to delete the employee. It may have related records (attendance, leave, payroll, etc.) that need to be removed first.";
            }

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


        // Backs the Create/Edit form's blur-triggered duplicate Employee
        // Code check. employeeId is the record currently being edited
        // (omitted/blank on Create) so the employee's own unchanged code is
        // never flagged as a duplicate of itself.
        [HttpGet]
        public async Task<JsonResult> CheckEmployeeCode(string employeeCode, string? employeeId)
        {
            if (string.IsNullOrWhiteSpace(employeeCode))
                return Json(new { exists = false });

            try
            {
                var url = $"Employee/check-employee-code?employeeCode={Uri.EscapeDataString(employeeCode.Trim())}"
                    + (string.IsNullOrWhiteSpace(employeeId) ? "" : $"&employeeId={Uri.EscapeDataString(employeeId)}");

                var response = await _apiService.GetAsync<ApiResponse<EmployeeCodeExistsDto>>(url);

                return Json(new { exists = response?.Data?.Exists ?? false });
            }
            catch (Exception)
            {
                return Json(new { error = true, message = "Unable to check Employee Code right now. Please try again." });
            }
        }

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

        // NOT YET MIGRATED to the new APP.Excel.ExcelEngine (see
        // APP/Excel/ExcelEngine.cs, IExcelEngine.cs and
        // SalaryComponentController for the reference migration). This
        // import has materially more going on than SalaryComponent's -
        // multiple cross-referenced lookups resolved per row (Company,
        // Branch scoped to Company, Department, Designation scoped to
        // Department, Role, Reporting Manager by Employee Code) each with
        // their own cache dictionaries, plus file uploads elsewhere in this
        // controller reusing similar patterns. Porting it correctly needs
        // its own focused pass rather than folding it into the engine's
        // initial build - left as inline ClosedXML for now so the working
        // bulk-import feature isn't put at risk. The next engineer wiring
        // up the remaining modules should treat this as the second/third
        // migration, once the engine has proven itself on a couple of
        // simpler modules.
        [HttpGet]
        public IActionResult Import()
        {
            if (!_isAdmin)
                return Forbid();

            return View(new EmployeeImportResultDto());
        }

        // Builds a ready-to-fill .xlsx: an "Employees" sheet with headers +
        // one sample row (mandatory columns marked with *, matching the
        // Import spec's 7 mandatory fields - Employee Code, First Name,
        // Gender, Role, Company, Employment Type, Shift - everything else is
        // optional), an in-cell dropdown (Excel Data Validation) on every
        // column that maps to a flat master list or a fixed enum, and a
        // "Reference Data" sheet backing those dropdowns and listing the
        // department-scoped Designation/Branch names for reference (those
        // two are typed free-text since a static Excel file can't cascade a
        // dropdown by another cell's value).
        [HttpGet]
        public async Task<IActionResult> DownloadImportTemplate()
        {
            if (!_isAdmin)
                return Forbid();

            var companies = await _apiService.GetAsync<List<DropdownDto>>("dropdown/company") ?? new();
            var departments = await _apiService.GetAsync<List<DropdownDto>>("dropdown/department") ?? new();
            var designations = await _apiService.GetAsync<List<DesignationListDto>>("designation") ?? new();
            var roles = await _apiService.GetAsync<List<DropdownDto>>("dropdown/role") ?? new();
            var shifts = await _apiService.GetAsync<List<DropdownDto>>("dropdown/shift") ?? new();

            using var workbook = new XLWorkbook();

            // ----- Employees sheet -----
            var sheet = workbook.Worksheets.Add("Employees");

            string[] headers =
            {
                "Employee Code*", "First Name*", "Last Name",
                "Gender*", "Marital Status", "Date Of Birth (yyyy-mm-dd)",
                "Email", "Phone", "Emergency Contact",
                "Company*", "Branch", "Department", "Designation",
                "Role*", "Reporting Manager (Employee Code)", "Shift*",
                "Employment Type*", "Joining Date (yyyy-mm-dd)",
                "Address", "Pincode", "PAN Number", "Aadhaar Number"
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
                "EMP1001", "John", "Doe",
                "Male", "Married", "1995-06-15",
                "john.doe@example.com", "9876543210", "9123456780",
                companies.FirstOrDefault()?.Text ?? "Company Name",
                "",
                departments.FirstOrDefault()?.Text ?? "",
                "",
                roles.FirstOrDefault()?.Text ?? "Role Name",
                "",
                shifts.FirstOrDefault()?.Text ?? "Shift Name",
                "Permanent", DateTime.Today.ToString("yyyy-MM-dd"),
                "12, MG Road, Pune", "411001", "ABCDE1234F", "123456789012"
            };

            for (int i = 0; i < sample.Length; i++)
                sheet.Cell(2, i + 1).Value = sample[i];

            sheet.SheetView.FreezeRows(1);
            sheet.Columns().AdjustToContents();

            // ----- Reference Data sheet -----
            var refSheet = workbook.Worksheets.Add("Reference Data");

            IXLRange WriteList(int col, string title, IReadOnlyList<string> values)
            {
                var header = refSheet.Cell(1, col);
                header.Value = title;
                header.Style.Font.Bold = true;

                for (int i = 0; i < values.Count; i++)
                    refSheet.Cell(i + 2, col).Value = values[i];

                // At least one (blank) row so the dropdown range is always valid.
                var lastRow = Math.Max(2, values.Count + 1);
                return refSheet.Range(2, col, lastRow, col);
            }

            var companyNames = companies.Select(x => x.Text).ToList();
            var departmentNames = departments.Select(x => x.Text).ToList();
            var roleNames = roles.Select(x => x.Text).ToList();
            var shiftNames = shifts.Select(x => x.Text).ToList();
            var genderNames = Enum.GetNames(typeof(EnumExtensions.Gender)).ToList();
            var maritalStatusNames = Enum.GetNames(typeof(EnumExtensions.MaritalStatus)).ToList();
            var employmentTypeNames = Enum.GetNames(typeof(EnumExtensions.EmploymentType))
                .Where(x => x != nameof(EnumExtensions.EmploymentType.Unknown)).ToList();

            var companyRange = WriteList(1, "Company", companyNames);
            var departmentRange = WriteList(2, "Department", departmentNames);
            var roleRange = WriteList(3, "Role", roleNames);
            var shiftRange = WriteList(4, "Shift", shiftNames);
            var genderRange = WriteList(5, "Gender", genderNames);
            var maritalStatusRange = WriteList(6, "Marital Status", maritalStatusNames);
            var employmentTypeRange = WriteList(7, "Employment Type", employmentTypeNames);
            WriteList(8, "Designation (reference only - type the name)", designations.Select(x => $"{x.Name} ({x.DepartmentName})").ToList());
            WriteList(9, "Branch (reference only - type the name)", new List<string>());

            refSheet.Columns().AdjustToContents();

            // ----- In-cell dropdowns on the Employees sheet, rows 2-1000 -----
            void ApplyDropdown(int col, IXLRange source)
            {
                var validation = sheet.Range(2, col, 1000, col).SetDataValidation();
                validation.List(source);
                validation.ErrorTitle = "Invalid value";
                validation.ErrorMessage = "Please pick a value from the dropdown list.";
                validation.InputTitle = "Choose from list";
            }

            ApplyDropdown(4, genderRange);          // Gender*
            ApplyDropdown(5, maritalStatusRange);   // Marital Status
            ApplyDropdown(10, companyRange);        // Company*
            ApplyDropdown(12, departmentRange);     // Department
            ApplyDropdown(14, roleRange);           // Role*
            ApplyDropdown(16, shiftRange);          // Shift*
            ApplyDropdown(17, employmentTypeRange); // Employment Type*

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Employee_Import_Template.xlsx");
        }

        // Reads every used data row (row 1 is the header) out of the
        // uploaded workbook into raw, unresolved EmployeeImportRowInputDto
        // rows - column order matches DownloadImportTemplate's headers
        // exactly. No validation happens here; that is entirely owned by
        // the API (EmployeeImportExportService), the single source of truth
        // shared by Preview and Commit.
        private static List<EmployeeImportRowInputDto> ParseImportRows(IXLWorksheet worksheet)
        {
            var rows = new List<EmployeeImportRowInputDto>();

            foreach (var row in worksheet.RowsUsed().Skip(1))
            {
                string? Cell(int col)
                {
                    var text = row.Cell(col).GetString().Trim();
                    return string.IsNullOrWhiteSpace(text) ? null : text;
                }

                string? CellDateText(int col)
                {
                    var c = row.Cell(col);

                    if (c.IsEmpty())
                        return null;

                    if (c.DataType == XLDataType.DateTime)
                        return c.GetDateTime().ToString("yyyy-MM-dd");

                    var text = c.GetString().Trim();
                    return string.IsNullOrWhiteSpace(text) ? null : text;
                }

                var employeeCode = Cell(1);
                var firstName = Cell(2);

                // Skip fully blank trailing rows.
                if (string.IsNullOrWhiteSpace(employeeCode) && string.IsNullOrWhiteSpace(firstName))
                    continue;

                rows.Add(new EmployeeImportRowInputDto
                {
                    RowNumber = row.RowNumber(),
                    EmployeeCode = employeeCode,
                    FirstName = firstName,
                    LastName = Cell(3),
                    Gender = Cell(4),
                    MaritalStatus = Cell(5),
                    DateOfBirth = CellDateText(6),
                    Email = Cell(7),
                    Phone = Cell(8),
                    EmergencyContact = Cell(9),
                    CompanyName = Cell(10),
                    BranchName = Cell(11),
                    DepartmentName = Cell(12),
                    DesignationName = Cell(13),
                    RoleName = Cell(14),
                    ReportingManagerCode = Cell(15),
                    ShiftName = Cell(16),
                    EmploymentType = Cell(17),
                    JoiningDate = CellDateText(18),
                    Address = Cell(19),
                    Pincode = Cell(20),
                    PANNumber = Cell(21),
                    AadharNumber = Cell(22)
                });
            }

            return rows;
        }

        private async Task<(bool Ok, List<EmployeeImportRowInputDto> Rows, string? Error)> ReadUploadedRowsAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return (false, new(), "Please choose an Excel file to import.");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (extension != ".xlsx" && extension != ".xls")
                return (false, new(), "Only .xlsx or .xls files are supported.");

            try
            {
                using var stream = file.OpenReadStream();
                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheet(1);

                return (true, ParseImportRows(worksheet), null);
            }
            catch (Exception ex)
            {
                return (false, new(), $"Unable to read the uploaded file: {ex.Message}");
            }
        }

        // ==============================
        // PREVIEW (Upload -> Preview -> Validate step) - AJAX, nothing written.
        // ==============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PreviewImport(IFormFile file)
        {
            if (!_isAdmin)
                return Forbid();

            var (ok, rows, error) = await ReadUploadedRowsAsync(file);

            if (!ok)
                return BadRequest(new { message = error });

            if (rows.Count == 0)
                return BadRequest(new { message = "The uploaded file didn't contain any employee rows." });

            try
            {
                var result = await _apiService
                    .PostAsync<List<EmployeeImportRowInputDto>, EmployeeImportPreviewResultDto>(
                        "Employee/import/validate", rows);

                return Json(result);
            }
            catch (ApiException apiEx)
            {
                return BadRequest(new { message = GetErrorMessage(apiEx.ResponseContent) });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ==============================
        // COMMIT (Import step) - AJAX. Re-uploads the SAME file; the API
        // re-validates everything and only inserts if 100% valid, so this
        // is safe to call even if the admin waited a while after Preview.
        // ==============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CommitImport(IFormFile file)
        {
            if (!_isAdmin)
                return Forbid();

            var (ok, rows, error) = await ReadUploadedRowsAsync(file);

            if (!ok)
                return BadRequest(new { message = error });

            try
            {
                var result = await _apiService
                    .PostAsync<List<EmployeeImportRowInputDto>, EmployeeImportCommitResultDto>(
                        "Employee/import/commit", rows);

                return Json(result);
            }
            catch (ApiException apiEx)
            {
                return BadRequest(new { message = GetErrorMessage(apiEx.ResponseContent) });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ==============================
        // DOWNLOAD ERROR EXCEL - re-validates the same file and returns
        // only the invalid rows, one line per error (Row / Field / Value /
        // Error), so a large file's failures can be reviewed/fixed outside
        // the browser.
        // ==============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DownloadImportErrors(IFormFile file)
        {
            if (!_isAdmin)
                return Forbid();

            var (ok, rows, error) = await ReadUploadedRowsAsync(file);

            if (!ok)
            {
                TempData["GlobalError"] = error;
                return RedirectToAction(nameof(Import));
            }

            EmployeeImportPreviewResultDto preview;

            try
            {
                preview = await _apiService
                    .PostAsync<List<EmployeeImportRowInputDto>, EmployeeImportPreviewResultDto>(
                        "Employee/import/validate", rows);
            }
            catch (ApiException apiEx)
            {
                TempData["GlobalError"] = GetErrorMessage(apiEx.ResponseContent);
                return RedirectToAction(nameof(Import));
            }

            var invalidRows = preview.Rows.Where(x => !x.IsValid).ToList();

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Import Errors");

            string[] headers = { "Row", "Field", "Value", "Error" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = sheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#C62828");
            }

            int line = 2;
            foreach (var row in invalidRows)
            {
                foreach (var err in row.Errors)
                {
                    sheet.Cell(line, 1).Value = row.RowNumber;
                    sheet.Cell(line, 2).Value = err.Field;
                    sheet.Cell(line, 3).Value = err.Value;
                    sheet.Cell(line, 4).Value = err.Error;
                    line++;
                }
            }

            sheet.SheetView.FreezeRows(1);
            sheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Employee_Import_Errors.xlsx");
        }

        // General-purpose counterpart to GetErrorMessage(json) below (which
        // is hard-coded to the bulk-import flow's "Import failed." fallback
        // and assumes its input is already bare JSON). This one also
        // strips the "Bad Request (400): {json}" / "API Error" prefixes
        // ApiService's various exception paths can produce (see
        // ApiService.HandleResponse and PostAsync<TRequest,TResponse>), so
        // Create/Edit can show the real server-side validation message
        // (e.g. a duplicate Employee Code) instead of a generic string.
        private static string GetFriendlyErrorMessage(string? raw, string fallback)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return fallback;

            try
            {
                var jsonStart = raw.IndexOf('{');
                var json = jsonStart >= 0 ? raw.Substring(jsonStart) : raw;

                var obj = Newtonsoft.Json.Linq.JObject.Parse(json);

                if (obj["Message"] != null)
                    return obj["Message"]!.ToString();

                if (obj["message"] != null)
                    return obj["message"]!.ToString();

                if (obj["Errors"] is Newtonsoft.Json.Linq.JArray errors && errors.Count > 0)
                    return errors[0]?.ToString() ?? fallback;

                if (obj["errors"] is Newtonsoft.Json.Linq.JObject validationErrors)
                {
                    foreach (var property in validationErrors.Properties())
                    {
                        if (property.Value is Newtonsoft.Json.Linq.JArray arr && arr.Count > 0)
                            return arr[0]?.ToString() ?? fallback;
                    }
                }

                return fallback;
            }
            catch
            {
                return fallback;
            }
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

        #region Export

        // Rebuilt Export: reuses the existing Employee/employee-list API
        // contract unchanged (no new endpoint needed - that response
        // already carries every latest Employee field with readable master
        // names instead of IDs, courtesy of EmployeeMapper.ToDto), applies
        // the SAME search/department/designation/status filter the Index
        // page's client-side JS applies (kept in one place here so the
        // exported file always matches what the admin is currently looking
        // at), and writes a fully-formatted .xlsx with every field.
        [HttpGet]
        public async Task<IActionResult> Export(string? search, string? department, string? designation, string? status)
        {
            var data = await _apiService.GetAsync<List<EmployeeListDto>>("Employee/employee-list") ?? new();

            IEnumerable<EmployeeListDto> filtered = data;

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLowerInvariant();
                filtered = filtered.Where(x =>
                    $"{x.FirstName} {x.LastName} {x.EmployeeCode} {x.Phone} {x.Email}"
                        .ToLowerInvariant()
                        .Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(department))
                filtered = filtered.Where(x => string.Equals(x.DepartmentName, department, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(designation))
                filtered = filtered.Where(x => string.Equals(x.DesignationName, designation, StringComparison.OrdinalIgnoreCase));

            if (string.Equals(status, "active", StringComparison.OrdinalIgnoreCase))
                filtered = filtered.Where(x => !x.RelievingDate.HasValue);
            else if (string.Equals(status, "relieved", StringComparison.OrdinalIgnoreCase))
                filtered = filtered.Where(x => x.RelievingDate.HasValue);

            var rows = filtered
                .OrderBy(x => x.EmployeeCode)
                .ToList();

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Employees");

            string[] headers =
            {
                "Employee Code", "First Name", "Last Name", "Gender", "Marital Status",
                "Date Of Birth", "Email", "Phone", "Emergency Contact",
                "Company", "Branch", "Department", "Designation", "Shift",
                "Reporting Manager", "Employment Type",
                "Joining Date", "Confirmation Date", "Relieving Date", "Status",
                "Address", "Pincode", "PAN Number", "Aadhaar Number",
                "Nationality", "Passport Number", "Passport Status",
                "Passport Issuing Country", "Passport Issue Date", "Passport Expiry Date"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = sheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1B2A4A");
            }

            static string DateText(DateTime? d) => d.HasValue ? d.Value.ToString("dd-MMM-yyyy") : "";

            int r = 2;
            foreach (var e in rows)
            {
                var col = 1;
                sheet.Cell(r, col++).Value = e.EmployeeCode;
                sheet.Cell(r, col++).Value = e.FirstName;
                sheet.Cell(r, col++).Value = e.LastName ?? "";
                sheet.Cell(r, col++).Value = e.Gender ?? "";
                sheet.Cell(r, col++).Value = e.MaritalStatus ?? "";
                sheet.Cell(r, col++).Value = DateText(e.DateOfBirth);
                sheet.Cell(r, col++).Value = e.Email ?? "";
                sheet.Cell(r, col++).Value = e.Phone ?? "";
                sheet.Cell(r, col++).Value = e.EmergencyContact ?? "";
                sheet.Cell(r, col++).Value = e.CompanyName ?? "";
                sheet.Cell(r, col++).Value = e.BrnachName ?? "";
                sheet.Cell(r, col++).Value = e.DepartmentName ?? "";
                sheet.Cell(r, col++).Value = e.DesignationName ?? "";
                sheet.Cell(r, col++).Value = e.ShiftName ?? "";
                sheet.Cell(r, col++).Value = e.ReportingManagerName ?? "";
                sheet.Cell(r, col++).Value = e.EmploymentType ?? "";
                sheet.Cell(r, col++).Value = DateText(e.JoiningDate);
                sheet.Cell(r, col++).Value = DateText(e.ConfirmationDate);
                sheet.Cell(r, col++).Value = DateText(e.RelievingDate);
                sheet.Cell(r, col++).Value = e.RelievingDate.HasValue ? "Relieved" : "Active";
                sheet.Cell(r, col++).Value = e.Address ?? "";
                sheet.Cell(r, col++).Value = e.Pincode ?? "";
                sheet.Cell(r, col++).Value = e.PANNumber ?? "";
                sheet.Cell(r, col++).Value = e.AadharNumber ?? "";
                sheet.Cell(r, col++).Value = e.Nationality.ToString();
                sheet.Cell(r, col++).Value = e.PassportNumber ?? "";
                sheet.Cell(r, col++).Value = e.PassportStatus.ToString();
                sheet.Cell(r, col++).Value = e.CountryName ?? "";
                sheet.Cell(r, col++).Value = DateText(e.IssueDate);
                sheet.Cell(r, col++).Value = DateText(e.ExpiryDate);
                r++;
            }

            sheet.SheetView.FreezeRows(1);
            sheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            var fileName = $"Employees_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
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
            // ROOT-CAUSE FIX: this used to call "dropdown/default-shift",
            // which (see DropdownService.GetDefaultShiftDropdownAsync) only
            // ever returns the shift(s) with IsDefaultShift = true - so the
            // Shift <select> on the Employee form only ever had ONE
            // selectable option ("General Shift") no matter how many real
            // shifts existed in the Shift master. "dropdown/shift" returns
            // every active shift, which is what the dropdown is supposed to
            // offer; which one is pre-selected on a NEW employee is handled
            // separately below via GetDefaultShiftIdAsync(), still driven by
            // the same IsDefaultShift master-data flag rather than any
            // hard-coded shift name/Id.
            var shifts = await _apiService
                .GetAsync<List<DropdownDto>>("dropdown/shift");

            ViewBag.ShiftList = new SelectList(
                shifts,
                "Value",
                "Text"
            );
        }

        #endregion

        #region GetDefaultShiftIdAsync

        // Identifies "the default shift" purely from existing Shift master
        // data (Shift.IsDefaultShift, seeded true on "General Shift" - see
        // DbSeeder.cs) via the already-existing "dropdown/default-shift" API
        // endpoint (Where(x => x.IsActive && x.IsDefaultShift) - see
        // DropdownService.GetDefaultShiftDropdownAsync), never by hard-coding
        // a shift name or Id here. Used only to PRE-SELECT the Shift field
        // when adding a brand-new employee; Edit always keeps the employee's
        // own already-saved ShiftId untouched (see Edit GET below). Returns
        // null (leaving the dropdown on its blank "Select Shift" option)
        // if no shift is marked default yet - the field is still mandatory,
        // so the user simply has to pick one themselves in that case.
        private async Task<string?> GetDefaultShiftIdAsync()
        {
            try
            {
                var defaultShifts = await _apiService
                    .GetAsync<List<DropdownDto>>("dropdown/default-shift");

                return defaultShifts?.FirstOrDefault()?.Value;
            }
            catch
            {
                // Best-effort only - a failure here must never block the
                // Create form from loading; the user can still pick a shift
                // manually.
                return null;
            }
        }

        #endregion
    }
}
