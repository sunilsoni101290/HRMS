using Application.DTOs.Employee;
using Application.DTOs.Onboarding;
using Application.Interfaces;
using Application.Interfaces.EmployeeInterface;
using Application.Interfaces.Masters;
using Application.Interfaces.Onboarding;
using Application.Mappings;
using Domain.Entities;
using Domain.Helper;
using Domain.Interfaces;
using Infrastructure;
using Infrastructure.Data;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;

namespace Application.Services.EmployeeServices
{
    public class EmployeeService : BaseService, IEmployeeService
    {
        private readonly ITenantService _tenantService;
        private readonly ISequenceService _sequenceService;
        private readonly IFinancialYearService _financialYearService;

        // One-directional dependency only (Onboarding -> Employee has no
        // reverse dependency, so no circular DI here) - used to
        // auto-start an OnboardingCase right after a new Employee is
        // created FROM a Recruitment Candidate conversion (EmployeeDto.CandidateId
        // set). See CreateAsync below.
        private readonly IOnboardingService _onboardingService;

        public EmployeeService(ApplicationDbContext db, ITenantService tenantService, ISequenceService sequenceService,
            IFinancialYearService financialYearService, IOnboardingService onboardingService) : base(db)
        {
            _tenantService = tenantService;
            _sequenceService = sequenceService;
            _financialYearService = financialYearService;
            _onboardingService = onboardingService;
        }
        public async Task<PagedResult<EmployeeListDto>> SearchAsync(EmployeeSearchRequest request)
        {
            try
            {
            var query = _db.Employees
                .Where(x => !x.IsDeleted)
                .AsQueryable();

            // =========================
            // 🔍 SEARCH
            // =========================
            if (!string.IsNullOrEmpty(request.SearchText))
            {
                var search = request.SearchText.ToLower();

                query = query.Where(x =>
                    x.FirstName.ToLower().Contains(search) ||
                    x.LastName.ToLower().Contains(search) ||
                    x.Phone.Contains(search) ||
                    x.Email.ToLower().Contains(search)
                );
            }

            // =========================
            // 🎯 FILTERS
            // =========================
            if (!string.IsNullOrEmpty(request.DepartmentId))
                query = query.Where(x => x.DepartmentId == request.DepartmentId);

            if (!string.IsNullOrEmpty(request.DesignationId))
                query = query.Where(x => x.DesignationId == request.DesignationId);

            if (!string.IsNullOrEmpty(request.CompanyId))
                query = query.Where(x => x.CompanyId == request.CompanyId);

            // =========================
            // 🔄 SORTING
            // =========================
            query = request.SortDirection?.ToLower() == "asc"
                ? query.OrderBy(x => EF.Property<object>(x, request.SortBy))
                : query.OrderByDescending(x => EF.Property<object>(x, request.SortBy));

            // =========================
            // 📊 TOTAL COUNT
            // =========================
            var totalRecords = await query.CountAsync();

            // =========================
            // 📄 PAGINATION
            // =========================
            var data = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(x => EmployeeMapper.ToDto(x))
                .ToListAsync();

            return new PagedResult<EmployeeListDto>
            {
                TotalRecords = totalRecords,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                Data = data
            };
            }
            catch (Exception)
            {
                return null;
            }
        }
        public async Task<string> CreateAsync(EmployeeDto dto)
        {
            try
            {
            // =========================================
            // VALIDATION
            // =========================================

            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            if (string.IsNullOrWhiteSpace(dto.FirstName))
                throw new Exception("First name is required");

            // Phone is optional on the Add/Edit Employee form (only
            // Employee Code, First Name, Gender, Role and Company are
            // required) - so it's no longer rejected when blank, only
            // format-validated when the caller actually provided one.
            if (!string.IsNullOrWhiteSpace(dto.Phone) && !Regex.IsMatch(dto.Phone, @"^[0-9]{10}$"))
                throw new Exception("Invalid phone number");

            if (string.IsNullOrWhiteSpace(dto.Email))
                throw new Exception("Email is required");

            if (!new EmailAddressAttribute().IsValid(dto.Email))
                throw new Exception("Invalid email format");

            try
            {
                // =========================================
                // DUPLICATE CHECK
                // =========================================

                // Skipped when blank - Phone is now optional, and without
                // this guard every employee with no phone on file would
                // incorrectly collide with each other (NULL = NULL).
                if (!string.IsNullOrWhiteSpace(dto.Phone))
                {
                    bool phoneExists = await _db.Employees
                        .AnyAsync(x => x.Phone == dto.Phone && !x.IsDeleted);

                    if (phoneExists)
                        throw new Exception("Employee already exists with same phone number");
                }

                bool emailExists = await _db.Users
                    .AnyAsync(x => x.Email == dto.Email && !x.IsDeleted);

                if (emailExists)
                    throw new Exception("User already exists with same email");

                // Employee Code duplicate check (defect fix - previously
                // never validated at all, relying only on the database's
                // unique index to reject a duplicate with a raw SQL
                // exception). Skipped when blank so the separate
                // required-field validation owns that case.
                if (!string.IsNullOrWhiteSpace(dto.EmployeeCode))
                {
                    bool codeExists = await CheckEmployeeCodeExistsAsync(dto.EmployeeCode, null);

                    if (codeExists)
                        throw new Exception($"Employee Code '{dto.EmployeeCode.Trim()}' already exists.");
                }

                // =========================================
                // CREATE EMPLOYEE
                // =========================================

                var employee = EmployeeMapper.ToEntity(dto);

                employee.Id = IDManager.GetNewId(new Employee());

                // Mutate the caller's dto in place so callers (e.g. the API
                // controller) can read back the newly-generated Id after
                // this method returns - CreateAsync's return type is the
                // shared IBaseService<TDto> string contract (credentials
                // message), so this is the only way to surface the new
                // Employee's Id without changing that shared interface.
                dto.Id = employee.Id;

                employee.CreatedBy = string.IsNullOrWhiteSpace(dto.CreatedBy)
                    ? "System"
                    : dto.CreatedBy;

                employee.IsDeleted = false;

                _db.Employees.Add(employee);

                try
                {
                    await _db.SaveChangesAsync();
                }
                catch (DbUpdateException ex) when (IsEmployeeCodeUniqueViolation(ex))
                {
                    // Race condition safety net (spec section 14): the
                    // AnyAsync check above already ran and passed, but
                    // another request could have inserted the same code in
                    // the gap between that check and this save. The
                    // Employees.EmployeeCode unique index (already present
                    // in ApplicationDbContext.OnModelCreating) is what
                    // actually guarantees correctness here - this just
                    // turns its raw SQL exception into the same
                    // user-friendly message as the pre-check above, instead
                    // of a generic "database error".
                    throw new Exception($"Employee Code '{dto.EmployeeCode?.Trim()}' already exists.");
                }

                // =========================================
                // GENERATE USERNAME
                // =========================================

                string username = await GenerateUsername(dto.Email);

                // =========================================
                // GENERATE PASSWORD
                // =========================================

                string plainPassword =
                    PasswordGenerator.GeneratePassword(6);

                string passwordHash =
                    BCrypt.Net.BCrypt.HashPassword(
                        plainPassword,
                        workFactor: 12);

                // =========================================
                // CREATE USER
                // =========================================

                var user = new User
                {
                    Id = IDManager.GetNewId(new User()),

                    Username = username,

                    Email = dto.Email.Trim().ToLower(),

                    PhoneNumber = dto.Phone,

                    EmployeeId = employee.Id,

                    TenantId = dto.TenantId,

                    CompanyId = dto.CompanyId,

                    BranchId = dto.BranchId,

                    EmailConfirmed = dto.EmailConfirmed,

                    PhoneConfirmed = dto.PhoneConfirmed,

                    PasswordHash = passwordHash,

                    PasswordChangedOn = DateTime.UtcNow,

                    IsActive = true,

                    IsDeleted = false,

                    CreatedBy = string.IsNullOrWhiteSpace(dto.CreatedBy)
                        ? "System"
                        : dto.CreatedBy
                };

                _db.Users.Add(user);

                // =========================================
                // ROLE ASSIGNMENT
                // =========================================

                string roleId = dto.RoleId;

                if (string.IsNullOrWhiteSpace(roleId))
                {
                    var defaultRole = await _db.Roles
                        .FirstOrDefaultAsync(x =>
                            x.Code == ConstantHelper.EMPLOYEE &&
                            !x.IsDeleted);

                    if (defaultRole == null)
                        throw new Exception("Default employee role not found");

                    roleId = defaultRole.Id;
                }

                bool roleExists = await _db.Roles
                    .AnyAsync(x => x.Id == roleId && !x.IsDeleted);

                if (!roleExists)
                    throw new Exception("Invalid role selected");

                bool userRoleExists = await _db.UserRoles
                    .AnyAsync(x =>
                        x.UserId == user.Id &&
                        x.RoleId == roleId);

                if (!userRoleExists)
                {
                    var userRole = new UserRole
                    {
                        Id = IDManager.GetNewId(new UserRole()),

                        UserId = user.Id,

                        RoleId = roleId,

                        CreatedBy = string.IsNullOrWhiteSpace(dto.CreatedBy)
                            ? "System"
                            : dto.CreatedBy
                    };

                    _db.UserRoles.Add(userRole);
                }

                // =========================================
                // SAVE ALL
                // =========================================

                await _db.SaveChangesAsync();

                // =========================================
                // ONBOARDING (best-effort, never blocks employee creation)
                // =========================================
                // If this Employee was created from a Recruitment Candidate
                // conversion (EmployeeDto.CandidateId set), auto-start their
                // OnboardingCase. Deliberately swallowed on failure - the
                // Employee + User + credentials above are already committed,
                // so a template/onboarding misconfiguration must not turn a
                // successful employee creation into a reported failure. HR
                // can always start the case manually afterwards.
                if (!string.IsNullOrWhiteSpace(dto.CandidateId))
                {
                    try
                    {
                        await _onboardingService.CreateCaseAsync(
                            new CreateOnboardingCaseDto
                            {
                                EmployeeId = employee.Id,
                                CandidateId = dto.CandidateId
                            },
                            dto.TenantId,
                            employee.CreatedBy);
                    }
                    catch
                    {
                        // Swallowed by design - see comment above.
                    }
                }

                // =========================================
                // COMMIT
                // =========================================

                return $"Username : {username} , Password : {plainPassword}";
            }
            catch
            {
                throw;
            }
            }
            catch (Exception)
            {
                // Previously swallowed every failure here (duplicate phone,
                // duplicate email, DB errors, etc.) and returned
                // ex.ToString() as if it were the success string - the API
                // controller never inspected the returned value, so it
                // always reported "Employee created successfully" even when
                // creation had actually failed, and the real error was
                // silently discarded. Rethrow instead so the caller's own
                // try/catch (API EmployeeController.Create) reports the
                // actual failure.
                throw;
            }
        }

