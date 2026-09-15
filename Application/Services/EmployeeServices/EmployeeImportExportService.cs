using Application.DTOs.Employee;
using Application.Interfaces.EmployeeInterface;
using Application.Interfaces.ErrorLog;
using Domain.Entities;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Domain.Enums.EnumExtensions;

namespace Application.Services.EmployeeServices
{
    // Bulk Import/Export validation + commit for the Employee module. The
    // Employee Add/Edit form (Application.DTOs.Employee.EmployeeDto) is the
    // single source of truth for every format/length rule applied below -
    // the same regexes and StringLength limits are reused verbatim so an
    // imported row can never pass validation here and then fail it again on
    // a later Edit. The only deliberate difference from Add/Edit is WHICH
    // fields are mandatory: per spec, bulk Import only requires EmployeeCode,
    // FirstName, Gender, Role, Company, EmploymentType and Shift - every
    // other field (including Email, which Add/Edit requires) is optional
    // for a bulk-imported row.
    public class EmployeeImportExportService : IEmployeeImportExportService
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmployeeService _employeeService;
        private readonly IErrorLogService _errorLogService;

        private static readonly Regex EmployeeCodePattern = new(@"^[A-Za-z0-9_-]+$", RegexOptions.Compiled);
        private static readonly Regex MobilePattern = new(@"^[6-9]\d{9}$", RegexOptions.Compiled);
        private static readonly Regex PincodePattern = new(@"^\d{6}$", RegexOptions.Compiled);
        private static readonly Regex PanPattern = new(@"^[A-Z]{5}[0-9]{4}[A-Z]{1}$", RegexOptions.Compiled);
        private static readonly Regex AadhaarPattern = new(@"^\d{12}$", RegexOptions.Compiled);
        private static readonly EmailAddressAttribute EmailValidator = new();

        public EmployeeImportExportService(
            ApplicationDbContext db,
            IEmployeeService employeeService,
            IErrorLogService errorLogService)
        {
            _db = db;
            _employeeService = employeeService;
            _errorLogService = errorLogService;
        }

        // ==============================================================
        // MASTER-DATA CACHE - loaded ONCE per import, not once per row.
        // ==============================================================
        private class MasterDataCache
        {
            public Dictionary<string, string> Companies = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, Dictionary<string, string>> BranchesByCompany = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, string> Departments = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, Dictionary<string, string>> DesignationsByDepartment = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, string> Roles = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, string> Shifts = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, string> EmployeeCodeToId = new(StringComparer.OrdinalIgnoreCase);
            public HashSet<string> ExistingPhones = new(StringComparer.OrdinalIgnoreCase);
            public HashSet<string> ExistingUserEmails = new(StringComparer.OrdinalIgnoreCase);
            public HashSet<string> ExistingUsernames = new(StringComparer.OrdinalIgnoreCase);
        }

