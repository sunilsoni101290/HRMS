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

            await AppFeatureSeeder.SeedAsync(context);
            await AppFeatureSeeder.SeedTenantFeatureAsync(context);

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

            await AppFeatureSeeder.SeedRoleFeatureAsync(context);

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
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            if (context.AppFeatures.Any())
                return;

            var tenantId = await context.Tenants
                .Select(x => x.Id)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(tenantId))
                throw new Exception("Tenant not found");

            var features = new List<AppFeature>();

            // 🔧 Helper
            AppFeature Add(string name, string code, string module, string parentId = null, int order = 0)
            {
                var f = new AppFeature
                {
                    Id = IDManager.GetNewId(new AppFeature()),
                    Name = name,
                    Code = code,
                    Module = module,
                    TenantId = tenantId,
                    ParentFeatureId = parentId,
                    DisplayOrder = order,

                    CreatedBy = "System"
                };

                features.Add(f);
                return f;
            }

            // =========================
            // ROOT
            // =========================
            var hrms = Add("HRMS", AppFeatureConstants.HRMS, AppFeatureConstants.HRMS, null, 1);

            // =========================
            // DASHBOARD
            // =========================
            Add("Dashboard", AppFeatureConstants.DASHBOARD, AppFeatureConstants.HRMS, hrms.Id, 1);

            // =========================
            // EMPLOYEE MANAGEMENT
            // =========================
            var empMgmt = Add("Employee Management", AppFeatureConstants.EMPLOYEE_MANAGEMENT, AppFeatureConstants.HRMS, hrms.Id, 2);

            Add("Employee", AppFeatureConstants.EMPLOYEE, AppFeatureConstants.HRMS, empMgmt.Id);
            Add("Employee Document", AppFeatureConstants.EMPLOYEE_DOCUMENT, AppFeatureConstants.HRMS, empMgmt.Id);
            Add("Employee Shift", AppFeatureConstants.EMPLOYEE_SHIFT, AppFeatureConstants.HRMS, empMgmt.Id);
            Add("Employee Bank", AppFeatureConstants.EMPLOYEE_BANK, AppFeatureConstants.HRMS, empMgmt.Id);
            Add("PF / ESIC", AppFeatureConstants.EMPLOYEE_PF_ESIC, AppFeatureConstants.HRMS, empMgmt.Id);

            // =========================
            // ORGANIZATION
            // =========================
            var org = Add("Organization", AppFeatureConstants.ORGANIZATION, AppFeatureConstants.HRMS, hrms.Id, 3);

            Add("Company", AppFeatureConstants.COMPANY, AppFeatureConstants.HRMS, org.Id);
            Add("Department", AppFeatureConstants.DEPARTMENT, AppFeatureConstants.HRMS, org.Id);
            Add("Designation", AppFeatureConstants.DESIGNATION, AppFeatureConstants.HRMS, org.Id);
            Add("Branch", AppFeatureConstants.BRANCH, AppFeatureConstants.HRMS, org.Id);
            Add("Location", AppFeatureConstants.LOCATION, AppFeatureConstants.HRMS, org.Id);

            // =========================
            // ATTENDANCE
            // =========================
            var att = Add("Attendance Management", AppFeatureConstants.ATTENDANCE_MANAGEMENT, AppFeatureConstants.HRMS, hrms.Id, 4);

            Add("Attendance", AppFeatureConstants.ATTENDANCE, AppFeatureConstants.HRMS, att.Id);
            Add("Attendance Log", AppFeatureConstants.ATTENDANCE_LOG, AppFeatureConstants.HRMS, att.Id);
            Add("Shift", AppFeatureConstants.SHIFT, AppFeatureConstants.HRMS, att.Id);

            // =========================
            // LEAVE
            // =========================
            var leave = Add("Leave Management", AppFeatureConstants.LEAVE_MANAGEMENT, AppFeatureConstants.HRMS, hrms.Id, 5);

            Add("Leave Type", AppFeatureConstants.LEAVE_TYPE, AppFeatureConstants.HRMS, leave.Id);
            Add("Leave Application", AppFeatureConstants.LEAVE_APPLICATION, AppFeatureConstants.HRMS, leave.Id);
            Add("Leave Balance", AppFeatureConstants.LEAVE_BALANCE, AppFeatureConstants.HRMS, leave.Id);
            Add("Leave Approval", AppFeatureConstants.LEAVE_APPROVAL, AppFeatureConstants.HRMS, leave.Id);

            // =========================
            // PAYROLL
            // =========================
            var payroll = Add("Payroll", AppFeatureConstants.PAYROLL, AppFeatureConstants.HRMS, hrms.Id, 6);

            Add("Salary Structure", AppFeatureConstants.SALARY_STRUCTURE, AppFeatureConstants.HRMS, payroll.Id);
            Add("Payroll Process", AppFeatureConstants.PAYROLL_PROCESS, AppFeatureConstants.HRMS, payroll.Id);
            Add("Payslip", AppFeatureConstants.PAYSLIP, AppFeatureConstants.HRMS, payroll.Id);

            // =========================
            // RECRUITMENT
            // =========================
            var rec = Add("Recruitment", AppFeatureConstants.RECRUITMENT, AppFeatureConstants.HRMS, hrms.Id, 7);

            Add("Job Opening", AppFeatureConstants.JOB_OPENING, AppFeatureConstants.HRMS, rec.Id);
            Add("Candidate", AppFeatureConstants.CANDIDATE, AppFeatureConstants.HRMS, rec.Id);
            Add("Interview", AppFeatureConstants.INTERVIEW, AppFeatureConstants.HRMS, rec.Id);

            // =========================
            // ASSET MANAGEMENT
            // =========================
            var asset = Add("Asset Management", AppFeatureConstants.ASSET_MANAGEMENT, AppFeatureConstants.HRMS, hrms.Id, 8);

            Add("Asset", AppFeatureConstants.ASSET, AppFeatureConstants.HRMS, asset.Id);
            Add("Asset Allocation", AppFeatureConstants.ASSET_ALLOCATION, AppFeatureConstants.HRMS, asset.Id);
            Add("Asset History", AppFeatureConstants.ASSET_HISTORY, AppFeatureConstants.HRMS, asset.Id);

            // =========================
            // COMMUNICATION
            // =========================
            var comm = Add("Communication", AppFeatureConstants.COMMUNICATION, AppFeatureConstants.HRMS, hrms.Id, 9);

            Add("Announcement", AppFeatureConstants.ANNOUNCEMENT, AppFeatureConstants.HRMS, comm.Id);
            Add("Event", AppFeatureConstants.EVENT, AppFeatureConstants.HRMS, comm.Id);
            Add("Event Participant", AppFeatureConstants.EVENT_PARTICIPANT, AppFeatureConstants.HRMS, comm.Id);

            // =========================
            // NOTIFICATION
            // =========================
            var notif = Add("Notification", AppFeatureConstants.NOTIFICATION, AppFeatureConstants.HRMS, hrms.Id, 10);

            Add("Notification Group", AppFeatureConstants.NOTIFICATION_GROUP, AppFeatureConstants.HRMS, notif.Id);
            Add("Notification Log", AppFeatureConstants.NOTIFICATION_LOG, AppFeatureConstants.HRMS, notif.Id);

            // =========================
            // MASTER DATA
            // =========================
            var master = Add("Master Data", AppFeatureConstants.MASTER, AppFeatureConstants.HRMS, hrms.Id, 11);

            Add("Country", AppFeatureConstants.COUNTRY, AppFeatureConstants.HRMS, master.Id);
            Add("State", AppFeatureConstants.STATE, AppFeatureConstants.HRMS, master.Id);
            Add("City", AppFeatureConstants.CITY, AppFeatureConstants.HRMS, master.Id);
            Add("Holiday", AppFeatureConstants.HOLIDAY, AppFeatureConstants.HRMS, master.Id);
            Add("Financial Year", AppFeatureConstants.FINANCIAL_YEAR, AppFeatureConstants.HRMS, master.Id);

            // =========================
            // SECURITY
            // =========================
            var sec = Add("Security", AppFeatureConstants.SECURITY, AppFeatureConstants.HRMS, hrms.Id, 12);

            Add("User", AppFeatureConstants.USER, AppFeatureConstants.HRMS, sec.Id);
            Add("Role", AppFeatureConstants.ROLE, AppFeatureConstants.HRMS, sec.Id);
            Add("Permission", AppFeatureConstants.PERMISSION, AppFeatureConstants.HRMS, sec.Id);
            Add("Login History", AppFeatureConstants.LOGIN_HISTORY, AppFeatureConstants.HRMS, sec.Id);

            // SAVE
            await context.AppFeatures.AddRangeAsync(features);
            await context.SaveChangesAsync();
        }
        public static async Task SeedTenantFeatureAsync(ApplicationDbContext context)
        {
            if (context.TenantFeatures.Any()) return;

            var tenantId = await context.Tenants
                .Select(x => x.Id)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(tenantId))
                throw new Exception("Tenant not found");

            var features = await context.AppFeatures.ToListAsync();

            var tenantFeatures = features.Select(f => new TenantFeature
            {
                Id = IDManager.GetNewId(new TenantFeature()),
                TenantId = tenantId,
                AppFeatureId = f.Id,
                IsEnabled = true, // 🔥 सब enable (or plan based)

                CreatedBy = "System"
            }).ToList();

            await context.TenantFeatures.AddRangeAsync(tenantFeatures);
            await context.SaveChangesAsync();
        }

        public static async Task SeedRoleFeatureAsync(ApplicationDbContext context)
        {
            var tenantId = await context.Tenants
                .Select(x => x.Id)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(tenantId))
                throw new Exception("Tenant not found");

            if (context.RoleFeatures.Any()) return;

            var roles = await context.Roles.ToListAsync();
            var features = await context.AppFeatures.ToListAsync();

            var roleFeatures = new List<RoleFeature>();

            var superAdmin = roles.FirstOrDefault(x => x.Code == "SUPER_ADMIN");
            var hrManager = roles.FirstOrDefault(x => x.Code == "HR_MANAGER");
            var employee = roles.FirstOrDefault(x => x.Code == "EMPLOYEE");

            if (superAdmin == null) throw new Exception("Roles not found");

            // 👑 SUPER ADMIN → All Features
            foreach (var f in features)
            {
                roleFeatures.Add(new RoleFeature
                {
                    Id = IDManager.GetNewId(new RoleFeature()),
                    TenantId = tenantId,
                    RoleId = superAdmin.Id,
                    AppFeatureId = f.Id,
                    IsEnabled = true,
                    CreatedBy = "System",
                });
            }

            // 👨‍💼 HR Manager → Limited Features
            var hrFeatures = features.Where(f =>
                f.Code.Contains("EMP") ||
                f.Code.Contains("LEAVE") ||
                f.Code.Contains("ATT")
            ).ToList();

            foreach (var f in hrFeatures)
            {
                roleFeatures.Add(new RoleFeature
                {
                    Id = IDManager.GetNewId(new RoleFeature()),
                    TenantId = tenantId,
                    RoleId = hrManager.Id,
                    AppFeatureId = f.Id,
                    IsEnabled = true,
                    CreatedBy = "System",
                });
            }

            // 👤 Employee → Only basic features
            var empFeatures = features.Where(f =>
                f.Code.Contains("DASHBOARD") ||
                f.Code.Contains("LEAVE")
            ).ToList();

            foreach (var f in empFeatures)
            {
                roleFeatures.Add(new RoleFeature
                {
                    Id = IDManager.GetNewId(new RoleFeature()),
                    TenantId = tenantId,
                    RoleId = employee.Id,
                    AppFeatureId = f.Id,
                    IsEnabled = true,
                    CreatedBy = "System",
                });
            }

            await context.RoleFeatures.AddRangeAsync(roleFeatures);
            await context.SaveChangesAsync();
        }
    }
}
