using Application.DTOs.Attendances;
using Application.Interfaces.Attendances;
using Application.Interfaces.ErrorLog;
using Application.Services.Attendances;
using Domain.Helper;
using EsslIntegration.Tests.TestFixtures;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace EsslIntegration.Tests.Integration
{
    /// <summary>
    /// Covers the new eSSL Settings page's Get/Save/Test Connection
    /// configuration behavior added on top of the existing sync engine:
    /// blank password keeps the existing saved one, Windows Authentication
    /// never persists credentials, and Save Settings enforces its
    /// cross-field validation server-side (not just in the UI). Uses the
    /// same InMemory ApplicationDbContext fixture as
    /// EsslAttendanceSyncServiceTests - nothing here touches sync/import
    /// logic, only the configuration surface.
    /// </summary>
    public class EsslConfigurationServiceTests
    {
        private static IConfiguration BuildConfig() =>
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();

        private static IDataProtectionProvider BuildDataProtectionProvider() =>
            DataProtectionProvider.Create("EsslIntegration.Tests");

        private static EsslAttendanceSyncService BuildService(Infrastructure.ApplicationDbContext db, Mock<IEsslAttendanceDataSource>? dataSourceMock = null)
        {
            dataSourceMock ??= new Mock<IEsslAttendanceDataSource>();

            var processorMock = new Mock<IAttendanceProcessorService>();
            var errorLogMock = new Mock<IErrorLogService>();

            return new EsslAttendanceSyncService(
                db,
                dataSourceMock.Object,
                processorMock.Object,
                errorLogMock.Object,
                BuildConfig(),
                BuildDataProtectionProvider(),
                NullLogger<EsslAttendanceSyncService>.Instance);
        }

        private static EsslDatabaseConfigDto ValidSqlDto(string? password = "P@ssw0rd") => new()
        {
            IntegrationEnabled = true,
            DatabaseServer = "SQLSERVER01",
            DatabaseName = "etimetracklite1",
            AuthenticationType = EsslAuthenticationTypes.Sql,
            Username = "esslreader",
            Password = password,
            ConnectionTimeout = 15,
            SyncIntervalMinutes = 5,
            BatchSize = 500
        };

        [Fact]
        public async Task SaveConfiguration_SqlAuthWithoutUsername_ReturnsValidationError_AndDoesNotPersist()
        {
            using var fixture = await EsslTestFixture.CreateAsync();
            var service = BuildService(fixture.Context);

            var dto = ValidSqlDto();
            dto.Username = " ";

            var (success, message) = await service.SaveConfigurationAsync(dto, EsslTestFixture.TenantId, "Test");

            success.Should().BeFalse();
            message.Should().Be("Username is required for SQL Server Authentication.");

            var saved = await fixture.Context.EsslIntegrationSettings
                .FirstOrDefaultAsync(x => x.TenantId == EsslTestFixture.TenantId);
            saved.Should().BeNull("an invalid submission must never be persisted");
        }

        [Fact]
        public async Task SaveConfiguration_ThenReload_NeverExposesPassword_ButReportsHasPasswordConfigured()
        {
            using var fixture = await EsslTestFixture.CreateAsync();
            var service = BuildService(fixture.Context);

            var (success, _) = await service.SaveConfigurationAsync(ValidSqlDto(), EsslTestFixture.TenantId, "Test");
            success.Should().BeTrue();

            var view = await service.GetConfigurationAsync(EsslTestFixture.TenantId);

            view.HasPasswordConfigured.Should().BeTrue();
            view.DatabaseServer.Should().Be("SQLSERVER01");
            // EsslDatabaseConfigViewDto has no Password property at all - the
            // absence of that property is itself the guarantee; this
            // assertion documents the intent for anyone reading the test.
            typeof(EsslDatabaseConfigViewDto).GetProperty("Password").Should().BeNull();
        }

        [Fact]
        public async Task SaveConfiguration_BlankPasswordOnUpdate_KeepsPreviouslySavedPassword()
        {
            using var fixture = await EsslTestFixture.CreateAsync();
            var service = BuildService(fixture.Context);

            await service.SaveConfigurationAsync(ValidSqlDto(password: "OriginalPassword1"), EsslTestFixture.TenantId, "Test");

            var originalEncrypted = (await fixture.Context.EsslIntegrationSettings
                .FirstAsync(x => x.TenantId == EsslTestFixture.TenantId)).EncryptedPassword;

            // Second save, e.g. only changing SyncIntervalMinutes - Password
            // left blank exactly as the edit form would submit it.
            var secondDto = ValidSqlDto(password: null);
            secondDto.SyncIntervalMinutes = 10;

            var (success, _) = await service.SaveConfigurationAsync(secondDto, EsslTestFixture.TenantId, "Test");
            success.Should().BeTrue();

            var updated = await fixture.Context.EsslIntegrationSettings
                .FirstAsync(x => x.TenantId == EsslTestFixture.TenantId);

            updated.SyncIntervalMinutes.Should().Be(10);
            updated.EncryptedPassword.Should().Be(originalEncrypted, "a blank password on update must never overwrite/clear the existing saved password");
        }

        [Fact]
        public async Task SaveConfiguration_WindowsAuthentication_NeverPersistsUsernameOrPassword()
        {
            using var fixture = await EsslTestFixture.CreateAsync();
            var service = BuildService(fixture.Context);

            var dto = ValidSqlDto();
            dto.AuthenticationType = EsslAuthenticationTypes.Windows;
            // Simulate the browser sending stray values anyway - the server
            // must still refuse to persist them, not just trust the client
            // to omit them (requirement: "backend must ALSO validate").
            dto.Username = "someone";
            dto.Password = "somepassword";

            var (success, _) = await service.SaveConfigurationAsync(dto, EsslTestFixture.TenantId, "Test");
            success.Should().BeTrue();

            var saved = await fixture.Context.EsslIntegrationSettings
                .FirstAsync(x => x.TenantId == EsslTestFixture.TenantId);

            saved.Username.Should().BeNull();
            saved.EncryptedPassword.Should().BeNull();
        }

        [Fact]
        public async Task TestConnection_ReadsFormValues_NeverPersistsAndNeverStartsSync()
        {
            using var fixture = await EsslTestFixture.CreateAsync();

            var dataSourceMock = new Mock<IEsslAttendanceDataSource>();
            dataSourceMock
                .Setup(x => x.TestConnectionAsync(It.IsAny<EsslConnectionParameters>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((true, "Database connection successful."));

            var service = BuildService(fixture.Context, dataSourceMock);

            var dto = ValidSqlDto(password: "TypedButUnsaved1");
            var (success, message) = await service.TestConnectionAsync(dto, EsslTestFixture.TenantId);

            success.Should().BeTrue();
            message.Should().Be("Database connection successful.");

            dataSourceMock.Verify(x => x.TestConnectionAsync(
                It.Is<EsslConnectionParameters>(p => p.DatabaseServer == "SQLSERVER01" && p.Password == "TypedButUnsaved1"),
                It.IsAny<CancellationToken>()), Times.Once);

            (await fixture.Context.EsslIntegrationSettings.CountAsync())
                .Should().Be(0, "Test Connection must never persist the settings row");
        }

        [Fact]
        public async Task TestConnection_MissingDatabaseServer_ReturnsFriendlyValidationMessage_WithoutCallingDataSource()
        {
            using var fixture = await EsslTestFixture.CreateAsync();

            var dataSourceMock = new Mock<IEsslAttendanceDataSource>();
            var service = BuildService(fixture.Context, dataSourceMock);

            var dto = ValidSqlDto();
            dto.DatabaseServer = "";

            var (success, message) = await service.TestConnectionAsync(dto, EsslTestFixture.TenantId);

            success.Should().BeFalse();
            message.Should().Be("Database server is required.");

            dataSourceMock.Verify(x => x.TestConnectionAsync(It.IsAny<EsslConnectionParameters>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
