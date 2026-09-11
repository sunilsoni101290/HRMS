-- Salary Structure Management - reusable "Salary Structure" template
-- (Domain/Entities/SalaryTemplate.cs), separate from the existing
-- per-employee SalaryStructure/SalaryDetail tables (now labeled "Salary
-- Assignments" in the menu - unchanged here except for the new nullable
-- SourceTemplateId traceability column added to SalaryStructures below).
-- Applying a template to employees (single or bulk, via
-- POST api/salary-template/assign) creates one new SalaryStructure row per
-- employee, copying the template's SalaryTemplateDetail lines - existing
-- Payroll generation, Excel import/export and Employee-Salary CRUD all
-- keep working exactly as before, untouched by this script.
--
-- Idempotent - safe to re-run. Mirrors this repo's existing
-- "add PasswordChangedOn column to Users.sql" / "add EsslIntegrationSettings
-- table.sql" style. If you have the .NET SDK available,
-- `dotnet ef migrations add AddSalaryTemplateModule --project Infrastructure
-- --startup-project API` should produce an equivalent migration from the
-- entity changes already made to Domain/Entities/SalaryTemplate.cs and
-- Domain/Entities/SalaryStructure.cs (SourceTemplateId) - this script is
-- the ready-to-run equivalent for environments without the SDK on hand
-- (see Infrastructure/Migrations/20260905000000_AddSalaryTemplateModule.cs
-- for the migration-history-tracked copy of these same statements).

-- =====================================================================
-- 1) SalaryTemplates - the reusable master (header)
-- =====================================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SalaryTemplates')
BEGIN
    CREATE TABLE [dbo].[SalaryTemplates] (
        [Id]            NVARCHAR(450)  NOT NULL,
        [TenantId]      NVARCHAR(MAX)  NULL,
        [Name]          NVARCHAR(150)  NOT NULL,
        [Description]   NVARCHAR(500)  NULL,
        [EffectiveFrom] DATETIME2      NOT NULL,
        [IsDeleted]     BIT            NOT NULL CONSTRAINT DF_SalaryTemplates_IsDeleted DEFAULT (0),
        [CreatedOn]     DATETIME2      NOT NULL CONSTRAINT DF_SalaryTemplates_CreatedOn DEFAULT (SYSUTCDATETIME()),
        [CreatedBy]     NVARCHAR(MAX)  NOT NULL,
        [ModifiedOn]    DATETIME2      NULL,
        [ModifiedBy]    NVARCHAR(MAX)  NULL,
        [IsActive]      BIT            NOT NULL CONSTRAINT DF_SalaryTemplates_IsActive DEFAULT (1),
        CONSTRAINT [PK_SalaryTemplates] PRIMARY KEY ([Id])
    );
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SalaryTemplates_Name' AND object_id = OBJECT_ID('dbo.SalaryTemplates'))
    CREATE INDEX [IX_SalaryTemplates_Name] ON [dbo].[SalaryTemplates] ([Name]);

-- =====================================================================
-- 2) SalaryTemplateDetails - the component/amount lines under a template
-- =====================================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SalaryTemplateDetails')
BEGIN
    CREATE TABLE [dbo].[SalaryTemplateDetails] (
        [Id]                NVARCHAR(450)   NOT NULL,
        [TenantId]          NVARCHAR(MAX)   NULL,
        [SalaryTemplateId]  NVARCHAR(450)   NOT NULL,
        [SalaryComponentId] NVARCHAR(450)   NOT NULL,
        [Amount]            DECIMAL(18,2)   NOT NULL,
        [CalculationType]   INT             NOT NULL CONSTRAINT DF_SalaryTemplateDetails_CalculationType DEFAULT (1),
        [IsDeleted]         BIT             NOT NULL CONSTRAINT DF_SalaryTemplateDetails_IsDeleted DEFAULT (0),
        [CreatedOn]         DATETIME2       NOT NULL CONSTRAINT DF_SalaryTemplateDetails_CreatedOn DEFAULT (SYSUTCDATETIME()),
        [CreatedBy]         NVARCHAR(MAX)   NOT NULL,
        [ModifiedOn]        DATETIME2       NULL,
        [ModifiedBy]        NVARCHAR(MAX)   NULL,
        [IsActive]          BIT             NOT NULL CONSTRAINT DF_SalaryTemplateDetails_IsActive DEFAULT (1),
        CONSTRAINT [PK_SalaryTemplateDetails] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SalaryTemplateDetails_SalaryTemplates_SalaryTemplateId] FOREIGN KEY ([SalaryTemplateId]) REFERENCES [dbo].[SalaryTemplates]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_SalaryTemplateDetails_SalaryComponents_SalaryComponentId] FOREIGN KEY ([SalaryComponentId]) REFERENCES [dbo].[SalaryComponents]([Id]) ON DELETE NO ACTION
    );
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SalaryTemplateDetails_SalaryTemplateId' AND object_id = OBJECT_ID('dbo.SalaryTemplateDetails'))
    CREATE INDEX [IX_SalaryTemplateDetails_SalaryTemplateId] ON [dbo].[SalaryTemplateDetails] ([SalaryTemplateId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SalaryTemplateDetails_SalaryComponentId' AND object_id = OBJECT_ID('dbo.SalaryTemplateDetails'))
    CREATE INDEX [IX_SalaryTemplateDetails_SalaryComponentId] ON [dbo].[SalaryTemplateDetails] ([SalaryComponentId]);

-- =====================================================================
-- 3) SalaryStructures.SourceTemplateId - traceability only (see
--    Domain/Entities/SalaryStructure.cs remarks). SET NULL on delete so
--    removing a template never blocks/cascades into existing employee
--    assignments or the Payroll already generated from them.
-- =====================================================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SalaryStructures') AND name = 'SourceTemplateId')
    ALTER TABLE [dbo].[SalaryStructures] ADD [SourceTemplateId] NVARCHAR(450) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SalaryStructures_SalaryTemplates_SourceTemplateId')
    ALTER TABLE [dbo].[SalaryStructures]
        ADD CONSTRAINT [FK_SalaryStructures_SalaryTemplates_SourceTemplateId]
        FOREIGN KEY ([SourceTemplateId]) REFERENCES [dbo].[SalaryTemplates]([Id]) ON DELETE SET NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SalaryStructures_SourceTemplateId' AND object_id = OBJECT_ID('dbo.SalaryStructures'))
    CREATE INDEX [IX_SalaryStructures_SourceTemplateId] ON [dbo].[SalaryStructures] ([SourceTemplateId]);
