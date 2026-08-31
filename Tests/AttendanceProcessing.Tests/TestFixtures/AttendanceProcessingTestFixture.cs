using Application.Interfaces;
using Domain.Entities;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;
using static Domain.Enums.EnumExtensions;

namespace AttendanceProcessing.Tests.TestFixtures
{
    /// <summary>
    /// Fresh, isolated ApplicationDbContext (EF Core InMemory, uniquely-named
    /// per instance) seeded with just enough for the biometric attendance
    /// processing tests: a Tenant, one Employee with a day Shift (General,
    /// 09:00-18:00, matching the scenarios in the spec), one active
    /// EmployeeBiometricMapping, and a second Employee + night Shift
    /// (22:00-06:00) for the cross-midnight scenario. Mirrors
    /// EsslIntegration.Tests/TestFixtures/EsslTestFixture.cs's shape and its
    /// documented InMemory-provider caveat (no real FK enforcement).
    /// </summary>
    public class AttendanceProcessingTestFixture : IDisposable
    {
        public ApplicationDbContext Context { get; }

        public const string TenantId = "TEST-TENANT";

        public const string EmployeeId = "EMP-TEST-1";
        public const string EmployeeCode = "EMP001";
        public const string BiometricDeviceCode = "1001";

        public const string NightShiftEmployeeId = "EMP-TEST-2";
        public const string NightShiftEmployeeCode = "EMP002";
        public const string NightShiftBiometricDeviceCode = "1002";

        public const string DeviceId = "DEV-1";

        public string DayShiftId { get; private set; } = "";
        public string NightShiftId { get; private set; } = "";

        private AttendanceProcessingTestFixture(ApplicationDbContext context)
        {
            Context = context;
        }

        public static async Task<AttendanceProcessingTestFixture> CreateAsync()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .EnableSensitiveDataLogging()
                .Options;

            var tenantServiceMock = new Mock<ITenantService>();
            tenantServiceMock.Setup(x => x.GetTenantId()).Returns(TenantId);

            var context = new ApplicationDbContext(options, tenantServiceMock.Object);

            var fixture = new AttendanceProcessingTestFixture(context);
            await fixture.SeedAsync();
            return fixture;
        }

        private async Task SeedAsync()
        {
            Context.Tenants.Add(new Tenant
            {
                Id = TenantId,
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

            var dayShift = new Shift
            {
                Id = IDManager.GetNewId(new Shift()),
                TenantId = TenantId,
                Name = "General Shift",
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(18, 0, 0),
                GraceInMinutes = 10,
                GraceOutMinutes = 10,
                HalfDayMinutes = 240,
                FullDayMinutes = 480,
                MinimumWorkingMinutes = 480,
                MaximumWorkingMinutes = 720,
                IsNightShift = false,
                IsDefaultShift = true,
                CreatedBy = "SEED"
            };
            DayShiftId = dayShift.Id;
            Context.Shifts.Add(dayShift);

            var nightShift = new Shift
            {
                Id = IDManager.GetNewId(new Shift()),
                TenantId = TenantId,
                Name = "Night Shift",
                StartTime = new TimeSpan(22, 0, 0),
                EndTime = new TimeSpan(6, 0, 0),
                GraceInMinutes = 10,
                GraceOutMinutes = 10,
                HalfDayMinutes = 240,
                FullDayMinutes = 480,
                MinimumWorkingMinutes = 480,
                MaximumWorkingMinutes = 720,
                IsNightShift = true,
                IsDefaultShift = false,
                CreatedBy = "SEED"
            };
            NightShiftId = nightShift.Id;
            Context.Shifts.Add(nightShift);

            Context.Employees.Add(new Employee
            {
                Id = EmployeeId,
                TenantId = TenantId,
                EmployeeCode = EmployeeCode,
                FirstName = "Test",
                LastName = "Employee",
                CompanyId = "PLACEHOLDER-COMPANY",
                DepartmentId = "PLACEHOLDER-DEPT",
                DesignationId = "PLACEHOLDER-DESIG",
                Phone = "9999999999",
                Address = "Test Address",
                Pincode = "123456",
                JoiningDate = new DateTime(2024, 1, 1),
                EmploymentType = EmploymentType.Permanent,
                Gender = Gender.Male,
                MaritalStatus = MaritalStatus.Unmarried,
                ShiftId = dayShift.Id,
                CreatedBy = "SEED"
            });

            Context.Employees.Add(new Employee
            {
                Id = NightShiftEmployeeId,
                TenantId = TenantId,
                EmployeeCode = NightShiftEmployeeCode,
                FirstName = "Night",
                LastName = "Employee",
                CompanyId = "PLACEHOLDER-COMPANY",
                DepartmentId = "PLACEHOLDER-DEPT",
                DesignationId = "PLACEHOLDER-DESIG",
                Phone = "9999999998",
                Address = "Test Address",
                Pincode = "123456",
                JoiningDate = new DateTime(2024, 1, 1),
                EmploymentType = EmploymentType.Permanent,
                Gender = Gender.Male,
                MaritalStatus = MaritalStatus.Unmarried,
                ShiftId = nightShift.Id,
                CreatedBy = "SEED"
            });

            Context.EmployeeBiometricMappings.Add(new EmployeeBiometricMapping
            {
                Id = "EBM-TEST-1",
                TenantId = TenantId,
                EmployeeId = EmployeeId,
                BiometricEmployeeCode = BiometricDeviceCode,
                IsActive = true,
                CreatedBy = "SEED"
            });

            Context.EmployeeBiometricMappings.Add(new EmployeeBiometricMapping
            {
                Id = "EBM-TEST-2",
                TenantId = TenantId,
                EmployeeId = NightShiftEmployeeId,
                BiometricEmployeeCode = NightShiftBiometricDeviceCode,
                IsActive = true,
                CreatedBy = "SEED"
            });

            await Context.SaveChangesAsync();

            // Need DefaultShift navigation populated for AttendanceService's
            // .Include(x => x.DefaultShift) - EF Core InMemory resolves this
            // via the FK (ShiftId) once both rows exist, which they now do.
        }

        public BiometricAttendanceLog MakePunch(string employeeCode, DateTime punchTime, PunchType punchType, string? deviceTransactionId = null)
        {
            return new BiometricAttendanceLog
            {
                Id = IDManager.GetNewId(new BiometricAttendanceLog()),
                TenantId = TenantId,
                DeviceId = DeviceId,
                EmployeeCode = employeeCode,
                PunchTime = punchTime,
                PunchType = punchType,
                DeviceTransactionId = deviceTransactionId,
                IsProcessed = false,
                CreatedBy = "SEED"
            };
        }

        public void Dispose()
        {
            Context.Dispose();
        }
    }
}
