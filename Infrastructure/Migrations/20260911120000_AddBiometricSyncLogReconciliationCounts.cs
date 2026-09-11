using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// Idempotent by convention with AddEsslMonthlyDeviceLogTableSupport/
    /// AddEsslIntegrationSettings (IF NOT EXISTS-guarded raw SQL, no
    /// accompanying .Designer.cs/model-snapshot update - see those
    /// migrations' remarks for why).
    ///
    /// Adds BiometricSyncLogs.DuplicateCount and .UnmappedCount - part of
    /// the fix for the reported "2671 eSSL source rows -&gt; only 303 ever
    /// reached Attendance" gap. Before this, BiometricSyncLog only tracked
    /// RecordsFetched/Inserted/Skipped/Failed, which conflated "already
    /// imported duplicate" with every other kind of skip and gave no
    /// per-run visibility into how many imported rows were unmapped at
    /// import time - exactly the number that matters for explaining why
    /// AttendanceProcessorService's output can be far smaller than the raw
    /// eSSL row count. See EsslAttendanceSyncService.SyncAsync's FINAL
    /// RESULT log line, which now populates both columns every run.
    /// </remarks>
    public partial class AddBiometricSyncLogReconciliationCounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BiometricSyncLogs') AND name = 'DuplicateCount')
    ALTER TABLE [dbo].[BiometricSyncLogs] ADD [DuplicateCount] INT NOT NULL CONSTRAINT DF_BiometricSyncLogs_DuplicateCount DEFAULT (0);
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BiometricSyncLogs') AND name = 'UnmappedCount')
    ALTER TABLE [dbo].[BiometricSyncLogs] ADD [UnmappedCount] INT NOT NULL CONSTRAINT DF_BiometricSyncLogs_UnmappedCount DEFAULT (0);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BiometricSyncLogs') AND name = 'DuplicateCount') ALTER TABLE [dbo].[BiometricSyncLogs] DROP COLUMN [DuplicateCount];");
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BiometricSyncLogs') AND name = 'UnmappedCount') ALTER TABLE [dbo].[BiometricSyncLogs] DROP COLUMN [UnmappedCount];");
        }
    }
}
