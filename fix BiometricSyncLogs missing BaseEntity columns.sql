-- Run this once against your HRMS/ERP database. Idempotent - safe to re-run.
-- Fixes: BiometricSyncLogs was created by an earlier ad-hoc process before
-- this table had a real EF migration, and is missing one or more BaseEntity
-- columns (surfaced as "Invalid column name 'IsActive'" when querying it).

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BiometricSyncLogs') AND name = 'IsDeleted')
    ALTER TABLE [dbo].[BiometricSyncLogs] ADD [IsDeleted] BIT NOT NULL CONSTRAINT DF_BiometricSyncLogs_IsDeleted DEFAULT (0);
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BiometricSyncLogs') AND name = 'IsActive')
    ALTER TABLE [dbo].[BiometricSyncLogs] ADD [IsActive] BIT NOT NULL CONSTRAINT DF_BiometricSyncLogs_IsActive DEFAULT (1);
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BiometricSyncLogs') AND name = 'CreatedOn')
    ALTER TABLE [dbo].[BiometricSyncLogs] ADD [CreatedOn] DATETIME2 NOT NULL CONSTRAINT DF_BiometricSyncLogs_CreatedOn DEFAULT (SYSUTCDATETIME());
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BiometricSyncLogs') AND name = 'CreatedBy')
    ALTER TABLE [dbo].[BiometricSyncLogs] ADD [CreatedBy] NVARCHAR(MAX) NOT NULL CONSTRAINT DF_BiometricSyncLogs_CreatedBy DEFAULT ('System');
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BiometricSyncLogs') AND name = 'ModifiedOn')
    ALTER TABLE [dbo].[BiometricSyncLogs] ADD [ModifiedOn] DATETIME2 NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BiometricSyncLogs') AND name = 'ModifiedBy')
    ALTER TABLE [dbo].[BiometricSyncLogs] ADD [ModifiedBy] NVARCHAR(MAX) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BiometricSyncLogs') AND name = 'FromDate')
    ALTER TABLE [dbo].[BiometricSyncLogs] ADD [FromDate] DATETIME2 NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BiometricSyncLogs') AND name = 'ToDate')
    ALTER TABLE [dbo].[BiometricSyncLogs] ADD [ToDate] DATETIME2 NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BiometricSyncLogs') AND name = 'TriggeredBy')
    ALTER TABLE [dbo].[BiometricSyncLogs] ADD [TriggeredBy] NVARCHAR(100) NULL;
GO

-- Sanity check - run this after the above to confirm every BaseEntity +
-- feature column now exists:
-- SELECT name FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BiometricSyncLogs') ORDER BY column_id;
