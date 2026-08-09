using Application.Interfaces;
using Application.Interfaces.Communication;
using Application.Interfaces.LoanAdvance;
using Application.Services.LoanAdvance;
using Domain.Entities;
using Infrastructure;
using Infrastructure.Interfaces;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using static Domain.Enums.EnumExtensions;

namespace LoanAdvance.Tests.TestFixtures
{
    /// <summary>
    /// Builds a fresh, isolated ApplicationDbContext (EF Core InMemory
    /// provider, uniquely-named database per instance) plus a real
    /// UnitOfWork/Repository&lt;T&gt; over it, and seeds the minimum graph of
    /// supporting rows every Loan/Advance workflow test needs: a Tenant, an
    /// Employee (the "maker"/applicant) reporting to a second Employee (the
    /// "manager"/approver), each with a linked User, plus a "Finance" User
    /// holding Approve permission on EMPLOYEE_LOAN/EMPLOYEE_ADVANCE, an
    /// active LoanType+LoanPolicy(+single ReportingManager approval level)
    /// and AdvanceType, and a processed Payroll row so
    /// LoanCalculationService.CheckEligibilityAsync doesn't reject every
    /// submission for "no processed payroll on file".
    ///
    /// IMPORTANT: EF Core's InMemory provider does NOT enforce referential
    /// integrity (no real FK constraint checks) - only "is this required
    /// scalar property null" is validated. Department/Designation/Company/
    /// Branch/Country/State/City are therefore referenced by plain
    /// placeholder ID strings below rather than fully seeded rows; this
    /// mirrors how a relational provider WITHOUT the corresponding FK
    /// constraint would behave, and keeps this fixture from needing to
    /// stand up the entire org-structure module just to test Loan/Advance
    /// workflows.
    /// </summary>
    public class LoanAdvanceTestFixture : IDisposable
    {
        public ApplicationDbContext Context { get; }
        public IUnitOfWork Uow { get; }

        public string TenantId { get; } = "TEST-TENANT";

        public string ApplicantEmployeeId { get; private set; } = default!;
        public string ApplicantUserId { get; private set; } = default!;

        public string ManagerEmployeeId { get; private set; } = default!;
        public string ManagerUserId { get; private set; } = default!;

        public string FinanceUserId { get; private set; } = default!;

        public string LoanTypeId { get; private set; } = default!;
        public string LoanPolicyId { get; private set; } = default!;

        public string AdvanceTypeId { get; private set; } = default!;

        private LoanAdvanceTestFixture(ApplicationDbContext context)
        {
            Context = context;
            Uow = new UnitOfWork(context);
        }

        public static async Task<LoanAdvanceTestFixture> CreateAsync()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .EnableSensitiveDataLogging()
                .Options;

            var tenantServiceMock = new Mock<ITenantService>();
            tenantServiceMock.Setup(x => x.GetTenantId()).Returns("TEST-TENANT");

            var context = new ApplicationDbContext(options, tenantServiceMock.Object);

            var fixture = new LoanAdvanceTestFixture(context);
            await fixture.SeedAsync();
            return fixture;
        }

