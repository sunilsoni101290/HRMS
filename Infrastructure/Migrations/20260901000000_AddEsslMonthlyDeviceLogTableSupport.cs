using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// Idempotent by convention with AddEsslIntegrationSettings/
    /// PendingMigrationEsslAttendanceSyncStates (IF NOT EXISTS-guarded raw
    /// SQL rather than typed AddColumn calls, and no accompanying
    /// .Designer.cs/model-snapshot update - see those migrations' own
    /// remarks for why). Adds the columns needed to support reading
    /// eTimeTrackLite1's monthly DeviceLogs_M_YYYY partition tables in
    /// addition to the bare DeviceLogs table:
    /// BiometricAttendanceLogs.SourceTable/DownloadDate (per-row audit),
    /// BiometricSyncLogs.SourceTables (per-run audit), and
    /// EsslAttendanceSyncStates.LastProcessedSourceTable (informational
    /// watermark disambiguation only - see that column's XML doc comment).
    /// Does not touch etimetracklite1 - see the equivalent, more heavily
    /// commented "add DeviceLogs monthly table support columns.sql" at the
    /// repo root for the deployment-facing version of these same statements.
    /// </remarks>
    public partial class AddEsslMonthlyDeviceLogTableSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BiometricAttendanceLogs') AND name = 'SourceTable')
    ALTER TABLE [dbo].[BiometricAttendanceLogs] ADD [SourceTable] NVARCHAR(60) NULL;
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BiometricAttendanceLogs') AND name = 'DownloadDate')
    ALTER TABLE [dbo].[BiometricAttendanceLogs] ADD [DownloadDate] DATETIME2 NULL;
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BiometricSyncLogs') AND name = 'SourceTables')
    ALTER TABLE [dbo].[BiometricSyncLogs] ADD [SourceTables] NVARCHAR(500) NULL;
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.EsslAttendanceSyncStates') AND name = 'LastProcessedSourceTable')
    ALTER TABLE [dbo].[EsslAttendanceSyncStates] ADD [LastProcessedSourceTable] NVARCHAR(60) NULL;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BiometricAttendanceLogs') AND name = 'SourceTable') ALTER TABLE [dbo].[BiometricAttendanceLogs] DROP COLUMN [SourceTable];");
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BiometricAttendanceLogs') AND name = 'DownloadDate') ALTER TABLE [dbo].[BiometricAttendanceLogs] DROP COLUMN [DownloadDate];");
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BiometricSyncLogs') AND name = 'SourceTables') ALTER TABLE [dbo].[BiometricSyncLogs] DROP COLUMN [SourceTables];");
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.EsslAttendanceSyncStates') AND name = 'LastProcessedSourceTable') ALTER TABLE [dbo].[EsslAttendanceSyncStates] DROP COLUMN [LastProcessedSourceTable];");
        }
    }
}
