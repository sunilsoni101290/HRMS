-- Adds the new Work From Home Request table, plus the MaxWfhDaysPerMonth
-- column on AttendancePolicies.
-- Run this once against the ERP database, OR let EF Core generate the
-- equivalent migration by running (from the Infrastructure project folder):
--   dotnet ef migrations add AddWfhRequestAndPolicyLimit --startup-project ..\API
--   dotnet ef database update --startup-project ..\API
-- If you use the migration route instead, skip this script.

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'WfhRequests' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.WfhRequests
    (
        Id              NVARCHAR(450)   NOT NULL,
        EmployeeId      NVARCHAR(450)   NOT NULL,
        FromDate        DATETIME2       NOT NULL,
        ToDate          DATETIME2       NOT NULL,
        TotalDays       INT             NOT NULL,
        Reason          NVARCHAR(MAX)   NOT NULL,
        Status          INT             NOT NULL,
        ApprovedBy      NVARCHAR(MAX)   NULL,
        ApprovedOn      DATETIME2       NULL,
        RejectedBy      NVARCHAR(MAX)   NULL,
        RejectedReason  NVARCHAR(MAX)   NULL,
        RejectedOn      DATETIME2       NULL,
        CancelledOn     DATETIME2       NULL,
        CancelledBy     NVARCHAR(MAX)   NULL,
        Remarks         NVARCHAR(500)   NULL,
        TenantId        NVARCHAR(450)   NULL,
        IsDeleted       BIT             NOT NULL CONSTRAINT DF_WfhRequests_IsDeleted DEFAULT (0),
        CreatedOn       DATETIME2       NOT NULL,
        CreatedBy       NVARCHAR(MAX)   NOT NULL,
        ModifiedOn      DATETIME2       NULL,
        ModifiedBy      NVARCHAR(MAX)   NULL,
        IsActive        BIT             NOT NULL CONSTRAINT DF_WfhRequests_IsActive DEFAULT (1),

        CONSTRAINT PK_WfhRequests PRIMARY KEY (Id),
        CONSTRAINT FK_WfhRequests_Employees_EmployeeId FOREIGN KEY (EmployeeId)
            REFERENCES dbo.Employees (Id) ON DELETE NO ACTION
    );

    CREATE INDEX IX_WfhRequests_EmployeeId ON dbo.WfhRequests (EmployeeId);
    CREATE INDEX IX_WfhRequests_TenantId_Status ON dbo.WfhRequests (TenantId, Status);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.AttendancePolicies')
      AND name = 'MaxWfhDaysPerMonth'
)
BEGIN
    ALTER TABLE dbo.AttendancePolicies
    ADD MaxWfhDaysPerMonth INT NOT NULL CONSTRAINT DF_AttendancePolicies_MaxWfhDaysPerMonth DEFAULT (0);
END
GO