        private async Task SeedAsync()
        {
            const string tenantId = "TEST-TENANT";

            Context.Tenants.Add(new Tenant
            {
                Id = tenantId,
                Name = "Test Tenant",
                Code = "TT",
                Phone = "9999999999",
                Address = "Test Address",
                Pincode = "123456",
                CountryId = "PLACEHOLDER-COUNTRY",
                StateId = "PLACEHOLDER-STATE",
                CityId = "PLACEHOLDER-CITY",
                CreatedBy = "SEED"
            });

            // ---- Roles / Permissions (Finance = Approve on EMPLOYEE_LOAN/EMPLOYEE_ADVANCE) ----
            var financeRole = new Role { Id = "ROLE-FINANCE", Name = "Finance", Code = "FINANCE", TenantId = tenantId, CreatedBy = "SEED" };
            Context.Roles.Add(financeRole);

            var loanApprovePermission = new Permission
            {
                Id = "PERM-LOAN-APPROVE",
                Name = "Approve Employee Loan",
                Code = "EMPLOYEE_LOAN_APPROVE",
                FeatureId = Domain.Helper.AppFeatureConstants.EMPLOYEE_LOAN,
                Action = Domain.Helper.Actions.Approve,
                CreatedBy = "SEED"
            };
            var advanceApprovePermission = new Permission
            {
                Id = "PERM-ADVANCE-APPROVE",
                Name = "Approve Employee Advance",
                Code = "EMPLOYEE_ADVANCE_APPROVE",
                FeatureId = Domain.Helper.AppFeatureConstants.EMPLOYEE_ADVANCE,
                Action = Domain.Helper.Actions.Approve,
                CreatedBy = "SEED"
            };
            Context.Permissions.AddRange(loanApprovePermission, advanceApprovePermission);

            Context.RolePermissions.AddRange(
                new RolePermission { Id = "RP-1", RoleId = financeRole.Id, PermissionId = loanApprovePermission.Id, IsAllowed = true, CreatedBy = "SEED" },
                new RolePermission { Id = "RP-2", RoleId = financeRole.Id, PermissionId = advanceApprovePermission.Id, IsAllowed = true, CreatedBy = "SEED" });

            // ---- Employees + Users ----
            ManagerEmployeeId = "EMP-MANAGER";
            Context.Employees.Add(new Employee
            {
                Id = ManagerEmployeeId,
                TenantId = tenantId,
                EmployeeCode = "MGR001",
                FirstName = "Maya",
                LastName = "Manager",
                CompanyId = "PLACEHOLDER-COMPANY",
                DepartmentId = "PLACEHOLDER-DEPT",
                DesignationId = "PLACEHOLDER-DESIG",
                Phone = "9000000001",
                Address = "Addr",
                Pincode = "123456",
                JoiningDate = DateTime.UtcNow.AddYears(-5),
                CreatedBy = "SEED"
            });

            ManagerUserId = "USER-MANAGER";
            Context.Users.Add(new User
            {
                Id = ManagerUserId,
                TenantId = tenantId,
                Username = "manager",
                PasswordHash = "x",
                EmployeeId = ManagerEmployeeId,
                CreatedBy = "SEED"
            });

            ApplicantEmployeeId = "EMP-APPLICANT";
            Context.Employees.Add(new Employee
            {
                Id = ApplicantEmployeeId,
                TenantId = tenantId,
                EmployeeCode = "EMP001",
                FirstName = "Asha",
                LastName = "Applicant",
                CompanyId = "PLACEHOLDER-COMPANY",
                DepartmentId = "PLACEHOLDER-DEPT",
                DesignationId = "PLACEHOLDER-DESIG",
                ReportingManagerId = ManagerEmployeeId,
                Phone = "9000000002",
                Address = "Addr",
                Pincode = "123456",
                JoiningDate = DateTime.UtcNow.AddYears(-2),
                CreatedBy = "SEED"
            });

            ApplicantUserId = "USER-APPLICANT";
            Context.Users.Add(new User
            {
                Id = ApplicantUserId,
                TenantId = tenantId,
                Username = "applicant",
                PasswordHash = "x",
                EmployeeId = ApplicantEmployeeId,
                CreatedBy = "SEED"
            });

            FinanceUserId = "USER-FINANCE";
            Context.Users.Add(new User
            {
                Id = FinanceUserId,
                TenantId = tenantId,
                Username = "finance",
                PasswordHash = "x",
                EmployeeId = null,
                CreatedBy = "SEED"
            });
            Context.UserRoles.Add(new UserRole { Id = "UR-1", UserId = FinanceUserId, RoleId = financeRole.Id, CreatedBy = "SEED" });

            // ---- A processed Payroll row - CheckEligibilityAsync requires one on file. ----
            Context.Payrolls.Add(new Payroll
            {
                Id = "PAYROLL-1",
                CompanyId = "PLACEHOLDER-COMPANY",
                EmployeeId = ApplicantEmployeeId,
                SalaryYear = DateTime.UtcNow.Year,
                SalaryMonth = DateTime.UtcNow.Month,
                GrossSalary = 100000m,
                TotalEarnings = 100000m,
                TotalDeductions = 10000m,
                NetSalary = 90000m,
                Status = "Processed",
                CreatedBy = "SEED"
            });

            // ---- LoanType + LoanPolicy (single ReportingManager approval level) ----
            LoanTypeId = "LOANTYPE-1";
            Context.LoanTypes.Add(new LoanType
            {
                Id = LoanTypeId,
                TenantId = tenantId,
                Code = "PL",
                Name = "Personal Loan",
                InterestMethod = InterestMethod.Reducing,
                DefaultInterestRatePercent = 12m,
                MaxTenureMonths = 60,
                CreatedBy = "SEED"
            });

            LoanPolicyId = "LOANPOLICY-1";
            Context.LoanPolicies.Add(new LoanPolicy
            {
                Id = LoanPolicyId,
                TenantId = tenantId,
                LoanTypeId = LoanTypeId,
                MinAmount = 0,
                MaxAmount = 1000000m,
                MinTenureMonths = 1,
                MaxTenureMonths = 60,
                InterestRatePercent = 12m,
                MinServiceMonthsRequired = 0,
                MaxActiveLoans = 5,
                MaxDeductionPercentOfNetSalary = 100m, // permissive - eligibility math isn't what these workflow tests are checking
                EligibilitySalaryMultiplier = 100m,
                PreClosurePenaltyPercent = 2m,
                VersionNumber = 1,
                EffectiveFrom = DateTime.UtcNow.AddDays(-1),
                IsActive = true,
                CreatedBy = "SEED"
            });

            Context.LoanPolicyApprovalLevels.Add(new LoanPolicyApprovalLevel
            {
                Id = "LEVEL-1",
                LoanPolicyId = LoanPolicyId,
                LevelNumber = 1,
                ApproverType = ApproverType.ReportingManager,
                MinAmountThreshold = 0,
                CreatedBy = "SEED"
            });

            // ---- AdvanceType (single-level approval is resolved purely from Employee.ReportingManagerId, no policy table) ----
            AdvanceTypeId = "ADVANCETYPE-1";
            Context.AdvanceTypes.Add(new AdvanceType
            {
                Id = AdvanceTypeId,
                TenantId = tenantId,
                Code = "SAL",
                Name = "Salary Advance",
                MaxAmount = 50000m,
                MaxInstallments = 6,
                IsInterestFree = true,
                CreatedBy = "SEED"
            });

            await Context.SaveChangesAsync();
        }