        // ==============================
        // 🔹 DELETE (SOFT DELETE)
        // ==============================
        public async Task<bool> DeleteMultipleAsync(List<string> ids)
        {
            try
            {
            if (ids == null || !ids.Any())
                return false;

            var employees = await _db.Employees
                .Where(x => ids.Contains(x.Id) && !x.IsDeleted)
                .ToListAsync();

            if (!employees.Any())
                return false;

            foreach (var emp in employees)
            {
                emp.IsDeleted = true;
                emp.ModifiedOn = DateTime.UtcNow;
                emp.ModifiedBy = "System";
            }

            await SaveAsync();
            return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // ==============================
        // 🔹 GET ALL
        // ==============================
        public async Task<List<EmployeeListDto>> GetAllAsync()
        {
            try
            {
            var employees = await _db.Employees
                .Include(x => x.Company)
                .Include(x => x.Branch)
                .Include(x => x.Department)
                .Include(x => x.Designation)
                .Include(x => x.ReportingManager)
                .Include(x => x.DefaultShift)
                .Include(x => x.Country)
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .ToListAsync();

            return employees
                .Select(EmployeeMapper.ToDto)
                .ToList();
            }
            catch (Exception)
            {
                return new List<EmployeeListDto>();
            }
        }

        // ==============================
        // 🔹 FILTER BY DEPARTMENT
        // ==============================
        public async Task<List<EmployeeListDto>> GetByDepartmentAsync(string departmentId)
        {
            try
            {
            return await _db.Employees
                .Where(x => x.DepartmentId == departmentId && !x.IsDeleted)
                .Select(x => EmployeeMapper.ToDto(x))
                .ToListAsync();
            }
            catch (Exception)
            {
                return new List<EmployeeListDto>();
            }
        }

        // ==============================
        // 🔹 FILTER BY DESIGNATION
        // ==============================
        public async Task<List<EmployeeListDto>> GetByDesignationAsync(string designationId)
        {
            try
            {
            return await _db.Employees
                .Where(x => x.DesignationId == designationId && !x.IsDeleted)
                .Select(x => EmployeeMapper.ToDto(x))
                .ToListAsync();
            }
            catch (Exception)
            {
                return new List<EmployeeListDto>();
            }
        }

        // ==============================
        // 🔹 GET BY ID
        // ==============================
        public async Task<EmployeeListDto> GetByIdAsync(string id)
        {
            try
            {
            var entity = await _db.Employees.AsNoTracking()
                .Include(x=>x.Company)
                .Include(x=>x.Branch)
                .Include(x=>x.Department)
                .Include(x=>x.Designation)
                .Include(x=>x.ReportingManager)
                .Include(x=>x.DefaultShift)
                .Include(x=>x.Country)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null)
                return null;

            return EmployeeMapper.ToDto(entity);
            }
            catch (Exception)
            {
                return null;
            }
        }

        // ==============================
        // 🔹 UPDATE
        // ==============================
        public async Task<bool> UpdateAsync(EmployeeDto dto)
        {
            // Previously wrapped everything (including "Employee not
            // found") in a try/catch that swallowed the real reason and
            // always returned false - the API controller then always
            // reported the same generic "Employee update failed" no matter
            // what actually went wrong. Now lets specific, meaningful
            // failures (not found, duplicate Employee Code) propagate as
            // exceptions with a real message, same contract CreateAsync
            // already uses - the API controller's Update action has been
            // updated to catch and surface these (see EmployeeController.cs).
            var entity = await _db.Employees.FindAsync(dto.Id);

            if (entity == null || entity.IsDeleted)
                throw new Exception("Employee not found");

            if (!string.IsNullOrWhiteSpace(dto.EmployeeCode))
            {
                bool codeExists = await CheckEmployeeCodeExistsAsync(dto.EmployeeCode, dto.Id);

                if (codeExists)
                    throw new Exception($"Employee Code '{dto.EmployeeCode.Trim()}' already exists.");
            }

            EmployeeMapper.UpdateEntity(entity, dto);

            try
            {
                await SaveAsync();
            }
            catch (DbUpdateException ex) when (IsEmployeeCodeUniqueViolation(ex))
            {
                // Race-condition safety net - see the matching comment in
                // CreateAsync.
                throw new Exception($"Employee Code '{dto.EmployeeCode?.Trim()}' already exists.");
            }

            return true;
        }

        // Single source of truth for "does this Employee Code already
        // belong to a different employee" - used by CreateAsync,
        // UpdateAsync, and the check-employee-code endpoint the Create/Edit
        // form calls on blur. Equality is a plain EF `==`, so it is
        // translated to SQL `=` and respects whatever collation the
        // Employees.EmployeeCode column already has (no ToUpper/ToLower
        // forced here, so behavior never diverges from what the database
        // itself, and its unique index, actually enforce).
        public async Task<bool> CheckEmployeeCodeExistsAsync(string employeeCode, string? excludeEmployeeId)
        {
            if (string.IsNullOrWhiteSpace(employeeCode))
                return false;

            var normalized = employeeCode.Trim();

            return await _db.Employees.AnyAsync(x =>
                x.EmployeeCode == normalized &&
                !x.IsDeleted &&
                (string.IsNullOrEmpty(excludeEmployeeId) || x.Id != excludeEmployeeId));
        }

        // Best-effort detection of a unique-index violation on
        // Employees.EmployeeCode specifically (SQL Server error 2601/2627
        // for a duplicate key), rather than treating every DbUpdateException
        // as a duplicate-code error.
        private static bool IsEmployeeCodeUniqueViolation(DbUpdateException ex)
        {
            var message = ex.InnerException?.Message ?? ex.Message;

            if (string.IsNullOrEmpty(message))
                return false;

            var looksLikeUniqueViolation =
                message.Contains("2601") ||
                message.Contains("2627") ||
                message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("unique index", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase);

            return looksLikeUniqueViolation && message.Contains("EmployeeCode", StringComparison.OrdinalIgnoreCase);
        }

        // ==============================
        // 🔹 UPDATE PROFILE PHOTO ONLY
        // ==============================
        public async Task<bool> UpdatePhotoAsync(string id, string filePath)
        {
            try
            {
                var entity = await _db.Employees.FindAsync(id);

                if (entity == null || entity.IsDeleted)
                    return false;

                entity.FilePath = filePath;
                entity.ModifiedOn = DateTime.UtcNow;

                await SaveAsync();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // ✅ HIERARCHY
        public async Task<List<EmployeeHierarchyDto>> GetHierarchyAsync(string tenantId)
        {
            try
            {
            var employees = await _db.Employees
                .Where(x => x.TenantId == tenantId && !x.IsDeleted)
                .Select(x => new
                {
                    x.Id,
                    x.FirstName,
                    x.LastName,
                    x.ReportingManagerId,
                    Designation = x.Designation.Name
                })
                .ToListAsync();

            // 🔹 Root employees (no manager)
            var roots = employees
                .Where(x => string.IsNullOrEmpty(x.ReportingManagerId))
                .ToList();

            List<EmployeeHierarchyDto> BuildTree(string parentId)
            {
                return employees
                    .Where(x => x.ReportingManagerId == parentId)
                    .Select(x => new EmployeeHierarchyDto
                    {
                        Id = x.Id,
                        Name = x.FirstName + " " + x.LastName,
                        Designation = x.Designation,
                        Children = BuildTree(x.Id)
                    })
                    .ToList();
            }

            var hierarchy = roots.Select(x => new EmployeeHierarchyDto
            {
                Id = x.Id,
                Name = x.FirstName + " " + x.LastName,
                Designation = x.Designation,
                Children = BuildTree(x.Id)
            }).ToList();

            return hierarchy;
            }
            catch (Exception)
            {
                return new List<EmployeeHierarchyDto>();
            }
        }

        #region GENERATE UNIQUE USERNAME
        private async Task<string> GenerateUsername(string empCode)
        {
            //// Take only part before @
            //string baseUsername = email
            //                        .Split('@')[0]
            //                        .Trim()
            //                        .Replace(" ", "")
            //                        .ToLower();

            string username = empCode;

            if (await _db.Users.AnyAsync(x => x.Username == username))
            {
                username = empCode; //+ PasswordGenerator.GenerateRandomNumber(3);
            }

            return username;
        }
        #endregion
    }
}
