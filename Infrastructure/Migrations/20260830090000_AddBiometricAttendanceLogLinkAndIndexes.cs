using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// Idempotent (IF NOT EXISTS-guarded raw SQL), same convention as this
    /// project's other recent migrations - adds AttendanceLogs.
    /// BiometricAttendanceLogId (traceability + idempotency guard for
    /// AttendanceProcessorService), a supporting index on
    /// BiometricAttendanceLogs(TenantId, IsProcessed), and two AttendanceLog
    /// lookup indexes that never existed before. No existing column is
    /// altered or removed.
    /// </remarks>
    public partial class AddBiometricAttendanceLogLinkAndIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.AttendanceLogs') AND name = 'BiometricAttendanceLogId')
    ALTER TABLE [dbo].[AttendanceLogs] ADD [BiometricAttendanceLogId] NVARCHAR(450) NULL;
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_AttendanceLogs_BiometricAttendanceLogs_BiometricAttendanceLogId')
    ALTER TABLE [dbo].[AttendanceLogs] ADD CONSTRAINT [FK_AttendanceLogs_BiometricAttendanceLogs_BiometricAttendanceLogId]
        FOREIGN KEY ([BiometricAttendanceLogId]) REFERENCES [dbo].[BiometricAttendanceLogs]([Id]) ON DELETE NO ACTION;
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AttendanceLogs_BiometricAttendanceLogId' AND object_id = OBJECT_ID('dbo.AttendanceLogs'))
    CREATE UNIQUE INDEX [IX_AttendanceLogs_BiometricAttendanceLogId] ON [dbo].[AttendanceLogs]([BiometricAttendanceLogId]) WHERE [BiometricAttendanceLogId] IS NOT NULL;
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AttendanceLogs_AttendanceId' AND object_id = OBJECT_ID('dbo.AttendanceLogs'))
    CREATE INDEX [IX_AttendanceLogs_AttendanceId] ON [dbo].[AttendanceLogs]([AttendanceId]);
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AttendanceLogs_EmployeeId_PunchTime' AND object_id = OBJECT_ID('dbo.AttendanceLogs'))
    CREATE INDEX [IX_AttendanceLogs_EmployeeId_PunchTime] ON [dbo].[AttendanceLogs]([EmployeeId], [PunchTime]);
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BiometricAttendanceLogs_Tenant_IsProcessed' AND object_id = OBJECT_ID('dbo.BiometricAttendanceLogs'))
    CREATE INDEX [IX_BiometricAttendanceLogs_Tenant_IsProcessed] ON [dbo].[BiometricAttendanceLogs]([TenantId], [IsProcessed]);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BiometricAttendanceLogs_Tenant_IsProcessed' AND object_id = OBJECT_ID('dbo.BiometricAttendanceLogs')) DROP INDEX [IX_BiometricAttendanceLogs_Tenant_IsProcessed] ON [dbo].[BiometricAttendanceLogs];");
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AttendanceLogs_EmployeeId_PunchTime' AND object_id = OBJECT_ID('dbo.AttendanceLogs')) DROP INDEX [IX_AttendanceLogs_EmployeeId_PunchTime] ON [dbo].[AttendanceLogs];");
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AttendanceLogs_AttendanceId' AND object_id = OBJECT_ID('dbo.AttendanceLogs')) DROP INDEX [IX_AttendanceLogs_AttendanceId] ON [dbo].[AttendanceLogs];");
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AttendanceLogs_BiometricAttendanceLogId' AND object_id = OBJECT_ID('dbo.AttendanceLogs')) DROP INDEX [IX_AttendanceLogs_BiometricAttendanceLogId] ON [dbo].[AttendanceLogs];");
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_AttendanceLogs_BiometricAttendanceLogs_BiometricAttendanceLogId') ALTER TABLE [dbo].[AttendanceLogs] DROP CONSTRAINT [FK_AttendanceLogs_BiometricAttendanceLogs_BiometricAttendanceLogId];");
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.AttendanceLogs') AND name = 'BiometricAttendanceLogId') ALTER TABLE [dbo].[AttendanceLogs] DROP COLUMN [BiometricAttendanceLogId];");
        }
    }
}
