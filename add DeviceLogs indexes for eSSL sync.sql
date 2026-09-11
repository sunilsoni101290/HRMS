-- ============================================================================
-- eSSL eTimeTrackLite1 - recommended indexes for the direct-SQL sync
-- (Application/Services/Attendances/EsslAttendanceSyncService.cs,
-- Application/Services/Attendances/EsslAttendanceDataSource.cs).
--
-- Run against the eTimeTrackLite1 database itself (NOT the HRMS database) -
-- the login configured in EsslIntegrationSetting only needs SELECT there,
-- but creating these indexes requires a login with ALTER permission on the
-- table, so run this once as an admin/DBA login instead.
--
-- Every statement below is guarded (IF NOT EXISTS) and safe to re-run.
-- Because the app discovers ALL DeviceLogs / DeviceLogs_M_YYYY tables at
-- runtime (see EsslDeviceLogTableName / DiscoverDeviceLogTablesAsync) - not
-- a fixed list of 9 - this script builds and runs the same CREATE INDEX
-- pattern against every matching table it finds, so a newly-created monthly
-- partition table picks up the same indexes without editing this file.
--
-- Why these three indexes specifically:
--   IX_<Table>_LogDate            - GetDeviceLogsAsync's UNION ALL filters
--                                    every branch on [LogDate] >= @FromDate
--                                    AND [LogDate] < @ToDate; without this,
--                                    every sync run (automatic or manual) is
--                                    a full table scan per physical table.
--   IX_<Table>_UserId_LogDate     - backs the user's own manual
--                                    reconciliation query
--                                    (UserId LIKE '260%' + LogDate range)
--                                    and any future per-employee lookup.
--   IX_<Table>_DeviceLogId        - DeviceLogId is already the table's
--                                    IDENTITY/PK in every real eTimeTrackLite1
--                                    deployment (so usually already indexed
--                                    via the PK), but included here as an
--                                    explicit, named, non-clustered index so
--                                    this script is correct even against a
--                                    non-standard/no-PK copy of the schema.
-- ============================================================================

DECLARE @TableName sysname;
DECLARE @Sql nvarchar(max);

DECLARE table_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT t.name
    FROM sys.tables t
    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = 'dbo'
      AND (
            t.name = 'DeviceLogs' OR t.name = 'Device_Logs'
            OR t.name LIKE 'DeviceLogs[_]%'
            OR t.name LIKE 'Device[_]Logs[_]%'
          );

OPEN table_cursor;
FETCH NEXT FROM table_cursor INTO @TableName;

WHILE @@FETCH_STATUS = 0
BEGIN
    -- IX_<Table>_LogDate
    SET @Sql = N'IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = ''IX_' + @TableName + N'_LogDate'' AND object_id = OBJECT_ID(''dbo.' + QUOTENAME(@TableName) + N'''))
        CREATE NONCLUSTERED INDEX ' + QUOTENAME('IX_' + @TableName + '_LogDate') + N'
        ON ' + QUOTENAME(@TableName) + N' ([LogDate]);';
    EXEC sp_executesql @Sql;

    -- IX_<Table>_UserId_LogDate
    SET @Sql = N'IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = ''IX_' + @TableName + N'_UserId_LogDate'' AND object_id = OBJECT_ID(''dbo.' + QUOTENAME(@TableName) + N'''))
        CREATE NONCLUSTERED INDEX ' + QUOTENAME('IX_' + @TableName + '_UserId_LogDate') + N'
        ON ' + QUOTENAME(@TableName) + N' ([UserId], [LogDate]);';
    EXEC sp_executesql @Sql;

    -- IX_<Table>_DeviceLogId
    SET @Sql = N'IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = ''IX_' + @TableName + N'_DeviceLogId'' AND object_id = OBJECT_ID(''dbo.' + QUOTENAME(@TableName) + N'''))
        CREATE NONCLUSTERED INDEX ' + QUOTENAME('IX_' + @TableName + '_DeviceLogId') + N'
        ON ' + QUOTENAME(@TableName) + N' ([DeviceLogId]);';
    EXEC sp_executesql @Sql;

    FETCH NEXT FROM table_cursor INTO @TableName;
END

CLOSE table_cursor;
DEALLOCATE table_cursor;

-- ============================================================================
-- HRMS side (ProsaecTotalDB / ApplicationDbContext) - already covered by
-- Infrastructure/ApplicationDbContext.cs's Fluent-API configuration, applied
-- automatically on next `dotnet ef database update` / app startup migration
-- run. Listed here only so this file is a complete "what indexes exist"
-- reference - do NOT run these manually, they already exist as EF-managed
-- indexes once migrations are applied:
--   IX_BiometricAttendanceLogs_Tenant_Device_Employee_PunchTime
--       UNIQUE (TenantId, DeviceId, EmployeeCode, PunchTime)
--   IX_BiometricAttendanceLogs_Device_TransactionId
--       UNIQUE (DeviceId, DeviceTransactionId) WHERE DeviceTransactionId IS NOT NULL
--   IX_BiometricAttendanceLogs_Tenant_IsProcessed
--       (TenantId, IsProcessed)  -- backs AttendanceProcessorService's main query
--   IX_EsslAttendanceSyncStates_Tenant       UNIQUE (TenantId)
--   IX_EsslIntegrationSettings_Tenant        UNIQUE (TenantId)
--   IX_BiometricSyncLogs_Tenant_SyncType_StartTime
-- ============================================================================
