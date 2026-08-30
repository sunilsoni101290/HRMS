-- ============================================================================
-- Daily Work Entry / Employee Work Tracking module - creates all 9 new
-- tables (Clients, WorkJobs, JobTypes, JobItems, WorkActivities,
-- WorkEntryReasons, DocumentStatuses, DailyWorkLogs, DailyWorkEntries,
-- DailyWorkLogApprovalHistories). Safe to run more than once (every
-- statement is IF NOT EXISTS-guarded), following the same convention as
-- "add EsslIntegrationSettings table.sql" earlier in this project. Run
-- directly against the ERP/Application database. Does not alter any
-- existing table.
-- ============================================================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Clients')
BEGIN
    CREATE TABLE [dbo].[Clients] (
        [Id] NVARCHAR(450) NOT NULL,
        [TenantId] NVARCHAR(450) NULL,
        [Name] NVARCHAR(200) NOT NULL,
        [Code] NVARCHAR(50) NULL,
        [IsDeleted] BIT NOT NULL CONSTRAINT DF_Clients_IsDeleted DEFAULT (0),
        [CreatedOn] DATETIME2 NOT NULL CONSTRAINT DF_Clients_CreatedOn DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] NVARCHAR(MAX) NOT NULL CONSTRAINT DF_Clients_CreatedBy DEFAULT ('System'),
        [ModifiedOn] DATETIME2 NULL,
        [ModifiedBy] NVARCHAR(MAX) NULL,
        [IsActive] BIT NOT NULL CONSTRAINT DF_Clients_IsActive DEFAULT (1),
        CONSTRAINT [PK_Clients] PRIMARY KEY ([Id])
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'WorkJobs')
BEGIN
    CREATE TABLE [dbo].[WorkJobs] (
        [Id] NVARCHAR(450) NOT NULL,
        [TenantId] NVARCHAR(450) NULL,
        [JobNumber] NVARCHAR(50) NOT NULL,
        [JobName] NVARCHAR(200) NOT NULL,
        [ClientId] NVARCHAR(450) NOT NULL,
        [Status] INT NOT NULL CONSTRAINT DF_WorkJobs_Status DEFAULT (1),
        [StartDate] DATETIME2 NULL,
        [EndDate] DATETIME2 NULL,
        [IsDeleted] BIT NOT NULL CONSTRAINT DF_WorkJobs_IsDeleted DEFAULT (0),
        [CreatedOn] DATETIME2 NOT NULL CONSTRAINT DF_WorkJobs_CreatedOn DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] NVARCHAR(MAX) NOT NULL CONSTRAINT DF_WorkJobs_CreatedBy DEFAULT ('System'),
        [ModifiedOn] DATETIME2 NULL,
        [ModifiedBy] NVARCHAR(MAX) NULL,
        [IsActive] BIT NOT NULL CONSTRAINT DF_WorkJobs_IsActive DEFAULT (1),
        CONSTRAINT [PK_WorkJobs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WorkJobs_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [dbo].[Clients]([Id]) ON DELETE NO ACTION
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'JobTypes')
BEGIN
    CREATE TABLE [dbo].[JobTypes] (
        [Id] NVARCHAR(450) NOT NULL,
        [TenantId] NVARCHAR(450) NULL,
        [Name] NVARCHAR(100) NOT NULL,
        [Code] NVARCHAR(30) NULL,
        [DisplayOrder] INT NOT NULL CONSTRAINT DF_JobTypes_DisplayOrder DEFAULT (0),
        [HasDisciplines] BIT NOT NULL CONSTRAINT DF_JobTypes_HasDisciplines DEFAULT (0),
        [IsDeleted] BIT NOT NULL CONSTRAINT DF_JobTypes_IsDeleted DEFAULT (0),
        [CreatedOn] DATETIME2 NOT NULL CONSTRAINT DF_JobTypes_CreatedOn DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] NVARCHAR(MAX) NOT NULL CONSTRAINT DF_JobTypes_CreatedBy DEFAULT ('System'),
        [ModifiedOn] DATETIME2 NULL,
        [ModifiedBy] NVARCHAR(MAX) NULL,
        [IsActive] BIT NOT NULL CONSTRAINT DF_JobTypes_IsActive DEFAULT (1),
        CONSTRAINT [PK_JobTypes] PRIMARY KEY ([Id])
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'JobItems')
BEGIN
    CREATE TABLE [dbo].[JobItems] (
        [Id] NVARCHAR(450) NOT NULL,
        [TenantId] NVARCHAR(450) NULL,
        [WorkJobId] NVARCHAR(450) NOT NULL,
        [Code] NVARCHAR(100) NOT NULL,
        [Description] NVARCHAR(300) NULL,
        [ItemType] INT NOT NULL CONSTRAINT DF_JobItems_ItemType DEFAULT (3),
        [TotalWeightMT] DECIMAL(18,2) NULL,
        [DocumentStatusId] NVARCHAR(450) NULL,
        [IsDeleted] BIT NOT NULL CONSTRAINT DF_JobItems_IsDeleted DEFAULT (0),
        [CreatedOn] DATETIME2 NOT NULL CONSTRAINT DF_JobItems_CreatedOn DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] NVARCHAR(MAX) NOT NULL CONSTRAINT DF_JobItems_CreatedBy DEFAULT ('System'),
        [ModifiedOn] DATETIME2 NULL,
        [ModifiedBy] NVARCHAR(MAX) NULL,
        [IsActive] BIT NOT NULL CONSTRAINT DF_JobItems_IsActive DEFAULT (1),
        CONSTRAINT [PK_JobItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_JobItems_WorkJobs_WorkJobId] FOREIGN KEY ([WorkJobId]) REFERENCES [dbo].[WorkJobs]([Id]) ON DELETE NO ACTION
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'WorkActivities')
BEGIN
    CREATE TABLE [dbo].[WorkActivities] (
        [Id] NVARCHAR(450) NOT NULL,
        [TenantId] NVARCHAR(450) NULL,
        [JobTypeId] NVARCHAR(450) NOT NULL,
        [SkidsDiscipline] INT NULL,
        [Name] NVARCHAR(200) NOT NULL,
        [WorkCategory] INT NOT NULL CONSTRAINT DF_WorkActivities_WorkCategory DEFAULT (1),
        [DisplayOrder] INT NOT NULL CONSTRAINT DF_WorkActivities_DisplayOrder DEFAULT (0),
        [RequiresReason] BIT NOT NULL CONSTRAINT DF_WorkActivities_RequiresReason DEFAULT (0),
        [AllowFreeTextOther] BIT NOT NULL CONSTRAINT DF_WorkActivities_AllowFreeTextOther DEFAULT (0),
        [IsDeleted] BIT NOT NULL CONSTRAINT DF_WorkActivities_IsDeleted DEFAULT (0),
        [CreatedOn] DATETIME2 NOT NULL CONSTRAINT DF_WorkActivities_CreatedOn DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] NVARCHAR(MAX) NOT NULL CONSTRAINT DF_WorkActivities_CreatedBy DEFAULT ('System'),
        [ModifiedOn] DATETIME2 NULL,
        [ModifiedBy] NVARCHAR(MAX) NULL,
        [IsActive] BIT NOT NULL CONSTRAINT DF_WorkActivities_IsActive DEFAULT (1),
        CONSTRAINT [PK_WorkActivities] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WorkActivities_JobTypes_JobTypeId] FOREIGN KEY ([JobTypeId]) REFERENCES [dbo].[JobTypes]([Id]) ON DELETE NO ACTION
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'WorkEntryReasons')
BEGIN
    CREATE TABLE [dbo].[WorkEntryReasons] (
        [Id] NVARCHAR(450) NOT NULL,
        [TenantId] NVARCHAR(450) NULL,
        [Category] INT NOT NULL,
        [Name] NVARCHAR(200) NOT NULL,
        [DisplayOrder] INT NOT NULL CONSTRAINT DF_WorkEntryReasons_DisplayOrder DEFAULT (0),
        [IsDeleted] BIT NOT NULL CONSTRAINT DF_WorkEntryReasons_IsDeleted DEFAULT (0),
        [CreatedOn] DATETIME2 NOT NULL CONSTRAINT DF_WorkEntryReasons_CreatedOn DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] NVARCHAR(MAX) NOT NULL CONSTRAINT DF_WorkEntryReasons_CreatedBy DEFAULT ('System'),
        [ModifiedOn] DATETIME2 NULL,
        [ModifiedBy] NVARCHAR(MAX) NULL,
        [IsActive] BIT NOT NULL CONSTRAINT DF_WorkEntryReasons_IsActive DEFAULT (1),
        CONSTRAINT [PK_WorkEntryReasons] PRIMARY KEY ([Id])
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'DocumentStatuses')
BEGIN
    CREATE TABLE [dbo].[DocumentStatuses] (
        [Id] NVARCHAR(450) NOT NULL,
        [TenantId] NVARCHAR(450) NULL,
        [Code] NVARCHAR(30) NOT NULL,
        [DisplayName] NVARCHAR(150) NOT NULL,
        [DisplayOrder] INT NOT NULL CONSTRAINT DF_DocumentStatuses_DisplayOrder DEFAULT (0),
        [IsDeleted] BIT NOT NULL CONSTRAINT DF_DocumentStatuses_IsDeleted DEFAULT (0),
        [CreatedOn] DATETIME2 NOT NULL CONSTRAINT DF_DocumentStatuses_CreatedOn DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] NVARCHAR(MAX) NOT NULL CONSTRAINT DF_DocumentStatuses_CreatedBy DEFAULT ('System'),
        [ModifiedOn] DATETIME2 NULL,
        [ModifiedBy] NVARCHAR(MAX) NULL,
        [IsActive] BIT NOT NULL CONSTRAINT DF_DocumentStatuses_IsActive DEFAULT (1),
        CONSTRAINT [PK_DocumentStatuses] PRIMARY KEY ([Id])
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_JobItems_DocumentStatuses_DocumentStatusId')
    ALTER TABLE [dbo].[JobItems] ADD CONSTRAINT [FK_JobItems_DocumentStatuses_DocumentStatusId]
        FOREIGN KEY ([DocumentStatusId]) REFERENCES [dbo].[DocumentStatuses]([Id]) ON DELETE NO ACTION;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'DailyWorkLogs')
