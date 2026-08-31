-- ============================================================================
-- Employee Job/Work Assignment module - Manager -> Employee assignment layer
-- between the Job master and Daily Work Entry. Safe to run more than once
-- (every statement is IF NOT EXISTS-guarded). Does not alter or remove any
-- existing table/column/data.
-- ============================================================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'EmployeeWorkAssignments')
BEGIN
    CREATE TABLE [dbo].[EmployeeWorkAssignments] (
        [Id] NVARCHAR(450) NOT NULL PRIMARY KEY,
        [TenantId] NVARCHAR(450) NULL,
        [EmployeeId] NVARCHAR(450) NOT NULL,
        [AssignedBy] NVARCHAR(450) NOT NULL,
        [WorkJobId] NVARCHAR(450) NOT NULL,
        [JobTypeId] NVARCHAR(450) NOT NULL,
        [JobItemId] NVARCHAR(450) NULL,
        [WorkActivityId] NVARCHAR(450) NULL,
        [AssignmentType] INT NOT NULL DEFAULT 1,
        [Priority] INT NOT NULL DEFAULT 2,
        [Status] INT NOT NULL DEFAULT 1,
        [StartDate] DATETIME2 NULL,
        [ExpectedEndDate] DATETIME2 NULL,
        [EstimatedHours] DECIMAL(18,2) NULL,
        [Instructions] NVARCHAR(1000) NULL,
        [AcceptedAt] DATETIME2 NULL,
        [StartedAt] DATETIME2 NULL,
        [CompletedAt] DATETIME2 NULL,
        [RejectedAt] DATETIME2 NULL,
        [RejectionReason] NVARCHAR(500) NULL,
        [ReassignedFromId] NVARCHAR(450) NULL,
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedOn] DATETIME2 NULL,
        [CreatedBy] NVARCHAR(450) NULL,
        [ModifiedOn] DATETIME2 NULL,
        [ModifiedBy] NVARCHAR(450) NULL,
        CONSTRAINT [FK_EmployeeWorkAssignments_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [dbo].[Employees]([Id]),
        CONSTRAINT [FK_EmployeeWorkAssignments_WorkJobs_WorkJobId] FOREIGN KEY ([WorkJobId]) REFERENCES [dbo].[WorkJobs]([Id]),
        CONSTRAINT [FK_EmployeeWorkAssignments_JobTypes_JobTypeId] FOREIGN KEY ([JobTypeId]) REFERENCES [dbo].[JobTypes]([Id]),
        CONSTRAINT [FK_EmployeeWorkAssignments_JobItems_JobItemId] FOREIGN KEY ([JobItemId]) REFERENCES [dbo].[JobItems]([Id]),
        CONSTRAINT [FK_EmployeeWorkAssignments_WorkActivities_WorkActivityId] FOREIGN KEY ([WorkActivityId]) REFERENCES [dbo].[WorkActivities]([Id]),
        CONSTRAINT [FK_EmployeeWorkAssignments_Self_ReassignedFromId] FOREIGN KEY ([ReassignedFromId]) REFERENCES [dbo].[EmployeeWorkAssignments]([Id])
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'EmployeeWorkAssignmentHistories')
BEGIN
    CREATE TABLE [dbo].[EmployeeWorkAssignmentHistories] (
        [Id] NVARCHAR(450) NOT NULL PRIMARY KEY,
        [TenantId] NVARCHAR(450) NULL,
        [EmployeeWorkAssignmentId] NVARCHAR(450) NOT NULL,
        [ActionBy] NVARCHAR(450) NOT NULL,
        [Action] INT NOT NULL,
        [ActionDate] DATETIME2 NOT NULL,
        [Remarks] NVARCHAR(500) NULL,
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedOn] DATETIME2 NULL,
        [CreatedBy] NVARCHAR(450) NULL,
        [ModifiedOn] DATETIME2 NULL,
        [ModifiedBy] NVARCHAR(450) NULL,
        CONSTRAINT [FK_EmployeeWorkAssignmentHistories_EmployeeWorkAssignments_AssignmentId]
            FOREIGN KEY ([EmployeeWorkAssignmentId]) REFERENCES [dbo].[EmployeeWorkAssignments]([Id])
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EmployeeWorkAssignments_Employee_Status' AND object_id = OBJECT_ID('dbo.EmployeeWorkAssignments'))
    CREATE INDEX [IX_EmployeeWorkAssignments_Employee_Status] ON [dbo].[EmployeeWorkAssignments]([EmployeeId], [Status]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EmployeeWorkAssignments_AssignedBy' AND object_id = OBJECT_ID('dbo.EmployeeWorkAssignments'))
    CREATE INDEX [IX_EmployeeWorkAssignments_AssignedBy] ON [dbo].[EmployeeWorkAssignments]([AssignedBy]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EmployeeWorkAssignments_Job_JobItem' AND object_id = OBJECT_ID('dbo.EmployeeWorkAssignments'))
    CREATE INDEX [IX_EmployeeWorkAssignments_Job_JobItem] ON [dbo].[EmployeeWorkAssignments]([WorkJobId], [JobItemId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EmployeeWorkAssignments_ReassignedFromId' AND object_id = OBJECT_ID('dbo.EmployeeWorkAssignments'))
    CREATE INDEX [IX_EmployeeWorkAssignments_ReassignedFromId] ON [dbo].[EmployeeWorkAssignments]([ReassignedFromId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EmployeeWorkAssignmentHistories_AssignmentId' AND object_id = OBJECT_ID('dbo.EmployeeWorkAssignmentHistories'))
    CREATE INDEX [IX_EmployeeWorkAssignmentHistories_AssignmentId] ON [dbo].[EmployeeWorkAssignmentHistories]([EmployeeWorkAssignmentId]);
GO

-- ----- DailyWorkEntries: link to assignment + new free-text columns -----

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.DailyWorkEntries') AND name = 'AssignmentId')
    ALTER TABLE [dbo].[DailyWorkEntries] ADD [AssignmentId] NVARCHAR(450) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_DailyWorkEntries_EmployeeWorkAssignments_AssignmentId')
    ALTER TABLE [dbo].[DailyWorkEntries] ADD CONSTRAINT [FK_DailyWorkEntries_EmployeeWorkAssignments_AssignmentId]
        FOREIGN KEY ([AssignmentId]) REFERENCES [dbo].[EmployeeWorkAssignments]([Id]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DailyWorkEntries_AssignmentId' AND object_id = OBJECT_ID('dbo.DailyWorkEntries'))
    CREATE INDEX [IX_DailyWorkEntries_AssignmentId] ON [dbo].[DailyWorkEntries]([AssignmentId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.DailyWorkEntries') AND name = 'WorkDoneToday')
    ALTER TABLE [dbo].[DailyWorkEntries] ADD [WorkDoneToday] NVARCHAR(1000) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.DailyWorkEntries') AND name = 'AdhocReason')
    ALTER TABLE [dbo].[DailyWorkEntries] ADD [AdhocReason] NVARCHAR(500) NULL;
GO
