-- =====================================================================
-- BiometricAgent + BiometricDevice integration schema changes
-- =====================================================================
-- Run this against the ERP database, OR regenerate/apply the equivalent
-- via `dotnet ef migrations add BiometricAgentIntegration` +
-- `dotnet ef database update` from Infrastructure (do not do both -
-- pick one path so EF's migration history table and the actual schema
-- don't disagree). This script mirrors the changes already made to
-- Domain/Entities/BiometricDevice.cs and Infrastructure/ApplicationDbContext.cs.
-- Safe to re-run - every step is guarded.
-- =====================================================================

SET NOCOUNT ON;

-- ---------------------------------------------------------------------
-- 1. BiometricAgents table
-- ---------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'BiometricAgents')
BEGIN
    CREATE TABLE [dbo].[BiometricAgents]
    (
        [Id]             nvarchar(450)   NOT NULL,
        [TenantId]       nvarchar(450)   NOT NULL,
        [AgentCode]      nvarchar(50)    NOT NULL,
        [AgentName]      nvarchar(100)   NOT NULL,
        [AgentKey]       nvarchar(100)   NULL,
        [MachineName]    nvarchar(max)   NULL,
        [AgentVersion]   nvarchar(max)   NULL,
        [LastHeartbeat]  datetime2       NULL,
        [IsDeleted]      bit             NOT NULL CONSTRAINT [DF_BiometricAgents_IsDeleted] DEFAULT (0),
        [CreatedOn]      datetime2       NOT NULL CONSTRAINT [DF_BiometricAgents_CreatedOn] DEFAULT (SYSUTCDATETIME()),
        [CreatedBy]      nvarchar(max)   NOT NULL CONSTRAINT [DF_BiometricAgents_CreatedBy] DEFAULT (''),
        [ModifiedOn]     datetime2       NULL,
        [ModifiedBy]     nvarchar(max)   NULL,
        [IsActive]       bit             NOT NULL CONSTRAINT [DF_BiometricAgents_IsActive] DEFAULT (1),

        CONSTRAINT [PK_BiometricAgents] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BiometricAgents_Tenants_TenantId]
            FOREIGN KEY ([TenantId]) REFERENCES [dbo].[Tenants] ([Id])
    );

    PRINT 'Created table BiometricAgents.';
END
ELSE
    PRINT 'BiometricAgents already exists - skipped.';
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BiometricAgents_Tenant_AgentCode'
      AND object_id = OBJECT_ID('dbo.BiometricAgents'))
BEGIN
    CREATE UNIQUE INDEX [IX_BiometricAgents_Tenant_AgentCode]
        ON [dbo].[BiometricAgents] ([TenantId], [AgentCode]);

    PRINT 'Created index IX_BiometricAgents_Tenant_AgentCode.';
END
GO

-- ---------------------------------------------------------------------
-- 2. BiometricDevices - new columns
-- ---------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BiometricDevices') AND name = 'LastSeen')
    ALTER TABLE [dbo].[BiometricDevices] ADD [LastSeen] datetime2 NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BiometricDevices') AND name = 'DeviceType')
    ALTER TABLE [dbo].[BiometricDevices] ADD [DeviceType] nvarchar(30) NOT NULL CONSTRAINT [DF_BiometricDevices_DeviceType] DEFAULT ('Essl');
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BiometricDevices') AND name = 'CommunicationType')
    ALTER TABLE [dbo].[BiometricDevices] ADD [CommunicationType] nvarchar(30) NOT NULL CONSTRAINT [DF_BiometricDevices_CommunicationType] DEFAULT ('TCP/IP');
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BiometricDevices') AND name = 'AgentId')
    ALTER TABLE [dbo].[BiometricDevices] ADD [AgentId] nvarchar(450) NULL;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_BiometricDevices_BiometricAgents_AgentId')
BEGIN
    ALTER TABLE [dbo].[BiometricDevices]
        ADD CONSTRAINT [FK_BiometricDevices_BiometricAgents_AgentId]
        FOREIGN KEY ([AgentId]) REFERENCES [dbo].[BiometricAgents] ([Id]);

    PRINT 'Added FK_BiometricDevices_BiometricAgents_AgentId.';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BiometricDevices_Tenant_DeviceCode'
      AND object_id = OBJECT_ID('dbo.BiometricDevices'))
BEGIN
    -- If old data has duplicate (TenantId, DeviceCode) pairs this will fail -
    -- resolve the duplicates first (see the diagnostic query at the bottom
    -- of this script) before re-running.
    CREATE UNIQUE INDEX [IX_BiometricDevices_Tenant_DeviceCode]
        ON [dbo].[BiometricDevices] ([TenantId], [DeviceCode]);

    PRINT 'Created index IX_BiometricDevices_Tenant_DeviceCode.';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BiometricDevices_AgentId'
      AND object_id = OBJECT_ID('dbo.BiometricDevices'))
BEGIN
    CREATE INDEX [IX_BiometricDevices_AgentId] ON [dbo].[BiometricDevices] ([AgentId]);
END
GO

-- ---------------------------------------------------------------------
-- 3. BiometricAttendanceLogs - transaction id + tenant backfill + unique indexes
-- ---------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BiometricAttendanceLogs') AND name = 'DeviceTransactionId')
    ALTER TABLE [dbo].[BiometricAttendanceLogs] ADD [DeviceTransactionId] nvarchar(100) NULL;
GO

-- Older rows may predate TenantId being populated on ingest - backfill from
-- the owning device so the new (TenantId, DeviceId, EmployeeCode, PunchTime)
-- unique index below doesn't choke on NULLs.
UPDATE bal
SET bal.TenantId = bd.TenantId
FROM [dbo].[BiometricAttendanceLogs] bal
JOIN [dbo].[BiometricDevices] bd ON bd.Id = bal.DeviceId
WHERE bal.TenantId IS NULL;
GO

-- Diagnostic: run this first if the unique index creation below fails -
-- it lists any pre-existing duplicate (Device, Employee, PunchTime) rows
-- that need manual resolution (keep the earliest CreatedOn, delete the rest).
--
-- SELECT DeviceId, EmployeeCode, PunchTime, COUNT(*) AS Cnt
-- FROM [dbo].[BiometricAttendanceLogs]
-- GROUP BY DeviceId, EmployeeCode, PunchTime
-- HAVING COUNT(*) > 1;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BiometricAttendanceLogs_Tenant_Device_Employee_PunchTime'
      AND object_id = OBJECT_ID('dbo.BiometricAttendanceLogs'))
BEGIN
    CREATE UNIQUE INDEX [IX_BiometricAttendanceLogs_Tenant_Device_Employee_PunchTime]
        ON [dbo].[BiometricAttendanceLogs] ([TenantId], [DeviceId], [EmployeeCode], [PunchTime]);

    PRINT 'Created index IX_BiometricAttendanceLogs_Tenant_Device_Employee_PunchTime.';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BiometricAttendanceLogs_Device_TransactionId'
      AND object_id = OBJECT_ID('dbo.BiometricAttendanceLogs'))
BEGIN
    CREATE UNIQUE INDEX [IX_BiometricAttendanceLogs_Device_TransactionId]
        ON [dbo].[BiometricAttendanceLogs] ([DeviceId], [DeviceTransactionId])
        WHERE [DeviceTransactionId] IS NOT NULL;

    PRINT 'Created index IX_BiometricAttendanceLogs_Device_TransactionId.';
END
GO

PRINT 'BiometricAgent integration schema changes complete.';
