using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PendingMigration02092026 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastProcessedSourceTable",
                table: "EsslAttendanceSyncStates",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceTables",
                table: "BiometricSyncLogs",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DownloadDate",
                table: "BiometricAttendanceLogs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceTable",
                table: "BiometricAttendanceLogs",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastProcessedSourceTable",
                table: "EsslAttendanceSyncStates");

            migrationBuilder.DropColumn(
                name: "SourceTables",
                table: "BiometricSyncLogs");

            migrationBuilder.DropColumn(
                name: "DownloadDate",
                table: "BiometricAttendanceLogs");

            migrationBuilder.DropColumn(
                name: "SourceTable",
                table: "BiometricAttendanceLogs");
        }
    }
}
