-- Adds BiometricSyncLogs - one row per synchronization run against a device
-- (SyncType: "Ingest" = agent/simulator push, "Pull" = the older HTTP-pull
-- path). Powers the Device Health dashboard's "Records fetched/inserted/
-- skipped/failed" and "Last successful sync" figures.
--
-- Idempotent: safe to run more than once.
--
-- NOTE: this repo has EF Core migrations under Infrastructure/Migrations as
-- well. Pick ONE path - either run this script by hand, OR add an EF
-- migration (`dotnet ef migrations add AddBiometricSyncLog`) - not both.

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'BiometricSyncLogs' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.BiometricSyncLogs
    (
        Id              NVARCHAR(50)    NOT NULL PRIMARY KEY,
        TenantId        NVARCHAR(50)    NOT NULL,
        DeviceId        NVARCHAR(50)    NULL,
        AgentId         NVARCHAR(50)    NULL,
        SyncType        NVARCHAR(30)    NOT NULL,
        StartTime       DATETIME2       NOT NULL DEFAULT (SYSUTCDATETIME()),
        EndTime         DATETIME2       NULL,
        RecordsFetched  INT             NOT NULL DEFAULT (0),
        RecordsInserted INT             NOT NULL DEFAULT (0),
        RecordsSkipped  INT             NOT NULL DEFAULT (0),
        RecordsFailed   INT             NOT NULL DEFAULT (0),
        Status          NVARCHAR(20)    NOT NULL DEFAULT ('Success'),
        ErrorMessage    NVARCHAR(500)   NULL,
        IsDeleted       BIT             NOT NULL DEFAULT (0),
        CreatedBy       NVARCHAR(50)    NULL,
        CreatedOn       DATETIME2       NULL,
        ModifiedBy      NVARCHAR(50)    NULL,
        ModifiedOn      DATETIME2       NULL,

        CONSTRAINT FK_BiometricSyncLogs_BiometricDevices
            FOREIGN KEY (DeviceId) REFERENCES dbo.BiometricDevices(Id)
    );

    CREATE INDEX IX_BiometricSyncLogs_Device_StartTime
        ON dbo.BiometricSyncLogs (DeviceId, StartTime);
END
GO
