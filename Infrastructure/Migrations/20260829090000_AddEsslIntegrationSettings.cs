using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// Idempotent by convention with PendingMigrationEsslAttendanceSyncStates
    /// (IF NOT EXISTS-guarded raw SQL rather than typed CreateTable/AddColumn
    /// calls) - this environment's migration history has previously drifted
    /// from its live database, so every operation here is written to be safe
    /// whether or not it was already applied by hand. Adds the new per-tenant
    /// EsslIntegrationSettings table backing the editable "Database
    /// Configuration" card on the eSSL Settings page - does not touch
    /// EsslAttendanceSyncStates, BiometricSyncLogs, or any other existing
    /// eSSL/biometric table.
    /// </remarks>
    public partial class AddEsslIntegrationSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'EsslIntegrationSettings')
BEGIN
    CREATE TABLE [dbo].[EsslIntegrationSettings] (
        [Id] NVARCHAR(450) NOT NULL,
        [TenantId] NVARCHAR(450) NOT NULL,
        [IntegrationEnabled] BIT NOT NULL CONSTRAINT DF_EsslIntegrationSettings_IntegrationEnabled DEFAULT (0),
        [DatabaseServer] NVARCHAR(300) NOT NULL,
        [DatabaseName] NVARCHAR(200) NOT NULL,
        [AuthenticationType] NVARCHAR(20) NOT NULL CONSTRAINT DF_EsslIntegrationSettings_AuthenticationType DEFAULT ('Sql'),
        [Username] NVARCHAR(200) NULL,
        [EncryptedPassword] NVARCHAR(MAX) NULL,
        [ConnectionTimeout] INT NOT NULL CONSTRAINT DF_EsslIntegrationSettings_ConnectionTimeout DEFAULT (15),
        [SyncIntervalMinutes] INT NOT NULL CONSTRAINT DF_EsslIntegrationSettings_SyncIntervalMinutes DEFAULT (5),
        [BatchSize] INT NOT NULL CONSTRAINT DF_EsslIntegrationSettings_BatchSize DEFAULT (500),
        [IsDeleted] BIT NOT NULL CONSTRAINT DF_EsslIntegrationSettings_IsDeleted DEFAULT (0),
        [CreatedOn] DATETIME2 NOT NULL CONSTRAINT DF_EsslIntegrationSettings_CreatedOn DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] NVARCHAR(MAX) NOT NULL CONSTRAINT DF_EsslIntegrationSettings_CreatedBy DEFAULT ('System'),
        [ModifiedOn] DATETIME2 NULL,
        [ModifiedBy] NVARCHAR(MAX) NULL,
        [IsActive] BIT NOT NULL CONSTRAINT DF_EsslIntegrationSettings_IsActive DEFAULT (1),
        CONSTRAINT [PK_EsslIntegrationSettings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EsslIntegrationSettings_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [dbo].[Tenants]([Id]) ON DELETE NO ACTION
    );
END
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EsslIntegrationSettings_Tenant' AND object_id = OBJECT_ID('dbo.EsslIntegrationSettings'))
    CREATE UNIQUE INDEX [IX_EsslIntegrationSettings_Tenant] ON [dbo].[EsslIntegrationSettings]([TenantId]);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF OBJECT_ID('dbo.EsslIntegrationSettings', 'U') IS NOT NULL DROP TABLE [dbo].[EsslIntegrationSettings];");
        }
    }
}
