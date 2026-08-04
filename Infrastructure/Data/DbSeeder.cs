using Domain.Entities;
using Domain.Helper;
using Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Infrastructure.Data
{
    public class DbSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            if (context == null) return;

            //context.Database.EnsureCreated();


            if (context == null)
                return;

            // Database accessible?
            if (!await context.Database.CanConnectAsync())
            {
                throw new Exception(
                    "Cannot connect to SQL Server. Please verify the connection string and ensure the database already exists.");
            }

            // Apply pending migrations only.
            //await context.Database.MigrateAsync();

            // =========================
            // 1. COUNTRY
            // =========================
            if (!context.Countries.Any())
            {
                var country = new Country
                {
                    Id = IDManager.GetNewId(new Country()),
                    Name = "India",
                    Code = "IN",
                    PhoneCode = "+91",
                    IsActive = true,
                    CreatedBy = "System"
                };

                await context.Countries.AddAsync(country);
                await context.SaveChangesAsync();
            }

            // 2. Location IDs लो
            var countryId = await context.Countries
                .Where(x => x.Code == "IN")
                .Select(x => x.Id)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(countryId))
            {
                throw new Exception("Country not found");
            }

            // =========================
            // 2. STATE
            // =========================
            if (!context.States.Any())
            {
                var states = new List<State>
                {
                    new State { Id ="BUILTIN_STATE_ANDHRA_PRADESH", Name = "ANDHRA PRADESH", Code = "AP", GSTStateCode = "37", CountryId = countryId, IsActive = true, CreatedBy = "System" },
                    new State { Id ="BUILTIN_STATE_ARUNACHAL_PRADESH", Name = "ARUNACHAL PRADESH", Code = "AR", GSTStateCode = "12", CountryId = countryId, IsActive = true, CreatedBy = "System" },
                    new State { Id ="BUILTIN_STATE_ASSAM", Name = "ASSAM", Code = "AS", GSTStateCode = "18", CountryId = countryId, IsActive = true, CreatedBy = "System" },
                    new State { Id ="BUILTIN_STATE_BIHAR", Name = "BIHAR", Code = "BR", GSTStateCode = "10", CountryId = countryId, IsActive = true, CreatedBy = "System" },
                    new State { Id ="BUILTIN_STATE_CHHATTISGARH", Name = "CHHATTISGARH", Code = "CG", GSTStateCode = "22", CountryId = countryId, IsActive = true, CreatedBy = "System" },
                    new State { Id ="BUILTIN_STATE_GOA", Name = "GOA", Code = "GA", GSTStateCode = "30", CountryId = countryId, IsActive = true, CreatedBy = "System" },
                    new State { Id ="BUILTIN_STATE_GUJARAT", Name = "GUJARAT", Code = "GJ", GSTStateCode = "24", CountryId = countryId, IsActive = true, CreatedBy = "System" },
                    new State { Id ="BUILTIN_STATE_HARYANA", Name = "HARYANA", Code = "HR", GSTStateCode = "06", CountryId = countryId, IsActive = true, CreatedBy = "System" },
                    new State { Id ="BUILTIN_STATE_HIMACHAL_PRADESH", Name = "HIMACHAL PRADESH", Code = "HP", GSTStateCode = "02", CountryId = countryId, IsActive = true, CreatedBy = "System" },
                    new State { Id ="BUILTIN_STATE_JHARKHAND", Name = "JHARKHAND", Code = "JH", GSTStateCode = "20", CountryId = countryId, IsActive = true, CreatedBy = "System" },
                    new State { Id ="BUILTIN_STATE_KARNATAKA", Name = "KARNATAKA", Code = "KA", GSTStateCode = "29", CountryId = countryId, IsActive = true, CreatedBy = "System" },
                    new State { Id ="BUILTIN_STATE_KERALA", Name = "KERALA", Code = "KL", GSTStateCode = "32", CountryId = countryId, IsActive = true, CreatedBy = "System" },
                    new State { Id ="BUILTIN_STATE_MADHYA_PRADESH", Name = "MADHYA PRADESH", Code = "MP", GSTStateCode = "23", CountryId = countryId, IsActive = true, CreatedBy = "System" },
                    new State { Id ="BUILTIN_STATE_MAHARASHTRA", Name = "MAHARASHTRA", Code = "MH", GSTStateCode = "27", CountryId = countryId, IsActive = true, CreatedBy = "System" },
                    new State { Id ="BUILTIN_STATE_MANIPUR", Name = "MANIPUR", Code = "MN", GSTStateCode = "14", CountryId = countryId, IsActive = true, CreatedBy = "System" },
                    new State { Id ="BUILTIN_STATE_MEGHALAYA", Name = "MEGHALAYA", Code = "ML", GSTStateCode = "17", CountryId = countryId, IsActive = true, CreatedBy = "System" },
                    new State { Id ="BUILTIN_STATE_MIZORAM", Name = "MIZORAM", Code = "MZ", GSTStateCode = "15", CountryId = countryId, IsActive = true, CreatedBy = "System" },
                    new State { Id ="BUILTIN_STATE_NAGALAND", Name = "NAGALAND", Code = "NL", GSTStateCode = "13", CountryId = countryId, IsActive = true, CreatedBy = "System" },
                    new State { Id ="BUILTIN_STATE_ODISHA", Name = "ODISHA", Code = "OD", GSTStateCode = "21", CountryId = countryId, IsActive = true, CreatedBy = "System" },
                    new State { Id ="BUILTIN_STATE_PUNJAB", Name = "PUNJAB", Code = "PB", GSTStateCode = "03", CountryId = countryId, IsActive = true, CreatedBy = "System" },
                    new State { Id ="BUILTIN_STATE_RAJASTHAN", Name = "RAJASTHAN", Code = "RJ", GSTStateCode = "08", CountryId = countryId, IsActive = true, CreatedBy = "System" },
                    new State { Id ="BUILTIN_STATE_SIKKIM", Name = "SIKKIM", Code = "SK", GSTStateCode = "11", CountryId = countryId, IsActive = true, CreatedBy = "System" },
                    new State { Id ="BUILTIN_STATE_TAMIL_NADU", Name = "TAMIL NADU", Code = "TN", GSTStateCode = "33", CountryId = countryId, IsActive = true, CreatedBy = "System" },
                    new State { Id ="BUILTIN_STATE_TELANGANA", Name = "TELANGANA", Code = "TS", GSTStateCode = "36", CountryId = countryId, IsActive = true, CreatedBy = "System" },
                    new State { Id ="BUILTIN_STATE_TRIPURA", Name = "TRIPURA", Code = "TR", GSTStateCode = "16", CountryId = countryId, IsActive = true, CreatedBy = "System" },
                    new State { Id ="BUILTIN_STATE_UTTAR_PRADESH", Name = "UTTAR PRADESH", Code = "UP", GSTStateCode = "09", CountryId = countryId, IsActive = true, CreatedBy = "System" },
                    new State { Id ="BUILTIN_STATE_UTTARAKHAND", Name = "UTTARAKHAND", Code = "UK", GSTStateCode = "05", CountryId = countryId, IsActive = true, CreatedBy = "System" },
                    new State { Id ="BUILTIN_STATE_WEST_BENGAL", Name = "WEST BENGAL", Code = "WB", GSTStateCode = "19", CountryId = countryId, IsActive = true, CreatedBy = "System" },

                    // 🔹 UNION TERRITORIES
                    new State { Id ="BUILTIN_STATE_ANDAMAN_AND_NICOBAR_ISLANDS", Name = "ANDAMAN AND NICOBAR ISLANDS", Code = "AN", GSTStateCode = "35", CountryId = countryId, IsActive = true, CreatedBy = "System" },
                    new State { Id ="BUILTIN_STATE_CHANDIGARH", Name = "CHANDIGARH", Code = "CH", GSTStateCode = "04", CountryId = countryId, IsActive = true, CreatedBy = "System" },
                    new State { Id ="BUILTIN_STATE_DADRA_AND_NAGAR_HAVELI_AND_DAMAN_AND_DIU", Name = "DADRA AND NAGAR HAVELI AND DAMAN AND DIU", Code = "DN", GSTStateCode = "26", CountryId = countryId, IsActive = true, CreatedBy = "System" },
                    new State { Id ="BUILTIN_STATE_DELHI", Name = "DELHI", Code = "DL", GSTStateCode = "07", CountryId = countryId, IsActive = true, CreatedBy = "System" },
                    new State { Id ="BUILTIN_STATE_JAMMU_AND_KASHMIR", Name = "JAMMU AND KASHMIR", Code = "JK", GSTStateCode = "01", CountryId = countryId, IsActive = true, CreatedBy = "System" },
                    new State { Id ="BUILTIN_STATE_LADAKH", Name = "LADAKH", Code = "LA", GSTStateCode = "38", CountryId = countryId, IsActive = true, CreatedBy = "System" },
                    new State { Id ="BUILTIN_STATE_LAKSHADWEEP", Name = "LAKSHADWEEP", Code = "LD", GSTStateCode = "31", CountryId = countryId, IsActive = true, CreatedBy = "System" },
                    new State { Id ="BUILTIN_STATE_PUDUCHERRY", Name = "PUDUCHERRY", Code = "PY", GSTStateCode = "34", CountryId = countryId, IsActive = true, CreatedBy = "System" }
                };

                // Insert
                await context.States.AddRangeAsync(states);
                await context.SaveChangesAsync();
            }

            var stateId = await context.States
                    .Where(x => x.Code == "MH")
                    .Select(x => x.Id)
                    .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(stateId))
            {
                throw new Exception("State not found");
            }
            
            // =========================
            // 3. CITY
            // =========================
            if (!context.Cities.Any())
            {
                var cities = new City
                {
                    Id = IDManager.GetNewId(new City()),
                    Name = "Mumbai",
                    StateId = stateId,
                    IsActive = true,
                    CreatedBy = "System"
                };

                await context.Cities.AddAsync(cities);
                await context.SaveChangesAsync();
            }

            var cityId = await context.Cities
                    .Select(x => x.Id)
                    .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(cityId))
                throw new Exception("City not found.");

            // =========================
            // 4. TENANT
            // =========================
            if (!context.Tenants.Any())
            {
                var tenant = new Tenant
                {

                    Id = IDManager.GetNewId(new Tenant()),
                    Name = "Default Tenant",
                    Code = "T001",

                    Domain = "localhost",
                    SubDomain = "default",

                    Email = "admin@default.com",
                    Phone = "9999999999",

                    Address = "Default Address",
                    Pincode = "400001",

                    CountryId = countryId,
                    StateId = stateId,
                    CityId = cityId,

                    SubscriptionStartDate = DateTime.UtcNow,
                    SubscriptionEndDate = DateTime.UtcNow.AddYears(1),

                    WebsiteUrl = "https://default.com",
                    CreatedBy = "System"
                };

                await context.Tenants.AddAsync(tenant);
                await context.SaveChangesAsync();
            }

            // 1. Tenant लो (mandatory)
            var tenantId = await context.Tenants
                .Select(x => x.Id)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(tenantId))
            {
                throw new Exception("Tenant not found");
            }

            await AppFeatureSeeder.SeedAsync(context,tenantId);

            // Idempotent: adds new module menus + rectifies existing ones on every startup
            await AppFeatureSeeder.ReconcileModulesAsync(context, tenantId);
            //await AppFeatureSeeder.SeedTenantFeatureAsync(context);

            // =========================
            // 5. COMPANY
            // =========================
            if (!context.Companies.Any())
            {
                // 3. Company बनाओ
                var company = new Company
                {

                    Id = IDManager.GetNewId(new Company()),
                    Name = "ABC Pvt Ltd",
                    Code = "C001",

                    GSTNumber = "27ABCDE1234F1Z5",
                    PANNumber = "ABCDE1234F",
                    CINNumber = "U12345MH2024PTC123456",

                    Email = "info@abc.com",
                    Phone = "9000000000",
                    AlternatePhone = "9000000001",

                    Address = "Mumbai, Maharashtra",
                    Pincode = "400001",

                    OwnershipType = BusinessOwnershipType.PrivateLimited,
                    BusinessCategory = BusinessCategory.Construction,

                    TenantId = tenantId,

                    CountryId = countryId,
                    StateId = stateId,
                    CityId = cityId,

                    IncorporationDate = new DateTime(2024, 01, 01),

                    WebsiteUrl = "https://abc.com",
                    CreatedBy = "System"
                };

                await context.Companies.AddAsync(company);
                await context.SaveChangesAsync();
            }

            var companyId = context.Companies.First().Id;

            // =========================
            // 7. FINANCIAL YEAR
            // =========================
            if (!context.FinancialYears.Any())
            {
                var start = new DateTime(DateTime.UtcNow.Year, 4, 1);
                var end = start.AddYears(1).AddDays(-1);

                var fy = new FinancialYear
                {

                    Id = IDManager.GetNewId(new FinancialYear()),
                    Name = $"{start.Year}-{end.Year}",
                    Code = $"FY{start:yy}-{end:yy}",

                    StartDate = new DateTime(2025, 04, 01),
                    EndDate = new DateTime(2026, 03, 31),

                    Status = FinancialYearStatus.Open,
                    IsCurrent = true,

                    TenantId = tenantId,
                    CompanyId = companyId,
                    CreatedBy = "System"
                };

                await context.FinancialYears.AddAsync(fy);
                await context.SaveChangesAsync();
            }

            // =========================
            // 8. DEPARTMENTS
            // =========================
            if (!context.Departments.Any())
            {
                // Root Departments
                var hr = new Department
                {
                    Id = IDManager.GetNewId(new Department()),
                    Name = "Human Resource",
                    Code = "HR",
                    TenantId = tenantId,
                    CompanyId = companyId,
                    CreatedBy = "System"
                };

                var it = new Department
                {
                    Id = IDManager.GetNewId(new Department()),
                    Name = "Information Technology",
                    Code = "IT",
                    TenantId = tenantId,
                    CompanyId = companyId,
                    CreatedBy = "System"
                };

                // Child Department
                var recruitment = new Department
                {
                    Id = IDManager.GetNewId(new Department()),
                    Name = "Recruitment",
                    Code = "HR-REC",
                    TenantId = tenantId,
                    CompanyId = companyId,
                    ParentDepartmentId = hr.Id,
                    CreatedBy = "System"
                };

                await context.Departments.AddRangeAsync(hr, it, recruitment);
                await context.SaveChangesAsync();
            }

            // =========================
            // 9. DESIGNATIONS
            // =========================
            if (!context.Designations.Any())
            {
                var hrDept = await context.Departments
                    .FirstOrDefaultAsync(x => x.Code == "HR");

                var itDept = await context.Departments
                    .FirstOrDefaultAsync(x => x.Code == "IT");

                if (hrDept == null || itDept == null)
                    throw new Exception("Departments not found");

                // Top Level
                var ceo = new Designation
                {
                    Id = IDManager.GetNewId(new Designation()),
                    Name = "CEO",
                    Code = "CEO",
                    TenantId = tenantId,
                    CompanyId = companyId,
                    DepartmentId = hrDept.Id,
                    Level = 1,
                    MinSalary = 200000,
                    CreatedBy = "System"
                };

                // HR Hierarchy
                var hrManager = new Designation
                {
                    Id = IDManager.GetNewId(new Designation()),
                    Name = "HR Manager",
                    Code = "HR-MGR",
                    TenantId = tenantId,
                    CompanyId = companyId,
                    DepartmentId = hrDept.Id,
                    ParentDesignationId = ceo.Id,
                    Level = 2,
                    MinSalary = 80000,
                    CreatedBy = "System"
                };

                var hrExecutive = new Designation
                {
                    Id = IDManager.GetNewId(new Designation()),
                    Name = "HR Executive",
                    Code = "HR-EXEC",
                    TenantId = tenantId,
                    CompanyId = companyId,
                    DepartmentId = hrDept.Id,
                    ParentDesignationId = hrManager.Id,
                    Level = 3,
                    MinSalary = 30000,
                    CreatedBy = "System"
                };

                // IT Hierarchy
                var itManager = new Designation
                {
                    Id = IDManager.GetNewId(new Designation()),
                    Name = "IT Manager",
                    Code = "IT-MGR",
                    TenantId = tenantId,
                    CompanyId = companyId,
                    DepartmentId = itDept.Id,
                    ParentDesignationId = ceo.Id,
                    Level = 2,
                    MinSalary = 90000,
                    CreatedBy = "System"
                };

                var developer = new Designation
                {
                    Id = IDManager.GetNewId(new Designation()),
                    Name = "Software Developer",
                    Code = "DEV",
                    TenantId = tenantId,
                    CompanyId = companyId,
                    DepartmentId = itDept.Id,
                    ParentDesignationId = itManager.Id,
                    Level = 3,
                    MinSalary = 40000,
                    CreatedBy = "System"
                };

                // System Configurator sits alongside the CEO at the top of
                // the org - they own application/master-data configuration,
                // independent of the HR reporting hierarchy.
                var systemConfigurator = new Designation
                {
                    Id = IDManager.GetNewId(new Designation()),
                    Name = "System Configurator",
                    Code = "SYS-CFG",
                    TenantId = tenantId,
                    CompanyId = companyId,
                    DepartmentId = itDept.Id,
                    Level = 1,
                    MinSalary = 90000,
                    CreatedBy = "System"
                };

                await context.Designations.AddRangeAsync(
                    ceo, hrManager, hrExecutive, itManager, developer, systemConfigurator
                );

                await context.SaveChangesAsync();
            }

            // =========================
            // LEAVE TYPES (Leave Policy Engine foundation)
            // =========================
            // Seeds the two LeaveType rows the later Comp Off and Restricted
            // Holiday features depend on. Idempotent per-row (checked by
            // Name, matching the upsert-by-name convention already used for
            // other seed blocks in this method), and placed BEFORE the
            // Shifts early-return below so it also runs - and can still
            // insert any still-missing row - on every subsequent app
            // startup against an already-seeded database, not just on a
            // brand-new install.
            //
            // Later agents: look these rows up by Name ("Comp Off" /
            // "Restricted Holiday") via context.LeaveTypes, exactly like
            // this block does, e.g.:
            //   await context.LeaveTypes.FirstOrDefaultAsync(x => x.Name == "Comp Off");
            if (!await context.LeaveTypes.AnyAsync(x => x.Name == "Comp Off"))
            {
                var compOff = new LeaveType
                {
                    Id = IDManager.GetNewId(new LeaveType()),
                    Name = "Comp Off",

                    // 0 = no fixed annual cap since Comp Off is earned (via
                    // the Comp Off feature crediting it directly), not
                    // allocated up front like Casual/Sick/Earned Leave.
                    MaxDaysPerYear = 0,
                    IsPaid = true,

                    AllowCarryForward = false,
                    MaxCarryForwardDays = null,
                    AllowHalfDay = false,

                    MinServiceDaysRequired = 0,
                    AccrualFrequency = LeaveAccrualFrequency.None,
                    AccrualDaysPerCycle = 0,
                    IsEncashable = false,
                    MaxEncashableDays = null,
                    ApplicableGender = LeaveApplicableGender.All,
                    IsRestrictedHolidayType = false,

                    TenantId = tenantId,
                    CreatedBy = "System"
                };

                await context.LeaveTypes.AddAsync(compOff);
                await context.SaveChangesAsync();
            }

            if (!await context.LeaveTypes.AnyAsync(x => x.Name == "Restricted Holiday"))
            {
                var restrictedHoliday = new LeaveType
                {
                    Id = IDManager.GetNewId(new LeaveType()),
                    Name = "Restricted Holiday",

                    // Sensible default allowance - HR can edit it later via
                    // the LeaveType CRUD screen.
                    MaxDaysPerYear = 2,
                    IsPaid = true,

                    AllowCarryForward = false,
                    MaxCarryForwardDays = null,
                    AllowHalfDay = false,

                    MinServiceDaysRequired = 0,
                    AccrualFrequency = LeaveAccrualFrequency.None,
                    AccrualDaysPerCycle = 0,
                    IsEncashable = false,
                    MaxEncashableDays = null,
                    ApplicableGender = LeaveApplicableGender.All,
                    IsRestrictedHolidayType = true,

                    TenantId = tenantId,
                    CreatedBy = "System"
                };

                await context.LeaveTypes.AddAsync(restrictedHoliday);
                await context.SaveChangesAsync();
            }

            #region Shift

            if (await context.Shifts.AnyAsync())
                return;

            var shift = new Shift
            {
                // BaseEntity
                Id = IDManager.GetNewId(new Shift()),
                TenantId = tenantId,
                IsActive = true,
                IsDeleted = false,
                CreatedBy = "System",
                CreatedOn = DateTime.UtcNow,

                // Shift
                Name = "General Shift",
                StartTime = new TimeSpan(9, 0, 0),      // 09:00 AM
                EndTime = new TimeSpan(18, 0, 0),       // 06:00 PM

                GraceInMinutes = 15,
                GraceOutMinutes = 15,

                // Attendance Rules
                HalfDayMinutes = 240,                   // 4 Hours
                FullDayMinutes = 480,                   // 8 Hours

                MinimumWorkingMinutes = 480,            // 8 Hours
                MaximumWorkingMinutes = 600,            // 10 Hours

                IsNightShift = false,
                IsDefaultShift = true
            };

            await context.Shifts.AddAsync(shift);
            await context.SaveChangesAsync();

            #endregion

            // =========================
            // 10. EMPLOYEE
            // =========================
            // Production deployment: exactly 2 default employee records,
            // one per default login account (Admin, System Configurator).
            // No sample HR/developer employees are seeded - the client adds
            // their real workforce after go-live via the Employee module
            // (or the bulk Excel import).
            if (!context.Employees.Any())
            {
                var hrDept = await context.Departments.FirstOrDefaultAsync(x => x.Code == "HR");
                var itDept = await context.Departments.FirstOrDefaultAsync(x => x.Code == "IT");

                var ceoDesg = await context.Designations.FirstOrDefaultAsync(x => x.Code == "CEO");
                var sysCfgDesg = await context.Designations.FirstOrDefaultAsync(x => x.Code == "SYS-CFG");

                var defaultShift = await context.Shifts.FirstOrDefaultAsync();
                var india = await context.Countries.FirstOrDefaultAsync(x => x.Name == "India");

                if (tenantId == null ||
                    companyId == null ||
                    hrDept == null ||
                    itDept == null ||
                    ceoDesg == null ||
                    sysCfgDesg == null ||
                    defaultShift == null)
                {
                    throw new Exception("Required master data not found.");
                }

                // ================= Admin =================
                // NOTE FOR DEPLOYMENT: update Email/Phone to the client's
                // real administrator details, and change the default
                // password (see the Admin user below) immediately after
                // first login.

                var admin = new Employee
                {
                    Id = IDManager.GetNewId(new Employee()),
                    EmployeeCode = "EMP001",

                    FirstName = "Admin",
                    LastName = "User",

                    TenantId = tenantId,
                    CompanyId = companyId,
                    BranchId = null,

                    DepartmentId = hrDept.Id,
                    DesignationId = ceoDesg.Id,

                    ReportingManagerId = null,

                    ShiftId = defaultShift.Id,

                    Gender = Gender.Male,
                    MaritalStatus = MaritalStatus.Unmarried,

                    Phone = "9000000001",
                    Email = "admin@yourcompany.com",

                    Address = "Mumbai",
                    Pincode = "400001",

                    JoiningDate = DateTime.UtcNow,

                    EmploymentType = EmploymentType.Permanent,

                    Nationality = Nationality.Indian,

                    CountryId = india?.Id,

                    CreatedBy = "System"
                };

                // ================= System Configurator =================
                // Owns application/master-data setup (roles, app features,
                // company/branch/department structure, salary components,
                // etc.) - kept separate from the Admin account so day-to-day
                // configuration work is auditable under its own login.

                var systemConfigurator = new Employee
                {
                    Id = IDManager.GetNewId(new Employee()),
                    EmployeeCode = "EMP002",

                    FirstName = "System",
                    LastName = "Configurator",

                    TenantId = tenantId,
                    CompanyId = companyId,
                    BranchId = null,

                    DepartmentId = itDept.Id,
                    DesignationId = sysCfgDesg.Id,

                    ReportingManagerId = null,

                    ShiftId = defaultShift.Id,

                    Gender = Gender.Male,
                    MaritalStatus = MaritalStatus.Unmarried,

                    Phone = "9000000002",
                    Email = "sysconfig@yourcompany.com",

                    Address = "Mumbai",
                    Pincode = "400001",

                    JoiningDate = DateTime.UtcNow,

                    EmploymentType = EmploymentType.Permanent,

                    Nationality = Nationality.Indian,

                    CountryId = india?.Id,

                    CreatedBy = "System"
                };

                await context.Employees.AddRangeAsync(admin, systemConfigurator);
                await context.SaveChangesAsync();
            }

            // =========================
            // 12. PERMISSIONS
            // =========================
            if (!context.Permissions.Any())
            {
                var permissions = new List<Permission>
                {
                    // 👨‍💼 Employee
                    new Permission { Id = IDManager.GetNewId(new Permission()), Name = "Create Employee", Code = "EMP_CREATE", Module = Modules.HRMS, FeatureId = AppFeatureConstants.EMPLOYEE, Action = Actions.Create, DisplayOrder = 1,CreatedBy="System"},
                    new Permission { Id = IDManager.GetNewId(new Permission()), Name = "View Employee", Code = "EMP_VIEW", Module = Modules.HRMS, FeatureId = AppFeatureConstants.EMPLOYEE, Action = Actions.View, DisplayOrder = 2,CreatedBy="System"},
                    new Permission {Id = IDManager.GetNewId(new Permission()),   Name = "Edit Employee", Code = "EMP_EDIT", Module = Modules.HRMS, FeatureId = AppFeatureConstants.EMPLOYEE, Action = Actions.Edit, DisplayOrder = 3,CreatedBy="System"},
                    new Permission { Id = IDManager.GetNewId(new Permission()), Name = "Delete Employee", Code = "EMP_DELETE", Module = Modules.HRMS, FeatureId = AppFeatureConstants.EMPLOYEE, Action = Actions.Delete, DisplayOrder = 4 , CreatedBy = "System"},

                    // 🏖 Leave
                    new Permission { Id = IDManager.GetNewId(new Permission()), Name = "Apply Leave", Code = "LEAVE_APPLY", Module = Modules.HRMS, FeatureId = AppFeatureConstants.LEAVE_APPLICATION, Action = Actions.Create,CreatedBy="System"},
                    new Permission {Id = IDManager.GetNewId(new Permission()),   Name = "Approve Leave", Code = "LEAVE_APPROVE", Module = Modules.HRMS, FeatureId = AppFeatureConstants.LEAVE_APPROVAL, Action = Actions.Approve,CreatedBy="System"},

                    // 💰 Payroll
                    new Permission {Id = IDManager.GetNewId(new Permission()),   Name = "View Payroll", Code = "PAYROLL_VIEW", Module = Modules.HRMS, FeatureId = AppFeatureConstants.PAYROLL, Action = Actions.View,CreatedBy="System"},
                };

                await context.Permissions.AddRangeAsync(permissions);
                await context.SaveChangesAsync();
            }

            // =========================
            // 13. ROLE
            // =========================
            if (!context.Roles.Any())
            {
                var roles = new List<Role>
                {
                    new Role
                    {
                        Id = IDManager.GetNewId(new Role()),
                        Name = "Admin",
                        Code = ConstantHelper.SUPER_ADMIN,
                        TenantId = tenantId,
                        Description = "Full system access",
                        CreatedBy="System"
                    },
                    // Manages application/master-data configuration (roles,
                    // app features, org structure, salary components, etc.)
                    // - granted every permission alongside Admin, see
                    // ReconcilePermissionsAsync below.
                    new Role
                    {
                        Id = IDManager.GetNewId(new Role()),
                        Name = "System Configurator",
                        Code = ConstantHelper.SYSTEM_CONFIGURATOR,
                        TenantId = tenantId,
                        Description = "Manages application configuration and master data",
                        CreatedBy="System"
                    },
                    new Role
                    {
                        Id = IDManager.GetNewId(new Role()),
                        Name = "HR Manager",
                        Code = ConstantHelper.HR_MANAGER,
                        TenantId = tenantId,
                        CreatedBy="System"
                    },
                    new Role
                    {
                        Id = IDManager.GetNewId(new Role()),
                        Name = "Employee",
                        Code = ConstantHelper.EMPLOYEE,
                        TenantId = tenantId,
                        CreatedBy="System"
                    }
                };

                await context.Roles.AddRangeAsync(roles);
                await context.SaveChangesAsync();
            }

            //await AppFeatureSeeder.SeedRoleFeatureAsync(context);

            if (!context.RolePermissions.Any())
            {
                var roles = await context.Roles.ToListAsync();
                var permissions = await context.Permissions.ToListAsync();

                var superAdmin = roles.First(x => x.Code == ConstantHelper.SUPER_ADMIN);
                var systemConfigurator = roles.First(x => x.Code == ConstantHelper.SYSTEM_CONFIGURATOR);
                var hrManager = roles.First(x => x.Code == "HR_MANAGER");
                var employee = roles.First(x => x.Code == "EMPLOYEE");

                var rolePermissions = new List<RolePermission>();

                // 👑 Admin + System Configurator → All Permissions (both are
                // full application-management accounts).
                foreach (var perm in permissions)
                {
                    rolePermissions.Add(new RolePermission
                    {
                        Id = IDManager.GetNewId(new RolePermission()),
                        RoleId = superAdmin.Id,
                        PermissionId = perm.Id,
                        IsAllowed = true,
                        CreatedBy = "System"
                    });

                    rolePermissions.Add(new RolePermission
                    {
                        Id = IDManager.GetNewId(new RolePermission()),
                        RoleId = systemConfigurator.Id,
                        PermissionId = perm.Id,
                        IsAllowed = true,
                        CreatedBy = "System"
                    });
                }

                // 👨‍💼 HR Manager → Employee + Leave (including LEAVE_APPROVAL/
                // Approve - IsAuthorizedForLevelAsync's Level 3 check is
                // permission-based, not a substring match on role name, so
                // the HR Manager role must actually hold this permission or
                // no HR user could ever approve a Level 3 leave request).
                var hrPermissions = permissions
                    .Where(p => p.FeatureId == AppFeatureConstants.EMPLOYEE ||
                                p.FeatureId == AppFeatureConstants.LEAVE_APPLICATION ||
                                p.FeatureId == AppFeatureConstants.LEAVE_APPROVAL)
                    .ToList();

                foreach (var perm in hrPermissions)
                {
                    rolePermissions.Add(new RolePermission
                    {
                        Id = IDManager.GetNewId(new RolePermission()),
                        RoleId = hrManager.Id,
                        PermissionId = perm.Id,
                        IsAllowed = true,
                        CreatedBy = "System"
                    });
                }

                // 👤 Employee → View only
                var empPermissions = permissions
                    .Where(p => p.Action == Actions.View)
                    .ToList();

                foreach (var perm in empPermissions)
                {
                    rolePermissions.Add(new RolePermission
                    {
                        Id = IDManager.GetNewId(new RolePermission()),
                        RoleId = employee.Id,
                        PermissionId = perm.Id,
                        IsAllowed = true,
                        CreatedBy = "System"
                    });
                }

                await context.RolePermissions.AddRangeAsync(rolePermissions);
                await context.SaveChangesAsync();
            }

            // =========================
            // 11. USERS (production defaults)
            // =========================
            // Exactly 2 login accounts are seeded, one per default
            // employee: Admin and System Configurator.
            //
            // ⚠️ DEPLOYMENT: change both default passwords immediately
            // after first login - these are placeholder credentials only.
            if (!context.Users.Any())
            {
                var adminEmp = await context.Employees.FirstOrDefaultAsync(x => x.EmployeeCode == "EMP001");
                var sysCfgEmp = await context.Employees.FirstOrDefaultAsync(x => x.EmployeeCode == "EMP002");

                if (adminEmp == null || sysCfgEmp == null)
                    throw new Exception("Default employees not found");

                var users = new List<User>
                {
                    new User
                    {
                        Id = IDManager.GetNewId(new User()),
                        Username = "admin",
                        Email = "admin@yourcompany.com",
                        PhoneNumber = "9000000001",

                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),

                        EmployeeId = adminEmp.Id,

                        EmailConfirmed = true,
                        PhoneConfirmed = true,

                        TenantId = tenantId,
                        CompanyId = companyId,
                        BranchId = null,

                        IsLocked = false,
                        AccessFailedCount = 0,

                        LastLoginIP = "Unknown",

                        CreatedBy = "System"
                    },
                    new User
                    {
                        Id = IDManager.GetNewId(new User()),
                        Username = "sysconfig",
                        Email = "sysconfig@yourcompany.com",
                        PhoneNumber = "9000000002",

                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("SysConfig@123"),

                        EmployeeId = sysCfgEmp.Id,

                        EmailConfirmed = true,
                        PhoneConfirmed = true,

                        TenantId = tenantId,
                        CompanyId = companyId,
                        BranchId = null,

                        IsLocked = false,
                        AccessFailedCount = 0,

                        LastLoginIP = "Unknown",

                        CreatedBy = "System"
                    }
                };

                await context.Users.AddRangeAsync(users);
                await context.SaveChangesAsync();
            }

            if (!context.UserRoles.Any())
            {
                var adminUser = await context.Users.FirstOrDefaultAsync(x => x.Username == "admin");
                var sysCfgUser = await context.Users.FirstOrDefaultAsync(x => x.Username == "sysconfig");

                var adminRole = await context.Roles.FirstOrDefaultAsync(x => x.Code == ConstantHelper.SUPER_ADMIN);
                var sysCfgRole = await context.Roles.FirstOrDefaultAsync(x => x.Code == ConstantHelper.SYSTEM_CONFIGURATOR);

                if (adminUser == null || sysCfgUser == null || adminRole == null || sysCfgRole == null)
                    throw new Exception("Default users or roles not found");

                var userRoles = new List<UserRole>
                {
                    new UserRole
                    {
                        Id = IDManager.GetNewId(new UserRole()),
                        UserId = adminUser.Id,
                        RoleId = adminRole.Id,
                        CreatedBy = "System"
                    },
                    new UserRole
                    {
                        Id = IDManager.GetNewId(new UserRole()),
                        UserId = sysCfgUser.Id,
                        RoleId = sysCfgRole.Id,
                        CreatedBy = "System"
                    }
                };

                await context.UserRoles.AddRangeAsync(userRoles);
                await context.SaveChangesAsync();
            }

            // Idempotent RBAC: generate permissions for every screen feature and
            // grant them to Super Admin. Runs after roles/features are in place.
            await AppFeatureSeeder.ReconcilePermissionsAsync(context, tenantId);
        }

        public static string GetNextCodeSequence(ApplicationDbContext _context, string module)
        {
            string fyrCode = string.Empty;
            if (_context.FinancialYears.Any())
            {
                fyrCode = _context.FinancialYears.First().Name;
            }
             
            using var transaction = _context.Database.BeginTransaction();

            var sequence = _context.SequenceMasters
                .FirstOrDefault(x => x.Prefix == module);

            if (sequence == null)
            {
                sequence = new SequenceMaster
                {
                    
                    Prefix = module,
                    CurrentNumber = 0,
                    CreatedBy = "System",
                    CreatedOn = DateTime.UtcNow
                };
                _context.SequenceMasters.Add(sequence);
            }

            sequence.CurrentNumber += 1;

            _context.SaveChanges();
            transaction.Commit();

            return $"{module}{sequence.CurrentNumber:D5}";
        }
    }

    public static class AppFeatureSeeder
    {
        public static async Task SeedAsync(
            ApplicationDbContext context,
            string tenantId)
        {
            if (context.AppFeatures.Any())
                return;

            var features = new List<AppFeature>();

            // =====================================================
            // 🔧 COMMON HELPER
            // =====================================================

            AppFeature Add(
                string name,
                string code,
                string module,
                string controller,
                string action,
                string? parentId = null,
                string? icon = null,
                AppFeatureType? category = null,
                int order = 0,
                bool isMenu = true,
                bool isVisible = true,
                bool canView = true,
                bool canAdd = false,
                bool canEdit = false,
                bool canDelete = false,
                bool canApprove = false,
                bool canExport = false,
                bool canPrint = false)
            {
                var feature = new AppFeature
                {
                    Id = IDManager.GetNewId(new AppFeature()),

                    Name = name,
                    Code = code,
                    Module = module,

                    AppFeatureType = category,

                    ControllerName = controller,
                    ActionName = action,

                    ParentFeatureId = parentId,

                    Icon = icon,

                    DisplayOrder = order,

                    TenantId = tenantId,

                    IsMenu = isMenu,
                    IsVisible = isVisible,
                    IsActive = true,

                    CanView = canView,
                    CanAdd = canAdd,
                    CanEdit = canEdit,
                    CanDelete = canDelete,
                    CanApprove = canApprove,
                    CanExport = canExport,
                    CanPrint = canPrint,

                    CreatedBy = "System",
                    CreatedOn = DateTime.UtcNow
                };

                features.Add(feature);

                return feature;
            }

            // =====================================================
            // DASHBOARD
            // =====================================================

            Add(
                "Dashboard",
                AppFeatureConstants.DASHBOARD,
                AppFeatureConstants.HRMS,
                AppFeatureConstants.DASHBOARD_CONTROLLER,
                AppFeatureConstants.DASHBOARD_ACTION,
                icon: "bi bi-grid-fill",
                category: AppFeatureType.Dashboard,
                order: 1);

            // =====================================================
            // MASTER
            // =====================================================

            var master = Add(
                "Master",
                AppFeatureConstants.MASTER,
                AppFeatureConstants.HRMS,
                "",
                "",
                icon: "bi bi-database-fill",
                category: AppFeatureType.Master,
                order: 10);

            Add("Country", AppFeatureConstants.COUNTRY, AppFeatureConstants.HRMS,
                AppFeatureConstants.COUNTRY_CONTROLLER,
                AppFeatureConstants.COUNTRY_ACTION,
                master.Id,
                "bi bi-globe",
                AppFeatureType.Master,
                11, canAdd: true, canEdit: true, canDelete: true);

            Add("State", AppFeatureConstants.STATE, AppFeatureConstants.HRMS,
                AppFeatureConstants.STATE_CONTROLLER,
                AppFeatureConstants.STATE_ACTION,
                master.Id,
                "bi bi-map",
                AppFeatureType.Master,
                12, canAdd: true, canEdit: true, canDelete: true);

            Add("City", AppFeatureConstants.CITY, AppFeatureConstants.HRMS,
                AppFeatureConstants.CITY_CONTROLLER,
                AppFeatureConstants.CITY_ACTION,
                master.Id,
                "bi bi-building",
                AppFeatureType.Master,
                13, canAdd: true, canEdit: true, canDelete: true);

            Add("Holiday Group", AppFeatureConstants.HOLIDAY_GROUP, AppFeatureConstants.HRMS,
                AppFeatureConstants.HOLIDAY_GROUP_CONTROLLER,
                AppFeatureConstants.HOLIDAY_GROUP_ACTION,
                master.Id,
                "bi bi-calendar-week",
                AppFeatureType.Master,
                14, canAdd: true, canEdit: true);

            Add("Week Off", AppFeatureConstants.WEEKOFF, AppFeatureConstants.HRMS,
                AppFeatureConstants.WEEKOFF_CONTROLLER,
                AppFeatureConstants.WEEKOFF_ACTION,
                master.Id,
                "bi bi-calendar-event",
                AppFeatureType.Master,
                15, canAdd: true, canEdit: true);

            Add("Financial Year", AppFeatureConstants.FINANCIAL_YEAR, AppFeatureConstants.HRMS,
                AppFeatureConstants.FINANCIAL_YEAR_CONTROLLER,
                AppFeatureConstants.FINANCIAL_YEAR_ACTION,
                master.Id,
                "bi bi-calendar-range",
                AppFeatureType.Master,
                16, canAdd: true, canEdit: true);

            // =====================================================
            // SECURITY
            // =====================================================

            var security = Add(
                "Security",
                AppFeatureConstants.SECURITY,
                AppFeatureConstants.HRMS,
                "",
                "",
                icon: "bi bi-shield-lock-fill",
                category: AppFeatureType.Security,
                order: 20);

            Add("Role", AppFeatureConstants.ROLE, AppFeatureConstants.HRMS,
                AppFeatureConstants.ROLE_CONTROLLER,
                AppFeatureConstants.ROLE_ACTION,
                security.Id,
                "bi bi-person-gear",
                AppFeatureType.Security,
                21, canAdd: true, canEdit: true, canDelete: true);

            Add("User", AppFeatureConstants.USER, AppFeatureConstants.HRMS,
                AppFeatureConstants.USER_CONTROLLER,
                AppFeatureConstants.USER_ACTION,
                security.Id,
                "bi bi-people-fill",
                AppFeatureType.Security,
                22, canAdd: true, canEdit: true, canDelete: true);

            Add("Permission", AppFeatureConstants.PERMISSION, AppFeatureConstants.HRMS,
                AppFeatureConstants.PERMISSION_CONTROLLER,
                AppFeatureConstants.PERMISSION_ACTION,
                security.Id,
                "bi bi-key-fill",
                AppFeatureType.Security,
                23, canEdit: true);

            Add("Login History", AppFeatureConstants.LOGIN_HISTORY, AppFeatureConstants.HRMS,
                AppFeatureConstants.LOGIN_HISTORY_CONTROLLER,
                AppFeatureConstants.LOGIN_HISTORY_ACTION,
                security.Id,
                "bi bi-clock-history",
                AppFeatureType.Security,
                24);

            // =====================================================
            // ORGANIZATION
            // =====================================================

            var organization = Add(
                "Organization",
                AppFeatureConstants.ORGANIZATION,
                AppFeatureConstants.HRMS,
                "",
                "",
                icon: "bi bi-diagram-3-fill",
                category: AppFeatureType.Master,
                order: 30);

            Add("Company", AppFeatureConstants.COMPANY, AppFeatureConstants.HRMS,
                AppFeatureConstants.COMPANY_CONTROLLER,
                AppFeatureConstants.COMPANY_ACTION,
                organization.Id,
                "bi bi-building-fill",
                AppFeatureType.Master,
                31, canAdd: true, canEdit: true);

            Add("Department", AppFeatureConstants.DEPARTMENT, AppFeatureConstants.HRMS,
                AppFeatureConstants.DEPARTMENT_CONTROLLER,
                AppFeatureConstants.DEPARTMENT_ACTION,
                organization.Id,
                "bi bi-diagram-2-fill",
                AppFeatureType.Master,
                32, canAdd: true, canEdit: true, canDelete: true);

            Add("Designation", AppFeatureConstants.DESIGNATION, AppFeatureConstants.HRMS,
                AppFeatureConstants.DESIGNATION_CONTROLLER,
                AppFeatureConstants.DESIGNATION_ACTION,
                organization.Id,
                "bi bi-award-fill",
                AppFeatureType.Master,
                33, canAdd: true, canEdit: true, canDelete: true);

            Add("Branch", AppFeatureConstants.BRANCH, AppFeatureConstants.HRMS,
                AppFeatureConstants.BRANCH_CONTROLLER,
                AppFeatureConstants.BRANCH_ACTION,
                organization.Id,
                "bi bi-shop",
                AppFeatureType.Master,
                34, canAdd: true, canEdit: true);

            Add("Location", AppFeatureConstants.LOCATION, AppFeatureConstants.HRMS,
                AppFeatureConstants.LOCATION_CONTROLLER,
                AppFeatureConstants.LOCATION_ACTION,
                organization.Id,
                "bi bi-geo-alt-fill",
                AppFeatureType.Master,
                35, canAdd: true, canEdit: true);

            // =====================================================
            // EMPLOYEE MANAGEMENT
            // =====================================================

            var employee = Add(
                "Employee Management",
                AppFeatureConstants.EMPLOYEE_MANAGEMENT,
                AppFeatureConstants.HRMS,
                "",
                "",
                icon: "bi bi-people-fill",
                category: AppFeatureType.Transaction,
                order: 40);

            Add("Employee", AppFeatureConstants.EMPLOYEE, AppFeatureConstants.HRMS,
                AppFeatureConstants.EMPLOYEE_CONTROLLER,
                AppFeatureConstants.EMPLOYEE_ACTION,
                employee.Id,
                "bi bi-person-fill",
                AppFeatureType.Transaction,
                41, canAdd: true, canEdit: true, canDelete: true);

            Add("Employee Document", AppFeatureConstants.EMPLOYEE_DOCUMENT, AppFeatureConstants.HRMS,
                AppFeatureConstants.EMPLOYEE_DOCUMENT_CONTROLLER,
                AppFeatureConstants.EMPLOYEE_DOCUMENT_ACTION,
                employee.Id,
                "bi bi-file-earmark-text-fill",
                AppFeatureType.Transaction,
                42, canAdd: true, canEdit: true);

            Add("Employee Shift", AppFeatureConstants.EMPLOYEE_SHIFT, AppFeatureConstants.HRMS,
                AppFeatureConstants.EMPLOYEE_SHIFT_CONTROLLER,
                AppFeatureConstants.EMPLOYEE_SHIFT_ACTION,
                employee.Id,
                "bi bi-clock-fill",
                AppFeatureType.Transaction,
                43, canAdd: true, canEdit: true);

            // =====================================================
            // ATTENDANCE
            // =====================================================

            var attendance = Add(
                "Attendance Management",
                AppFeatureConstants.ATTENDANCE_MANAGEMENT,
                AppFeatureConstants.HRMS,
                "",
                "",
                icon: "bi bi-calendar-check-fill",
                category: AppFeatureType.Transaction,
                order: 50);

            Add("Attendance", AppFeatureConstants.ATTENDANCE, AppFeatureConstants.HRMS,
                AppFeatureConstants.ATTENDANCE_CONTROLLER,
                AppFeatureConstants.ATTENDANCE_ACTION,
                attendance.Id,
                "bi bi-calendar2-check-fill",
                AppFeatureType.Transaction,
                51, canAdd: true, canEdit: true, canExport: true);

            Add("Attendance Log", AppFeatureConstants.ATTENDANCE_LOG, AppFeatureConstants.HRMS,
                AppFeatureConstants.ATTENDANCE_LOG_CONTROLLER,
                AppFeatureConstants.ATTENDANCE_LOG_ACTION,
                attendance.Id,
                "bi bi-list-check",
                AppFeatureType.Transaction,
                52);

            Add("Shift", AppFeatureConstants.SHIFT, AppFeatureConstants.HRMS,
                AppFeatureConstants.SHIFT_CONTROLLER,
                AppFeatureConstants.SHIFT_ACTION,
                attendance.Id,
                "bi bi-clock-history",
                AppFeatureType.Transaction,
                53, canAdd: true, canEdit: true);

            // =====================================================
            // LEAVE MANAGEMENT
            // =====================================================

            var leave = Add(
                "Leave Management",
                AppFeatureConstants.LEAVE_MANAGEMENT,
                AppFeatureConstants.HRMS,
                "",
                "",
                icon: "bi bi-calendar-minus-fill",
                category: AppFeatureType.Transaction,
                order: 60);

            Add("Leave Type", AppFeatureConstants.LEAVE_TYPE, AppFeatureConstants.HRMS,
                AppFeatureConstants.LEAVE_TYPE_CONTROLLER,
                AppFeatureConstants.LEAVE_TYPE_ACTION,
                leave.Id,
                "bi bi-tags-fill",
                AppFeatureType.Transaction,
                61, canAdd: true, canEdit: true);

            Add("Leave Application", AppFeatureConstants.LEAVE_APPLICATION, AppFeatureConstants.HRMS,
                AppFeatureConstants.LEAVE_APPLICATION_CONTROLLER,
                AppFeatureConstants.LEAVE_APPLICATION_ACTION,
                leave.Id,
                "bi bi-send-fill",
                AppFeatureType.Transaction,
                62, canAdd: true, canEdit: true);

            Add("Leave Approval", AppFeatureConstants.LEAVE_APPROVAL, AppFeatureConstants.HRMS,
                AppFeatureConstants.LEAVE_APPROVAL_CONTROLLER,
                AppFeatureConstants.LEAVE_APPROVAL_ACTION,
                leave.Id,
                "bi bi-check-circle-fill",
                AppFeatureType.Transaction,
                63, canApprove: true);

            Add("Leave Balance", AppFeatureConstants.LEAVE_BALANCE, AppFeatureConstants.HRMS,
                AppFeatureConstants.LEAVE_BALANCE_CONTROLLER,
                AppFeatureConstants.LEAVE_BALANCE_ACTION,
                leave.Id,
                "bi bi-check-circle-fill",
                AppFeatureType.Transaction,
                64, canApprove: true);

            // =====================================================
            // PAYROLL
            // =====================================================

            var payroll = Add(
                "Payroll",
                AppFeatureConstants.PAYROLL,
                AppFeatureConstants.HRMS,
                "",
                "",
                icon: "bi bi-cash-stack",
                category: AppFeatureType.Transaction,
                order: 70);

            Add("Salary Structure", AppFeatureConstants.SALARY_STRUCTURE, AppFeatureConstants.HRMS,
                AppFeatureConstants.SALARY_STRUCTURE_CONTROLLER,
                AppFeatureConstants.SALARY_STRUCTURE_ACTION,
                payroll.Id,
                "bi bi-wallet-fill",
                AppFeatureType.Transaction,
                71, canAdd: true, canEdit: true);

            Add("Payroll Process", AppFeatureConstants.PAYROLL_PROCESS, AppFeatureConstants.HRMS,
                AppFeatureConstants.PAYROLL_PROCESS_CONTROLLER,
                AppFeatureConstants.PAYROLL_PROCESS_ACTION,
                payroll.Id,
                "bi bi-cpu-fill",
                AppFeatureType.Transaction,
                72, canAdd: true, canApprove: true);

            Add("Payslip", AppFeatureConstants.PAYSLIP, AppFeatureConstants.HRMS,
                AppFeatureConstants.PAYSLIP_CONTROLLER,
                AppFeatureConstants.PAYSLIP_ACTION,
                payroll.Id,
                "bi bi-receipt",
                AppFeatureType.Transaction,
                73, canPrint: true, canExport: true);

            // =====================================================
            // SAVE
            // =====================================================

            await context.AppFeatures.AddRangeAsync(features);

            await context.SaveChangesAsync();
        }

        // =====================================================
        // 🔁 IDEMPOTENT RECONCILE (safe to run on every startup)
        //    - adds new module menus (Assets, Recruitment, Communication)
        //    - extends Payroll (Salary Component, Dashboard, Register)
        //    - rectifies broken controllers (PayrollProcess/Payslip/Interview)
        //    Upserts by Code: inserts if missing, updates if present.
        // =====================================================
        public static async Task ReconcileModulesAsync(
            ApplicationDbContext context,
            string tenantId)
        {
            var desired = new List<AppFeature>();

            // NOTE: ParentFeatureId temporarily holds the parent CODE; resolved below.
            AppFeature Def(
                string name,
                string code,
                string controller,
                string action,
                string? parentCode = null,
                string? icon = null,
                AppFeatureType category = AppFeatureType.Transaction,
                int order = 0,
                bool isMenu = true,
                bool canView = true,
                bool canAdd = false,
                bool canEdit = false,
                bool canDelete = false,
                bool canApprove = false,
                bool canExport = false,
                bool canPrint = false)
            {
                var f = new AppFeature
                {
                    Id = IDManager.GetNewId(new AppFeature()),
                    Name = name,
                    Code = code,
                    Module = AppFeatureConstants.HRMS,
                    AppFeatureType = category,
                    ControllerName = controller,
                    ActionName = action,
                    ParentFeatureId = parentCode,
                    Icon = icon,
                    DisplayOrder = order,
                    TenantId = tenantId,
                    IsMenu = isMenu,
                    IsVisible = true,
                    IsActive = true,
                    CanView = canView,
                    CanAdd = canAdd,
                    CanEdit = canEdit,
                    CanDelete = canDelete,
                    CanApprove = canApprove,
                    CanExport = canExport,
                    CanPrint = canPrint,
                    CreatedBy = "System",
                    CreatedOn = DateTime.UtcNow
                };
                desired.Add(f);
                return f;
            }

            // ---------------- EMPLOYEE SELF-SERVICE ----------------
            Def("My Dashboard", AppFeatureConstants.EMPLOYEE_DASHBOARD,
                AppFeatureConstants.EMPLOYEE_DASHBOARD_CONTROLLER, AppFeatureConstants.EMPLOYEE_DASHBOARD_ACTION,
                null, "bi bi-speedometer2", AppFeatureType.Dashboard, 2);

            Def("Tasks", AppFeatureConstants.EMPLOYEE_TASK,
                AppFeatureConstants.EMPLOYEE_TASK_CONTROLLER, AppFeatureConstants.EMPLOYEE_TASK_ACTION,
                null, "bi bi-check2-square", AppFeatureType.Transaction, 3,
                canAdd: true, canEdit: true, canDelete: true);

            // ---------------- BIOMETRIC ATTENDANCE (children of the existing Attendance Management group) ----------------
            Def("Biometric Devices", AppFeatureConstants.BIOMETRIC_DEVICE,
                AppFeatureConstants.BIOMETRIC_DEVICE_CONTROLLER, AppFeatureConstants.BIOMETRIC_DEVICE_ACTION,
                AppFeatureConstants.ATTENDANCE_MANAGEMENT, "bi bi-fingerprint", AppFeatureType.Master, 54,
                canAdd: true, canEdit: true, canDelete: true);

            Def("Device Health", AppFeatureConstants.BIOMETRIC_DEVICE_HEALTH,
                AppFeatureConstants.BIOMETRIC_DEVICE_HEALTH_CONTROLLER, AppFeatureConstants.BIOMETRIC_DEVICE_HEALTH_ACTION,
                AppFeatureConstants.ATTENDANCE_MANAGEMENT, "bi bi-heart-pulse-fill", AppFeatureType.Dashboard, 55);

            Def("Employee Biometric Mapping", AppFeatureConstants.EMPLOYEE_BIOMETRIC_MAPPING,
                AppFeatureConstants.EMPLOYEE_BIOMETRIC_MAPPING_CONTROLLER, AppFeatureConstants.EMPLOYEE_BIOMETRIC_MAPPING_ACTION,
                AppFeatureConstants.ATTENDANCE_MANAGEMENT, "bi bi-person-vcard-fill", AppFeatureType.Master, 56,
                canAdd: true, canEdit: true, canDelete: true);

            // Employee Bank Details - child of the existing Employee
            // Management group (same level as Employee Document/Employee
            // Shift), standalone top-level list rather than a tab on the
            // Employee Details/Edit page. PayrollBusinessService.GenerateAsync
            // reads Domain.Entities.EmployeeBankDetail directly (not through
            // this menu) to resolve the primary account for a payslip, so
            // this entry only wires up the CRUD screen.
            Def("Employee Bank Details", AppFeatureConstants.EMPLOYEE_BANK,
                AppFeatureConstants.EMPLOYEE_BANK_CONTROLLER, AppFeatureConstants.EMPLOYEE_BANK_ACTION,
                AppFeatureConstants.EMPLOYEE_MANAGEMENT, "bi bi-bank2", AppFeatureType.Transaction, 44,
                canAdd: true, canEdit: true, canDelete: true);

            // Leave Calendar - month-grid view of approved leaves, scoped
            // org-wide for admin/HR and to "my department" for a
            // self-service employee. Deliberately not in
            // EssRestrictionAttribute's LeaveApplication deny-list, so it
            // stays reachable by everyone (the whole point is letting
            // employees plan around their teammates).
            Def("Leave Calendar", AppFeatureConstants.LEAVE_CALENDAR,
                AppFeatureConstants.LEAVE_CALENDAR_CONTROLLER, AppFeatureConstants.LEAVE_CALENDAR_ACTION,
                AppFeatureConstants.LEAVE_MANAGEMENT, "bi bi-calendar-week", AppFeatureType.Transaction, 65);

            // Approval Delegation (out-of-office proxy approver) - reachable
            // by any employee who might be an approver (Reporting Manager or
            // Department Head), not admin-gated - see
            // EssRestrictionAttribute, which deliberately does not list this
            // controller.
            Def("Approval Delegation", AppFeatureConstants.APPROVAL_DELEGATION,
                AppFeatureConstants.APPROVAL_DELEGATION_CONTROLLER, AppFeatureConstants.APPROVAL_DELEGATION_ACTION,
                AppFeatureConstants.LEAVE_MANAGEMENT, "bi bi-person-arms-up", AppFeatureType.Transaction, 66,
                canAdd: true, canDelete: true);

            // Attendance Regularization - employee-submitted correction of a
            // missing/wrong punch for a date, routed through the same
            // multi-level approval chain as Leave. Child of the existing
            // Attendance Management group.
            Def("Attendance Correction", AppFeatureConstants.ATTENDANCE_REGULARIZATION,
                AppFeatureConstants.ATTENDANCE_REGULARIZATION_CONTROLLER, AppFeatureConstants.ATTENDANCE_REGULARIZATION_ACTION,
                AppFeatureConstants.ATTENDANCE_MANAGEMENT, "bi bi-calendar2-check", AppFeatureType.Transaction, 57,
                canAdd: true, canApprove: true);

            // Attendance Policy - company-wide attendance rules (grace/
            // regularization limits, late-mark penalty, minimum attendance %
            // for full salary, comp-off eligible extra hours), separate from
            // Shift's per-shift timing fields. Simple master-data CRUD,
            // child of the existing Attendance Management group (same level
            // as Shift/Attendance Correction).
            Def("Attendance Policy", AppFeatureConstants.ATTENDANCE_POLICY,
                AppFeatureConstants.ATTENDANCE_POLICY_CONTROLLER, AppFeatureConstants.ATTENDANCE_POLICY_ACTION,
                AppFeatureConstants.ATTENDANCE_MANAGEMENT, "bi bi-shield-check", AppFeatureType.Master, 58,
                canAdd: true, canEdit: true, canDelete: true);

            // Attendance Calendar / Team Attendance / Attendance Summary /
            // Attendance Dashboard - the four remaining "screens" requested
            // alongside My Attendance (which reuses the existing Attendance
            // menu entry's MyAttendance action, no new menu item needed).
            // All read from AttendanceInsightsController; scoping (self vs
            // manager vs HR) is enforced server-side per request.
            Def("Attendance Calendar", AppFeatureConstants.ATTENDANCE_CALENDAR,
                AppFeatureConstants.ATTENDANCE_CALENDAR_CONTROLLER, AppFeatureConstants.ATTENDANCE_CALENDAR_ACTION,
                AppFeatureConstants.ATTENDANCE_MANAGEMENT, "bi bi-calendar3", AppFeatureType.Dashboard, 59);

            Def("Team Attendance", AppFeatureConstants.TEAM_ATTENDANCE,
                AppFeatureConstants.TEAM_ATTENDANCE_CONTROLLER, AppFeatureConstants.TEAM_ATTENDANCE_ACTION,
                AppFeatureConstants.ATTENDANCE_MANAGEMENT, "bi bi-people-fill", AppFeatureType.Dashboard, 60);

            Def("Attendance Summary", AppFeatureConstants.ATTENDANCE_SUMMARY,
                AppFeatureConstants.ATTENDANCE_SUMMARY_CONTROLLER, AppFeatureConstants.ATTENDANCE_SUMMARY_ACTION,
                AppFeatureConstants.ATTENDANCE_MANAGEMENT, "bi bi-bar-chart-fill", AppFeatureType.Dashboard, 61,
                canExport: true);

            Def("Attendance Dashboard", AppFeatureConstants.ATTENDANCE_DASHBOARD,
                AppFeatureConstants.ATTENDANCE_DASHBOARD_CONTROLLER, AppFeatureConstants.ATTENDANCE_DASHBOARD_ACTION,
                AppFeatureConstants.ATTENDANCE_MANAGEMENT, "bi bi-speedometer2", AppFeatureType.Dashboard, 62);

            // Work From Home Request - employee-submitted date-range
            // request, single-level approval (Reporting Manager or HR/Admin
            // override), child of the existing Attendance Management group
            // (same level as Attendance Correction/Attendance Policy).
            // Reachable by both HR Manager (approve/view-all override) and
            // plain Employee (create/view own + approve direct reports) -
            // see the role-permission grants below.
            Def("Work From Home Request", AppFeatureConstants.WFH_REQUEST,
                AppFeatureConstants.WFH_REQUEST_CONTROLLER, AppFeatureConstants.WFH_REQUEST_ACTION,
                AppFeatureConstants.ATTENDANCE_MANAGEMENT, "bi bi-house-door", AppFeatureType.Transaction, 63,
                canAdd: true, canApprove: true);

            // On Duty Request - employee-submitted date-range request for
            // official work carried out away from the office, single-level
            // approval (Reporting Manager or HR/Admin override), child of
            // the existing Attendance Management group (same level as Work
            // From Home Request). Reachable by both HR Manager (approve/
            // view-all override) and plain Employee (create/view own +
            // approve direct reports) - see the role-permission grants
            // below.
            Def("On Duty Request", AppFeatureConstants.ON_DUTY_REQUEST,
                AppFeatureConstants.ON_DUTY_REQUEST_CONTROLLER, AppFeatureConstants.ON_DUTY_REQUEST_ACTION,
                AppFeatureConstants.ATTENDANCE_MANAGEMENT, "bi bi-briefcase", AppFeatureType.Transaction, 64,
                canAdd: true, canApprove: true);

            // Short Leave Request - employee-submitted request for a few
            // hours off during a working day, single-level approval
            // (Reporting Manager or HR/Admin override), child of the
            // existing Attendance Management group (same level as Work From
            // Home Request/On Duty Request). Reachable by both HR Manager
            // (approve/view-all override) and plain Employee (create/view
            // own + approve direct reports) - see the role-permission
            // grants below.
            Def("Short Leave Request", AppFeatureConstants.SHORT_LEAVE_REQUEST,
                AppFeatureConstants.SHORT_LEAVE_REQUEST_CONTROLLER, AppFeatureConstants.SHORT_LEAVE_REQUEST_ACTION,
                AppFeatureConstants.ATTENDANCE_MANAGEMENT, "bi bi-clock-history", AppFeatureType.Transaction, 65,
                canAdd: true, canApprove: true);

            // Comp Off ("System-detected, HR-approved") - unlike Work From
            // Home/On Duty/Short Leave Request above, there is no
            // canAdd/Create here: candidates are only ever system-created by
            // CompOffDetectionService, never submitted by an employee or
            // created via the API. Child of the existing Attendance
            // Management group (same level as the request features above).
            // Reachable by both HR Manager (view/approve - the only role
            // that can act on a candidate) and plain Employee (view own
            // history only) - see the role-permission grants below.
            Def("Comp Off", AppFeatureConstants.COMP_OFF,
                AppFeatureConstants.COMP_OFF_CONTROLLER, AppFeatureConstants.COMP_OFF_ACTION,
                AppFeatureConstants.ATTENDANCE_MANAGEMENT, "bi bi-calendar-plus", AppFeatureType.Transaction, 67,
                canApprove: true);

            // ---------------- PAYROLL (ensure parent + children + fixes) ----------------
            Def("Payroll", AppFeatureConstants.PAYROLL, "", "",
                null, "bi bi-cash-stack", AppFeatureType.Transaction, 70);

            Def("Salary Component", AppFeatureConstants.SALARY_COMPONENT,
                AppFeatureConstants.SALARY_COMPONENT_CONTROLLER, AppFeatureConstants.SALARY_COMPONENT_ACTION,
                AppFeatureConstants.PAYROLL, "bi bi-cash-coin", AppFeatureType.Master, 70,
                canAdd: true, canEdit: true, canDelete: true);

            Def("Salary Structure", AppFeatureConstants.SALARY_STRUCTURE,
                AppFeatureConstants.SALARY_STRUCTURE_CONTROLLER, AppFeatureConstants.SALARY_STRUCTURE_ACTION,
                AppFeatureConstants.PAYROLL, "bi bi-wallet-fill", AppFeatureType.Transaction, 71,
                canAdd: true, canEdit: true, canDelete: true);

            Def("Payroll Processing", AppFeatureConstants.PAYROLL_PROCESS,
                AppFeatureConstants.PAYROLL_PROCESS_CONTROLLER, AppFeatureConstants.PAYROLL_PROCESS_ACTION,
                AppFeatureConstants.PAYROLL, "bi bi-cpu-fill", AppFeatureType.Transaction, 72,
                canAdd: true, canApprove: true);

            Def("Payroll Dashboard", AppFeatureConstants.PAYROLL_DASHBOARD,
                AppFeatureConstants.PAYROLL_DASHBOARD_CONTROLLER, AppFeatureConstants.PAYROLL_DASHBOARD_ACTION,
                AppFeatureConstants.PAYROLL, "bi bi-graph-up", AppFeatureType.Dashboard, 73);

            Def("Payroll Register", AppFeatureConstants.PAYROLL_REGISTER,
                AppFeatureConstants.PAYROLL_REGISTER_CONTROLLER, AppFeatureConstants.PAYROLL_REGISTER_ACTION,
                AppFeatureConstants.PAYROLL, "bi bi-file-earmark-spreadsheet", AppFeatureType.Report, 74,
                canExport: true, canPrint: true);

            // Retire the broken Payslip menu (opened per-payroll instead)
            Def("Payslip", AppFeatureConstants.PAYSLIP,
                AppFeatureConstants.PAYSLIP_CONTROLLER, AppFeatureConstants.PAYSLIP_ACTION,
                AppFeatureConstants.PAYROLL, "bi bi-receipt", AppFeatureType.Transaction, 75,
                isMenu: false, canPrint: true);

            // ---------------- ASSET MANAGEMENT ----------------
            Def("Asset Management", AppFeatureConstants.ASSET_MANAGEMENT, "", "",
                null, "bi bi-box-seam-fill", AppFeatureType.Transaction, 80);

            Def("Asset Category", AppFeatureConstants.ASSET_CATEGORY,
                AppFeatureConstants.ASSET_CATEGORY_CONTROLLER, AppFeatureConstants.ASSET_CATEGORY_ACTION,
                AppFeatureConstants.ASSET_MANAGEMENT, "bi bi-grid-fill", AppFeatureType.Master, 81,
                canAdd: true, canEdit: true, canDelete: true);

            Def("Asset", AppFeatureConstants.ASSET,
                AppFeatureConstants.ASSET_CONTROLLER, AppFeatureConstants.ASSET_ACTION,
                AppFeatureConstants.ASSET_MANAGEMENT, "bi bi-laptop", AppFeatureType.Transaction, 82,
                canAdd: true, canEdit: true, canDelete: true);

            Def("Asset Allocation", AppFeatureConstants.ASSET_ALLOCATION,
                AppFeatureConstants.ASSET_ALLOCATION_CONTROLLER, AppFeatureConstants.ASSET_ALLOCATION_ACTION,
                AppFeatureConstants.ASSET_MANAGEMENT, "bi bi-arrow-left-right", AppFeatureType.Transaction, 83,
                canAdd: true, canEdit: true);

            // ---------------- RECRUITMENT ----------------
            Def("Recruitment", AppFeatureConstants.RECRUITMENT, "", "",
                null, "bi bi-person-badge-fill", AppFeatureType.Transaction, 90);

            Def("Job Openings", AppFeatureConstants.JOB_OPENING,
                AppFeatureConstants.JOB_OPENING_CONTROLLER, AppFeatureConstants.JOB_OPENING_ACTION,
                AppFeatureConstants.RECRUITMENT, "bi bi-briefcase-fill", AppFeatureType.Transaction, 91,
                canAdd: true, canEdit: true, canDelete: true);

            Def("Candidates", AppFeatureConstants.CANDIDATE,
                AppFeatureConstants.CANDIDATE_CONTROLLER, AppFeatureConstants.CANDIDATE_ACTION,
                AppFeatureConstants.RECRUITMENT, "bi bi-people-fill", AppFeatureType.Transaction, 92,
                canAdd: true, canEdit: true, canDelete: true);

            Def("Applications", AppFeatureConstants.CANDIDATE_APPLICATION,
                AppFeatureConstants.CANDIDATE_APPLICATION_CONTROLLER, AppFeatureConstants.CANDIDATE_APPLICATION_ACTION,
                AppFeatureConstants.RECRUITMENT, "bi bi-clipboard-check-fill", AppFeatureType.Transaction, 93,
                canAdd: true, canEdit: true);

            Def("Interviews", AppFeatureConstants.INTERVIEW,
                AppFeatureConstants.INTERVIEW_CONTROLLER, AppFeatureConstants.INTERVIEW_ACTION,
                AppFeatureConstants.RECRUITMENT, "bi bi-calendar-event-fill", AppFeatureType.Transaction, 94,
                canAdd: true, canEdit: true);

            Def("Recruitment Dashboard", AppFeatureConstants.RECRUITMENT_DASHBOARD,
                AppFeatureConstants.RECRUITMENT_DASHBOARD_CONTROLLER, AppFeatureConstants.RECRUITMENT_DASHBOARD_ACTION,
                AppFeatureConstants.RECRUITMENT, "bi bi-graph-up-arrow", AppFeatureType.Dashboard, 95);

            // ---------------- EMPLOYEE ONBOARDING ----------------
            // An Onboarding case is started either manually by HR for any
            // existing Employee, or automatically when a Recruitment
            // Candidate is converted into an Employee (see
            // Employee.CandidateId / EmployeeService.CreateAsync). Grouped
            // as its own top-level module rather than nested under
            // Recruitment, since HR starts most cases directly.
            Def("Onboarding", AppFeatureConstants.ONBOARDING_MANAGEMENT, "", "",
                null, "bi bi-person-check-fill", AppFeatureType.Transaction, 96);

            Def("Onboarding Tracker", AppFeatureConstants.ONBOARDING,
                AppFeatureConstants.ONBOARDING_CONTROLLER, AppFeatureConstants.ONBOARDING_ACTION,
                AppFeatureConstants.ONBOARDING_MANAGEMENT, "bi bi-list-check", AppFeatureType.Transaction, 97,
                canAdd: true, canEdit: true, canDelete: true);

            Def("Onboarding Checklist Templates", AppFeatureConstants.ONBOARDING_TEMPLATE,
                AppFeatureConstants.ONBOARDING_TEMPLATE_CONTROLLER, AppFeatureConstants.ONBOARDING_TEMPLATE_ACTION,
                AppFeatureConstants.ONBOARDING_MANAGEMENT, "bi bi-card-checklist", AppFeatureType.Master, 98,
                canAdd: true, canEdit: true, canDelete: true);

            // ---------------- PROBATION & CONFIRMATION (Maker-Checker) ----------------
            // New top-level module (sibling of Onboarding/Asset
            // Management/Recruitment above), NOT nested under Attendance
            // Management or Employee Management - see the
            // PROBATION_CONFIRMATION_MANAGEMENT constant in
            // AppFeatureConstants.cs. This is the first feature in this
            // codebase using the Maker-Checker (segregation of duties)
            // pattern - HR-only (no Employee self-service grant below).
            // LATER AGENTS building PIP/Transfer: add your child features
            // here too (order 122/123+), nested under this same
            // PROBATION_CONFIRMATION_MANAGEMENT parent - do not create a
            // new parent group.
            Def("Probation & Confirmation", AppFeatureConstants.PROBATION_CONFIRMATION_MANAGEMENT, "", "",
                null, "bi bi-person-check", AppFeatureType.Transaction, 120);

            Def("Probation Confirmation", AppFeatureConstants.PROBATION_CONFIRMATION,
                AppFeatureConstants.PROBATION_CONFIRMATION_CONTROLLER, AppFeatureConstants.PROBATION_CONFIRMATION_ACTION,
                AppFeatureConstants.PROBATION_CONFIRMATION_MANAGEMENT, "bi bi-person-check-fill", AppFeatureType.Transaction, 121,
                canAdd: true, canApprove: true);

            // PIP (Performance Improvement Plan) - Phase 2, a SIBLING
            // child feature under the SAME PROBATION_CONFIRMATION_MANAGEMENT
            // parent above (not a new parent). Creation (hand-off from an
            // Approved Probation Confirmation with Recommendation ==
            // PlaceOnPIP) has no maker-checker gate; the gate applies to
            // the final outcome resolution (propose/approve/reject) - see
            // Domain/Entities/PipRecord.cs / PipService.
            Def("PIP", AppFeatureConstants.PIP,
                AppFeatureConstants.PIP_CONTROLLER, AppFeatureConstants.PIP_ACTION,
                AppFeatureConstants.PROBATION_CONFIRMATION_MANAGEMENT, "bi bi-graph-up-arrow", AppFeatureType.Transaction, 122,
                canAdd: true, canApprove: true);

            // Employee Transfer - Phase 3, a SIBLING child feature under
            // the SAME PROBATION_CONFIRMATION_MANAGEMENT parent above (not
            // a new parent). MAKER proposes new Company/Branch/Department/
            // Designation/ReportingManager values for an Employee; a
            // DIFFERENT person as CHECKER must Approve/Reject before the
            // change is applied to the live Employee record - see
            // Domain/Entities/EmployeeTransfer.cs / EmployeeTransferService.
            Def("Employee Transfer", AppFeatureConstants.EMPLOYEE_TRANSFER,
                AppFeatureConstants.EMPLOYEE_TRANSFER_CONTROLLER, AppFeatureConstants.EMPLOYEE_TRANSFER_ACTION,
                AppFeatureConstants.PROBATION_CONFIRMATION_MANAGEMENT, "bi bi-arrow-left-right", AppFeatureType.Transaction, 123,
                canAdd: true, canApprove: true);

            // Employee Feedback - Phase 4, a SIBLING child feature under the
            // SAME PROBATION_CONFIRMATION_MANAGEMENT parent above (not a new
            // parent). UNLIKE the three phases above, this feature has NO
            // maker-checker workflow - plain CRUD (View/Create/Edit/Delete),
            // and a BROADER audience: Reporting Managers and Employees
            // (self-view) interact with it directly, not just HR - see the
            // dual RolePermission grant blocks below (hrManagerRole gets
            // full CRUD, employeeRole gets View/Create since Reporting
            // Managers giving feedback are ordinary Employee-role users in
            // this codebase, not a separate role).
            Def("Employee Feedback", AppFeatureConstants.EMPLOYEE_FEEDBACK,
                AppFeatureConstants.EMPLOYEE_FEEDBACK_CONTROLLER, AppFeatureConstants.EMPLOYEE_FEEDBACK_ACTION,
                AppFeatureConstants.PROBATION_CONFIRMATION_MANAGEMENT, "bi bi-chat-square-text-fill", AppFeatureType.Transaction, 124,
                canAdd: true, canEdit: true, canDelete: true);

            // Rejoining - Phase 5 (final), a SIBLING child feature under the
            // SAME PROBATION_CONFIRMATION_MANAGEMENT parent above (not a
            // new parent). Like Employee Feedback above, NO maker-checker
            // workflow - a single-step, HR-only rehire action for a FORMER
            // employee. Only View/Create are needed (canAdd: true; no
            // canEdit/canDelete) - rejoining history is an append-only
            // audit log - see Domain/Entities/RejoiningHistory.cs /
            // RejoiningService.
            Def("Rejoining", AppFeatureConstants.REJOINING,
                AppFeatureConstants.REJOINING_CONTROLLER, AppFeatureConstants.REJOINING_ACTION,
                AppFeatureConstants.PROBATION_CONFIRMATION_MANAGEMENT, "bi bi-arrow-repeat", AppFeatureType.Transaction, 125,
                canAdd: true);

            // ---------------- TAXATION (Income Tax / TDS - India) ----------------
            // New top-level module (sibling of Probation & Confirmation/
            // Payroll above) - see AppFeatureConstants.TAXATION_MANAGEMENT.
            Def("Taxation", AppFeatureConstants.TAXATION_MANAGEMENT, "", "",
                null, "bi bi-cash-coin", AppFeatureType.Transaction, 126);

            // Tax Slabs - Admin-only master data, plain CRUD.
            Def("Tax Slabs", AppFeatureConstants.TAX_SLAB,
                AppFeatureConstants.TAX_SLAB_CONTROLLER, AppFeatureConstants.TAX_SLAB_ACTION,
                AppFeatureConstants.TAXATION_MANAGEMENT, "bi bi-bar-chart-steps", AppFeatureType.Master, 127,
                canAdd: true, canEdit: true, canDelete: true);

            // Tax Declaration - BROADER audience than most Probation &
            // Confirmation siblings (like Employee Feedback): every
            // Employee needs Create/View on their OWN declaration in
            // addition to the HR Manager's full View/Create/Approve grant
            // below - see the dual RolePermission grant blocks.
            Def("Tax Declaration", AppFeatureConstants.TAX_DECLARATION,
                AppFeatureConstants.TAX_DECLARATION_CONTROLLER, AppFeatureConstants.TAX_DECLARATION_ACTION,
                AppFeatureConstants.TAXATION_MANAGEMENT, "bi bi-file-earmark-text", AppFeatureType.Transaction, 128,
                canAdd: true, canApprove: true);

            // Tax Computation - HR/Payroll-only (View only; the actual
            // compute-trigger action is gated by TAX_DECLARATION's Approve
            // permission inside TaxComputationService, not a separate
            // permission check - this feature entry exists for menu/
            // routing purposes on the APP side).
            Def("Tax Computation", AppFeatureConstants.TAX_COMPUTATION,
                AppFeatureConstants.TAX_COMPUTATION_CONTROLLER, AppFeatureConstants.TAX_COMPUTATION_ACTION,
                AppFeatureConstants.TAXATION_MANAGEMENT, "bi bi-calculator", AppFeatureType.Transaction, 129);

            // ---------------- COMMUNICATION ----------------
            Def("Communication", AppFeatureConstants.COMMUNICATION, "", "",
                null, "bi bi-megaphone-fill", AppFeatureType.Transaction, 100);

            Def("Announcements", AppFeatureConstants.ANNOUNCEMENT,
                AppFeatureConstants.ANNOUNCEMENT_CONTROLLER, AppFeatureConstants.ANNOUNCEMENT_ACTION,
                AppFeatureConstants.COMMUNICATION, "bi bi-bullhorn-fill", AppFeatureType.Transaction, 101,
                canAdd: true, canEdit: true, canDelete: true);

            Def("Events", AppFeatureConstants.EVENT,
                AppFeatureConstants.EVENT_CONTROLLER, AppFeatureConstants.EVENT_ACTION,
                AppFeatureConstants.COMMUNICATION, "bi bi-calendar3", AppFeatureType.Transaction, 102,
                canAdd: true, canEdit: true, canDelete: true);

            Def("Notifications", AppFeatureConstants.NOTIFICATION,
                AppFeatureConstants.NOTIFICATION_CONTROLLER, AppFeatureConstants.NOTIFICATION_ACTION,
                AppFeatureConstants.COMMUNICATION, "bi bi-bell-fill", AppFeatureType.Transaction, 103);

            // ---------------- HELP & SUPPORT ----------------
            Def("Help & Support", AppFeatureConstants.HELP_SUPPORT, "", "",
                null, "bi bi-life-preserver", AppFeatureType.Transaction, 110);

            // Points at the admin-only "manage all tickets" screen
            // (SupportTicket/Index) - self-service employees reach their
            // own tickets via the ESS sidebar (SupportTicket/MyTickets)
            // instead, since Index is blocked for them by EssRestriction.
            Def("Support Tickets", AppFeatureConstants.SUPPORT_TICKET,
                AppFeatureConstants.SUPPORT_TICKET_CONTROLLER, AppFeatureConstants.SUPPORT_TICKET_ACTION,
                AppFeatureConstants.HELP_SUPPORT, "bi bi-ticket-detailed-fill", AppFeatureType.Transaction, 111,
                canAdd: true, canDelete: false);

            // Points at the admin "manage FAQ" screen (Faq/Index) -
            // everyone (including self-service) reaches the public list via
            // the ESS sidebar / Faq/Browse instead.
            Def("FAQ / Knowledge Base", AppFeatureConstants.FAQ,
                AppFeatureConstants.FAQ_CONTROLLER, AppFeatureConstants.FAQ_ACTION,
                AppFeatureConstants.HELP_SUPPORT, "bi bi-question-circle-fill", AppFeatureType.Master, 112,
                canAdd: true, canEdit: true, canDelete: true);

            // Security / Role / Permission menu items already exist from the
            // initial one-time seed above (see the "SECURITY" region) - just
            // make sure Permission also gets a Delete permission generated,
            // since the original seed only granted canEdit.
            Def("Permissions", AppFeatureConstants.PERMISSION,
                AppFeatureConstants.PERMISSION_CONTROLLER, AppFeatureConstants.PERMISSION_ACTION,
                AppFeatureConstants.SECURITY, "bi bi-key-fill", AppFeatureType.Security, 23,
                canAdd: true, canEdit: true, canDelete: true);

            // Feature Maintenance (AppFeatures screen) never had a menu entry
            // of its own - the controller/views have existed for a while but
            // were only reachable by typing the URL directly. Added here
            // (rather than the one-time seed above) so it also backfills
            // onto databases that were already seeded before this existed.
            Def("Feature Maintenance", AppFeatureConstants.APP_FEATURE,
                AppFeatureConstants.APP_FEATURE_CONTROLLER, AppFeatureConstants.APP_FEATURE_ACTION,
                AppFeatureConstants.SECURITY, "bi bi-diagram-3", AppFeatureType.Security, 25,
                canAdd: true, canEdit: true, canDelete: true);


            // =====================================================
            // TAXATION
            // =====================================================

            // Parent Menu
            Def("Taxation", AppFeatureConstants.TAXATION, "", "",
                null, "bi bi-cash-stack", AppFeatureType.Transaction, 70);

            // Income Tax
            Def("Income Tax", AppFeatureConstants.INCOME_TAX,
                AppFeatureConstants.INCOME_TAX_CONTROLLER,
                AppFeatureConstants.INCOME_TAX_ACTION,
                AppFeatureConstants.TAXATION,
                "bi bi-cash-coin",
                AppFeatureType.Master,
                71,
                canAdd: true,
                canEdit: true,
                canDelete: true);

            // Tax Declaration
            Def("Tax Declaration", AppFeatureConstants.TAX_DECLARATION,
                AppFeatureConstants.TAX_DECLARATION_CONTROLLER,
                AppFeatureConstants.TAX_DECLARATION_ACTION,
                AppFeatureConstants.TAXATION,
                "bi bi-wallet-fill",
                AppFeatureType.Transaction,
                72,
                canAdd: true,
                canEdit: true,
                canDelete: true);

            // Investment Declaration
            Def("Investment Declaration", AppFeatureConstants.INVESTMENT_DECLARATION,
                AppFeatureConstants.INVESTMENT_DECLARATION_CONTROLLER,
                AppFeatureConstants.INVESTMENT_DECLARATION_ACTION,
                AppFeatureConstants.TAXATION,
                "bi bi-piggy-bank",
                AppFeatureType.Transaction,
                73,
                canAdd: true,
                canEdit: true,
                canApprove: true);

            // House Property
            Def("House Property", AppFeatureConstants.HOUSE_PROPERTY,
                AppFeatureConstants.HOUSE_PROPERTY_CONTROLLER,
                AppFeatureConstants.HOUSE_PROPERTY_ACTION,
                AppFeatureConstants.TAXATION,
                "bi bi-house-door",
                AppFeatureType.Transaction,
                74,
                canAdd: true,
                canEdit: true);

            // Other Income
            Def("Other Income", AppFeatureConstants.OTHER_INCOME,
                AppFeatureConstants.OTHER_INCOME_CONTROLLER,
                AppFeatureConstants.OTHER_INCOME_ACTION,
                AppFeatureConstants.TAXATION,
                "bi bi-receipt",
                AppFeatureType.Transaction,
                75,
                canAdd: true,
                canEdit: true);

            // TDS Projection
            Def("TDS Projection", AppFeatureConstants.TDS_PROJECTION,
                AppFeatureConstants.TDS_PROJECTION_CONTROLLER,
                AppFeatureConstants.TDS_PROJECTION_ACTION,
                AppFeatureConstants.TAXATION,
                "bi bi-file-earmark-spreadsheet",
                AppFeatureType.Report,
                76,
                canExport: true,
                canPrint: true);

            // Tax Regime
            Def("Tax Regime", AppFeatureConstants.TAX_REGIME,
                AppFeatureConstants.TAX_REGIME_CONTROLLER,
                AppFeatureConstants.TAX_REGIME_ACTION,
                AppFeatureConstants.TAXATION,
                "bi bi-list-check",
                AppFeatureType.Transaction,
                77,
                canAdd: true,
                canEdit: true);

            // Form 16
            Def("Form 16", AppFeatureConstants.FORM_16,
                AppFeatureConstants.FORM_16_CONTROLLER,
                AppFeatureConstants.FORM_16_ACTION,
                AppFeatureConstants.TAXATION,
                "bi bi-file-earmark-pdf",
                AppFeatureType.Report,
                78,
                canPrint: true,
                canExport: true);


            // ---------------- RECONCILE (upsert by Code) ----------------
            var existing = await context.AppFeatures.ToListAsync();

            var existingByCode = existing
                .Where(f => !string.IsNullOrEmpty(f.Code))
                .GroupBy(f => f.Code)
                .ToDictionary(g => g.Key, g => g.First());

            string? FinalId(string? code)
            {
                if (string.IsNullOrEmpty(code)) return null;
                if (existingByCode.TryGetValue(code, out var e)) return e.Id;
                var d = desired.FirstOrDefault(x => x.Code == code);
                return d?.Id;
            }

            foreach (var d in desired)
            {
                var parentFinalId = FinalId(d.ParentFeatureId);

                if (existingByCode.TryGetValue(d.Code, out var current))
                {
                    current.Name = d.Name;
                    current.Module = d.Module;
                    current.ControllerName = d.ControllerName;
                    current.ActionName = d.ActionName;
                    current.ParentFeatureId = parentFinalId;
                    current.Icon = d.Icon;
                    current.DisplayOrder = d.DisplayOrder;
                    current.AppFeatureType = d.AppFeatureType;
                    current.IsMenu = d.IsMenu;
                    current.IsVisible = d.IsVisible;
                    current.IsActive = d.IsActive;
                    current.CanView = d.CanView;
                    current.CanAdd = d.CanAdd;
                    current.CanEdit = d.CanEdit;
                    current.CanDelete = d.CanDelete;
                    current.CanApprove = d.CanApprove;
                    current.CanExport = d.CanExport;
                    current.CanPrint = d.CanPrint;
                    current.ModifiedBy = "System";
                    current.ModifiedOn = DateTime.UtcNow;
                    context.AppFeatures.Update(current);
                }
                else
                {
                    d.ParentFeatureId = parentFinalId;
                    await context.AppFeatures.AddAsync(d);
                }
            }

            await context.SaveChangesAsync();
        }

        // =====================================================
        // 🔐 IDEMPOTENT PERMISSION RECONCILE
        //    - generates View/Create/Edit/Delete/Approve/Export/Print
        //      permissions per screen feature (driven by its Can* flags)
        //    - grants every permission to the Super Admin and System
        //      Configurator roles (both are full application-management
        //      accounts; new features should never silently lock them out)
        //    Safe to run on every startup.
        // =====================================================
        public static async Task ReconcilePermissionsAsync(
            ApplicationDbContext context,
            string tenantId)
        {
            // Only features that map to an actual screen get permissions.
            var features = await context.AppFeatures
                .Where(f => f.IsActive
                         && f.ControllerName != null
                         && f.ControllerName != "")
                .ToListAsync();

            var existingByCode = (await context.Permissions.ToListAsync())
                .Where(p => !string.IsNullOrEmpty(p.Code))
                .GroupBy(p => p.Code)
                .ToDictionary(g => g.Key, g => g.First());

            var toAdd = new List<Permission>();

            void AddPerm(AppFeature f, string action, string label)
            {
                var code = f.Code + "_" + action.ToUpper();
                if (existingByCode.ContainsKey(code)) return;
                if (toAdd.Any(p => p.Code == code)) return;

                toAdd.Add(new Permission
                {
                    Id = IDManager.GetNewId(new Permission()),
                    Name = label + " " + f.Name,
                    Code = code,
                    Module = string.IsNullOrEmpty(f.Module) ? Modules.HRMS : f.Module,
                    FeatureId = f.Code,
                    Action = action,
                    DisplayOrder = f.DisplayOrder,
                    TenantId = tenantId,
                    CreatedBy = "System",
                    CreatedOn = DateTime.UtcNow
                });
            }

            foreach (var f in features)
            {
                AddPerm(f, Actions.View, "View");
                if (f.CanAdd) AddPerm(f, Actions.Create, "Create");
                if (f.CanEdit) AddPerm(f, Actions.Edit, "Edit");
                if (f.CanDelete) AddPerm(f, Actions.Delete, "Delete");
                if (f.CanApprove) AddPerm(f, Actions.Approve, "Approve");
                if (f.CanExport) AddPerm(f, Actions.Export, "Export");
                if (f.CanPrint) AddPerm(f, "Print", "Print");
            }

            if (toAdd.Count > 0)
            {
                await context.Permissions.AddRangeAsync(toAdd);
                await context.SaveChangesAsync();
            }

            // Super Admin + System Configurator → every permission (fills
            // the gaps for new features on every startup).
            var fullAccessRoles = await context.Roles
                .Where(r => r.Code == ConstantHelper.SUPER_ADMIN || r.Code == ConstantHelper.SYSTEM_CONFIGURATOR)
                .ToListAsync();

            if (fullAccessRoles.Count > 0)
            {
                var allPermissionIds = await context.Permissions
                    .Select(p => p.Id)
                    .ToListAsync();

                var newLinks = new List<RolePermission>();

                foreach (var role in fullAccessRoles)
                {
                    var linked = (await context.RolePermissions
                            .Where(rp => rp.RoleId == role.Id)
                            .Select(rp => rp.PermissionId)
                            .ToListAsync())
                        .ToHashSet();

                    newLinks.AddRange(allPermissionIds
                        .Where(pid => !linked.Contains(pid))
                        .Select(pid => new RolePermission
                        {
                            Id = IDManager.GetNewId(new RolePermission()),
                            RoleId = role.Id,
                            PermissionId = pid,
                            IsAllowed = true,
                            CreatedBy = "System",
                            CreatedOn = DateTime.UtcNow
                        }));
                }

                if (newLinks.Count > 0)
                {
                    await context.RolePermissions.AddRangeAsync(newLinks);
                    await context.SaveChangesAsync();
                }
            }

            // HR Manager → Approve Leave (LEAVE_APPROVAL/Approve).
            // LeaveApplicationService.IsAuthorizedForLevelAsync's Level 3
            // check was formalized from a loose actingRoleName.Contains("HR")
            // substring match to a real permission check (does the acting
            // user hold an allowed RolePermission for this Permission,
            // regardless of what their role happens to be named). Databases
            // seeded before this permission existed - or before it was
            // included in the one-time HR Manager seed above - would
            // otherwise have an HR Manager role that can no longer approve
            // anything at Level 3. Runs on every startup, so it also
            // backfills any tenant/role created before this reconcile step
            // existed.
            var hrManagerRole = await context.Roles
                .FirstOrDefaultAsync(r => r.Code == ConstantHelper.HR_MANAGER);

            if (hrManagerRole != null)
            {
                var hrApprovalPermissionIds = await context.Permissions
                    .Where(p => p.FeatureId == AppFeatureConstants.LEAVE_APPROVAL && p.Action == Actions.Approve)
                    .Select(p => p.Id)
                    .ToListAsync();

                var alreadyLinked = (await context.RolePermissions
                        .Where(rp => rp.RoleId == hrManagerRole.Id)
                        .Select(rp => rp.PermissionId)
                        .ToListAsync())
                    .ToHashSet();

                var hrNewLinks = hrApprovalPermissionIds
                    .Where(pid => !alreadyLinked.Contains(pid))
                    .Select(pid => new RolePermission
                    {
                        Id = IDManager.GetNewId(new RolePermission()),
                        RoleId = hrManagerRole.Id,
                        PermissionId = pid,
                        IsAllowed = true,
                        CreatedBy = "System",
                        CreatedOn = DateTime.UtcNow
                    })
                    .ToList();

                if (hrNewLinks.Count > 0)
                {
                    await context.RolePermissions.AddRangeAsync(hrNewLinks);
                    await context.SaveChangesAsync();
                }
            }

            // HR Manager → full access (View/Create/Edit/Delete) on the new
            // Employee Onboarding tracker + checklist template screens.
            // Super Admin / System Configurator already received every
            // permission (including these) from the fullAccessRoles loop
            // above; Onboarding is a straightforward HR-manages-this-list
            // feature with no approval chain, so no separate Approve grant
            // is needed here (mirrors Asset/Recruitment's simple CRUD
            // permission style rather than Leave/Attendance Regularization's
            // approval-chain style).
            if (hrManagerRole != null)
            {
                var onboardingPermissionIds = await context.Permissions
                    .Where(p => p.FeatureId == AppFeatureConstants.ONBOARDING
                             || p.FeatureId == AppFeatureConstants.ONBOARDING_TEMPLATE)
                    .Select(p => p.Id)
                    .ToListAsync();

                var alreadyLinked = (await context.RolePermissions
                        .Where(rp => rp.RoleId == hrManagerRole.Id)
                        .Select(rp => rp.PermissionId)
                        .ToListAsync())
                    .ToHashSet();

                var onboardingNewLinks = onboardingPermissionIds
                    .Where(pid => !alreadyLinked.Contains(pid))
                    .Select(pid => new RolePermission
                    {
                        Id = IDManager.GetNewId(new RolePermission()),
                        RoleId = hrManagerRole.Id,
                        PermissionId = pid,
                        IsAllowed = true,
                        CreatedBy = "System",
                        CreatedOn = DateTime.UtcNow
                    })
                    .ToList();

                if (onboardingNewLinks.Count > 0)
                {
                    await context.RolePermissions.AddRangeAsync(onboardingNewLinks);
                    await context.SaveChangesAsync();
                }
            }

            // HR Manager → full access (View/Create/Edit/Delete) on the new
            // Employee Bank Details screen. Same simple CRUD style as the
            // Onboarding grant above - no approval chain involved. Super
            // Admin / System Configurator already received every permission
            // (including this one) from the fullAccessRoles loop above.
            if (hrManagerRole != null)
            {
                var bankPermissionIds = await context.Permissions
                    .Where(p => p.FeatureId == AppFeatureConstants.EMPLOYEE_BANK)
                    .Select(p => p.Id)
                    .ToListAsync();

                var alreadyLinked = (await context.RolePermissions
                        .Where(rp => rp.RoleId == hrManagerRole.Id)
                        .Select(rp => rp.PermissionId)
                        .ToListAsync())
                    .ToHashSet();

                var bankNewLinks = bankPermissionIds
                    .Where(pid => !alreadyLinked.Contains(pid))
                    .Select(pid => new RolePermission
                    {
                        Id = IDManager.GetNewId(new RolePermission()),
                        RoleId = hrManagerRole.Id,
                        PermissionId = pid,
                        IsAllowed = true,
                        CreatedBy = "System",
                        CreatedOn = DateTime.UtcNow
                    })
                    .ToList();

                if (bankNewLinks.Count > 0)
                {
                    await context.RolePermissions.AddRangeAsync(bankNewLinks);
                    await context.SaveChangesAsync();
                }
            }

            // HR Manager → full access (View/Create/Edit/Delete) on the new
            // Attendance Policy screen. Same simple CRUD style as the
            // Onboarding/Employee Bank Details grants above - no approval
            // chain involved. Super Admin / System Configurator already
            // received every permission (including this one) from the
            // fullAccessRoles loop above.
            if (hrManagerRole != null)
            {
                var attendancePolicyPermissionIds = await context.Permissions
                    .Where(p => p.FeatureId == AppFeatureConstants.ATTENDANCE_POLICY)
                    .Select(p => p.Id)
                    .ToListAsync();

                var alreadyLinked = (await context.RolePermissions
                        .Where(rp => rp.RoleId == hrManagerRole.Id)
                        .Select(rp => rp.PermissionId)
                        .ToListAsync())
                    .ToHashSet();

                var attendancePolicyNewLinks = attendancePolicyPermissionIds
                    .Where(pid => !alreadyLinked.Contains(pid))
                    .Select(pid => new RolePermission
                    {
                        Id = IDManager.GetNewId(new RolePermission()),
                        RoleId = hrManagerRole.Id,
                        PermissionId = pid,
                        IsAllowed = true,
                        CreatedBy = "System",
                        CreatedOn = DateTime.UtcNow
                    })
                    .ToList();

                if (attendancePolicyNewLinks.Count > 0)
                {
                    await context.RolePermissions.AddRangeAsync(attendancePolicyNewLinks);
                    await context.SaveChangesAsync();
                }
            }

            // Work From Home Request - dual-audience feature (unlike
            // Attendance Correction above, which only ever granted HR
            // Manager - Employee never actually got a View grant there,
            // which would leave the menu unreachable for plain employees):
            //   - HR Manager -> View/Create/Approve (the HR override path
            //     inside WfhRequestService.EnsureApproverAuthorizedAsync).
            //   - Employee -> View/Create (create their own request, view
            //     their own list; a Reporting Manager approving a direct
            //     report's request is still authorized purely by
            //     Employee.ReportingManagerId inside the service - no
            //     RolePermission check gates the Approve/Reject/Cancel API
            //     actions themselves, only menu visibility does).
            // Super Admin / System Configurator already received every
            // permission from the fullAccessRoles loop above.
            if (hrManagerRole != null)
            {
                var wfhPermissionIds = await context.Permissions
                    .Where(p => p.FeatureId == AppFeatureConstants.WFH_REQUEST)
                    .Select(p => p.Id)
                    .ToListAsync();

                var alreadyLinked = (await context.RolePermissions
                        .Where(rp => rp.RoleId == hrManagerRole.Id)
                        .Select(rp => rp.PermissionId)
                        .ToListAsync())
                    .ToHashSet();

                var wfhNewLinks = wfhPermissionIds
                    .Where(pid => !alreadyLinked.Contains(pid))
                    .Select(pid => new RolePermission
                    {
                        Id = IDManager.GetNewId(new RolePermission()),
                        RoleId = hrManagerRole.Id,
                        PermissionId = pid,
                        IsAllowed = true,
                        CreatedBy = "System",
                        CreatedOn = DateTime.UtcNow
                    })
                    .ToList();

                if (wfhNewLinks.Count > 0)
                {
                    await context.RolePermissions.AddRangeAsync(wfhNewLinks);
                    await context.SaveChangesAsync();
                }
            }

            var employeeRole = await context.Roles
                .FirstOrDefaultAsync(r => r.Code == ConstantHelper.EMPLOYEE);

            if (employeeRole != null)
            {
                var wfhSelfServicePermissionIds = await context.Permissions
                    .Where(p => p.FeatureId == AppFeatureConstants.WFH_REQUEST
                             && (p.Action == Actions.View || p.Action == Actions.Create))
                    .Select(p => p.Id)
                    .ToListAsync();

                var alreadyLinked = (await context.RolePermissions
                        .Where(rp => rp.RoleId == employeeRole.Id)
                        .Select(rp => rp.PermissionId)
                        .ToListAsync())
                    .ToHashSet();

                var wfhEmployeeNewLinks = wfhSelfServicePermissionIds
                    .Where(pid => !alreadyLinked.Contains(pid))
                    .Select(pid => new RolePermission
                    {
                        Id = IDManager.GetNewId(new RolePermission()),
                        RoleId = employeeRole.Id,
                        PermissionId = pid,
                        IsAllowed = true,
                        CreatedBy = "System",
                        CreatedOn = DateTime.UtcNow
                    })
                    .ToList();

                if (wfhEmployeeNewLinks.Count > 0)
                {
                    await context.RolePermissions.AddRangeAsync(wfhEmployeeNewLinks);
                    await context.SaveChangesAsync();
                }
            }

            // On Duty Request - same dual-audience shape as Work From Home
            // Request above:
            //   - HR Manager -> View/Create/Approve (the HR override path
            //     inside OnDutyRequestService.EnsureApproverAuthorizedAsync).
            //   - Employee -> View/Create (create their own request, view
            //     their own list; a Reporting Manager approving a direct
            //     report's request is still authorized purely by
            //     Employee.ReportingManagerId inside the service - no
            //     RolePermission check gates the Approve/Reject/Cancel API
            //     actions themselves, only menu visibility does).
            // Super Admin / System Configurator already received every
            // permission from the fullAccessRoles loop above.
            if (hrManagerRole != null)
            {
                var onDutyPermissionIds = await context.Permissions
                    .Where(p => p.FeatureId == AppFeatureConstants.ON_DUTY_REQUEST)
                    .Select(p => p.Id)
                    .ToListAsync();

                var alreadyLinked = (await context.RolePermissions
                        .Where(rp => rp.RoleId == hrManagerRole.Id)
                        .Select(rp => rp.PermissionId)
                        .ToListAsync())
                    .ToHashSet();

                var onDutyNewLinks = onDutyPermissionIds
                    .Where(pid => !alreadyLinked.Contains(pid))
                    .Select(pid => new RolePermission
                    {
                        Id = IDManager.GetNewId(new RolePermission()),
                        RoleId = hrManagerRole.Id,
                        PermissionId = pid,
                        IsAllowed = true,
                        CreatedBy = "System",
                        CreatedOn = DateTime.UtcNow
                    })
                    .ToList();

                if (onDutyNewLinks.Count > 0)
                {
                    await context.RolePermissions.AddRangeAsync(onDutyNewLinks);
                    await context.SaveChangesAsync();
                }
            }

            if (employeeRole != null)
            {
                var onDutySelfServicePermissionIds = await context.Permissions
                    .Where(p => p.FeatureId == AppFeatureConstants.ON_DUTY_REQUEST
                             && (p.Action == Actions.View || p.Action == Actions.Create))
                    .Select(p => p.Id)
                    .ToListAsync();

                var alreadyLinked = (await context.RolePermissions
                        .Where(rp => rp.RoleId == employeeRole.Id)
                        .Select(rp => rp.PermissionId)
                        .ToListAsync())
                    .ToHashSet();

                var onDutyEmployeeNewLinks = onDutySelfServicePermissionIds
                    .Where(pid => !alreadyLinked.Contains(pid))
                    .Select(pid => new RolePermission
                    {
                        Id = IDManager.GetNewId(new RolePermission()),
                        RoleId = employeeRole.Id,
                        PermissionId = pid,
                        IsAllowed = true,
                        CreatedBy = "System",
                        CreatedOn = DateTime.UtcNow
                    })
                    .ToList();

                if (onDutyEmployeeNewLinks.Count > 0)
                {
                    await context.RolePermissions.AddRangeAsync(onDutyEmployeeNewLinks);
                    await context.SaveChangesAsync();
                }
            }

            // Short Leave Request - same dual-audience shape as Work From
            // Home Request/On Duty Request above:
            //   - HR Manager -> View/Create/Approve (the HR override path
            //     inside ShortLeaveRequestService.EnsureApproverAuthorizedAsync).
            //   - Employee -> View/Create (create their own request, view
            //     their own list; a Reporting Manager approving a direct
            //     report's request is still authorized purely by
            //     Employee.ReportingManagerId inside the service - no
            //     RolePermission check gates the Approve/Reject/Cancel API
            //     actions themselves, only menu visibility does).
            // Super Admin / System Configurator already received every
            // permission from the fullAccessRoles loop above.
            if (hrManagerRole != null)
            {
                var shortLeavePermissionIds = await context.Permissions
                    .Where(p => p.FeatureId == AppFeatureConstants.SHORT_LEAVE_REQUEST)
                    .Select(p => p.Id)
                    .ToListAsync();

                var alreadyLinked = (await context.RolePermissions
                        .Where(rp => rp.RoleId == hrManagerRole.Id)
                        .Select(rp => rp.PermissionId)
                        .ToListAsync())
                    .ToHashSet();

                var shortLeaveNewLinks = shortLeavePermissionIds
                    .Where(pid => !alreadyLinked.Contains(pid))
                    .Select(pid => new RolePermission
                    {
                        Id = IDManager.GetNewId(new RolePermission()),
                        RoleId = hrManagerRole.Id,
                        PermissionId = pid,
                        IsAllowed = true,
                        CreatedBy = "System",
                        CreatedOn = DateTime.UtcNow
                    })
                    .ToList();

                if (shortLeaveNewLinks.Count > 0)
                {
                    await context.RolePermissions.AddRangeAsync(shortLeaveNewLinks);
                    await context.SaveChangesAsync();
                }
            }

            if (employeeRole != null)
            {
                var shortLeaveSelfServicePermissionIds = await context.Permissions
                    .Where(p => p.FeatureId == AppFeatureConstants.SHORT_LEAVE_REQUEST
                             && (p.Action == Actions.View || p.Action == Actions.Create))
                    .Select(p => p.Id)
                    .ToListAsync();

                var alreadyLinked = (await context.RolePermissions
                        .Where(rp => rp.RoleId == employeeRole.Id)
                        .Select(rp => rp.PermissionId)
                        .ToListAsync())
                    .ToHashSet();

                var shortLeaveEmployeeNewLinks = shortLeaveSelfServicePermissionIds
                    .Where(pid => !alreadyLinked.Contains(pid))
                    .Select(pid => new RolePermission
                    {
                        Id = IDManager.GetNewId(new RolePermission()),
                        RoleId = employeeRole.Id,
                        PermissionId = pid,
                        IsAllowed = true,
                        CreatedBy = "System",
                        CreatedOn = DateTime.UtcNow
                    })
                    .ToList();

                if (shortLeaveEmployeeNewLinks.Count > 0)
                {
                    await context.RolePermissions.AddRangeAsync(shortLeaveEmployeeNewLinks);
                    await context.SaveChangesAsync();
                }
            }

            // Comp Off - unlike Work From Home Request/On Duty Request/Short
            // Leave Request above, this is NOT dual-audience for ACTING:
            //   - HR Manager -> View/Approve (full: review + Approve/Reject
            //     every candidate tenant-wide - the only role that can act,
            //     enforced inside CompOffService.ApproveAsync/RejectAsync
            //     via IsHrOrAdminForAttendanceAsync).
            //   - Employee -> View only (their own Comp Off history/status,
            //     for transparency - they never create or act on a
            //     candidate; candidates are only ever system-created by
            //     CompOffDetectionService).
            // Super Admin / System Configurator already received every
            // permission from the fullAccessRoles loop above.
            if (hrManagerRole != null)
            {
                var compOffPermissionIds = await context.Permissions
                    .Where(p => p.FeatureId == AppFeatureConstants.COMP_OFF)
                    .Select(p => p.Id)
                    .ToListAsync();

                var alreadyLinked = (await context.RolePermissions
                        .Where(rp => rp.RoleId == hrManagerRole.Id)
                        .Select(rp => rp.PermissionId)
                        .ToListAsync())
                    .ToHashSet();

                var compOffNewLinks = compOffPermissionIds
                    .Where(pid => !alreadyLinked.Contains(pid))
                    .Select(pid => new RolePermission
                    {
                        Id = IDManager.GetNewId(new RolePermission()),
                        RoleId = hrManagerRole.Id,
                        PermissionId = pid,
                        IsAllowed = true,
                        CreatedBy = "System",
                        CreatedOn = DateTime.UtcNow
                    })
                    .ToList();

                if (compOffNewLinks.Count > 0)
                {
                    await context.RolePermissions.AddRangeAsync(compOffNewLinks);
                    await context.SaveChangesAsync();
                }
            }

            if (employeeRole != null)
            {
                var compOffSelfServicePermissionIds = await context.Permissions
                    .Where(p => p.FeatureId == AppFeatureConstants.COMP_OFF
                             && p.Action == Actions.View)
                    .Select(p => p.Id)
                    .ToListAsync();

                var alreadyLinked = (await context.RolePermissions
                        .Where(rp => rp.RoleId == employeeRole.Id)
                        .Select(rp => rp.PermissionId)
                        .ToListAsync())
                    .ToHashSet();

                var compOffEmployeeNewLinks = compOffSelfServicePermissionIds
                    .Where(pid => !alreadyLinked.Contains(pid))
                    .Select(pid => new RolePermission
                    {
                        Id = IDManager.GetNewId(new RolePermission()),
                        RoleId = employeeRole.Id,
                        PermissionId = pid,
                        IsAllowed = true,
                        CreatedBy = "System",
                        CreatedOn = DateTime.UtcNow
                    })
                    .ToList();

                if (compOffEmployeeNewLinks.Count > 0)
                {
                    await context.RolePermissions.AddRangeAsync(compOffEmployeeNewLinks);
                    await context.SaveChangesAsync();
                }
            }

            // HR Manager -> access to the 4 new Attendance Insights screens
            // (Calendar/Team/Summary/Dashboard). Team Attendance also works
            // for Reporting Managers who aren't HR Manager - that scoping is
            // enforced server-side per-request (self/direct-reports/all),
            // not via this role-permission grant, so no extra role needs a
            // grant here beyond what already reaches this feature.
            if (hrManagerRole != null)
            {
                var insightsFeatureIds = new[]
                {
                    AppFeatureConstants.ATTENDANCE_CALENDAR,
                    AppFeatureConstants.TEAM_ATTENDANCE,
                    AppFeatureConstants.ATTENDANCE_SUMMARY,
                    AppFeatureConstants.ATTENDANCE_DASHBOARD
                };

                var insightsPermissionIds = await context.Permissions
                    .Where(p => insightsFeatureIds.Contains(p.FeatureId))
                    .Select(p => p.Id)
                    .ToListAsync();

                var alreadyLinked = (await context.RolePermissions
                        .Where(rp => rp.RoleId == hrManagerRole.Id)
                        .Select(rp => rp.PermissionId)
                        .ToListAsync())
                    .ToHashSet();

                var insightsNewLinks = insightsPermissionIds
                    .Where(pid => !alreadyLinked.Contains(pid))
                    .Select(pid => new RolePermission
                    {
                        Id = IDManager.GetNewId(new RolePermission()),
                        RoleId = hrManagerRole.Id,
                        PermissionId = pid,
                        IsAllowed = true,
                        CreatedBy = "System",
                        CreatedOn = DateTime.UtcNow
                    })
                    .ToList();

                if (insightsNewLinks.Count > 0)
                {
                    await context.RolePermissions.AddRangeAsync(insightsNewLinks);
                    await context.SaveChangesAsync();
                }
            }

            // Probation Confirmation (Maker-Checker) - HR-only feature, no
            // Employee self-service grant (unlike WFH/On Duty/Short Leave
            // above): a Probation Confirmation is proposed by HR (the
            // Maker) about an employee, never self-submitted. HR Manager
            // gets full View/Create/Approve so the same role can both
            // propose AND check records - the actingUserId != MakerId
            // invariant enforced inside
            // ProbationConfirmationService.ApproveAsync/RejectAsync (not a
            // RolePermission grant) is what stops a single HR Manager user
            // from checking their own submission; a different HR Manager
            // user still can. Super Admin / System Configurator already
            // received every permission from the fullAccessRoles loop
            // above. LATER AGENTS building PIP/Transfer: mirror this exact
            // grant block for your own feature's FeatureId constant.
            if (hrManagerRole != null)
            {
                var probationConfirmationPermissionIds = await context.Permissions
                    .Where(p => p.FeatureId == AppFeatureConstants.PROBATION_CONFIRMATION)
                    .Select(p => p.Id)
                    .ToListAsync();

                var alreadyLinked = (await context.RolePermissions
                        .Where(rp => rp.RoleId == hrManagerRole.Id)
                        .Select(rp => rp.PermissionId)
                        .ToListAsync())
                    .ToHashSet();

                var probationConfirmationNewLinks = probationConfirmationPermissionIds
                    .Where(pid => !alreadyLinked.Contains(pid))
                    .Select(pid => new RolePermission
                    {
                        Id = IDManager.GetNewId(new RolePermission()),
                        RoleId = hrManagerRole.Id,
                        PermissionId = pid,
                        IsAllowed = true,
                        CreatedBy = "System",
                        CreatedOn = DateTime.UtcNow
                    })
                    .ToList();

                if (probationConfirmationNewLinks.Count > 0)
                {
                    await context.RolePermissions.AddRangeAsync(probationConfirmationNewLinks);
                    await context.SaveChangesAsync();
                }
            }

            // PIP (Performance Improvement Plan) - Phase 2, same HR-only
            // model as Probation Confirmation above (no Employee self-
            // service grant): HR Manager gets full View/Create/Approve so
            // the same role can both propose AND check outcome
            // resolutions - the actingUserId != MakerId invariant enforced
            // inside PipService.ApproveOutcomeAsync/RejectOutcomeAsync
            // (not a RolePermission grant) is what stops a single HR
            // Manager user from checking their own proposal; a different
            // HR Manager user still can. Super Admin / System Configurator
            // already received every permission from the fullAccessRoles
            // loop above.
            if (hrManagerRole != null)
            {
                var pipPermissionIds = await context.Permissions
                    .Where(p => p.FeatureId == AppFeatureConstants.PIP)
                    .Select(p => p.Id)
                    .ToListAsync();

                var alreadyLinked = (await context.RolePermissions
                        .Where(rp => rp.RoleId == hrManagerRole.Id)
                        .Select(rp => rp.PermissionId)
                        .ToListAsync())
                    .ToHashSet();

                var pipNewLinks = pipPermissionIds
                    .Where(pid => !alreadyLinked.Contains(pid))
                    .Select(pid => new RolePermission
                    {
                        Id = IDManager.GetNewId(new RolePermission()),
                        RoleId = hrManagerRole.Id,
                        PermissionId = pid,
                        IsAllowed = true,
                        CreatedBy = "System",
                        CreatedOn = DateTime.UtcNow
                    })
                    .ToList();

                if (pipNewLinks.Count > 0)
                {
                    await context.RolePermissions.AddRangeAsync(pipNewLinks);
                    await context.SaveChangesAsync();
                }
            }

            // Employee Transfer - Phase 3, same HR-only model as Probation
            // Confirmation/PIP above (no Employee self-service grant): HR
            // Manager gets full View/Create/Approve so the same role can
            // both propose AND check transfers - the actingUserId !=
            // MakerId invariant enforced inside
            // EmployeeTransferService.ApproveAsync/RejectAsync (not a
            // RolePermission grant) is what stops a single HR Manager user
            // from checking their own submission; a different HR Manager
            // user still can. Super Admin / System Configurator already
            // received every permission from the fullAccessRoles loop
            // above.
            if (hrManagerRole != null)
            {
                var employeeTransferPermissionIds = await context.Permissions
                    .Where(p => p.FeatureId == AppFeatureConstants.EMPLOYEE_TRANSFER)
                    .Select(p => p.Id)
                    .ToListAsync();

                var alreadyLinked = (await context.RolePermissions
                        .Where(rp => rp.RoleId == hrManagerRole.Id)
                        .Select(rp => rp.PermissionId)
                        .ToListAsync())
                    .ToHashSet();

                var employeeTransferNewLinks = employeeTransferPermissionIds
                    .Where(pid => !alreadyLinked.Contains(pid))
                    .Select(pid => new RolePermission
                    {
                        Id = IDManager.GetNewId(new RolePermission()),
                        RoleId = hrManagerRole.Id,
                        PermissionId = pid,
                        IsAllowed = true,
                        CreatedBy = "System",
                        CreatedOn = DateTime.UtcNow
                    })
                    .ToList();

                if (employeeTransferNewLinks.Count > 0)
                {
                    await context.RolePermissions.AddRangeAsync(employeeTransferNewLinks);
                    await context.SaveChangesAsync();
                }
            }

            // Employee Feedback - Phase 4, UNLIKE Probation Confirmation/
            // PIP/Employee Transfer above (all HR-only, no maker-checker),
            // this feature is plain CRUD with a BROADER audience:
            //   - HR Manager -> full View/Create/Edit/Delete (the HR
            //     override path inside EmployeeFeedbackService's Update/
            //     Delete author-or-Edit-permission check, and the HR path
            //     inside the Create reporting-manager-or-Create-permission
            //     check).
            //   - Employee -> View/Create. There is no separate "Manager"
            //     role in this codebase (see the dual-audience WFH/On Duty/
            //     Short Leave grants above) - a Reporting Manager giving
            //     feedback about a direct report is an ordinary Employee-
            //     role user, authorized purely by
            //     Employee.ReportingManagerId inside
            //     EmployeeFeedbackService.CreateAsync (no RolePermission
            //     check gates that comparison, only menu visibility does).
            //     The SAME View grant also covers an Employee viewing
            //     feedback about themself - filtered to
            //     IsVisibleToEmployee == true by
            //     EmployeeFeedbackService.GetForEmployeeAsync/GetByIdAsync,
            //     not by a separate permission.
            // Super Admin / System Configurator already received every
            // permission from the fullAccessRoles loop above.
            if (hrManagerRole != null)
            {
                var employeeFeedbackPermissionIds = await context.Permissions
                    .Where(p => p.FeatureId == AppFeatureConstants.EMPLOYEE_FEEDBACK)
                    .Select(p => p.Id)
                    .ToListAsync();

                var alreadyLinked = (await context.RolePermissions
                        .Where(rp => rp.RoleId == hrManagerRole.Id)
                        .Select(rp => rp.PermissionId)
                        .ToListAsync())
                    .ToHashSet();

                var employeeFeedbackNewLinks = employeeFeedbackPermissionIds
                    .Where(pid => !alreadyLinked.Contains(pid))
                    .Select(pid => new RolePermission
                    {
                        Id = IDManager.GetNewId(new RolePermission()),
                        RoleId = hrManagerRole.Id,
                        PermissionId = pid,
                        IsAllowed = true,
                        CreatedBy = "System",
                        CreatedOn = DateTime.UtcNow
                    })
                    .ToList();

                if (employeeFeedbackNewLinks.Count > 0)
                {
                    await context.RolePermissions.AddRangeAsync(employeeFeedbackNewLinks);
                    await context.SaveChangesAsync();
                }
            }

            if (employeeRole != null)
            {
                var employeeFeedbackSelfServicePermissionIds = await context.Permissions
                    .Where(p => p.FeatureId == AppFeatureConstants.EMPLOYEE_FEEDBACK
                             && (p.Action == Actions.View || p.Action == Actions.Create))
                    .Select(p => p.Id)
                    .ToListAsync();

                var alreadyLinked = (await context.RolePermissions
                        .Where(rp => rp.RoleId == employeeRole.Id)
                        .Select(rp => rp.PermissionId)
                        .ToListAsync())
                    .ToHashSet();

                var employeeFeedbackEmployeeNewLinks = employeeFeedbackSelfServicePermissionIds
                    .Where(pid => !alreadyLinked.Contains(pid))
                    .Select(pid => new RolePermission
                    {
                        Id = IDManager.GetNewId(new RolePermission()),
                        RoleId = employeeRole.Id,
                        PermissionId = pid,
                        IsAllowed = true,
                        CreatedBy = "System",
                        CreatedOn = DateTime.UtcNow
                    })
                    .ToList();

                if (employeeFeedbackEmployeeNewLinks.Count > 0)
                {
                    await context.RolePermissions.AddRangeAsync(employeeFeedbackEmployeeNewLinks);
                    await context.SaveChangesAsync();
                }
            }

            // Rejoining - Phase 5 (final), same HR-only model as Probation
            // Confirmation/PIP/Employee Transfer above (no Employee self-
            // service grant): rejoining is a simple, single-step,
            // HR-permission-gated rehire action for a FORMER employee, not
            // self-submitted. HR Manager gets View/Create only (no
            // Edit/Delete were seeded for this feature - rejoining history
            // is an append-only audit log). Super Admin / System
            // Configurator already received every permission from the
            // fullAccessRoles loop above.
            if (hrManagerRole != null)
            {
                var rejoiningPermissionIds = await context.Permissions
                    .Where(p => p.FeatureId == AppFeatureConstants.REJOINING)
                    .Select(p => p.Id)
                    .ToListAsync();

                var alreadyLinked = (await context.RolePermissions
                        .Where(rp => rp.RoleId == hrManagerRole.Id)
                        .Select(rp => rp.PermissionId)
                        .ToListAsync())
                    .ToHashSet();

                var rejoiningNewLinks = rejoiningPermissionIds
                    .Where(pid => !alreadyLinked.Contains(pid))
                    .Select(pid => new RolePermission
                    {
                        Id = IDManager.GetNewId(new RolePermission()),
                        RoleId = hrManagerRole.Id,
                        PermissionId = pid,
                        IsAllowed = true,
                        CreatedBy = "System",
                        CreatedOn = DateTime.UtcNow
                    })
                    .ToList();

                if (rejoiningNewLinks.Count > 0)
                {
                    await context.RolePermissions.AddRangeAsync(rejoiningNewLinks);
                    await context.SaveChangesAsync();
                }
            }

            // Tax Slabs - Admin-only master data, HR Manager gets full
            // View/Create/Edit/Delete (no Employee self-service grant -
            // slab rates are configured by HR/Payroll admins, never by
            // employees). Super Admin / System Configurator already
            // received every permission from the fullAccessRoles loop
            // above.
            if (hrManagerRole != null)
            {
                var taxSlabPermissionIds = await context.Permissions
                    .Where(p => p.FeatureId == AppFeatureConstants.TAX_SLAB)
                    .Select(p => p.Id)
                    .ToListAsync();

                var alreadyLinked = (await context.RolePermissions
                        .Where(rp => rp.RoleId == hrManagerRole.Id)
                        .Select(rp => rp.PermissionId)
                        .ToListAsync())
                    .ToHashSet();

                var taxSlabNewLinks = taxSlabPermissionIds
                    .Where(pid => !alreadyLinked.Contains(pid))
                    .Select(pid => new RolePermission
                    {
                        Id = IDManager.GetNewId(new RolePermission()),
                        RoleId = hrManagerRole.Id,
                        PermissionId = pid,
                        IsAllowed = true,
                        CreatedBy = "System",
                        CreatedOn = DateTime.UtcNow
                    })
                    .ToList();

                if (taxSlabNewLinks.Count > 0)
                {
                    await context.RolePermissions.AddRangeAsync(taxSlabNewLinks);
                    await context.SaveChangesAsync();
                }
            }

            // Tax Computation - HR Manager gets View only, same reasoning
            // as the feature's Def() comment above (menu/routing only, the
            // actual compute-trigger authorization goes through
            // TAX_DECLARATION's Approve permission).
            if (hrManagerRole != null)
            {
                var taxComputationPermissionIds = await context.Permissions
                    .Where(p => p.FeatureId == AppFeatureConstants.TAX_COMPUTATION && p.Action == Actions.View)
                    .Select(p => p.Id)
                    .ToListAsync();

                var alreadyLinked = (await context.RolePermissions
                        .Where(rp => rp.RoleId == hrManagerRole.Id)
                        .Select(rp => rp.PermissionId)
                        .ToListAsync())
                    .ToHashSet();

                var taxComputationNewLinks = taxComputationPermissionIds
                    .Where(pid => !alreadyLinked.Contains(pid))
                    .Select(pid => new RolePermission
                    {
                        Id = IDManager.GetNewId(new RolePermission()),
                        RoleId = hrManagerRole.Id,
                        PermissionId = pid,
                        IsAllowed = true,
                        CreatedBy = "System",
                        CreatedOn = DateTime.UtcNow
                    })
                    .ToList();

                if (taxComputationNewLinks.Count > 0)
                {
                    await context.RolePermissions.AddRangeAsync(taxComputationNewLinks);
                    await context.SaveChangesAsync();
                }
            }

            // Tax Declaration - BROADER audience, same dual-grant shape as
            // Employee Feedback above: HR Manager gets full View/Create/
            // Approve (so HR can both submit on an employee's behalf AND
            // verify/reject); every Employee additionally gets View/Create
            // on their OWN declaration - TaxDeclarationService's
            // actingEmployeeId == dto.EmployeeId check (not a
            // RolePermission grant) is what stops an Employee from
            // declaring on someone else's behalf. Super Admin / System
            // Configurator already received every permission from the
            // fullAccessRoles loop above.
            if (hrManagerRole != null)
            {
                var taxDeclarationPermissionIds = await context.Permissions
                    .Where(p => p.FeatureId == AppFeatureConstants.TAX_DECLARATION)
                    .Select(p => p.Id)
                    .ToListAsync();

                var alreadyLinked = (await context.RolePermissions
                        .Where(rp => rp.RoleId == hrManagerRole.Id)
                        .Select(rp => rp.PermissionId)
                        .ToListAsync())
                    .ToHashSet();

                var taxDeclarationNewLinks = taxDeclarationPermissionIds
                    .Where(pid => !alreadyLinked.Contains(pid))
                    .Select(pid => new RolePermission
                    {
                        Id = IDManager.GetNewId(new RolePermission()),
                        RoleId = hrManagerRole.Id,
                        PermissionId = pid,
                        IsAllowed = true,
                        CreatedBy = "System",
                        CreatedOn = DateTime.UtcNow
                    })
                    .ToList();

                if (taxDeclarationNewLinks.Count > 0)
                {
                    await context.RolePermissions.AddRangeAsync(taxDeclarationNewLinks);
                    await context.SaveChangesAsync();
                }
            }

            if (employeeRole != null)
            {
                var taxDeclarationSelfServicePermissionIds = await context.Permissions
                    .Where(p => p.FeatureId == AppFeatureConstants.TAX_DECLARATION
                             && (p.Action == Actions.View || p.Action == Actions.Create))
                    .Select(p => p.Id)
                    .ToListAsync();

                var alreadyLinked = (await context.RolePermissions
                        .Where(rp => rp.RoleId == employeeRole.Id)
                        .Select(rp => rp.PermissionId)
                        .ToListAsync())
                    .ToHashSet();

                var taxDeclarationEmployeeNewLinks = taxDeclarationSelfServicePermissionIds
                    .Where(pid => !alreadyLinked.Contains(pid))
                    .Select(pid => new RolePermission
                    {
                        Id = IDManager.GetNewId(new RolePermission()),
                        RoleId = employeeRole.Id,
                        PermissionId = pid,
                        IsAllowed = true,
                        CreatedBy = "System",
                        CreatedOn = DateTime.UtcNow
                    })
                    .ToList();

                if (taxDeclarationEmployeeNewLinks.Count > 0)
                {
                    await context.RolePermissions.AddRangeAsync(taxDeclarationEmployeeNewLinks);
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
