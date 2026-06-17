using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BiometricDeviceIntegrationTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FilePath",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBiometricAttendance",
                table: "Attendances",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ProcessedBy",
                table: "Attendances",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessedOn",
                table: "Attendances",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceDeviceId",
                table: "Attendances",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BiometricAttendanceLogs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmployeeCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PunchTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PunchType = table.Column<int>(type: "int", nullable: false),
                    DeviceId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDuplicate = table.Column<bool>(type: "bit", nullable: false),
                    IsProcessed = table.Column<bool>(type: "bit", nullable: false),
                    ProcessedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BiometricAttendanceLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BiometricDevices",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CompanyId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BranchId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    DeviceName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DeviceCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IPAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Port = table.Column<int>(type: "int", nullable: false),
                    ApiUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SerialNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastSyncDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BiometricDevices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BiometricDevices_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BiometricDevices_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BiometricDevices_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeBiometricMappings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmployeeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BiometricEmployeeCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CardNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FaceId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FingerTemplateId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeBiometricMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeBiometricMappings_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BiometricDevices_BranchId",
                table: "BiometricDevices",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_BiometricDevices_CompanyId",
                table: "BiometricDevices",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_BiometricDevices_TenantId",
                table: "BiometricDevices",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeBiometricMappings_EmployeeId",
                table: "EmployeeBiometricMappings",
                column: "EmployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BiometricAttendanceLogs");

            migrationBuilder.DropTable(
                name: "BiometricDevices");

            migrationBuilder.DropTable(
                name: "EmployeeBiometricMappings");

            migrationBuilder.DropColumn(
                name: "FilePath",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "IsBiometricAttendance",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "ProcessedBy",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "ProcessedOn",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "SourceDeviceId",
                table: "Attendances");
        }
    }
}