BEGIN
    CREATE TABLE [dbo].[DailyWorkLogs] (
        [Id] NVARCHAR(450) NOT NULL,
        [TenantId] NVARCHAR(450) NULL,
        [EmployeeId] NVARCHAR(450) NOT NULL,
        [WorkDate] DATETIME2 NOT NULL,
        [Status] INT NOT NULL CONSTRAINT DF_DailyWorkLogs_Status DEFAULT (0),
        [ClockedHours] DECIMAL(5,2) NULL,
        [SubmittedAt] DATETIME2 NULL,
        [SubmittedBy] NVARCHAR(450) NULL,
        [ApprovedAt] DATETIME2 NULL,
        [ApprovedBy] NVARCHAR(450) NULL,
        [RejectedAt] DATETIME2 NULL,
        [RejectedBy] NVARCHAR(450) NULL,
        [RejectionReason] NVARCHAR(500) NULL,
        [IsDeleted] BIT NOT NULL CONSTRAINT DF_DailyWorkLogs_IsDeleted DEFAULT (0),
        [CreatedOn] DATETIME2 NOT NULL CONSTRAINT DF_DailyWorkLogs_CreatedOn DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] NVARCHAR(MAX) NOT NULL CONSTRAINT DF_DailyWorkLogs_CreatedBy DEFAULT ('System'),
        [ModifiedOn] DATETIME2 NULL,
        [ModifiedBy] NVARCHAR(MAX) NULL,
        [IsActive] BIT NOT NULL CONSTRAINT DF_DailyWorkLogs_IsActive DEFAULT (1),
        CONSTRAINT [PK_DailyWorkLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DailyWorkLogs_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [dbo].[Employees]([Id]) ON DELETE NO ACTION
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'DailyWorkEntries')
BEGIN
    CREATE TABLE [dbo].[DailyWorkEntries] (
        [Id] NVARCHAR(450) NOT NULL,
        [TenantId] NVARCHAR(450) NULL,
        [DailyWorkLogId] NVARCHAR(450) NOT NULL,
        [WorkJobId] NVARCHAR(450) NULL,
        [JobTypeId] NVARCHAR(450) NULL,
        [JobItemId] NVARCHAR(450) NULL,
        [WorkActivityId] NVARCHAR(450) NULL,
        [WorkEntryReasonId] NVARCHAR(450) NULL,
        [Hours] DECIMAL(5,2) NOT NULL,
        [Remarks] NVARCHAR(500) NULL,
        [IsDeleted] BIT NOT NULL CONSTRAINT DF_DailyWorkEntries_IsDeleted DEFAULT (0),
        [CreatedOn] DATETIME2 NOT NULL CONSTRAINT DF_DailyWorkEntries_CreatedOn DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] NVARCHAR(MAX) NOT NULL CONSTRAINT DF_DailyWorkEntries_CreatedBy DEFAULT ('System'),
        [ModifiedOn] DATETIME2 NULL,
        [ModifiedBy] NVARCHAR(MAX) NULL,
        [IsActive] BIT NOT NULL CONSTRAINT DF_DailyWorkEntries_IsActive DEFAULT (1),
        CONSTRAINT [PK_DailyWorkEntries] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DailyWorkEntries_DailyWorkLogs_DailyWorkLogId] FOREIGN KEY ([DailyWorkLogId]) REFERENCES [dbo].[DailyWorkLogs]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_DailyWorkEntries_WorkJobs_WorkJobId] FOREIGN KEY ([WorkJobId]) REFERENCES [dbo].[WorkJobs]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DailyWorkEntries_JobTypes_JobTypeId] FOREIGN KEY ([JobTypeId]) REFERENCES [dbo].[JobTypes]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DailyWorkEntries_JobItems_JobItemId] FOREIGN KEY ([JobItemId]) REFERENCES [dbo].[JobItems]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DailyWorkEntries_WorkActivities_WorkActivityId] FOREIGN KEY ([WorkActivityId]) REFERENCES [dbo].[WorkActivities]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DailyWorkEntries_WorkEntryReasons_WorkEntryReasonId] FOREIGN KEY ([WorkEntryReasonId]) REFERENCES [dbo].[WorkEntryReasons]([Id]) ON DELETE NO ACTION
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'DailyWorkLogApprovalHistories')
BEGIN
    CREATE TABLE [dbo].[DailyWorkLogApprovalHistories] (
        [Id] NVARCHAR(450) NOT NULL,
        [TenantId] NVARCHAR(450) NULL,
        [DailyWorkLogId] NVARCHAR(450) NOT NULL,
        [ActionBy] NVARCHAR(450) NOT NULL,
        [Action] INT NOT NULL,
        [Remarks] NVARCHAR(500) NULL,
        [ActionDate] DATETIME2 NOT NULL,
        [IsDeleted] BIT NOT NULL CONSTRAINT DF_DailyWorkLogApprovalHistories_IsDeleted DEFAULT (0),
        [CreatedOn] DATETIME2 NOT NULL CONSTRAINT DF_DailyWorkLogApprovalHistories_CreatedOn DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] NVARCHAR(MAX) NOT NULL CONSTRAINT DF_DailyWorkLogApprovalHistories_CreatedBy DEFAULT ('System'),
        [ModifiedOn] DATETIME2 NULL,
        [ModifiedBy] NVARCHAR(MAX) NULL,
        [IsActive] BIT NOT NULL CONSTRAINT DF_DailyWorkLogApprovalHistories_IsActive DEFAULT (1),
        CONSTRAINT [PK_DailyWorkLogApprovalHistories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DailyWorkLogApprovalHistories_DailyWorkLogs_DailyWorkLogId] FOREIGN KEY ([DailyWorkLogId]) REFERENCES [dbo].[DailyWorkLogs]([Id]) ON DELETE CASCADE
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_WorkJobs_JobNumber' AND object_id = OBJECT_ID('dbo.WorkJobs'))
    CREATE INDEX [IX_WorkJobs_JobNumber] ON [dbo].[WorkJobs]([JobNumber]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_JobItems_WorkJobId' AND object_id = OBJECT_ID('dbo.JobItems'))
    CREATE INDEX [IX_JobItems_WorkJobId] ON [dbo].[JobItems]([WorkJobId]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_WorkActivities_JobTypeId' AND object_id = OBJECT_ID('dbo.WorkActivities'))
    CREATE INDEX [IX_WorkActivities_JobTypeId] ON [dbo].[WorkActivities]([JobTypeId]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DailyWorkLogs_Employee_Date' AND object_id = OBJECT_ID('dbo.DailyWorkLogs'))
    CREATE UNIQUE INDEX [IX_DailyWorkLogs_Employee_Date] ON [dbo].[DailyWorkLogs]([EmployeeId], [WorkDate]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DailyWorkLogs_Tenant_Status' AND object_id = OBJECT_ID('dbo.DailyWorkLogs'))
    CREATE INDEX [IX_DailyWorkLogs_Tenant_Status] ON [dbo].[DailyWorkLogs]([TenantId], [Status]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DailyWorkEntries_DailyWorkLogId' AND object_id = OBJECT_ID('dbo.DailyWorkEntries'))
    CREATE INDEX [IX_DailyWorkEntries_DailyWorkLogId] ON [dbo].[DailyWorkEntries]([DailyWorkLogId]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DailyWorkEntries_WorkJobId_JobItemId' AND object_id = OBJECT_ID('dbo.DailyWorkEntries'))
    CREATE INDEX [IX_DailyWorkEntries_WorkJobId_JobItemId] ON [dbo].[DailyWorkEntries]([WorkJobId], [JobItemId]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DailyWorkLogApprovalHistories_DailyWorkLogId' AND object_id = OBJECT_ID('dbo.DailyWorkLogApprovalHistories'))
    CREATE INDEX [IX_DailyWorkLogApprovalHistories_DailyWorkLogId] ON [dbo].[DailyWorkLogApprovalHistories]([DailyWorkLogId]);
GO

-- Sanity check (uncomment to run manually):
-- SELECT * FROM [dbo].[DailyWorkLogs];
