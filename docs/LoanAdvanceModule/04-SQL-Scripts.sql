/* =============================================================================
   Loan & Advance Module — Phase 4: SQL Scripts
   Target: SQL Server 2022
   -----------------------------------------------------------------------------
   Same convention as the existing repo script
   ("add DeviceKey column to BiometricDevice.sql"): this is the MANUAL /
   reference path. The recommended path is still EF Core migrations once the
   entities from Phase 5 exist:

       dotnet ef migrations add AddLoanAdvanceModule --startup-project ..\API
       dotnet ef database update --startup-project ..\API

   Run this script only if you are provisioning the schema by hand (e.g. a
   fresh environment before the app assemblies are deployed) or need to
   compare against what the migration should produce. Idempotent — safe to
   re-run.
   ========================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;
GO

/* -----------------------------------------------------------------------
   1. LoanTypes
   ----------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'LoanTypes')
BEGIN
    CREATE TABLE dbo.LoanTypes
    (
        Id                          NVARCHAR(50)    NOT NULL CONSTRAINT PK_LoanTypes PRIMARY KEY,
        TenantId                    NVARCHAR(50)    NOT NULL,
        Code                        NVARCHAR(20)    NOT NULL,
        Name                        NVARCHAR(100)   NOT NULL,
        Description                 NVARCHAR(500)   NULL,
        InterestMethod              TINYINT         NOT NULL CONSTRAINT DF_LoanTypes_InterestMethod DEFAULT (1), -- 1=Reducing, 2=Flat
        DefaultInterestRatePercent  DECIMAL(5,2)    NOT NULL CONSTRAINT DF_LoanTypes_InterestRate DEFAULT (0),
        MaxTenureMonths             INT             NOT NULL,
        RequiresGuarantor           BIT             NOT NULL CONSTRAINT DF_LoanTypes_ReqGuarantor DEFAULT (0),
        RequiresCollateral          BIT             NOT NULL CONSTRAINT DF_LoanTypes_ReqCollateral DEFAULT (0),
        IsActive                    BIT             NOT NULL CONSTRAINT DF_LoanTypes_IsActive DEFAULT (1),
        IsDeleted                   BIT             NOT NULL CONSTRAINT DF_LoanTypes_IsDeleted DEFAULT (0),
        CreatedOn                   DATETIME2       NOT NULL CONSTRAINT DF_LoanTypes_CreatedOn DEFAULT (SYSUTCDATETIME()),
        CreatedBy                   NVARCHAR(50)    NOT NULL,
        ModifiedOn                  DATETIME2       NULL,
        ModifiedBy                  NVARCHAR(50)    NULL,
        CONSTRAINT FK_LoanTypes_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id) ON DELETE NO ACTION,
        CONSTRAINT CK_LoanTypes_InterestMethod CHECK (InterestMethod IN (1,2))
    );

    CREATE UNIQUE INDEX UX_LoanTypes_Tenant_Code
        ON dbo.LoanTypes (TenantId, Code)
        WHERE IsDeleted = 0;
END
GO

/* -----------------------------------------------------------------------
   2. AdvanceTypes
   ----------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AdvanceTypes')
BEGIN
    CREATE TABLE dbo.AdvanceTypes
    (
        Id                          NVARCHAR(50)    NOT NULL CONSTRAINT PK_AdvanceTypes PRIMARY KEY,
        TenantId                    NVARCHAR(50)    NOT NULL,
        Code                        NVARCHAR(20)    NOT NULL,
        Name                        NVARCHAR(100)   NOT NULL,
        Description                 NVARCHAR(500)   NULL,
        MaxAmount                   DECIMAL(18,2)   NULL,
        MaxAmountSalaryMultiplier   DECIMAL(5,2)    NULL,
        MaxInstallments             INT             NOT NULL CONSTRAINT DF_AdvanceTypes_MaxInst DEFAULT (1),
        IsInterestFree              BIT             NOT NULL CONSTRAINT DF_AdvanceTypes_InterestFree DEFAULT (1),
        IsActive                    BIT             NOT NULL CONSTRAINT DF_AdvanceTypes_IsActive DEFAULT (1),
        IsDeleted                   BIT             NOT NULL CONSTRAINT DF_AdvanceTypes_IsDeleted DEFAULT (0),
        CreatedOn                   DATETIME2       NOT NULL CONSTRAINT DF_AdvanceTypes_CreatedOn DEFAULT (SYSUTCDATETIME()),
        CreatedBy                   NVARCHAR(50)    NOT NULL,
        ModifiedOn                  DATETIME2       NULL,
        ModifiedBy                  NVARCHAR(50)    NULL,
        CONSTRAINT FK_AdvanceTypes_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id) ON DELETE NO ACTION,
        CONSTRAINT CK_AdvanceTypes_MaxSource CHECK (MaxAmount IS NOT NULL OR MaxAmountSalaryMultiplier IS NOT NULL)
    );

    CREATE UNIQUE INDEX UX_AdvanceTypes_Tenant_Code
        ON dbo.AdvanceTypes (TenantId, Code)
        WHERE IsDeleted = 0;
END
GO

/* -----------------------------------------------------------------------
   3. LoanPolicies
   ----------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'LoanPolicies')
BEGIN
    CREATE TABLE dbo.LoanPolicies
    (
        Id                                  NVARCHAR(50)    NOT NULL CONSTRAINT PK_LoanPolicies PRIMARY KEY,
        TenantId                            NVARCHAR(50)    NOT NULL,
        CompanyId                           NVARCHAR(50)    NULL,
        BranchId                            NVARCHAR(50)    NULL,
        LoanTypeId                          NVARCHAR(50)    NOT NULL,
        MinAmount                           DECIMAL(18,2)   NOT NULL,
        MaxAmount                           DECIMAL(18,2)   NOT NULL,
        MinTenureMonths                     INT             NOT NULL,
        MaxTenureMonths                     INT             NOT NULL,
        InterestRatePercent                 DECIMAL(5,2)    NULL,
        MinServiceMonthsRequired            INT             NOT NULL CONSTRAINT DF_LoanPolicies_MinService DEFAULT (0),
        MaxActiveLoans                      INT             NOT NULL CONSTRAINT DF_LoanPolicies_MaxActive DEFAULT (1),
        MaxDeductionPercentOfNetSalary      DECIMAL(5,2)    NOT NULL CONSTRAINT DF_LoanPolicies_MaxDeduction DEFAULT (40),
        EligibilitySalaryMultiplier         DECIMAL(5,2)    NOT NULL CONSTRAINT DF_LoanPolicies_EligMultiplier DEFAULT (10),
        PreClosurePenaltyPercent            DECIMAL(5,2)    NOT NULL CONSTRAINT DF_LoanPolicies_PenaltyPct DEFAULT (0),
        VersionNumber                       INT             NOT NULL CONSTRAINT DF_LoanPolicies_Version DEFAULT (1),
        EffectiveFrom                       DATETIME2       NOT NULL,
        EffectiveTo                         DATETIME2       NULL,
        IsActive                            BIT             NOT NULL CONSTRAINT DF_LoanPolicies_IsActive DEFAULT (1),
        IsDeleted                           BIT             NOT NULL CONSTRAINT DF_LoanPolicies_IsDeleted DEFAULT (0),
        CreatedOn                           DATETIME2       NOT NULL CONSTRAINT DF_LoanPolicies_CreatedOn DEFAULT (SYSUTCDATETIME()),
        CreatedBy                           NVARCHAR(50)    NOT NULL,
        ModifiedOn                          DATETIME2       NULL,
        ModifiedBy                          NVARCHAR(50)    NULL,
        CONSTRAINT FK_LoanPolicies_Tenants  FOREIGN KEY (TenantId)   REFERENCES dbo.Tenants(Id)    ON DELETE NO ACTION,
        CONSTRAINT FK_LoanPolicies_Companies FOREIGN KEY (CompanyId) REFERENCES dbo.Companies(Id)  ON DELETE NO ACTION,
        CONSTRAINT FK_LoanPolicies_Branches FOREIGN KEY (BranchId)   REFERENCES dbo.Branches(Id)   ON DELETE NO ACTION,
        CONSTRAINT FK_LoanPolicies_LoanTypes FOREIGN KEY (LoanTypeId) REFERENCES dbo.LoanTypes(Id) ON DELETE NO ACTION,
        CONSTRAINT CK_LoanPolicies_AmountRange CHECK (MaxAmount >= MinAmount),
        CONSTRAINT CK_LoanPolicies_TenureRange CHECK (MaxTenureMonths >= MinTenureMonths)
    );

    CREATE INDEX IX_LoanPolicies_Lookup
        ON dbo.LoanPolicies (TenantId, CompanyId, BranchId, LoanTypeId, IsActive);
END
GO

/* -----------------------------------------------------------------------
   4. LoanPolicyApprovalLevels
   ----------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'LoanPolicyApprovalLevels')
BEGIN
    CREATE TABLE dbo.LoanPolicyApprovalLevels
    (
        Id                  NVARCHAR(50)    NOT NULL CONSTRAINT PK_LoanPolicyApprovalLevels PRIMARY KEY,
        LoanPolicyId        NVARCHAR(50)    NOT NULL,
        LevelNumber         INT             NOT NULL,
        ApproverType        TINYINT         NOT NULL, -- 1=ReportingManager, 2=SpecificRole, 3=SpecificUser
        ApproverRoleId      NVARCHAR(50)    NULL,
        ApproverUserId      NVARCHAR(50)    NULL,
        MinAmountThreshold  DECIMAL(18,2)   NOT NULL CONSTRAINT DF_LoanPolicyApprovalLevels_MinAmt DEFAULT (0),
        IsActive            BIT             NOT NULL CONSTRAINT DF_LoanPolicyApprovalLevels_IsActive DEFAULT (1),
        CreatedOn           DATETIME2       NOT NULL CONSTRAINT DF_LoanPolicyApprovalLevels_CreatedOn DEFAULT (SYSUTCDATETIME()),
        CreatedBy           NVARCHAR(50)    NOT NULL,
        ModifiedOn          DATETIME2       NULL,
        ModifiedBy          NVARCHAR(50)    NULL,
        CONSTRAINT FK_LoanPolicyApprovalLevels_Policy FOREIGN KEY (LoanPolicyId) REFERENCES dbo.LoanPolicies(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_LoanPolicyApprovalLevels_Role FOREIGN KEY (ApproverRoleId) REFERENCES dbo.Roles(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_LoanPolicyApprovalLevels_User FOREIGN KEY (ApproverUserId) REFERENCES dbo.Users(Id) ON DELETE NO ACTION,
        CONSTRAINT CK_LoanPolicyApprovalLevels_Type CHECK (ApproverType IN (1,2,3))
    );

    CREATE UNIQUE INDEX UX_LoanPolicyApprovalLevels
        ON dbo.LoanPolicyApprovalLevels (LoanPolicyId, LevelNumber);
END
GO

/* -----------------------------------------------------------------------
   5. EmployeeLoans
   ----------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'EmployeeLoans')
BEGIN
    CREATE TABLE dbo.EmployeeLoans
    (
        Id                      NVARCHAR(50)    NOT NULL CONSTRAINT PK_EmployeeLoans PRIMARY KEY,
        TenantId                NVARCHAR(50)    NOT NULL,
        CompanyId               NVARCHAR(50)    NULL,
        BranchId                NVARCHAR(50)    NULL,
        EmployeeId              NVARCHAR(50)    NOT NULL,
        LoanTypeId              NVARCHAR(50)    NOT NULL,
        LoanPolicyId            NVARCHAR(50)    NOT NULL,
        RequestedAmount         DECIMAL(18,2)   NOT NULL,
        ApprovedAmount          DECIMAL(18,2)   NULL,
        TenureMonths            INT             NOT NULL,
        InterestRatePercent     DECIMAL(5,2)    NOT NULL,
        InterestMethod          TINYINT         NOT NULL,
        Purpose                 NVARCHAR(500)   NULL,
        Status                  TINYINT         NOT NULL CONSTRAINT DF_EmployeeLoans_Status DEFAULT (1), -- 1=Draft
        CurrentApprovalLevel    INT             NOT NULL CONSTRAINT DF_EmployeeLoans_CurLevel DEFAULT (0),
        MakerId                 NVARCHAR(50)    NOT NULL,
        MakerActionOn           DATETIME2       NOT NULL,
        MakerRemarks            NVARCHAR(1000)  NULL,
        DisbursedAmount         DECIMAL(18,2)   NULL,
        DisbursedOn             DATETIME2       NULL,
        DisbursementMode        TINYINT         NULL,
        DisbursementReference   NVARCHAR(100)   NULL,
        OutstandingPrincipal    DECIMAL(18,2)   NOT NULL CONSTRAINT DF_EmployeeLoans_Outstanding DEFAULT (0),
        ClosedOn                DATETIME2       NULL,
        ClosureReason           TINYINT         NULL,
        IsActive                BIT             NOT NULL CONSTRAINT DF_EmployeeLoans_IsActive DEFAULT (1),
        IsDeleted               BIT             NOT NULL CONSTRAINT DF_EmployeeLoans_IsDeleted DEFAULT (0),
        CreatedOn               DATETIME2       NOT NULL CONSTRAINT DF_EmployeeLoans_CreatedOn DEFAULT (SYSUTCDATETIME()),
        CreatedBy               NVARCHAR(50)    NOT NULL,
        ModifiedOn               DATETIME2      NULL,
        ModifiedBy              NVARCHAR(50)    NULL,
        CONSTRAINT FK_EmployeeLoans_Tenants   FOREIGN KEY (TenantId)   REFERENCES dbo.Tenants(Id)   ON DELETE NO ACTION,
        CONSTRAINT FK_EmployeeLoans_Companies FOREIGN KEY (CompanyId) REFERENCES dbo.Companies(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_EmployeeLoans_Branches  FOREIGN KEY (BranchId)  REFERENCES dbo.Branches(Id)  ON DELETE NO ACTION,
        CONSTRAINT FK_EmployeeLoans_Employees FOREIGN KEY (EmployeeId) REFERENCES dbo.Employees(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_EmployeeLoans_LoanTypes FOREIGN KEY (LoanTypeId) REFERENCES dbo.LoanTypes(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_EmployeeLoans_LoanPolicies FOREIGN KEY (LoanPolicyId) REFERENCES dbo.LoanPolicies(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_EmployeeLoans_Maker     FOREIGN KEY (MakerId)    REFERENCES dbo.Users(Id)     ON DELETE NO ACTION,
        CONSTRAINT CK_EmployeeLoans_Status CHECK (Status BETWEEN 1 AND 12),
        CONSTRAINT CK_EmployeeLoans_InterestMethod CHECK (InterestMethod IN (1,2))
    );

    CREATE INDEX IX_EmployeeLoans_Employee_Status ON dbo.EmployeeLoans (EmployeeId, Status);
    CREATE INDEX IX_EmployeeLoans_Tenant_Company_Branch ON dbo.EmployeeLoans (TenantId, CompanyId, BranchId);
    CREATE INDEX IX_EmployeeLoans_Status_Outstanding ON dbo.EmployeeLoans (Status) INCLUDE (OutstandingPrincipal);
END
GO

/* -----------------------------------------------------------------------
   6. LoanApprovalHistories
   ----------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'LoanApprovalHistories')
BEGIN
    CREATE TABLE dbo.LoanApprovalHistories
    (
        Id                          NVARCHAR(50)    NOT NULL CONSTRAINT PK_LoanApprovalHistories PRIMARY KEY,
        EmployeeLoanId              NVARCHAR(50)    NOT NULL,
        LevelNumber                 INT             NOT NULL,
        CheckerId                   NVARCHAR(50)    NOT NULL,
        ActedAsDelegateForUserId    NVARCHAR(50)    NULL,
        Decision                    TINYINT         NOT NULL, -- 1=Approved, 2=Rejected
        Remarks                     NVARCHAR(1000)  NULL,
        ActionOn                    DATETIME2       NOT NULL,
        IsActive                    BIT             NOT NULL CONSTRAINT DF_LoanApprovalHistories_IsActive DEFAULT (1),
        IsDeleted                   BIT             NOT NULL CONSTRAINT DF_LoanApprovalHistories_IsDeleted DEFAULT (0),
        CreatedOn                   DATETIME2       NOT NULL CONSTRAINT DF_LoanApprovalHistories_CreatedOn DEFAULT (SYSUTCDATETIME()),
        CreatedBy                   NVARCHAR(50)    NOT NULL,
        ModifiedOn                  DATETIME2       NULL,
        ModifiedBy                  NVARCHAR(50)    NULL,
        CONSTRAINT FK_LoanApprovalHistories_Loan FOREIGN KEY (EmployeeLoanId) REFERENCES dbo.EmployeeLoans(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_LoanApprovalHistories_Checker FOREIGN KEY (CheckerId) REFERENCES dbo.Users(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_LoanApprovalHistories_Delegate FOREIGN KEY (ActedAsDelegateForUserId) REFERENCES dbo.Users(Id) ON DELETE NO ACTION,
        CONSTRAINT CK_LoanApprovalHistories_Decision CHECK (Decision IN (1,2)),
        CONSTRAINT CK_LoanApprovalHistories_MakerNeChecker CHECK (1 = 1) -- Maker<>Checker enforced in service layer (needs cross-table compare)
    );

    CREATE INDEX IX_LoanApprovalHistories_Loan ON dbo.LoanApprovalHistories (EmployeeLoanId, LevelNumber);
END
GO

/* -----------------------------------------------------------------------
   7. LoanEmiSchedules
   ----------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'LoanEmiSchedules')
BEGIN
    CREATE TABLE dbo.LoanEmiSchedules
    (
        Id                  NVARCHAR(50)    NOT NULL CONSTRAINT PK_LoanEmiSchedules PRIMARY KEY,
        EmployeeLoanId      NVARCHAR(50)    NOT NULL,
        InstallmentNumber   INT             NOT NULL,
        DueDate             DATE            NOT NULL,
        OpeningBalance      DECIMAL(18,2)   NOT NULL,
        PrincipalComponent  DECIMAL(18,2)   NOT NULL,
        InterestComponent   DECIMAL(18,2)   NOT NULL,
        EmiAmount           DECIMAL(18,2)   NOT NULL,
        ClosingBalance      DECIMAL(18,2)   NOT NULL,
        Status              TINYINT         NOT NULL CONSTRAINT DF_LoanEmiSchedules_Status DEFAULT (1), -- 1=Pending
        RecoveredOn         DATETIME2       NULL,
        PayrollRunId        NVARCHAR(50)    NULL,
        IsActive            BIT             NOT NULL CONSTRAINT DF_LoanEmiSchedules_IsActive DEFAULT (1),
        IsDeleted           BIT             NOT NULL CONSTRAINT DF_LoanEmiSchedules_IsDeleted DEFAULT (0),
        CreatedOn           DATETIME2       NOT NULL CONSTRAINT DF_LoanEmiSchedules_CreatedOn DEFAULT (SYSUTCDATETIME()),
        CreatedBy           NVARCHAR(50)    NOT NULL,
        ModifiedOn          DATETIME2       NULL,
        ModifiedBy          NVARCHAR(50)    NULL,
        CONSTRAINT FK_LoanEmiSchedules_Loan FOREIGN KEY (EmployeeLoanId) REFERENCES dbo.EmployeeLoans(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_LoanEmiSchedules_Payroll FOREIGN KEY (PayrollRunId) REFERENCES dbo.Payrolls(Id) ON DELETE NO ACTION,
        CONSTRAINT CK_LoanEmiSchedules_Status CHECK (Status BETWEEN 1 AND 5)
    );

    CREATE UNIQUE INDEX UX_LoanEmiSchedules ON dbo.LoanEmiSchedules (EmployeeLoanId, InstallmentNumber);
    CREATE INDEX IX_LoanEmiSchedules_DueTracking ON dbo.LoanEmiSchedules (Status, DueDate);
END
GO

/* -----------------------------------------------------------------------
   8. LoanPaymentHistories
   ----------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'LoanPaymentHistories')
BEGIN
    CREATE TABLE dbo.LoanPaymentHistories
    (
        Id                  NVARCHAR(50)    NOT NULL CONSTRAINT PK_LoanPaymentHistories PRIMARY KEY,
        EmployeeLoanId      NVARCHAR(50)    NOT NULL,
        LoanEmiScheduleId   NVARCHAR(50)    NULL,
        PaymentSource       TINYINT         NOT NULL, -- 1=PayrollDeduction, 2=ManualReceipt, 3=PreClosure
        AmountPaid          DECIMAL(18,2)   NOT NULL,
        PrincipalPaid       DECIMAL(18,2)   NOT NULL,
        InterestPaid        DECIMAL(18,2)   NOT NULL,
        PaymentDate         DATETIME2       NOT NULL,
        PayrollRunId        NVARCHAR(50)    NULL,
        ReceiptReference    NVARCHAR(100)   NULL,
        Remarks             NVARCHAR(500)   NULL,
        IsActive            BIT             NOT NULL CONSTRAINT DF_LoanPaymentHistories_IsActive DEFAULT (1),
        IsDeleted           BIT             NOT NULL CONSTRAINT DF_LoanPaymentHistories_IsDeleted DEFAULT (0),
        CreatedOn           DATETIME2       NOT NULL CONSTRAINT DF_LoanPaymentHistories_CreatedOn DEFAULT (SYSUTCDATETIME()),
        CreatedBy           NVARCHAR(50)    NOT NULL,
        ModifiedOn          DATETIME2       NULL,
        ModifiedBy          NVARCHAR(50)    NULL,
        CONSTRAINT FK_LoanPaymentHistories_Loan FOREIGN KEY (EmployeeLoanId) REFERENCES dbo.EmployeeLoans(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_LoanPaymentHistories_Emi FOREIGN KEY (LoanEmiScheduleId) REFERENCES dbo.LoanEmiSchedules(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_LoanPaymentHistories_Payroll FOREIGN KEY (PayrollRunId) REFERENCES dbo.Payrolls(Id) ON DELETE NO ACTION,
        CONSTRAINT CK_LoanPaymentHistories_Source CHECK (PaymentSource IN (1,2,3))
    );

    CREATE INDEX IX_LoanPaymentHistories_Loan_Date ON dbo.LoanPaymentHistories (EmployeeLoanId, PaymentDate);
END
GO

/* -----------------------------------------------------------------------
   9. EmployeeAdvances
   ----------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'EmployeeAdvances')
BEGIN
    CREATE TABLE dbo.EmployeeAdvances
    (
        Id                      NVARCHAR(50)    NOT NULL CONSTRAINT PK_EmployeeAdvances PRIMARY KEY,
        TenantId                NVARCHAR(50)    NOT NULL,
        CompanyId               NVARCHAR(50)    NULL,
        BranchId                NVARCHAR(50)    NULL,
        EmployeeId              NVARCHAR(50)    NOT NULL,
        AdvanceTypeId           NVARCHAR(50)    NOT NULL,
        RequestedAmount         DECIMAL(18,2)   NOT NULL,
        ApprovedAmount          DECIMAL(18,2)   NULL,
        Installments            INT             NOT NULL,
        Purpose                 NVARCHAR(500)   NULL,
        Status                  TINYINT         NOT NULL CONSTRAINT DF_EmployeeAdvances_Status DEFAULT (1),
        CurrentApprovalLevel    INT             NOT NULL CONSTRAINT DF_EmployeeAdvances_CurLevel DEFAULT (0),
        MakerId                 NVARCHAR(50)    NOT NULL,
        MakerActionOn           DATETIME2       NOT NULL,
        MakerRemarks            NVARCHAR(1000)  NULL,
        DisbursedAmount         DECIMAL(18,2)   NULL,
        DisbursedOn             DATETIME2       NULL,
        DisbursementMode        TINYINT         NULL,
        DisbursementReference   NVARCHAR(100)   NULL,
        OutstandingAmount       DECIMAL(18,2)   NOT NULL CONSTRAINT DF_EmployeeAdvances_Outstanding DEFAULT (0),
        ClosedOn                DATETIME2       NULL,
        IsActive                BIT             NOT NULL CONSTRAINT DF_EmployeeAdvances_IsActive DEFAULT (1),
        IsDeleted               BIT             NOT NULL CONSTRAINT DF_EmployeeAdvances_IsDeleted DEFAULT (0),
        CreatedOn               DATETIME2       NOT NULL CONSTRAINT DF_EmployeeAdvances_CreatedOn DEFAULT (SYSUTCDATETIME()),
        CreatedBy               NVARCHAR(50)    NOT NULL,
        ModifiedOn              DATETIME2       NULL,
        ModifiedBy              NVARCHAR(50)    NULL,
        CONSTRAINT FK_EmployeeAdvances_Tenants   FOREIGN KEY (TenantId)   REFERENCES dbo.Tenants(Id)   ON DELETE NO ACTION,
        CONSTRAINT FK_EmployeeAdvances_Companies FOREIGN KEY (CompanyId) REFERENCES dbo.Companies(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_EmployeeAdvances_Branches  FOREIGN KEY (BranchId)  REFERENCES dbo.Branches(Id)  ON DELETE NO ACTION,
        CONSTRAINT FK_EmployeeAdvances_Employees FOREIGN KEY (EmployeeId) REFERENCES dbo.Employees(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_EmployeeAdvances_AdvanceTypes FOREIGN KEY (AdvanceTypeId) REFERENCES dbo.AdvanceTypes(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_EmployeeAdvances_Maker     FOREIGN KEY (MakerId)    REFERENCES dbo.Users(Id)     ON DELETE NO ACTION,
        CONSTRAINT CK_EmployeeAdvances_Status CHECK (Status BETWEEN 1 AND 9)
    );

    CREATE INDEX IX_EmployeeAdvances_Employee_Status ON dbo.EmployeeAdvances (EmployeeId, Status);
    CREATE INDEX IX_EmployeeAdvances_Tenant_Company_Branch ON dbo.EmployeeAdvances (TenantId, CompanyId, BranchId);
END
GO

/* -----------------------------------------------------------------------
   10. AdvanceApprovalHistories
   ----------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AdvanceApprovalHistories')
BEGIN
    CREATE TABLE dbo.AdvanceApprovalHistories
    (
        Id                          NVARCHAR(50)    NOT NULL CONSTRAINT PK_AdvanceApprovalHistories PRIMARY KEY,
        EmployeeAdvanceId           NVARCHAR(50)    NOT NULL,
        LevelNumber                 INT             NOT NULL,
        CheckerId                   NVARCHAR(50)    NOT NULL,
        ActedAsDelegateForUserId    NVARCHAR(50)    NULL,
        Decision                    TINYINT         NOT NULL,
        Remarks                     NVARCHAR(1000)  NULL,
        ActionOn                    DATETIME2       NOT NULL,
        IsActive                    BIT             NOT NULL CONSTRAINT DF_AdvanceApprovalHistories_IsActive DEFAULT (1),
        IsDeleted                   BIT             NOT NULL CONSTRAINT DF_AdvanceApprovalHistories_IsDeleted DEFAULT (0),
        CreatedOn                   DATETIME2       NOT NULL CONSTRAINT DF_AdvanceApprovalHistories_CreatedOn DEFAULT (SYSUTCDATETIME()),
        CreatedBy                   NVARCHAR(50)    NOT NULL,
        ModifiedOn                  DATETIME2       NULL,
        ModifiedBy                  NVARCHAR(50)    NULL,
        CONSTRAINT FK_AdvanceApprovalHistories_Advance FOREIGN KEY (EmployeeAdvanceId) REFERENCES dbo.EmployeeAdvances(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_AdvanceApprovalHistories_Checker FOREIGN KEY (CheckerId) REFERENCES dbo.Users(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_AdvanceApprovalHistories_Delegate FOREIGN KEY (ActedAsDelegateForUserId) REFERENCES dbo.Users(Id) ON DELETE NO ACTION,
        CONSTRAINT CK_AdvanceApprovalHistories_Decision CHECK (Decision IN (1,2))
    );

    CREATE INDEX IX_AdvanceApprovalHistories_Advance ON dbo.AdvanceApprovalHistories (EmployeeAdvanceId, LevelNumber);
END
GO

/* -----------------------------------------------------------------------
   11. AdvanceInstallments
   ----------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AdvanceInstallments')
BEGIN
    CREATE TABLE dbo.AdvanceInstallments
    (
        Id                  NVARCHAR(50)    NOT NULL CONSTRAINT PK_AdvanceInstallments PRIMARY KEY,
        EmployeeAdvanceId   NVARCHAR(50)    NOT NULL,
        InstallmentNumber   INT             NOT NULL,
        DueDate             DATE            NOT NULL,
        InstallmentAmount   DECIMAL(18,2)   NOT NULL,
        Status              TINYINT         NOT NULL CONSTRAINT DF_AdvanceInstallments_Status DEFAULT (1),
        RecoveredOn         DATETIME2       NULL,
        PayrollRunId        NVARCHAR(50)    NULL,
        IsActive            BIT             NOT NULL CONSTRAINT DF_AdvanceInstallments_IsActive DEFAULT (1),
        IsDeleted           BIT             NOT NULL CONSTRAINT DF_AdvanceInstallments_IsDeleted DEFAULT (0),
        CreatedOn           DATETIME2       NOT NULL CONSTRAINT DF_AdvanceInstallments_CreatedOn DEFAULT (SYSUTCDATETIME()),
        CreatedBy           NVARCHAR(50)    NOT NULL,
        ModifiedOn          DATETIME2       NULL,
        ModifiedBy          NVARCHAR(50)    NULL,
        CONSTRAINT FK_AdvanceInstallments_Advance FOREIGN KEY (EmployeeAdvanceId) REFERENCES dbo.EmployeeAdvances(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_AdvanceInstallments_Payroll FOREIGN KEY (PayrollRunId) REFERENCES dbo.Payrolls(Id) ON DELETE NO ACTION,
        CONSTRAINT CK_AdvanceInstallments_Status CHECK (Status BETWEEN 1 AND 5)
    );

    CREATE UNIQUE INDEX UX_AdvanceInstallments ON dbo.AdvanceInstallments (EmployeeAdvanceId, InstallmentNumber);
    CREATE INDEX IX_AdvanceInstallments_DueTracking ON dbo.AdvanceInstallments (Status, DueDate);
END
GO

/* -----------------------------------------------------------------------
   12. AdvancePaymentHistories
   ----------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AdvancePaymentHistories')
BEGIN
    CREATE TABLE dbo.AdvancePaymentHistories
    (
        Id                      NVARCHAR(50)    NOT NULL CONSTRAINT PK_AdvancePaymentHistories PRIMARY KEY,
        EmployeeAdvanceId       NVARCHAR(50)    NOT NULL,
        AdvanceInstallmentId    NVARCHAR(50)    NULL,
        PaymentSource           TINYINT         NOT NULL,
        AmountPaid              DECIMAL(18,2)   NOT NULL,
        PaymentDate             DATETIME2       NOT NULL,
        PayrollRunId            NVARCHAR(50)    NULL,
        ReceiptReference        NVARCHAR(100)   NULL,
        Remarks                 NVARCHAR(500)   NULL,
        IsActive                BIT             NOT NULL CONSTRAINT DF_AdvancePaymentHistories_IsActive DEFAULT (1),
        IsDeleted               BIT             NOT NULL CONSTRAINT DF_AdvancePaymentHistories_IsDeleted DEFAULT (0),
        CreatedOn               DATETIME2       NOT NULL CONSTRAINT DF_AdvancePaymentHistories_CreatedOn DEFAULT (SYSUTCDATETIME()),
        CreatedBy               NVARCHAR(50)    NOT NULL,
        ModifiedOn              DATETIME2       NULL,
        ModifiedBy              NVARCHAR(50)    NULL,
        CONSTRAINT FK_AdvancePaymentHistories_Advance FOREIGN KEY (EmployeeAdvanceId) REFERENCES dbo.EmployeeAdvances(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_AdvancePaymentHistories_Installment FOREIGN KEY (AdvanceInstallmentId) REFERENCES dbo.AdvanceInstallments(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_AdvancePaymentHistories_Payroll FOREIGN KEY (PayrollRunId) REFERENCES dbo.Payrolls(Id) ON DELETE NO ACTION,
        CONSTRAINT CK_AdvancePaymentHistories_Source CHECK (PaymentSource IN (1,2,3))
    );

    CREATE INDEX IX_AdvancePaymentHistories_Advance_Date ON dbo.AdvancePaymentHistories (EmployeeAdvanceId, PaymentDate);
END
GO

/* -----------------------------------------------------------------------
   13. LoanAdvanceAttachments (polymorphic: EntityType 1=Loan, 2=Advance)
   ----------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'LoanAdvanceAttachments')
BEGIN
    CREATE TABLE dbo.LoanAdvanceAttachments
    (
        Id              NVARCHAR(50)    NOT NULL CONSTRAINT PK_LoanAdvanceAttachments PRIMARY KEY,
        EntityType      TINYINT         NOT NULL, -- 1=Loan, 2=Advance
        EntityId        NVARCHAR(50)    NOT NULL,
        FileName        NVARCHAR(255)   NOT NULL,
        FilePath        NVARCHAR(500)   NOT NULL,
        ContentType     NVARCHAR(100)   NOT NULL,
        FileSizeBytes   BIGINT          NOT NULL,
        UploadedBy      NVARCHAR(50)    NOT NULL,
        IsActive        BIT             NOT NULL CONSTRAINT DF_LoanAdvanceAttachments_IsActive DEFAULT (1),
        IsDeleted       BIT             NOT NULL CONSTRAINT DF_LoanAdvanceAttachments_IsDeleted DEFAULT (0),
        CreatedOn       DATETIME2       NOT NULL CONSTRAINT DF_LoanAdvanceAttachments_CreatedOn DEFAULT (SYSUTCDATETIME()),
        CreatedBy       NVARCHAR(50)    NOT NULL,
        ModifiedOn      DATETIME2       NULL,
        ModifiedBy      NVARCHAR(50)    NULL,
        CONSTRAINT FK_LoanAdvanceAttachments_Uploader FOREIGN KEY (UploadedBy) REFERENCES dbo.Users(Id) ON DELETE NO ACTION,
        CONSTRAINT CK_LoanAdvanceAttachments_EntityType CHECK (EntityType IN (1,2)),
        CONSTRAINT CK_LoanAdvanceAttachments_FileSize CHECK (FileSizeBytes > 0 AND FileSizeBytes <= 10485760) -- 10 MB cap
    );

    CREATE INDEX IX_LoanAdvanceAttachments_Entity ON dbo.LoanAdvanceAttachments (EntityType, EntityId);
END
GO

/* -----------------------------------------------------------------------
   14. LoanAdvanceAuditLogs (append-only, no soft-delete, no BaseEntity)
   ----------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'LoanAdvanceAuditLogs')
BEGIN
    CREATE TABLE dbo.LoanAdvanceAuditLogs
    (
        Id              NVARCHAR(50)    NOT NULL CONSTRAINT PK_LoanAdvanceAuditLogs PRIMARY KEY,
        EntityType      NVARCHAR(50)    NOT NULL,
        EntityId        NVARCHAR(50)    NOT NULL,
        Action          NVARCHAR(50)    NOT NULL,
        OldValuesJson   NVARCHAR(MAX)   NULL,
        NewValuesJson   NVARCHAR(MAX)   NULL,
        PerformedBy     NVARCHAR(50)    NOT NULL,
        PerformedOn     DATETIME2       NOT NULL CONSTRAINT DF_LoanAdvanceAuditLogs_PerformedOn DEFAULT (SYSUTCDATETIME()),
        IpAddress       NVARCHAR(50)    NULL,
        TenantId        NVARCHAR(50)    NOT NULL,
        CONSTRAINT FK_LoanAdvanceAuditLogs_User FOREIGN KEY (PerformedBy) REFERENCES dbo.Users(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_LoanAdvanceAuditLogs_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id) ON DELETE NO ACTION
    );

    CREATE INDEX IX_LoanAdvanceAuditLogs_Entity ON dbo.LoanAdvanceAuditLogs (EntityType, EntityId, PerformedOn DESC);
END
GO

COMMIT TRANSACTION;
GO

/* =============================================================================
   SEED DATA — example Loan/Advance Types (safe to skip / customize per tenant)
   Replace @TenantId / @CreatedBy with real values, or drive this from the
   DbSeeder.cs pattern already used elsewhere in the repo instead of raw SQL.
   ========================================================================== */

