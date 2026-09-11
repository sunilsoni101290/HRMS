-- Salary Processing revision - fixes attendance-based salary proration to
-- use a fixed Calendar/Working Days denominator (per Application/Services/
-- PayrollService/SalaryCalculationService.cs) instead of the old ad-hoc
-- "count of Attendance rows that month" ratio, and reads Paid/Unpaid Leave
-- directly from approved LeaveApplication (Attendance is never synced from
-- Leave in this codebase - see that file's class remarks for the full
-- root-cause writeup). No existing Payroll/PayrollDetail/SalaryStructure
-- data is touched or reinterpreted by this script - it only adds new
-- nullable/defaulted columns and one new audit table.
--
-- Idempotent - safe to re-run. If you have the .NET SDK available,
-- `dotnet ef migrations add AddSalaryProcessingRevision --project
-- Infrastructure --startup-project API` should produce an equivalent
-- migration from the entity changes already made to
-- Domain/Entities/AttendancePolicy.cs, Domain/Entities/Payroll.cs and the
-- new Domain/Entities/PayrollAuditLog.cs - this script is the ready-to-run
-- equivalent for environments without the SDK on hand (see
-- Infrastructure/Migrations/20260905120000_AddSalaryProcessingRevision.cs
-- for the migration-history-tracked copy of these same statements).

-- =====================================================================
-- 1) AttendancePolicies - company-configurable Salary Proration Basis.
--    Default matches the confirmed system default: Working Days (2),
--    fixed at 26 days/month, until an admin changes it per company via
--    the Attendance Policy screen.
-- =====================================================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.AttendancePolicies') AND name = 'SalaryProrationBasis')
    ALTER TABLE [dbo].[AttendancePolicies] ADD [SalaryProrationBasis] INT NOT NULL CONSTRAINT DF_AttendancePolicies_SalaryProrationBasis DEFAULT (2);

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.AttendancePolicies') AND name = 'FixedWorkingDaysPerMonth')
    ALTER TABLE [dbo].[AttendancePolicies] ADD [FixedWorkingDaysPerMonth] DECIMAL(5,2) NOT NULL CONSTRAINT DF_AttendancePolicies_FixedWorkingDaysPerMonth DEFAULT (26);

-- =====================================================================
-- 2) Payrolls - precise Payable Days breakdown + recalculation audit
--    summary. All nullable/defaulted so existing rows remain valid as-is
--    (their PayableDays etc. are simply NULL until that specific payroll
--    is recalculated - a policy/formula change never silently
--    reinterprets an already-generated payroll's stored numbers).
-- =====================================================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Payrolls') AND name = 'PaidLeaveDays')
    ALTER TABLE [dbo].[Payrolls] ADD [PaidLeaveDays] DECIMAL(18,2) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Payrolls') AND name = 'UnpaidLeaveDays')
    ALTER TABLE [dbo].[Payrolls] ADD [UnpaidLeaveDays] DECIMAL(18,2) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Payrolls') AND name = 'PayableDays')
    ALTER TABLE [dbo].[Payrolls] ADD [PayableDays] DECIMAL(18,2) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Payrolls') AND name = 'ProrationBasisUsed')
    ALTER TABLE [dbo].[Payrolls] ADD [ProrationBasisUsed] INT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Payrolls') AND name = 'RecalculatedCount')
    ALTER TABLE [dbo].[Payrolls] ADD [RecalculatedCount] INT NOT NULL CONSTRAINT DF_Payrolls_RecalculatedCount DEFAULT (0);

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Payrolls') AND name = 'LastRecalculatedOn')
    ALTER TABLE [dbo].[Payrolls] ADD [LastRecalculatedOn] DATETIME2 NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Payrolls') AND name = 'LastRecalculatedBy')
    ALTER TABLE [dbo].[Payrolls] ADD [LastRecalculatedBy] NVARCHAR(MAX) NULL;

-- =====================================================================
-- 3) PayrollAuditLogs - append-only Generate/Recalculate event trail
--    (Section 14 "Salary history और audit maintain हो").
-- =====================================================================
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

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PayrollAuditLogs_PayrollId' AND object_id = OBJECT_ID('dbo.PayrollAuditLogs'))
    CREATE INDEX [IX_PayrollAuditLogs_PayrollId] ON [dbo].[PayrollAuditLogs] ([PayrollId]);
