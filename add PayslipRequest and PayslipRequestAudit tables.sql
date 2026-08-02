-- Adds the PayslipRequests and PayslipRequestAudits tables backing the new
-- Payslip Request approval workflow (Employee -> Reporting Manager ->
-- Finance). See Domain/Entities/PayslipRequest.cs and
-- Domain/Entities/PayslipRequestAudit.cs.
-- Run this once against the ERP database, OR let EF Core generate the
-- equivalent migration by running (from the Infrastructure project folder):
--   dotnet ef migrations add AddPayslipRequestWorkflow --startup-project ..\API
--   dotnet ef database update --startup-project ..\API
-- If you use the migration route instead, skip this script.

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PayslipRequests' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.PayslipRequests
    (
        Id                  NVARCHAR(450)   NOT NULL,
        EmployeeId          NVARCHAR(450)   NOT NULL,
        PayrollId           NVARCHAR(450)   NOT NULL,
        PayrollYear         INT             NOT NULL,
        PayrollMonth        INT             NOT NULL,
        Status              INT             NOT NULL,

        ManagerRemarks      NVARCHAR(500)   NULL,
        ManagerActionBy     NVARCHAR(450)   NULL,
        ManagerActionOn     DATETIME2       NULL,

        FinanceRemarks      NVARCHAR(500)   NULL,
        FinanceActionBy     NVARCHAR(450)   NULL,
        FinanceActionOn     DATETIME2       NULL,

        DocumentUrl         NVARCHAR(MAX)   NULL,
        DocumentFileName    NVARCHAR(MAX)   NULL,
        GeneratedBy         NVARCHAR(450)   NULL,
        GeneratedOn         DATETIME2       NULL,

        CompletedBy         NVARCHAR(450)   NULL,
        CompletedOn         DATETIME2       NULL,

        TenantId            NVARCHAR(450)   NULL,
        IsDeleted           BIT             NOT NULL CONSTRAINT DF_PayslipRequests_IsDeleted DEFAULT (0),
        CreatedOn           DATETIME2       NOT NULL,
        CreatedBy           NVARCHAR(MAX)   NOT NULL,
        ModifiedOn          DATETIME2       NULL,
        ModifiedBy          NVARCHAR(MAX)   NULL,
        IsActive            BIT             NOT NULL CONSTRAINT DF_PayslipRequests_IsActive DEFAULT (1),

        CONSTRAINT PK_PayslipRequests PRIMARY KEY (Id),
        CONSTRAINT FK_PayslipRequests_Employees_EmployeeId FOREIGN KEY (EmployeeId)
            REFERENCES dbo.Employees (Id) ON DELETE NO ACTION,
        CONSTRAINT FK_PayslipRequests_Payrolls_PayrollId FOREIGN KEY (PayrollId)
            REFERENCES dbo.Payrolls (Id) ON DELETE NO ACTION
    );

    CREATE INDEX IX_PayslipRequests_EmployeeId ON dbo.PayslipRequests (EmployeeId);
    CREATE INDEX IX_PayslipRequests_PayrollId ON dbo.PayslipRequests (PayrollId);
    CREATE INDEX IX_PayslipRequests_TenantId_Status ON dbo.PayslipRequests (TenantId, Status);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PayslipRequestAudits' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.PayslipRequestAudits
    (
        Id                  NVARCHAR(450)   NOT NULL,
        PayslipRequestId    NVARCHAR(450)   NOT NULL,
        Action              NVARCHAR(MAX)   NOT NULL,
        PerformedBy         NVARCHAR(450)   NOT NULL,
        Remarks             NVARCHAR(500)   NULL,
        PerformedOn         DATETIME2       NOT NULL,

        TenantId            NVARCHAR(450)   NULL,
        IsDeleted           BIT             NOT NULL CONSTRAINT DF_PayslipRequestAudits_IsDeleted DEFAULT (0),
        CreatedOn           DATETIME2       NOT NULL,
        CreatedBy           NVARCHAR(MAX)   NOT NULL,
        ModifiedOn          DATETIME2       NULL,
        ModifiedBy          NVARCHAR(MAX)   NULL,
        IsActive            BIT             NOT NULL CONSTRAINT DF_PayslipRequestAudits_IsActive DEFAULT (1),

        CONSTRAINT PK_PayslipRequestAudits PRIMARY KEY (Id),
        CONSTRAINT FK_PayslipRequestAudits_PayslipRequests_PayslipRequestId FOREIGN KEY (PayslipRequestId)
            REFERENCES dbo.PayslipRequests (Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_PayslipRequestAudits_PayslipRequestId ON dbo.PayslipRequestAudits (PayslipRequestId);
END
GO