/*
DECLARE @TenantId NVARCHAR(50) = N'<tenant-id-here>';
DECLARE @CreatedBy NVARCHAR(50) = N'SYSTEM';

IF NOT EXISTS (SELECT 1 FROM dbo.LoanTypes WHERE TenantId = @TenantId AND Code = 'PERSONAL')
BEGIN
    INSERT INTO dbo.LoanTypes
        (Id, TenantId, Code, Name, Description, InterestMethod, DefaultInterestRatePercent, MaxTenureMonths, RequiresGuarantor, RequiresCollateral, CreatedBy)
    VALUES
        (N'LNT-0000000001', @TenantId, 'PERSONAL', 'Personal Loan', 'General-purpose employee personal loan', 1, 10.50, 36, 0, 0, @CreatedBy),
        (N'LNT-0000000002', @TenantId, 'VEHICLE',  'Vehicle Loan',  'Two/four-wheeler purchase loan',        1, 8.00,  48, 1, 1, @CreatedBy),
        (N'LNT-0000000003', @TenantId, 'EMERGENCY','Emergency Loan','Fast-tracked, single-level approval',    1, 6.00,  12, 0, 0, @CreatedBy);
END

IF NOT EXISTS (SELECT 1 FROM dbo.AdvanceTypes WHERE TenantId = @TenantId AND Code = 'SALARY')
BEGIN
    INSERT INTO dbo.AdvanceTypes
        (Id, TenantId, Code, Name, Description, MaxAmountSalaryMultiplier, MaxInstallments, IsInterestFree, CreatedBy)
    VALUES
        (N'ADT-0000000001', @TenantId, 'SALARY',   'Salary Advance',   'Advance against next month salary', 1.0, 1, 1, @CreatedBy),
        (N'ADT-0000000002', @TenantId, 'FESTIVAL', 'Festival Advance', 'Seasonal advance, festival season',  0.5, 3, 1, @CreatedBy),
        (N'ADT-0000000003', @TenantId, 'MEDICAL',  'Medical Advance',  'Medical emergency advance',          1.5, 6, 1, @CreatedBy);
END
*/
