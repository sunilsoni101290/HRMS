using Application.Services.Attendances;
using Application.Services.PayrollService;
using Domain.Entities;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using static Domain.Enums.EnumExtensions;

namespace Payroll.Tests.Integration
{
    /// <summary>
    /// SalaryCalculationService exercised end-to-end against a REAL
    /// ApplicationDbContext (EF Core InMemory, fresh per test) with a REAL
    /// AttendancePolicyService (its constructor takes only the DbContext -
    /// nothing to mock). Only ITenantService (a DbContext-plumbing
    /// dependency this suite has no reason to touch) is mocked, exactly
    /// like Tests/EsslIntegration.Tests's fixture.
    ///
    /// Covers the 4 verification cases given in the original requirement:
    ///   1. Rs.31,000 / 31 (Jan, Calendar Days) * 17 present      = Rs.17,000.00
    ///   2. Rs.30,000 / 30 (Apr, Calendar Days) * 17 present      = Rs.17,000.00
    ///   3. Rs.31,000 / 31 (Jan, Calendar Days) * 20 payable
    ///      (17 present + 3 paid leave)                          = Rs.20,000.00
    ///   4. Rs.31,000 / 26 (Working Days policy) * 17 present     = Rs.20,269.23
    /// plus two regression scenarios for the bugs identified in the
    /// SalaryCalculationService class remarks:
    ///   - duplicate Attendance rows for the same date must not inflate
    ///     Present Days (most-recently-updated row per date wins)
    ///   - Unpaid Leave must reduce Payable Days (NOT be silently excluded
    ///     the way it was under the old ratio-based formula)
    ///
    /// NOTE: no `dotnet` CLI was available in the sandbox this was authored
    /// in - see Payroll.Tests.csproj's header comment. Run `dotnet test`
    /// from this folder as the first verification step after pulling these
    /// changes.
    /// </summary>
    public class SalaryCalculationServiceTests
    {
        private const string TenantId = "TEST-TENANT";
        private const string CompanyId = "TEST-COMPANY";

        private static async Task<(ApplicationDbContext Db, SalaryCalculationService Service)> BuildAsync()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .EnableSensitiveDataLogging()
                .Options;

            var tenantServiceMock = new Mock<ITenantService>();
            tenantServiceMock.Setup(x => x.GetTenantId()).Returns(TenantId);

            var db = new ApplicationDbContext(options, tenantServiceMock.Object);

            db.Tenants.Add(new Tenant
            {
                Id = TenantId,
                Name = "Test Tenant",
                Code = "TT"
            });

            await db.SaveChangesAsync();

            var attendancePolicyService = new AttendancePolicyService(db);
            var service = new SalaryCalculationService(db, attendancePolicyService);
            return (db, service);
        }

        /// <summary>
        /// Seeds an AttendancePolicy effective well in the past (so it is
        /// always "active" regardless of when the test actually runs - see
        /// AttendancePolicyService.GetActiveForTenantAsync's
        /// EffectiveFrom.Date &lt;= today filter) with the given proration
        /// basis.
        /// </summary>
        private static async Task SeedPolicyAsync(ApplicationDbContext db, SalaryProrationBasis basis, decimal fixedWorkingDays = 26m)
        {
            db.AttendancePolicies.Add(new AttendancePolicy
            {
                Id = $"APOL-{Guid.NewGuid()}",
                TenantId = TenantId,
                CompanyId = CompanyId,
                PolicyName = "Test Policy",
                EffectiveFrom = new DateTime(2020, 1, 1),
                IsActive = true,
                SalaryProrationBasis = basis,
                FixedWorkingDaysPerMonth = fixedWorkingDays,
                CreatedBy = "SEED"
            });
            await db.SaveChangesAsync();
        }

        private static async Task<Employee> SeedEmployeeAsync(ApplicationDbContext db, DateTime joiningDate)
        {
            var employee = new Employee
            {
                Id = $"EMP-{Guid.NewGuid()}",
                TenantId = TenantId,
                CompanyId = CompanyId,
                EmployeeCode = "E001",
                FirstName = "Test",
                LastName = "Employee",
                JoiningDate = joiningDate,
                CreatedBy = "SEED"
            };
            db.Employees.Add(employee);
            await db.SaveChangesAsync();
            return employee;
        }

        private static async Task SeedSalaryStructureAsync(ApplicationDbContext db, string employeeId, decimal monthlySalary, DateTime effectiveFrom)
        {
            var component = new SalaryComponent
            {
                Id = $"SC-{Guid.NewGuid()}",
                TenantId = TenantId,
                Name = "Basic",
                Code = "BASIC",
                ComponentType = SalaryComponentType.Earning,
                CreatedBy = "SEED"
            };
            db.SalaryComponents.Add(component);

            var structure = new SalaryStructure
            {
                Id = $"SS-{Guid.NewGuid()}",
                TenantId = TenantId,
                EmployeeId = employeeId,
                EffectiveFrom = effectiveFrom,
                CreatedBy = "SEED"
            };
            db.SalaryStructures.Add(structure);

            db.SalaryDetails.Add(new SalaryDetail
            {
                Id = $"SD-{Guid.NewGuid()}",
                TenantId = TenantId,
                SalaryStructureId = structure.Id,
                SalaryComponentId = component.Id,
                Amount = monthlySalary,
                CreatedBy = "SEED"
            });

            await db.SaveChangesAsync();
        }

