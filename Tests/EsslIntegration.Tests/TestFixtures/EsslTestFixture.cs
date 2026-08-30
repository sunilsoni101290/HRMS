using Application.Interfaces;
using Domain.Entities;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace EsslIntegration.Tests.TestFixtures
{
    /// <summary>
    /// Fresh, isolated ApplicationDbContext (EF Core InMemory, uniquely-named
    /// per instance) seeded with just enough for eSSL sync tests: a Tenant
    /// and one active EmployeeBiometricMapping. Mirrors
    /// LoanAdvance.Tests/TestFixtures/LoanAdvanceTestFixture.cs's shape and
    /// its documented InMemory-provider caveat (no real FK enforcement, so
    /// EmployeeId below is a placeholder string, not a seeded Employee row -
    /// nothing under test here touches the Employee table).
    /// </summary>
    public class EsslTestFixture : IDisposable
    {
        public ApplicationDbContext Context { get; }

        public const string TenantId = "TEST-TENANT";

        /// <summary>The device-side code an active mapping exists for.</summary>
        public const string MappedBiometricCode = "1001";

        /// <summary>A device-side code with NO mapping - used to exercise the "Unknown Employee" path.</summary>
        public const string UnmappedBiometricCode = "9999";

        private EsslTestFixture(ApplicationDbContext context)
        {
            Context = context;
        }

        public static async Task<EsslTestFixture> CreateAsync()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .EnableSensitiveDataLogging()
                .Options;

            var tenantServiceMock = new Mock<ITenantService>();
            tenantServiceMock.Setup(x => x.GetTenantId()).Returns(TenantId);

            var context = new ApplicationDbContext(options, tenantServiceMock.Object);

            var fixture = new EsslTestFixture(context);
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

            Context.EmployeeBiometricMappings.Add(new EmployeeBiometricMapping
            {
                Id = "EBM-TEST-1",
                TenantId = TenantId,
                EmployeeId = "PLACEHOLDER-EMPLOYEE",
                BiometricEmployeeCode = MappedBiometricCode,
                IsActive = true,
                CreatedBy = "SEED"
            });

            await Context.SaveChangesAsync();
        }

        public void Dispose()
        {
            Context.Dispose();
        }
    }
}
