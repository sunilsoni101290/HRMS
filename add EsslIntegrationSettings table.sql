-- ============================================================================
-- Adds the EsslIntegrationSettings table backing the editable "Database
-- Configuration" card on the eSSL eTimeTrackLite1 Integration Settings page.
-- Safe to run more than once (every statement is IF NOT EXISTS-guarded),
-- following the same convention as "fix BiometricSyncLogs missing BaseEntity
-- columns.sql" earlier in this project. Run this directly against the ERP/
-- Application database - it does NOT touch eTimeTrackLite1.
-- ============================================================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'EsslIntegrationSettings')
BEGIN
    CREATE TABLE [dbo].[EsslIntegrationSettings] (
        [Id]                  NVARCHAR(450)  NOT NULL,
        [TenantId]            NVARCHAR(450)  NOT NULL,
        [IntegrationEnabled]  BIT            NOT NULL CONSTRAINT DF_EsslIntegrationSettings_IntegrationEnabled DEFAULT (0),
        [DatabaseServer]      NVARCHAR(300)  NOT NULL,
        [DatabaseName]        NVARCHAR(200)  NOT NULL,
        [AuthenticationType]  NVARCHAR(20)   NOT NULL CONSTRAINT DF_EsslIntegrationSettings_AuthenticationType DEFAULT ('Sql'),
        [Username]            NVARCHAR(200)  NULL,
        [EncryptedPassword]   NVARCHAR(MAX)  NULL,
        [ConnectionTimeout]   INT            NOT NULL CONSTRAINT DF_EsslIntegrationSettings_ConnectionTimeout DEFAULT (15),
        [SyncIntervalMinutes] INT            NOT NULL CONSTRAINT DF_EsslIntegrationSettings_SyncIntervalMinutes DEFAULT (5),
        [BatchSize]           INT            NOT NULL CONSTRAINT DF_EsslIntegrationSettings_BatchSize DEFAULT (500),
        [IsDeleted]           BIT            NOT NULL CONSTRAINT DF_EsslIntegrationSettings_IsDeleted DEFAULT (0),
        [CreatedOn]           DATETIME2      NOT NULL CONSTRAINT DF_EsslIntegrationSettings_CreatedOn DEFAULT (SYSUTCDATETIME()),
        [CreatedBy]           NVARCHAR(MAX)  NOT NULL CONSTRAINT DF_EsslIntegrationSettings_CreatedBy DEFAULT ('System'),
        [ModifiedOn]          DATETIME2      NULL,
        [ModifiedBy]          NVARCHAR(MAX)  NULL,
        [IsActive]            BIT            NOT NULL CONSTRAINT DF_EsslIntegrationSettings_IsActive DEFAULT (1),
        CONSTRAINT [PK_EsslIntegrationSettings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EsslIntegrationSettings_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [dbo].[Tenants]([Id]) ON DELETE NO ACTION
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EsslIntegrationSettings_Tenant' AND object_id = OBJECT_ID('dbo.EsslIntegrationSettings'))
    CREATE UNIQUE INDEX [IX_EsslIntegrationSettings_Tenant] ON [dbo].[EsslIntegrationSettings]([TenantId]);
GO

-- Sanity check (uncomment to run manually):
-- SELECT * FROM [dbo].[EsslIntegrationSettings];