        private static async Task SeedPresentDaysAsync(ApplicationDbContext db, string employeeId, int year, int month, int presentDaysCount)
        {
            for (var day = 1; day <= presentDaysCount; day++)
            {
                db.Attendances.Add(new Attendance
                {
                    Id = $"ATT-{Guid.NewGuid()}",
                    TenantId = TenantId,
                    CompanyId = CompanyId,
                    EmployeeId = employeeId,
                    Date = new DateTime(year, month, day),
                    Status = AttendanceStatus.Present,
                    CreatedBy = "SEED"
                });
            }
            await db.SaveChangesAsync();
        }

        private static async Task<string> SeedLeaveTypeAsync(ApplicationDbContext db, bool isPaid)
        {
            var leaveType = new LeaveType
            {
                Id = $"LT-{Guid.NewGuid()}",
                TenantId = TenantId,
                Name = isPaid ? "Paid Leave" : "Unpaid Leave",
                MaxDaysPerYear = 30,
                IsPaid = isPaid,
                CreatedBy = "SEED"
            };
            db.LeaveTypes.Add(leaveType);
            await db.SaveChangesAsync();
            return leaveType.Id;
        }

        private static async Task SeedApprovedLeaveAsync(ApplicationDbContext db, string employeeId, string leaveTypeId, DateTime fromDate, DateTime toDate)
        {
            var totalDays = (decimal)(toDate.Date - fromDate.Date).Days + 1;
            db.LeaveApplications.Add(new LeaveApplication
            {
                Id = $"LA-{Guid.NewGuid()}",
                TenantId = TenantId,
                CompanyId = CompanyId,
                EmployeeId = employeeId,
                LeaveTypeId = leaveTypeId,
                FromDate = fromDate,
                ToDate = toDate,
                TotalDays = totalDays,
                Status = ApprovalStatus.Approved,
                CreatedBy = "SEED"
            });
            await db.SaveChangesAsync();
        }

        // ---------------------------------------------------------------
        // Case 1: Rs.31,000 / 31 (Jan, Calendar Days) * 17 present = Rs.17,000
        // ---------------------------------------------------------------
        [Fact]
        public async Task Case1_CalendarDays_JanuaryWith17Present_Gives17000()
        {
            var (db, service) = await BuildAsync();
            await SeedPolicyAsync(db, SalaryProrationBasis.CalendarDays);
            var employee = await SeedEmployeeAsync(db, new DateTime(2019, 1, 1));
            await SeedSalaryStructureAsync(db, employee.Id, 31000m, new DateTime(2019, 1, 1));
            await SeedPresentDaysAsync(db, employee.Id, 2026, 1, 17);

            var result = await service.CalculateAsync(employee.Id, 2026, 1, TenantId);

            result.CanProcess.Should().BeTrue();
            result.TotalDaysInPeriod.Should().Be(31);
            result.PresentDays.Should().Be(17);
            result.PayableDays.Should().Be(17);
            result.NetSalary.Should().Be(17000.00m);
        }

        // ---------------------------------------------------------------
        // Case 2: Rs.30,000 / 30 (Apr, Calendar Days) * 17 present = Rs.17,000
        // ---------------------------------------------------------------
        [Fact]
        public async Task Case2_CalendarDays_AprilWith17Present_Gives17000()
        {
            var (db, service) = await BuildAsync();
            await SeedPolicyAsync(db, SalaryProrationBasis.CalendarDays);
            var employee = await SeedEmployeeAsync(db, new DateTime(2019, 1, 1));
            await SeedSalaryStructureAsync(db, employee.Id, 30000m, new DateTime(2019, 1, 1));
            await SeedPresentDaysAsync(db, employee.Id, 2026, 4, 17);

            var result = await service.CalculateAsync(employee.Id, 2026, 4, TenantId);

            result.CanProcess.Should().BeTrue();
            result.TotalDaysInPeriod.Should().Be(30);
            result.PresentDays.Should().Be(17);
            result.PayableDays.Should().Be(17);
            result.NetSalary.Should().Be(17000.00m);
        }

