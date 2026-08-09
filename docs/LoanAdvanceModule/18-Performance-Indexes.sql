-- Phase 18 (Performance Optimization) - Loan & Advance module.
-- Adds the indexes now configured in Infrastructure/ApplicationDbContext.cs
-- (OnModelCreating) to an already-provisioned database. Run this once
-- against the ERP database, OR let EF Core generate the equivalent
-- migration by running (from the Infrastructure project folder):
--   dotnet ef migrations add LoanAdvancePerformanceIndexes --startup-project ..\API
--   dotnet ef database update --startup-project ..\API
-- If you use the migration route instead, skip this script.
--
-- Two groups of indexes:
--   1. NEW for Phase 18 - support the tenant-wide aggregate scans added in
--      Phase 14 (dashboard/reports) and the per-day de-dup check added in
--      Phase 15 (reminder sweep).
--   2. RECONCILED from the original Phase 4 design
--      (docs/LoanAdvanceModule/04-SQL-Scripts.sql) - present in that design
--      doc but missing from the database/EF model that actually got built;
--      caught during this Phase 18 review and added here so code and
--      documentation agree again.

/* ---------- Group 1: new for Phase 18 ---------- */

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EmployeeLoans_Tenant_Status' AND object_id = OBJECT_ID('dbo.EmployeeLoans'))
    CREATE INDEX IX_EmployeeLoans_Tenant_Status ON dbo.EmployeeLoans (TenantId, Status);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EmployeeAdvances_Tenant_Status' AND object_id = OBJECT_ID('dbo.EmployeeAdvances'))
    CREATE INDEX IX_EmployeeAdvances_Tenant_Status ON dbo.EmployeeAdvances (TenantId, Status);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LoanPaymentHistories_PaymentDate' AND object_id = OBJECT_ID('dbo.LoanPaymentHistories'))
    CREATE INDEX IX_LoanPaymentHistories_PaymentDate ON dbo.LoanPaymentHistories (PaymentDate);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AdvancePaymentHistories_PaymentDate' AND object_id = OBJECT_ID('dbo.AdvancePaymentHistories'))
    CREATE INDEX IX_AdvancePaymentHistories_PaymentDate ON dbo.AdvancePaymentHistories (PaymentDate);
GO

-- Supports LoanAdvanceReminderService.AlreadyNotifiedTodayAsync's
-- once-per-day de-dup check ("ReferenceId IN (...) AND CreatedOn >= today"),
-- run on the shared Notifications table every 6 hours.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Notifications_Reference_CreatedOn' AND object_id = OBJECT_ID('dbo.Notifications'))
    CREATE INDEX IX_Notifications_Reference_CreatedOn ON dbo.Notifications (ReferenceId, CreatedOn);
GO

/* ---------- Group 2: reconciled from the original Phase 4 design ---------- */

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EmployeeLoans_Tenant_Company_Branch' AND object_id = OBJECT_ID('dbo.EmployeeLoans'))
    CREATE INDEX IX_EmployeeLoans_Tenant_Company_Branch ON dbo.EmployeeLoans (TenantId, CompanyId, BranchId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EmployeeAdvances_Tenant_Company_Branch' AND object_id = OBJECT_ID('dbo.EmployeeAdvances'))
    CREATE INDEX IX_EmployeeAdvances_Tenant_Company_Branch ON dbo.EmployeeAdvances (TenantId, CompanyId, BranchId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LoanPaymentHistories_Loan_Date' AND object_id = OBJECT_ID('dbo.LoanPaymentHistories'))
    CREATE INDEX IX_LoanPaymentHistories_Loan_Date ON dbo.LoanPaymentHistories (EmployeeLoanId, PaymentDate);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AdvancePaymentHistories_Advance_Date' AND object_id = OBJECT_ID('dbo.AdvancePaymentHistories'))
    CREATE INDEX IX_AdvancePaymentHistories_Advance_Date ON dbo.AdvancePaymentHistories (EmployeeAdvanceId, PaymentDate);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LoanApprovalHistories_Loan' AND object_id = OBJECT_ID('dbo.LoanApprovalHistories'))
    CREATE INDEX IX_LoanApprovalHistories_Loan ON dbo.LoanApprovalHistories (EmployeeLoanId, LevelNumber);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AdvanceApprovalHistories_Advance' AND object_id = OBJECT_ID('dbo.AdvanceApprovalHistories'))
    CREATE INDEX IX_AdvanceApprovalHistories_Advance ON dbo.AdvanceApprovalHistories (EmployeeAdvanceId, LevelNumber);
GO