        private async Task<MasterDataCache> LoadMasterDataAsync(string tenantId)
        {
            var cache = new MasterDataCache();

            var companies = await _db.Companies
                .Where(x => x.TenantId == tenantId && !x.IsDeleted)
                .Select(x => new { x.Id, x.Name })
                .ToListAsync();
            foreach (var c in companies)
                if (!string.IsNullOrWhiteSpace(c.Name) && !cache.Companies.ContainsKey(c.Name.Trim()))
                    cache.Companies[c.Name.Trim()] = c.Id;

            var branches = await _db.Branches
                .Where(x => x.TenantId == tenantId && !x.IsDeleted)
                .Select(x => new { x.Id, x.Name, x.CompanyId })
                .ToListAsync();
            foreach (var b in branches)
            {
                if (string.IsNullOrWhiteSpace(b.Name) || string.IsNullOrWhiteSpace(b.CompanyId))
                    continue;

                if (!cache.BranchesByCompany.TryGetValue(b.CompanyId, out var map))
                {
                    map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    cache.BranchesByCompany[b.CompanyId] = map;
                }

                map[b.Name.Trim()] = b.Id;
            }

            var departments = await _db.Departments
                .Where(x => x.TenantId == tenantId && !x.IsDeleted)
                .Select(x => new { x.Id, x.Name })
                .ToListAsync();
            foreach (var d in departments)
                if (!string.IsNullOrWhiteSpace(d.Name) && !cache.Departments.ContainsKey(d.Name.Trim()))
                    cache.Departments[d.Name.Trim()] = d.Id;

            var designations = await _db.Designations
                .Where(x => x.TenantId == tenantId && !x.IsDeleted)
                .Select(x => new { x.Id, x.Name, x.DepartmentId })
                .ToListAsync();
            foreach (var d in designations)
            {
                if (string.IsNullOrWhiteSpace(d.Name) || string.IsNullOrWhiteSpace(d.DepartmentId))
                    continue;

                if (!cache.DesignationsByDepartment.TryGetValue(d.DepartmentId, out var map))
                {
                    map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    cache.DesignationsByDepartment[d.DepartmentId] = map;
                }

                map[d.Name.Trim()] = d.Id;
            }

            var roles = await _db.Roles
                .Where(x => x.TenantId == tenantId && !x.IsDeleted)
                .Select(x => new { x.Id, x.Name })
                .ToListAsync();
            foreach (var r in roles)
                if (!string.IsNullOrWhiteSpace(r.Name) && !cache.Roles.ContainsKey(r.Name.Trim()))
                    cache.Roles[r.Name.Trim()] = r.Id;

            var shifts = await _db.Shifts
                .Where(x => x.TenantId == tenantId && !x.IsDeleted)
                .Select(x => new { x.Id, x.Name })
                .ToListAsync();
            foreach (var s in shifts)
                if (!string.IsNullOrWhiteSpace(s.Name) && !cache.Shifts.ContainsKey(s.Name.Trim()))
                    cache.Shifts[s.Name.Trim()] = s.Id;

            var employees = await _db.Employees
                .Where(x => x.TenantId == tenantId && !x.IsDeleted)
                .Select(x => new { x.Id, x.EmployeeCode, x.Phone })
                .ToListAsync();
            foreach (var e in employees)
            {
                if (!string.IsNullOrWhiteSpace(e.EmployeeCode))
                    cache.EmployeeCodeToId[e.EmployeeCode.Trim()] = e.Id;

                if (!string.IsNullOrWhiteSpace(e.Phone))
                    cache.ExistingPhones.Add(e.Phone.Trim());
            }

            // Email uniqueness is enforced on Users (see EmployeeService.CreateAsync),
            // not Employees - mirrored here exactly. Username has NO IsDeleted
            // filter, matching EmployeeService.GenerateUsername's own check,
            // since a soft-deleted employee's User row (and its Username)
            // still exists and would otherwise collide silently at commit time.
            var users = await _db.Users
                .Where(x => x.TenantId == tenantId)
                .Select(x => new { x.Email, x.Username, x.IsDeleted })
                .ToListAsync();
            foreach (var u in users)
            {
                if (!u.IsDeleted && !string.IsNullOrWhiteSpace(u.Email))
                    cache.ExistingUserEmails.Add(u.Email.Trim());

                if (!string.IsNullOrWhiteSpace(u.Username))
                    cache.ExistingUsernames.Add(u.Username.Trim());
            }

            return cache;
        }

        // ==============================================================
        // ROW VALIDATION - the single method both Preview and Commit call.
        // ==============================================================
        private class RowValidationOutcome
        {
            public EmployeeImportRowValidationDto Result = null!;
            public EmployeeDto? Dto;
        }

