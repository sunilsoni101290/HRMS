using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// Idempotent by convention with AddSalaryTemplateModule/
    /// AddUserPasswordChangedOn (IF NOT EXISTS-guarded raw SQL rather than
    /// typed AddColumn/CreateTable calls, and no accompanying
    /// .Designer.cs/model-snapshot update - see those migrations' own
    /// remarks for why). Adds the Salary Processing revision: a
    /// company-configurable Salary Proration Basis on AttendancePolicy, a
    /// precise Payable Days breakdown + recalculation summary on Payroll,
    /// and a new PayrollAuditLogs event trail - see
    /// Domain/Entities/AttendancePolicy.cs, Domain/Entities/Payroll.cs and
    /// Domain/Entities/PayrollAuditLog.cs, and
    /// Application/Services/PayrollService/SalaryCalculationService.cs for
    /// the root-cause writeup and the algorithm these columns support. No
    /// existing data is touched - only new nullable/defaulted columns and
    /// one new table. See the equivalent, more heavily commented
    /// "add Salary Processing revision (attendance-based proration).sql" at
    /// the repo root for the deployment-facing version of these same
    /// statements.
    /// </remarks>
    public partial class AddSalaryProcessingRevision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.AttendancePolicies') AND name = 'SalaryProrationBasis')
    ALTER TABLE [dbo].[AttendancePolicies] ADD [SalaryProrationBasis] INT NOT NULL CONSTRAINT DF_AttendancePolicies_SalaryProrationBasis DEFAULT (2);
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.AttendancePolicies') AND name = 'FixedWorkingDaysPerMonth')
    ALTER TABLE [dbo].[AttendancePolicies] ADD [FixedWorkingDaysPerMonth] DECIMAL(5,2) NOT NULL CONSTRAINT DF_AttendancePolicies_FixedWorkingDaysPerMonth DEFAULT (26);
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Payrolls') AND name = 'PaidLeaveDays')
    ALTER TABLE [dbo].[Payrolls] ADD [PaidLeaveDays] DECIMAL(18,2) NULL;
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Payrolls') AND name = 'UnpaidLeaveDays')
    ALTER TABLE [dbo].[Payrolls] ADD [UnpaidLeaveDays] DECIMAL(18,2) NULL;
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Payrolls') AND name = 'PayableDays')
    ALTER TABLE [dbo].[Payrolls] ADD [PayableDays] DECIMAL(18,2) NULL;
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Payrolls') AND name = 'ProrationBasisUsed')
    ALTER TABLE [dbo].[Payrolls] ADD [ProrationBasisUsed] INT NULL;
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Payrolls') AND name = 'RecalculatedCount')
    ALTER TABLE [dbo].[Payrolls] ADD [RecalculatedCount] INT NOT NULL CONSTRAINT DF_Payrolls_RecalculatedCount DEFAULT (0);
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Payrolls') AND name = 'LastRecalculatedOn')
    ALTER TABLE [dbo].[Payrolls] ADD [LastRecalculatedOn] DATETIME2 NULL;
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Payrolls') AND name = 'LastRecalculatedBy')
    ALTER TABLE [dbo].[Payrolls] ADD [LastRecalculatedBy] NVARCHAR(MAX) NULL;
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PayrollAuditLogs')
BEGIN
    CREATE TABLE [dbo].[PayrollAuditLogs] (
        [Id]              NVARCHAR(450)  NOT NULL,
        [TenantId]        NVARCHAR(MAX)  NULL,
        [PayrollId]       NVARCHAR(450)  NOT NULL,
        [Action]          NVARCHAR(MAX)  NOT NULL,
        [OldNetSalary]    DECIMAL(18,2)  NULL,
        [NewNetSalary]    DECIMAL(18,2)  NULL,
        [OldPayableDays]  DECIMAL(18,2)  NULL,
        [NewPayableDays]  DECIMAL(18,2)  NULL,
        [PerformedBy]     NVARCHAR(MAX)  NOT NULL,
        [Remarks]         NVARCHAR(500)  NULL,
        [PerformedOn]     DATETIME2      NOT NULL CONSTRAINT DF_PayrollAuditLogs_PerformedOn DEFAULT (SYSUTCDATETIME()),
        [IsDeleted]       BIT            NOT NULL CONSTRAINT DF_PayrollAuditLogs_IsDeleted DEFAULT (0),
        [CreatedOn]       DATETIME2      NOT NULL CONSTRAINT DF_PayrollAuditLogs_CreatedOn DEFAULT (SYSUTCDATETIME()),
        [CreatedBy]       NVARCHAR(MAX)  NOT NULL,
        [ModifiedOn]      DATETIME2      NULL,
        [ModifiedBy]      NVARCHAR(MAX)  NULL,
        [IsActive]        BIT            NOT NULL CONSTRAINT DF_PayrollAuditLogs_IsActive DEFAULT (1),
        CONSTRAINT [PK_PayrollAuditLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PayrollAuditLogs_Payrolls_PayrollId] FOREIGN KEY ([PayrollId]) REFERENCES [dbo].[Payrolls]([Id]) ON DELETE CASCADE
    );
END
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PayrollAuditLogs_PayrollId' AND object_id = OBJECT_ID('dbo.PayrollAuditLogs'))
    CREATE INDEX [IX_PayrollAuditLogs_PayrollId] ON [dbo].[PayrollAuditLogs] ([PayrollId]);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PayrollAuditLogs')
    DROP TABLE [dbo].[PayrollAuditLogs];
");

            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Payrolls') AND name = 'LastRecalculatedBy')
    ALTER TABLE [dbo].[Payrolls] DROP COLUMN [LastRecalculatedBy];
");
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Payrolls') AND name = 'LastRecalculatedOn')
    ALTER TABLE [dbo].[Payrolls] DROP COLUMN [LastRecalculatedOn];
");
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Payrolls') AND name = 'RecalculatedCount')
    ALTER TABLE [dbo].[Payrolls] DROP COLUMN [RecalculatedCount];
");
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Payrolls') AND name = 'ProrationBasisUsed')
    ALTER TABLE [dbo].[Payrolls] DROP COLUMN [ProrationBasisUsed];
");
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Payrolls') AND name = 'PayableDays')
    ALTER TABLE [dbo].[Payrolls] DROP COLUMN [PayableDays];
");
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Payrolls') AND name = 'UnpaidLeaveDays')
    ALTER TABLE [dbo].[Payrolls] DROP COLUMN [UnpaidLeaveDays];
");
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Payrolls') AND name = 'PaidLeaveDays')
    ALTER TABLE [dbo].[Payrolls] DROP COLUMN [PaidLeaveDays];
");

            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.AttendancePolicies') AND name = 'FixedWorkingDaysPerMonth')
    ALTER TABLE [dbo].[AttendancePolicies] DROP COLUMN [FixedWorkingDaysPerMonth];
");
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.AttendancePolicies') AND name = 'SalaryProrationBasis')
    ALTER TABLE [dbo].[AttendancePolicies] DROP COLUMN [SalaryProrationBasis];
");
        }
    }
}
