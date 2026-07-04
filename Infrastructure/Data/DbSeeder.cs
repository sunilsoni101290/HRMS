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
    public static class DbSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            if (context == null) return;

            // Apply Pending Migrations
            await context.Database.MigrateAsync();

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

                await context.Designations.AddRangeAsync(
                    ceo, hrManager, hrExecutive, itManager, developer
                );

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
            if (!context.Employees.Any())
            {
                var hrDept = await context.Departments.FirstOrDefaultAsync(x => x.Code == "HR");
                var itDept = await context.Departments.FirstOrDefaultAsync(x => x.Code == "IT");

                var ceoDesg = await context.Designations.FirstOrDefaultAsync(x => x.Code == "CEO");
                var hrMgrDesg = await context.Designations.FirstOrDefaultAsync(x => x.Code == "HR-MGR");
                var devDesg = await context.Designations.FirstOrDefaultAsync(x => x.Code == "DEV");

                var defaultShift = await context.Shifts.FirstOrDefaultAsync();
                var india = await context.Countries.FirstOrDefaultAsync(x => x.Name == "India");

                if (tenantId == null ||
                    companyId == null ||
                    hrDept == null ||
                    itDept == null ||
                    ceoDesg == null ||
                    hrMgrDesg == null ||
                    devDesg == null ||
                    defaultShift == null)
                {
                    throw new Exception("Required master data not found.");
                }

                // ================= CEO =================

                var ceo = new Employee
                {
                    Id = IDManager.GetNewId(new Employee()),
                    EmployeeCode = "EMP001",

                    FirstName = "Sunil",
                    LastName = "Soni",

                    TenantId = tenantId,
                    CompanyId = companyId,
                    BranchId = null,

                    DepartmentId = hrDept.Id,
                    DesignationId = ceoDesg.Id,

                    ReportingManagerId = null,

                    ShiftId = defaultShift.Id,

                    DateOfBirth = new DateTime(1980, 5, 10),

                    Gender = Gender.Male,
                    MaritalStatus = MaritalStatus.Married,

                    Phone = "9000000001",
                    Email = "sunil.soni190@gmail.com",

                    Address = "Mumbai",
                    Pincode = "400001",

                    PANNumber = "ABCDE1234F",
                    AadharNumber = "123412341234",

                    JoiningDate = DateTime.UtcNow.AddYears(-5),
                    ConfirmationDate = DateTime.UtcNow.AddYears(-5).AddMonths(6),

                    EmploymentType = EmploymentType.Permanent,

                    PassportNumber = "N1234567",
                    IssueDate = new DateTime(2022, 1, 1),
                    ExpiryDate = new DateTime(2032, 1, 1),
                    PlaceOfIssue = "Mumbai",

                    Nationality = Nationality.Indian,
                    PassportStatus = PassportStatus.Active,

                    CountryId = india?.Id,

                    CreatedBy = "System"
                };

                // ================= HR Manager =================

                var hrManager = new Employee
                {
                    Id = IDManager.GetNewId(new Employee()),
                    EmployeeCode = "EMP002",

                    FirstName = "Neha",
                    LastName = "Verma",

                    TenantId = tenantId,
                    CompanyId = companyId,

                    DepartmentId = hrDept.Id,
                    DesignationId = hrMgrDesg.Id,

                    ReportingManagerId = ceo.Id,

                    ShiftId = defaultShift.Id,

                    DateOfBirth = new DateTime(1990, 8, 15),

                    Gender = Gender.Female,
                    MaritalStatus = MaritalStatus.Unmarried,

                    Phone = "9000000002",
                    Email = "hr@company.com",

                    Address = "Mumbai",
                    Pincode = "400001",

                    PANNumber = "PQRSX2345L",
                    AadharNumber = "234523452345",

                    JoiningDate = DateTime.UtcNow.AddYears(-3),
                    ConfirmationDate = DateTime.UtcNow.AddYears(-3).AddMonths(6),

                    EmploymentType = EmploymentType.Permanent,

                    PassportNumber = "N2345678",
                    IssueDate = new DateTime(2021, 3, 15),
                    ExpiryDate = new DateTime(2031, 3, 15),
                    PlaceOfIssue = "Mumbai",

                    Nationality = Nationality.Indian,
                    PassportStatus = PassportStatus.Active,

                    CountryId = india?.Id,

                    CreatedBy = "System"
                };

                // ================= Developer =================

                var developer = new Employee
                {
                    Id = IDManager.GetNewId(new Employee()),
                    EmployeeCode = "EMP003",

                    FirstName = "Rahul",
                    LastName = "Patel",

                    TenantId = tenantId,
                    CompanyId = companyId,

                    DepartmentId = itDept.Id,
                    DesignationId = devDesg.Id,

                    ReportingManagerId = hrManager.Id,

                    ShiftId = defaultShift.Id,

                    DateOfBirth = new DateTime(1998, 2, 20),

                    Gender = Gender.Male,
                    MaritalStatus = MaritalStatus.Unmarried,

                    Phone = "9000000003",
                    Email = "developer@company.com",

                    Address = "Pune",
                    Pincode = "411001",

                    PANNumber = "LMNOP6789Q",
                    AadharNumber = "345634563456",

                    JoiningDate = DateTime.UtcNow.AddYears(-1),
                    ConfirmationDate = DateTime.UtcNow.AddMonths(-6),

                    EmploymentType = EmploymentType.Permanent,

                    PassportNumber = "N3456789",
                    IssueDate = new DateTime(2023, 5, 1),
                    ExpiryDate = new DateTime(2033, 5, 1),
                    PlaceOfIssue = "Pune",

                    Nationality = Nationality.Indian,
                    PassportStatus = PassportStatus.Active,

                    CountryId = india?.Id,

                    CreatedBy = "System"
                };

                await context.Employees.AddRangeAsync(ceo, hrManager, developer);
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
                        Name = "Super Admin",
                        Code = ConstantHelper.SUPER_ADMIN,
                        TenantId = tenantId,
                        Description = "Full system access",
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

                var superAdmin = roles.First(x => x.Code == "SUPER_ADMIN");
                var hrManager = roles.First(x => x.Code == "HR_MANAGER");
                var employee = roles.First(x => x.Code == "EMPLOYEE");

                var rolePermissions = new List<RolePermission>();

                // 👑 Super Admin → All Permissions
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
                }

                // 👨‍💼 HR Manager → Employee + Leave
                var hrPermissions = permissions
                    .Where(p => p.FeatureId == AppFeatureConstants.EMPLOYEE || p.FeatureId == AppFeatureConstants.LEAVE_APPLICATION)
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

            if (!context.Users.Any())
            {
                var hrManagerEmp = await context.Employees
                .FirstOrDefaultAsync(x => x.EmployeeCode == "EMP002");

                if (hrManagerEmp == null)
                    throw new Exception("HR Manager not found");

                // 🔐 Password Hash (use your hashing service ideally)
                var password = "Admin@123";
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

                var user = new User
                {

                    Id = IDManager.GetNewId(new User()),
                    Username = "hrmanager48",
                    Email = "hr@system.com",
                    PhoneNumber = "9999999999",

                    PasswordHash = passwordHash,
                    
                    EmployeeId = hrManagerEmp.Id,

                    EmailConfirmed = true,
                    PhoneConfirmed = true,

                    TenantId = tenantId,
                    CompanyId = companyId,
                    BranchId = null,

                    IsLocked = false,
                    AccessFailedCount = 0,

                    LastLoginIP = "Unknown" ,


                    CreatedBy = "System"
                };

                await context.Users.AddAsync(user);
                await context.SaveChangesAsync();
            }

            if (!context.UserRoles.Any())
            {
                var user = await context.Users.FirstOrDefaultAsync(x => x.Username == "hrmanager48");
                var role = await context.Roles.FirstOrDefaultAsync(x => x.Code == "HR_MANAGER");

                if (user == null || role == null)
                    throw new Exception("User or Role not found");

                var userRole = new UserRole
                {
                    Id = IDManager.GetNewId(new UserRole()),
                    UserId = user.Id,
                    RoleId = role.Id,
                    CreatedBy="System"
                };

                await context.UserRoles.AddAsync(userRole);
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

            Add("Holiday", AppFeatureConstants.HOLIDAY, AppFeatureConstants.HRMS,
                AppFeatureConstants.HOLIDAY_CONTROLLER,
                AppFeatureConstants.HOLIDAY_ACTION,
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
        //    - grants every permission to the Super Admin role
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

            // Super Admin → every permission (fills the gaps for new features)
            var superAdmin = await context.Roles
                .FirstOrDefaultAsync(r => r.Code == ConstantHelper.SUPER_ADMIN);

            if (superAdmin != null)
            {
                var allPermissionIds = await context.Permissions
                    .Select(p => p.Id)
                    .ToListAsync();

                var linked = (await context.RolePermissions
                        .Where(rp => rp.RoleId == superAdmin.Id)
                        .Select(rp => rp.PermissionId)
                        .ToListAsync())
                    .ToHashSet();

                var newLinks = allPermissionIds
                    .Where(pid => !linked.Contains(pid))
                    .Select(pid => new RolePermission
                    {
                        Id = IDManager.GetNewId(new RolePermission()),
                        RoleId = superAdmin.Id,
                        PermissionId = pid,
                        IsAllowed = true,
                        CreatedBy = "System",
                        CreatedOn = DateTime.UtcNow
                    })
                    .ToList();

                if (newLinks.Count > 0)
                {
                    await context.RolePermissions.AddRangeAsync(newLinks);
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