        private List<RowValidationOutcome> ValidateRows(
            List<EmployeeImportRowInputDto> rows,
            MasterDataCache cache,
            string tenantId,
            string userId)
        {
            var outcomes = new List<RowValidationOutcome>();

            // Within-file duplicate detection - a batch can never contain
            // two rows claiming the same Employee Code / Username, Email or
            // Phone, even if none of them collide with the database.
            var seenCodes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var seenEmails = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var seenPhones = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows.OrderBy(x => x.RowNumber))
            {
                var errors = new List<ImportFieldError>();

                string? Norm(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();

                var employeeCode = Norm(row.EmployeeCode);
                var firstName = Norm(row.FirstName);
                var lastName = Norm(row.LastName);
                var genderText = Norm(row.Gender);
                var maritalStatusText = Norm(row.MaritalStatus);
                var dobText = Norm(row.DateOfBirth);
                var email = Norm(row.Email);
                var phone = Norm(row.Phone);
                var emergencyContact = Norm(row.EmergencyContact);
                var companyName = Norm(row.CompanyName);
                var branchName = Norm(row.BranchName);
                var departmentName = Norm(row.DepartmentName);
                var designationName = Norm(row.DesignationName);
                var roleName = Norm(row.RoleName);
                var managerCode = Norm(row.ReportingManagerCode);
                var shiftName = Norm(row.ShiftName);
                var employmentTypeText = Norm(row.EmploymentType);
                var joiningDateText = Norm(row.JoiningDate);
                var address = Norm(row.Address);
                var pincode = Norm(row.Pincode);
                var pan = Norm(row.PANNumber);
                var aadhaar = Norm(row.AadharNumber);

                // ---------- Mandatory fields (single source of truth: the
                // 7 fields named in the Import spec) ----------
                if (string.IsNullOrWhiteSpace(employeeCode))
                    errors.Add(new ImportFieldError { Field = "Employee Code", Value = "", Error = "Employee Code is required." });
                else if (employeeCode.Length > 20)
                    errors.Add(new ImportFieldError { Field = "Employee Code", Value = employeeCode, Error = "Employee Code cannot exceed 20 characters." });
                else if (!EmployeeCodePattern.IsMatch(employeeCode))
                    errors.Add(new ImportFieldError { Field = "Employee Code", Value = employeeCode, Error = "Employee Code can contain only letters, numbers, hyphen (-) and underscore (_)." });

                if (string.IsNullOrWhiteSpace(firstName))
                    errors.Add(new ImportFieldError { Field = "First Name", Value = "", Error = "First Name is required." });
                else if (firstName.Length < 2 || firstName.Length > 100)
                    errors.Add(new ImportFieldError { Field = "First Name", Value = firstName, Error = "First Name must be between 2 and 100 characters." });

                if (!string.IsNullOrWhiteSpace(lastName) && lastName.Length > 100)
                    errors.Add(new ImportFieldError { Field = "Last Name", Value = lastName, Error = "Last Name cannot exceed 100 characters." });

                Gender gender = default;
                if (string.IsNullOrWhiteSpace(genderText))
                    errors.Add(new ImportFieldError { Field = "Gender", Value = "", Error = "Gender is required." });
                else if (!Enum.TryParse(genderText, true, out gender))
                    errors.Add(new ImportFieldError { Field = "Gender", Value = genderText, Error = "Invalid Gender. Use Male, Female or Other." });

                string? companyId = null;
                if (string.IsNullOrWhiteSpace(companyName))
                    errors.Add(new ImportFieldError { Field = "Company", Value = "", Error = "Company is required." });
                else if (!cache.Companies.TryGetValue(companyName, out companyId))
                    errors.Add(new ImportFieldError { Field = "Company", Value = companyName, Error = $"Company '{companyName}' was not found." });

                string? roleId = null;
                if (string.IsNullOrWhiteSpace(roleName))
                    errors.Add(new ImportFieldError { Field = "Role", Value = "", Error = "Role is required." });
                else if (!cache.Roles.TryGetValue(roleName, out roleId))
                    errors.Add(new ImportFieldError { Field = "Role", Value = roleName, Error = $"Role '{roleName}' was not found." });

                string? shiftId = null;
                if (string.IsNullOrWhiteSpace(shiftName))
                    errors.Add(new ImportFieldError { Field = "Shift", Value = "", Error = "Shift is required." });
                else if (!cache.Shifts.TryGetValue(shiftName, out shiftId))
                    errors.Add(new ImportFieldError { Field = "Shift", Value = shiftName, Error = $"Shift '{shiftName}' was not found." });

                EmploymentType employmentType = default;
                if (string.IsNullOrWhiteSpace(employmentTypeText))
                    errors.Add(new ImportFieldError { Field = "Employment Type", Value = "", Error = "Employment Type is required." });
                else if (!Enum.TryParse(employmentTypeText, true, out employmentType) || employmentType == EmploymentType.Unknown)
                    errors.Add(new ImportFieldError { Field = "Employment Type", Value = employmentTypeText, Error = "Invalid Employment Type." });

                // ---------- Optional fields - format-validated only when present ----------
                MaritalStatus maritalStatus = MaritalStatus.Unmarried;
                if (!string.IsNullOrWhiteSpace(maritalStatusText) && !Enum.TryParse(maritalStatusText, true, out maritalStatus))
                    errors.Add(new ImportFieldError { Field = "Marital Status", Value = maritalStatusText, Error = "Invalid Marital Status. Use Married, Unmarried or Divorced." });

                DateTime? dob = null;
                if (!string.IsNullOrWhiteSpace(dobText))
                {
                    if (!DateTime.TryParse(dobText, out var parsedDob))
                        errors.Add(new ImportFieldError { Field = "Date of Birth", Value = dobText, Error = "Invalid date. Use yyyy-mm-dd." });
                    else if (parsedDob.Date > DateTime.Today)
                        errors.Add(new ImportFieldError { Field = "Date of Birth", Value = dobText, Error = "Date of Birth cannot be in the future." });
                    else
                        dob = parsedDob;
                }

                if (!string.IsNullOrWhiteSpace(email))
                {
                    if (email.Length > 150 || !EmailValidator.IsValid(email))
                        errors.Add(new ImportFieldError { Field = "Email", Value = email, Error = "Invalid email address." });
                    else
                    {
                        if (cache.ExistingUserEmails.Contains(email))
                            errors.Add(new ImportFieldError { Field = "Email", Value = email, Error = "A user already exists with this email." });
                        else if (seenEmails.TryGetValue(email, out var firstRow))
                            errors.Add(new ImportFieldError { Field = "Email", Value = email, Error = $"Duplicate Email - also used on row {firstRow}." });
                        else
                            seenEmails[email] = row.RowNumber;
                    }
                }

                if (!string.IsNullOrWhiteSpace(phone))
                {
                    if (!MobilePattern.IsMatch(phone))
                        errors.Add(new ImportFieldError { Field = "Phone", Value = phone, Error = "Invalid mobile number. Must be 10 digits starting 6-9." });
                    else
                    {
                        if (cache.ExistingPhones.Contains(phone))
                            errors.Add(new ImportFieldError { Field = "Phone", Value = phone, Error = "An employee already exists with this phone number." });
                        else if (seenPhones.TryGetValue(phone, out var firstRow))
                            errors.Add(new ImportFieldError { Field = "Phone", Value = phone, Error = $"Duplicate Phone - also used on row {firstRow}." });
                        else
                            seenPhones[phone] = row.RowNumber;
                    }
                }

                if (!string.IsNullOrWhiteSpace(emergencyContact) && !MobilePattern.IsMatch(emergencyContact))
                    errors.Add(new ImportFieldError { Field = "Emergency Contact", Value = emergencyContact, Error = "Invalid Emergency Contact. Must be 10 digits starting 6-9." });

                string? branchId = null;
                if (!string.IsNullOrWhiteSpace(branchName))
                {
                    if (companyId == null)
                        errors.Add(new ImportFieldError { Field = "Branch", Value = branchName, Error = "Branch cannot be validated because Company is invalid." });
                    else if (!cache.BranchesByCompany.TryGetValue(companyId, out var branchMap) || !branchMap.TryGetValue(branchName, out branchId))
                        errors.Add(new ImportFieldError { Field = "Branch", Value = branchName, Error = $"Branch '{branchName}' was not found under the selected Company." });
                }

                string? departmentId = null;
                if (!string.IsNullOrWhiteSpace(departmentName) && !cache.Departments.TryGetValue(departmentName, out departmentId))
                    errors.Add(new ImportFieldError { Field = "Department", Value = departmentName, Error = $"Department '{departmentName}' was not found." });

                string? designationId = null;
                if (!string.IsNullOrWhiteSpace(designationName))
                {
                    if (string.IsNullOrWhiteSpace(departmentName))
                        errors.Add(new ImportFieldError { Field = "Designation", Value = designationName, Error = "Department is required when Designation is specified." });
                    else if (departmentId == null)
                        errors.Add(new ImportFieldError { Field = "Designation", Value = designationName, Error = "Designation cannot be validated because Department is invalid." });
                    else if (!cache.DesignationsByDepartment.TryGetValue(departmentId, out var desigMap) || !desigMap.TryGetValue(designationName, out designationId))
                        errors.Add(new ImportFieldError { Field = "Designation", Value = designationName, Error = $"Designation '{designationName}' was not found under Department '{departmentName}'." });
                }

                string? reportingManagerId = null;
                if (!string.IsNullOrWhiteSpace(managerCode))
                {
                    if (!cache.EmployeeCodeToId.TryGetValue(managerCode, out reportingManagerId))
                        errors.Add(new ImportFieldError { Field = "Reporting Manager", Value = managerCode, Error = $"Reporting Manager with Employee Code '{managerCode}' was not found. The manager must already exist in the system." });
                    else if (!string.IsNullOrWhiteSpace(employeeCode) && string.Equals(managerCode, employeeCode, StringComparison.OrdinalIgnoreCase))
                        errors.Add(new ImportFieldError { Field = "Reporting Manager", Value = managerCode, Error = "Employee cannot report to themselves." });
                }

                DateTime joiningDate = DateTime.Today;
                if (!string.IsNullOrWhiteSpace(joiningDateText))
                {
                    if (!DateTime.TryParse(joiningDateText, out joiningDate))
                    {
                        errors.Add(new ImportFieldError { Field = "Joining Date", Value = joiningDateText, Error = "Invalid date. Use yyyy-mm-dd." });
                        joiningDate = DateTime.Today;
                    }
                }

                if (dob.HasValue && joiningDate <= dob.Value)
                    errors.Add(new ImportFieldError { Field = "Joining Date", Value = joiningDateText ?? "", Error = "Joining Date must be after Date of Birth." });

                if (!string.IsNullOrWhiteSpace(address) && address.Length > 500)
                    errors.Add(new ImportFieldError { Field = "Address", Value = address, Error = "Address cannot exceed 500 characters." });

                if (!string.IsNullOrWhiteSpace(pincode) && !PincodePattern.IsMatch(pincode))
                    errors.Add(new ImportFieldError { Field = "Pincode", Value = pincode, Error = "Invalid Pincode. Must be 6 digits." });

                if (!string.IsNullOrWhiteSpace(pan) && !PanPattern.IsMatch(pan))
                    errors.Add(new ImportFieldError { Field = "PAN Number", Value = pan, Error = "Invalid PAN Number format (e.g. ABCDE1234F)." });

                if (!string.IsNullOrWhiteSpace(aadhaar) && !AadhaarPattern.IsMatch(aadhaar))
                    errors.Add(new ImportFieldError { Field = "Aadhaar Number", Value = aadhaar, Error = "Invalid Aadhaar Number. Must be 12 digits." });

                // ---------- Employee Code / Username duplicate checks ----------
                if (!string.IsNullOrWhiteSpace(employeeCode))
                {
                    if (cache.EmployeeCodeToId.ContainsKey(employeeCode))
                        errors.Add(new ImportFieldError { Field = "Employee Code", Value = employeeCode, Error = $"Employee Code '{employeeCode}' already exists." });
                    else if (cache.ExistingUsernames.Contains(employeeCode))
                        errors.Add(new ImportFieldError { Field = "Employee Code", Value = employeeCode, Error = $"Username '{employeeCode}' (generated from Employee Code) is already taken." });
                    else if (seenCodes.TryGetValue(employeeCode, out var firstRow))
                        errors.Add(new ImportFieldError { Field = "Employee Code", Value = employeeCode, Error = $"Duplicate Employee Code - also used on row {firstRow}." });
                    else
                        seenCodes[employeeCode] = row.RowNumber;
                }

                var isValid = errors.Count == 0;

                var validationResult = new EmployeeImportRowValidationDto
                {
                    RowNumber = row.RowNumber,
                    EmployeeCode = employeeCode,
                    EmployeeName = string.Join(" ", new[] { firstName, lastName }.Where(x => !string.IsNullOrWhiteSpace(x))),
                    CompanyName = companyName,
                    RoleName = roleName,
                    ShiftName = shiftName,
                    EmploymentType = employmentTypeText,
                    Email = email,
                    Phone = phone,
                    IsValid = isValid,
                    Errors = errors
                };

                EmployeeDto? dto = null;

                if (isValid)
                {
                    dto = new EmployeeDto
                    {
                        FirstName = firstName!,
                        LastName = lastName,
                        EmployeeCode = employeeCode!,
                        RoleId = roleId!,
                        TenantId = tenantId,
                        CompanyId = companyId!,
                        BranchId = branchId,
                        ShiftId = shiftId!,
                        DepartmentId = departmentId,
                        DesignationId = designationId,
                        ReportingManagerId = reportingManagerId,
                        DateOfBirth = dob,
                        Gender = gender,
                        MaritalStatus = maritalStatus,
                        Email = email!, // EmployeeDto.Email is non-nullable; blank import rows are handled in EmployeeService (Phone-style optional path) - see remarks below.
                        Phone = phone,
                        EmergencyContact = emergencyContact,
                        Address = address,
                        Pincode = pincode,
                        PANNumber = pan,
                        AadharNumber = aadhaar,
                        JoiningDate = joiningDate,
                        EmploymentType = employmentType,
                        CreatedBy = userId
                    };
                }

                outcomes.Add(new RowValidationOutcome { Result = validationResult, Dto = dto });
            }

            return outcomes;
        }

