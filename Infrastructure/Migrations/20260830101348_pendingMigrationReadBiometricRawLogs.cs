using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class pendingMigrationReadBiometricRawLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AttendanceLogs_EmployeeId",
                table: "AttendanceLogs");

            migrationBuilder.AddColumn<string>(
                name: "BiometricAttendanceLogId",
                table: "AttendanceLogs",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BiometricAttendanceLogs_Tenant_IsProcessed",
                table: "BiometricAttendanceLogs",
                columns: new[] { "TenantId", "IsProcessed" });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceLogs_BiometricAttendanceLogId",
                table: "AttendanceLogs",
                column: "BiometricAttendanceLogId",
                unique: true,
                filter: "[BiometricAttendanceLogId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceLogs_EmployeeId_PunchTime",
                table: "AttendanceLogs",
                columns: new[] { "EmployeeId", "PunchTime" });

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceLogs_BiometricAttendanceLogs_BiometricAttendanceLogId",
                table: "AttendanceLogs",
                column: "BiometricAttendanceLogId",
                principalTable: "BiometricAttendanceLogs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceLogs_BiometricAttendanceLogs_BiometricAttendanceLogId",
                table: "AttendanceLogs");

            migrationBuilder.DropIndex(
                name: "IX_BiometricAttendanceLogs_Tenant_IsProcessed",
                table: "BiometricAttendanceLogs");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceLogs_BiometricAttendanceLogId",
                table: "AttendanceLogs");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceLogs_EmployeeId_PunchTime",
                table: "AttendanceLogs");

            migrationBuilder.DropColumn(
                name: "BiometricAttendanceLogId",
                table: "AttendanceLogs");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceLogs_EmployeeId",
                table: "AttendanceLogs",
                column: "EmployeeId");
        }
    }
}
