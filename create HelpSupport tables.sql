-- Creates the tables needed for the Help & Support feature
-- (SupportTickets, SupportTicketReplies, FaqItems).
--
-- Alternative (if you prefer EF migrations):
--   dotnet ef migrations add AddHelpAndSupport --project Infrastructure --startup-project API
--   dotnet ef database update --project Infrastructure --startup-project API

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SupportTickets')
BEGIN
    CREATE TABLE dbo.SupportTickets (
        Id              NVARCHAR(50)    NOT NULL PRIMARY KEY,
        TenantId        NVARCHAR(50)    NULL,
        CompanyId       NVARCHAR(50)    NULL,
        BranchId        NVARCHAR(50)    NULL,
        EmployeeId      NVARCHAR(50)    NOT NULL,
        Subject         NVARCHAR(200)   NOT NULL,
        Description     NVARCHAR(MAX)   NOT NULL,
        Category        INT             NOT NULL DEFAULT 6, -- General
        Priority        INT             NOT NULL DEFAULT 2, -- Medium
        Status          INT             NOT NULL DEFAULT 1, -- Open
        AttachmentUrl   NVARCHAR(500)   NULL,
        AssignedToUserId NVARCHAR(50)   NULL,
        ResolvedDate    DATETIME2       NULL,
        ResolvedBy      NVARCHAR(50)    NULL,
        IsDeleted       BIT             NOT NULL DEFAULT 0,
        IsActive        BIT             NOT NULL DEFAULT 1,
        CreatedOn       DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy       NVARCHAR(50)    NULL,
        ModifiedOn      DATETIME2       NULL,
        ModifiedBy      NVARCHAR(50)    NULL
    );

    CREATE INDEX IX_SupportTickets_EmployeeId ON dbo.SupportTickets(EmployeeId);
    CREATE INDEX IX_SupportTickets_Status ON dbo.SupportTickets(Status);
    CREATE INDEX IX_SupportTickets_TenantId ON dbo.SupportTickets(TenantId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SupportTicketReplies')
BEGIN
    CREATE TABLE dbo.SupportTicketReplies (
        Id              NVARCHAR(50)    NOT NULL PRIMARY KEY,
        TenantId        NVARCHAR(50)    NULL,
        SupportTicketId NVARCHAR(50)    NOT NULL,
        RepliedByUserId NVARCHAR(50)    NOT NULL,
        Message         NVARCHAR(MAX)   NOT NULL,
        IsDeleted       BIT             NOT NULL DEFAULT 0,
        IsActive        BIT             NOT NULL DEFAULT 1,
        CreatedOn       DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy       NVARCHAR(50)    NULL,
        ModifiedOn      DATETIME2       NULL,
        ModifiedBy      NVARCHAR(50)    NULL,
        CONSTRAINT FK_SupportTicketReplies_SupportTickets
            FOREIGN KEY (SupportTicketId) REFERENCES dbo.SupportTickets(Id)
    );

    CREATE INDEX IX_SupportTicketReplies_SupportTicketId ON dbo.SupportTicketReplies(SupportTicketId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'FaqItems')
BEGIN
    CREATE TABLE dbo.FaqItems (
        Id              NVARCHAR(50)    NOT NULL PRIMARY KEY,
        TenantId        NVARCHAR(50)    NULL,
        CompanyId       NVARCHAR(50)    NULL,
        Category        NVARCHAR(100)   NOT NULL,
        Question        NVARCHAR(300)   NOT NULL,
        Answer          NVARCHAR(MAX)   NOT NULL,
        DisplayOrder    INT             NOT NULL DEFAULT 0,
        IsDeleted       BIT             NOT NULL DEFAULT 0,
        IsActive        BIT             NOT NULL DEFAULT 1,
        CreatedOn       DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy       NVARCHAR(50)    NULL,
        ModifiedOn      DATETIME2       NULL,
        ModifiedBy      NVARCHAR(50)    NULL
    );

    CREATE INDEX IX_FaqItems_TenantId ON dbo.FaqItems(TenantId);
END
GO