        // ==============================================================
        // PUBLIC API
        // ==============================================================
        public async Task<EmployeeImportPreviewResultDto> ValidateImportAsync(
            List<EmployeeImportRowInputDto> rows,
            string tenantId,
            string userId,
            string userName)
        {
            try
            {
                var cache = await LoadMasterDataAsync(tenantId);
                var outcomes = ValidateRows(rows ?? new(), cache, tenantId, userId);

                var resultRows = outcomes.Select(x => x.Result).ToList();

                return new EmployeeImportPreviewResultDto
                {
                    TotalRows = resultRows.Count,
                    ValidCount = resultRows.Count(x => x.IsValid),
                    InvalidCount = resultRows.Count(x => !x.IsValid),
                    Rows = resultRows
                };
            }
            catch (Exception ex)
            {
                await _errorLogService.LogAsync(ex, module: "Employee", feature: "Import",
                    controller: "EmployeeImportExportService", action: nameof(ValidateImportAsync),
                    userId: userId, userName: userName, tenantId: tenantId);
                throw;
            }
        }

        public async Task<EmployeeImportCommitResultDto> CommitImportAsync(
            List<EmployeeImportRowInputDto> rows,
            string tenantId,
            string userId,
            string userName)
        {
            try
            {
                var cache = await LoadMasterDataAsync(tenantId);
                var outcomes = ValidateRows(rows ?? new(), cache, tenantId, userId);

                var resultRows = outcomes.Select(x => x.Result).ToList();
                var invalidCount = resultRows.Count(x => !x.IsValid);

                // "Validate all rows before database insertion; do not
                // partially import invalid data" - a single invalid row
                // rejects the entire batch, nothing is written.
                if (invalidCount > 0 || resultRows.Count == 0)
                {
                    return new EmployeeImportCommitResultDto
                    {
                        Success = false,
                        TotalRows = resultRows.Count,
                        SuccessCount = 0,
                        FailureCount = resultRows.Count,
                        Message = resultRows.Count == 0
                            ? "The uploaded file didn't contain any employee rows."
                            : $"{invalidCount} of {resultRows.Count} row(s) failed validation. Nothing was imported - fix the errors below and re-upload.",
                        Rows = resultRows
                    };
                }

                // Every row is valid - create them all inside one
                // transaction so a failure on any single row (e.g. a rare
                // race-condition duplicate) rolls back everything already
                // created in this same commit, instead of leaving a
                // partially-imported batch behind.
                //
                // The DbContext is configured with SqlServer's
                // EnableRetryOnFailure (SqlServerRetryingExecutionStrategy),
                // which refuses a plain Database.BeginTransactionAsync()
                // call - "does not support user-initiated transactions" -
                // because a retried transient failure would otherwise replay
                // only part of the work. The fix is to run the whole
                // begin/commit/rollback sequence as a single retriable unit
                // via CreateExecutionStrategy().ExecuteAsync(...), which is
                // what EF Core requires whenever retry-on-failure is on. Any
                // failure is still handled (and NOT rethrown) inside the
                // callback itself, so this executes as a single attempt -
                // consistent with "do not partially import invalid data".
                var strategy = _db.Database.CreateExecutionStrategy();
                EmployeeImportCommitResultDto? failureResult = null;

                await strategy.ExecuteAsync(async () =>
                {
                    await using var transaction = await _db.Database.BeginTransactionAsync();

                    try
                    {
                        foreach (var outcome in outcomes)
                        {
                            await _employeeService.CreateAsync(outcome.Dto!);
                            outcome.Result.Message = "Imported successfully.";
                        }

                        await transaction.CommitAsync();
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();

                        await _errorLogService.LogAsync(ex, module: "Employee", feature: "Import",
                            controller: "EmployeeImportExportService", action: nameof(CommitImportAsync),
                            userId: userId, userName: userName, tenantId: tenantId);

                        failureResult = new EmployeeImportCommitResultDto
                        {
                            Success = false,
                            TotalRows = resultRows.Count,
                            SuccessCount = 0,
                            FailureCount = resultRows.Count,
                            Message = $"Import failed and was rolled back - no employees were created. Reason: {ex.Message}",
                            Rows = resultRows
                        };
                    }
                });

                if (failureResult != null)
                {
                    return failureResult;
                }

                return new EmployeeImportCommitResultDto
                {
                    Success = true,
                    TotalRows = resultRows.Count,
                    SuccessCount = resultRows.Count,
                    FailureCount = 0,
                    Message = $"All {resultRows.Count} employee(s) imported successfully.",
                    Rows = resultRows
                };
            }
            catch (Exception ex)
            {
                await _errorLogService.LogAsync(ex, module: "Employee", feature: "Import",
                    controller: "EmployeeImportExportService", action: nameof(CommitImportAsync),
                    userId: userId, userName: userName, tenantId: tenantId);
                throw;
            }
        }
    }
}
