using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PendingBiometricandAgent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BiometricDevices_TenantId",
                table: "BiometricDevices");

            migrationBuilder.AddColumn<string>(
                name: "AgentId",
                table: "BiometricDevices",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CommunicationType",
                table: "BiometricDevices",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DeviceType",
                table: "BiometricDevices",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSeen",
                table: "BiometricDevices",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "BiometricAttendanceLogs",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EmployeeCode",
                table: "BiometricAttendanceLogs",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "DeviceId",
                table: "BiometricAttendanceLogs",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "DeviceTransactionId",
                table: "BiometricAttendanceLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BiometricAgents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AgentCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AgentName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AgentKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MachineName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AgentVersion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastHeartbeat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BiometricAgents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BiometricAgents_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BiometricDevices_AgentId",
                table: "BiometricDevices",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_BiometricDevices_Tenant_DeviceCode",
                table: "BiometricDevices",
                columns: new[] { "TenantId", "DeviceCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BiometricAttendanceLogs_Device_TransactionId",
                table: "BiometricAttendanceLogs",
                columns: new[] { "DeviceId", "DeviceTransactionId" },
                unique: true,
                filter: "[DeviceTransactionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BiometricAttendanceLogs_Tenant_Device_Employee_PunchTime",
                table: "BiometricAttendanceLogs",
                columns: new[] { "TenantId", "DeviceId", "EmployeeCode", "PunchTime" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BiometricAgents_Tenant_AgentCode",
                table: "BiometricAgents",
                columns: new[] { "TenantId", "AgentCode" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BiometricDevices_BiometricAgents_AgentId",
                table: "BiometricDevices",
                column: "AgentId",
                principalTable: "BiometricAgents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BiometricDevices_BiometricAgents_AgentId",
                table: "BiometricDevices");

            migrationBuilder.DropTable(
                name: "BiometricAgents");

            migrationBuilder.DropIndex(
                name: "IX_BiometricDevices_AgentId",
                table: "BiometricDevices");

            migrationBuilder.DropIndex(
                name: "IX_BiometricDevices_Tenant_DeviceCode",
                table: "BiometricDevices");

            migrationBuilder.DropIndex(
                name: "IX_BiometricAttendanceLogs_Device_TransactionId",
                table: "BiometricAttendanceLogs");

            migrationBuilder.DropIndex(
                name: "IX_BiometricAttendanceLogs_Tenant_Device_Employee_PunchTime",
                table: "BiometricAttendanceLogs");

            migrationBuilder.DropColumn(
                name: "AgentId",
                table: "BiometricDevices");

            migrationBuilder.DropColumn(
                name: "CommunicationType",
                table: "BiometricDevices");

            migrationBuilder.DropColumn(
                name: "DeviceType",
                table: "BiometricDevices");

            migrationBuilder.DropColumn(
                name: "LastSeen",
                table: "BiometricDevices");

            migrationBuilder.DropColumn(
                name: "DeviceTransactionId",
                table: "BiometricAttendanceLogs");

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "BiometricAttendanceLogs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EmployeeCode",
                table: "BiometricAttendanceLogs",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "DeviceId",
                table: "BiometricAttendanceLogs",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_BiometricDevices_TenantId",
                table: "BiometricDevices",
                column: "TenantId");
        }
    }
}
