-- ============================================================================
-- Adds AttendanceLogs.BiometricAttendanceLogId (traceability + idempotency
-- guard for the hardened AttendanceProcessorService) plus supporting
-- indexes on AttendanceLogs and BiometricAttendanceLogs. Safe to run more
-- than once (every statement is IF NOT EXISTS-guarded). Does not alter or
-- remove any existing column/data.
-- ============================================================================

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.AttendanceLogs') AND name = 'BiometricAttendanceLogId')
    ALTER TABLE [dbo].[AttendanceLogs] ADD [BiometricAttendanceLogId] NVARCHAR(450) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_AttendanceLogs_BiometricAttendanceLogs_BiometricAttendanceLogId')
    ALTER TABLE [dbo].[AttendanceLogs] ADD CONSTRAINT [FK_AttendanceLogs_BiometricAttendanceLogs_BiometricAttendanceLogId]
        FOREIGN KEY ([BiometricAttendanceLogId]) REFERENCES [dbo].[BiometricAttendanceLogs]([Id]) ON DELETE NO ACTION;
GO

-- DB-level idempotency backstop - the same raw biometric punch can never be
-- linked to more than one AttendanceLog. Filtered so manual/web punches
-- (BiometricAttendanceLogId IS NULL) are never constrained by this.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AttendanceLogs_BiometricAttendanceLogId' AND object_id = OBJECT_ID('dbo.AttendanceLogs'))
    CREATE UNIQUE INDEX [IX_AttendanceLogs_BiometricAttendanceLogId] ON [dbo].[AttendanceLogs]([BiometricAttendanceLogId]) WHERE [BiometricAttendanceLogId] IS NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AttendanceLogs_AttendanceId' AND object_id = OBJECT_ID('dbo.AttendanceLogs'))
    CREATE INDEX [IX_AttendanceLogs_AttendanceId] ON [dbo].[AttendanceLogs]([AttendanceId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AttendanceLogs_EmployeeId_PunchTime' AND object_id = OBJECT_ID('dbo.AttendanceLogs'))
    CREATE INDEX [IX_AttendanceLogs_EmployeeId_PunchTime] ON [dbo].[AttendanceLogs]([EmployeeId], [PunchTime]);
GO

-- AttendanceProcessorService's main query is "WHERE IsProcessed = 0" across
-- the whole table - without this it becomes a full scan as the table grows.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BiometricAttendanceLogs_Tenant_IsProcessed' AND object_id = OBJECT_ID('dbo.BiometricAttendanceLogs'))
    CREATE INDEX [IX_BiometricAttendanceLogs_Tenant_IsProcessed] ON [dbo].[BiometricAttendanceLogs]([TenantId], [IsProcessed]);
GO

-- Sanity check (uncomment to run manually):
-- SELECT TOP 20 * FROM [dbo].[AttendanceLogs] WHERE [BiometricAttendanceLogId] IS NOT NULL ORDER BY CreatedOn DESC;
