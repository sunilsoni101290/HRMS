-- eSSL eTimeTrackLite1 direct-SQL attendance integration - schema changes
-- against the HRMS/ERP database (ApplicationDbContext) ONLY. This script
-- never touches etimetracklite1 - that database is read-only for this
-- integration (SELECT-only SQL login), per the architecture requirement.
--
-- Idempotent - safe to re-run. Mirrors the existing "add ErrorLog
-- ModuleName and FeatureName columns.sql" style already used in this repo.
-- If you have the .NET SDK available, `dotnet ef migrations add
-- AddEsslAttendanceSync --project Infrastructure --startup-project API`
-- (context ApplicationDbContext, the default) should produce an equivalent
-- migration from the entity/OnModelCreating changes already made to
-- Domain/Entities/EsslAttendanceSyncState.cs, Domain/Entities/BiometricDevice.cs
-- (BiometricSyncLog), and Infrastructure/ApplicationDbContext.cs - this
-- script exists as the reviewable, ready-to-run equivalent for
-- environments without the SDK on hand.

-- =====================================================================
-- 1) New table: EsslAttendanceSyncStates (one row per tenant - the sync
--    cursor/lock; see Domain/Entities/EsslAttendanceSyncState.cs)
-- =====================================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'EsslAttendanceSyncStates')
BEGIN
    CREATE TABLE [dbo].[EsslAttendanceSyncStates]
    (
        [Id]                        NVARCHAR(450)   NOT NULL PRIMARY KEY,
        [TenantId]                  NVARCHAR(450)   NULL,
        [IsDeleted]                 BIT             NOT NULL CONSTRAINT DF_EsslAttendanceSyncStates_IsDeleted DEFAULT (0),
        [CreatedOn]                 DATETIME2       NOT NULL,
        [CreatedBy]                 NVARCHAR(450)   NULL,
        [ModifiedOn]                DATETIME2       NULL,
        [ModifiedBy]                NVARCHAR(450)   NULL,
        [IsActive]                  BIT             NOT NULL CONSTRAINT DF_EsslAttendanceSyncStates_IsActive DEFAULT (1),

        [LastProcessedDeviceLogId]  INT             NULL,
        [LastProcessedLogDate]      DATETIME2       NULL,
        [LastSyncStartedAt]         DATETIME2       NULL,
        [LastSyncCompletedAt]       DATETIME2       NULL,
        [LastSyncStatus]            NVARCHAR(20)    NULL,
        [LastError]                 NVARCHAR(1000)  NULL,
        [RecordsRead]               INT             NOT NULL CONSTRAINT DF_EsslAttendanceSyncStates_RecordsRead DEFAULT (0),
        [RecordsImported]           INT             NOT NULL CONSTRAINT DF_EsslAttendanceSyncStates_RecordsImported DEFAULT (0),
        [RecordsSkipped]            INT             NOT NULL CONSTRAINT DF_EsslAttendanceSyncStates_RecordsSkipped DEFAULT (0),
        [RecordsFailed]             INT             NOT NULL CONSTRAINT DF_EsslAttendanceSyncStates_RecordsFailed DEFAULT (0),
        [IsSyncRunning]             BIT             NOT NULL CONSTRAINT DF_EsslAttendanceSyncStates_IsSyncRunning DEFAULT (0)
    );

    CREATE UNIQUE INDEX [IX_EsslAttendanceSyncStates_Tenant]
        ON [dbo].[EsslAttendanceSyncStates]([TenantId]);
END
GO

-- =====================================================================
-- 2) Extend existing BiometricSyncLogs with FromDate/ToDate/TriggeredBy
--    (reused as this feature's sync-history table via SyncType =
--    'EsslDbPull' - no new history table created)
-- =====================================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'BiometricSyncLogs')
BEGIN
    RAISERROR('Table dbo.BiometricSyncLogs does not exist - the Biometric Device feature must be deployed before this script.', 16, 1);
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BiometricSyncLogs') AND name = 'FromDate')
        ALTER TABLE [dbo].[BiometricSyncLogs] ADD [FromDate] DATETIME2 NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BiometricSyncLogs') AND name = 'ToDate')
        ALTER TABLE [dbo].[BiometricSyncLogs] ADD [ToDate] DATETIME2 NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BiometricSyncLogs') AND name = 'TriggeredBy')
        ALTER TABLE [dbo].[BiometricSyncLogs] ADD [TriggeredBy] NVARCHAR(100) NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.BiometricSyncLogs') AND name = 'IX_BiometricSyncLogs_Tenant_SyncType_StartTime')
        CREATE INDEX [IX_BiometricSyncLogs_Tenant_SyncType_StartTime]
            ON [dbo].[BiometricSyncLogs]([TenantId], [SyncType], [StartTime]);
END
GO

-- =====================================================================
-- NOTE: no changes required to BiometricAttendanceLogs - its existing
-- unique indexes (IX_BiometricAttendanceLogs_Tenant_Device_Employee_PunchTime
-- and IX_BiometricAttendanceLogs_Device_TransactionId) already provide full
-- idempotency for eSSL-sourced rows too (DeviceId = 'ESSL-{eSSL DeviceId}',
-- DeviceTransactionId = 'ESSL-{eSSL DeviceLogId}' - see
-- EsslAttendanceSyncService.cs).
-- =====================================================================