        // ---------------------------------------------------------------
        // Case 3: Rs.31,000 / 31 (Jan, Calendar Days) * (17 present + 3 paid
        // leave = 20 payable) = Rs.20,000
        // ---------------------------------------------------------------
        [Fact]
        public async Task Case3_CalendarDays_17PresentPlus3PaidLeave_Gives20000()
        {
            var (db, service) = await BuildAsync();
            await SeedPolicyAsync(db, SalaryProrationBasis.CalendarDays);
            var employee = await SeedEmployeeAsync(db, new DateTime(2019, 1, 1));
            await SeedSalaryStructureAsync(db, employee.Id, 31000m, new DateTime(2019, 1, 1));
            await SeedPresentDaysAsync(db, employee.Id, 2026, 1, 17);

            var paidLeaveTypeId = await SeedLeaveTypeAsync(db, isPaid: true);
            // 3 paid-leave days immediately after the 17 present days (18,19,20 Jan) -
            // no overlap with the seeded Attendance rows (1-17 Jan).
            await SeedApprovedLeaveAsync(db, employee.Id, paidLeaveTypeId, new DateTime(2026, 1, 18), new DateTime(2026, 1, 20));

            var result = await service.CalculateAsync(employee.Id, 2026, 1, TenantId);

            result.CanProcess.Should().BeTrue();
            result.PresentDays.Should().Be(17);
            result.PaidLeaveDays.Should().Be(3);
            result.PayableDays.Should().Be(20);
            result.NetSalary.Should().Be(20000.00m);
        }

        // ---------------------------------------------------------------
        // Case 4: Working Days policy - Rs.31,000 / 26 * 17 present = Rs.20,269.23
        // ---------------------------------------------------------------
        [Fact]
        public async Task Case4_WorkingDaysBasis_31000Over26TimesSeventeen_Gives20269_23()
        {
            var (db, service) = await BuildAsync();
            await SeedPolicyAsync(db, SalaryProrationBasis.WorkingDays, fixedWorkingDays: 26m);
            var employee = await SeedEmployeeAsync(db, new DateTime(2019, 1, 1));
            await SeedSalaryStructureAsync(db, employee.Id, 31000m, new DateTime(2019, 1, 1));
            await SeedPresentDaysAsync(db, employee.Id, 2026, 1, 17);

            var result = await service.CalculateAsync(employee.Id, 2026, 1, TenantId);

            result.CanProcess.Should().BeTrue();
            result.TotalDaysInPeriod.Should().Be(26);
            result.PresentDays.Should().Be(17);
            result.PayableDays.Should().Be(17);
            // 31000 * 17 / 26 = 20269.230769... -> rounds to 20269.23
            result.NetSalary.Should().Be(20269.23m);
        }

        // ---------------------------------------------------------------
        // Regression: duplicate Attendance rows for the same date must not
        // inflate Present Days.
        // ---------------------------------------------------------------
        [Fact]
        public async Task DuplicateAttendanceRowsForSameDate_AreDeduplicated_NotDoubleCounted()
        {
            var (db, service) = await BuildAsync();
            await SeedPolicyAsync(db, SalaryProrationBasis.CalendarDays);
            var employee = await SeedEmployeeAsync(db, new DateTime(2019, 1, 1));
            await SeedSalaryStructureAsync(db, employee.Id, 31000m, new DateTime(2019, 1, 1));
            await SeedPresentDaysAsync(db, employee.Id, 2026, 1, 17);

            // Duplicate row for 1 Jan (same date, second Attendance record) -
            // must not push Present Days to 18.
            db.Attendances.Add(new Attendance
            {
                Id = $"ATT-DUP-{Guid.NewGuid()}",
                TenantId = TenantId,
                CompanyId = CompanyId,
                EmployeeId = employee.Id,
                Date = new DateTime(2026, 1, 1),
                Status = AttendanceStatus.Present,
                CreatedBy = "SEED",
                ModifiedOn = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var result = await service.CalculateAsync(employee.Id, 2026, 1, TenantId);

            result.PresentDays.Should().Be(17);
            result.Warnings.Should().Contain(w => w.Contains("duplicate"));
        }

        // ---------------------------------------------------------------
        // Regression: Unpaid Leave must reduce Payable Days (not be
        // silently excluded from both sides like the pre-fix bug).
        // ---------------------------------------------------------------
        [Fact]
        public async Task UnpaidLeave_ReducesPayableDays_NotSilentlyIgnored()
        {
            var (db, service) = await BuildAsync();
            await SeedPolicyAsync(db, SalaryProrationBasis.CalendarDays);
            var employee = await SeedEmployeeAsync(db, new DateTime(2019, 1, 1));
            await SeedSalaryStructureAsync(db, employee.Id, 31000m, new DateTime(2019, 1, 1));
            // Only 17 present days out of 31 (Jan) - the remaining 14 days
            // are neither attendance nor leave, i.e. plain absence.
            await SeedPresentDaysAsync(db, employee.Id, 2026, 1, 17);

            var unpaidLeaveTypeId = await SeedLeaveTypeAsync(db, isPaid: false);
            await SeedApprovedLeaveAsync(db, employee.Id, unpaidLeaveTypeId, new DateTime(2026, 1, 18), new DateTime(2026, 1, 20)); // 3 unpaid days

            var result = await service.CalculateAsync(employee.Id, 2026, 1, TenantId);

            result.PresentDays.Should().Be(17);
            result.UnpaidLeaveDays.Should().Be(3);
            // Unpaid leave contributes 0 to Payable Days - must stay 17, not 20.
            result.PayableDays.Should().Be(17);
            result.NetSalary.Should().Be(17000.00m);
        }
    }
}
