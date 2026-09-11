using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// Idempotent by convention with AddUserPasswordChangedOn/
    /// AddEsslIntegrationSettings (IF NOT EXISTS-guarded raw SQL rather than
    /// typed CreateTable/AddColumn calls, and no accompanying
    /// .Designer.cs/model-snapshot update - see those migrations' own
    /// remarks for why). Adds the reusable Salary Structure ("template")
    /// module: SalaryTemplates (header) + SalaryTemplateDetails (component
    /// lines), plus a nullable SalaryStructures.SourceTemplateId
    /// traceability column - see Domain/Entities/SalaryTemplate.cs and
    /// Domain/Entities/SalaryStructure.cs for the corresponding entity
    /// changes. Existing SalaryStructure/SalaryDetail/Payroll tables and
    /// data are untouched. See the equivalent, more heavily commented
    /// "add SalaryTemplate module (Salary Structure Management).sql" at the
    /// repo root for the deployment-facing version of these same
    /// statements.
    /// </remarks>
    public partial class AddSalaryTemplateModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
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
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SalaryTemplates_Name' AND object_id = OBJECT_ID('dbo.SalaryTemplates'))
    CREATE INDEX [IX_SalaryTemplates_Name] ON [dbo].[SalaryTemplates] ([Name]);
");

            migrationBuilder.Sql(@"
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
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SalaryTemplateDetails_SalaryTemplateId' AND object_id = OBJECT_ID('dbo.SalaryTemplateDetails'))
    CREATE INDEX [IX_SalaryTemplateDetails_SalaryTemplateId] ON [dbo].[SalaryTemplateDetails] ([SalaryTemplateId]);
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SalaryTemplateDetails_SalaryComponentId' AND object_id = OBJECT_ID('dbo.SalaryTemplateDetails'))
    CREATE INDEX [IX_SalaryTemplateDetails_SalaryComponentId] ON [dbo].[SalaryTemplateDetails] ([SalaryComponentId]);
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SalaryStructures') AND name = 'SourceTemplateId')
    ALTER TABLE [dbo].[SalaryStructures] ADD [SourceTemplateId] NVARCHAR(450) NULL;
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SalaryStructures_SalaryTemplates_SourceTemplateId')
    ALTER TABLE [dbo].[SalaryStructures]
        ADD CONSTRAINT [FK_SalaryStructures_SalaryTemplates_SourceTemplateId]
        FOREIGN KEY ([SourceTemplateId]) REFERENCES [dbo].[SalaryTemplates]([Id]) ON DELETE SET NULL;
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SalaryStructures_SourceTemplateId' AND object_id = OBJECT_ID('dbo.SalaryStructures'))
    CREATE INDEX [IX_SalaryStructures_SourceTemplateId] ON [dbo].[SalaryStructures] ([SourceTemplateId]);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SalaryStructures_SalaryTemplates_SourceTemplateId')
    ALTER TABLE [dbo].[SalaryStructures] DROP CONSTRAINT [FK_SalaryStructures_SalaryTemplates_SourceTemplateId];
");

            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SalaryStructures') AND name = 'SourceTemplateId')
    ALTER TABLE [dbo].[SalaryStructures] DROP COLUMN [SourceTemplateId];
");

            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SalaryTemplateDetails')
    DROP TABLE [dbo].[SalaryTemplateDetails];
");

            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SalaryTemplates')
    DROP TABLE [dbo].[SalaryTemplates];
");
        }
    }
}