        /// <summary>Real EmployeeLoanService wired to this fixture's UnitOfWork - INotificationService/IEmailSender are mocked no-ops (best-effort side channels, not what these tests assert on).</summary>
        public IEmployeeLoanService CreateLoanService()
        {
            var calc = new LoanCalculationService(Uow);
            var policyService = new LoanPolicyService(Uow);

            var notificationMock = new Mock<INotificationService>();
            notificationMock
                .Setup(x => x.CreateDirectAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
                .ReturnsAsync((string?)null);

            var emailMock = new Mock<IEmailSender>();
            emailMock
                .Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(false);

            return new EmployeeLoanService(
                Uow, calc, policyService,
                notificationMock.Object, emailMock.Object,
                NullLogger<EmployeeLoanService>.Instance);
        }

        /// <summary>Real EmployeeAdvanceService wired to this fixture's UnitOfWork - same mocked-no-op notification channels as CreateLoanService.</summary>
        public IEmployeeAdvanceService CreateAdvanceService()
        {
            var notificationMock = new Mock<INotificationService>();
            notificationMock
                .Setup(x => x.CreateDirectAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
                .ReturnsAsync((string?)null);

            var emailMock = new Mock<IEmailSender>();
            emailMock
                .Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(false);

            return new EmployeeAdvanceService(
                Uow,
                notificationMock.Object, emailMock.Object,
                NullLogger<EmployeeAdvanceService>.Instance);
        }

        public void Dispose()
        {
            Context.Dispose();
        }
    }
}
