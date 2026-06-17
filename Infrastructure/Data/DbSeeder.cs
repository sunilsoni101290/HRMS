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

                    PlanName = "Enterprise",
                    MaxUsers = 100,
                    MaxBranches = 10,

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

                if (tenantId == null || companyId == null ||
                    hrDept == null || itDept == null || ceoDesg == null)
                    {
                        throw new Exception("Required master data not found.");
                    }

                // 👑 CEO (Top Level - No Manager)
                var ceo = new Employee
                {
                    Id = IDManager.GetNewId(new Employee()),
                    EmployeeCode = "EMP001",
                    FirstName = "Amit",
                    LastName = "Sharma",

                    TenantId = tenantId,
                    CompanyId = companyId,
                    BranchId = null,
                    DepartmentId = hrDept.Id,
                    DesignationId = ceoDesg.Id,

                    JoiningDate = DateTime.UtcNow.AddYears(-5),
                    EmploymentType = EmploymentType.Permanent,

                    Gender = Gender.Male,
                    MaritalStatus = MaritalStatus.Married,

                    Phone = "9000000001",
                    Email = "ceo@company.com",
                    Address = "Mumbai",
                    Pincode = "400001",

                    CreatedBy = "System"
                };

                // 👨‍💼 HR Manager (Reports to CEO)
                var hrManager = new Employee
                {
                    Id = IDManager.GetNewId(new Employee()),
                    EmployeeCode = "EMP002",
                    FirstName = "Neha",
                    LastName = "Verma",

                    TenantId = tenantId,
                    CompanyId = companyId,
                    BranchId = null,
                    DepartmentId = hrDept.Id,
                    DesignationId = hrMgrDesg.Id,

                    ReportingManagerId = ceo.Id,

                    JoiningDate = DateTime.UtcNow.AddYears(-3),
                    EmploymentType = EmploymentType.Permanent,

                    Gender = Gender.Female,
                    MaritalStatus = MaritalStatus.Unmarried,

                    Phone = "9000000002",
                    Email = "hr@company.com",
                    Address = "Mumbai",
                    Pincode = "400001",

                    CreatedBy = "System"
                };

                // 👨‍💻 Developer (Reports to HR Manager / IT Manager ideally)
                var developer = new Employee
                {
                    Id = IDManager.GetNewId(new Employee()),
                    EmployeeCode = "EMP003",
                    FirstName = "Rahul",
                    LastName = "Patel",

                    TenantId = tenantId,
                    CompanyId = companyId,
                    BranchId = null,
                    DepartmentId = itDept.Id,
                    DesignationId = devDesg.Id,

                    ReportingManagerId = hrManager.Id,

                    JoiningDate = DateTime.UtcNow.AddYears(-1),
                    EmploymentType = EmploymentType.Permanent,

                    Gender = Gender.Male,
                    MaritalStatus = MaritalStatus.Unmarried,

                    Phone = "9000000003",
                    Email = "dev@company.com",
                    Address = "Pune",
                    Pincode = "411001",

                    CreatedBy = "System"
                };

                await context.Employees.AddRangeAsync(ceo, hrManager, developer);
                await context.SaveChangesAsync();
            }

            // =========================
            // 10. SHIFT
            // =========================
            // Check if shifts already exist
                if (await context.Shifts.AnyAsync())
                    return;

                var shifts = new List<Shift>();
                shifts.AddRange(new List<Shift>
                {
                    new Shift
                    {
                        Id = IDManager.GetNewId(new Shift()),
                        Name = "General Shift",
                        StartTime = new TimeSpan(9, 0, 0),
                        EndTime = new TimeSpan(18, 0, 0),

                        GraceInMinutes = 15,
                        GraceOutMinutes = 15,

                        HalfDayMinutes = 240,      // 4 Hours
                        FullDayMinutes = 480,      // 8 Hours

                        MinimumWorkingMinutes = 450,
                        MaximumWorkingMinutes = 540,

                        IsNightShift = false,

                        IsDefaultShift=true,

                        TenantId = tenantId,

                        IsActive = true,
                        CreatedBy="System",
                        CreatedOn = DateTime.UtcNow
                    }
                });

                await context.Shifts.AddRangeAsync(shifts);
                await context.SaveChangesAsync();
        
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
    }
}
