using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class pendingMigrationApply05092026 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastRecalculatedBy",
                table: "Payrolls",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastRecalculatedOn",
                table: "Payrolls",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PaidLeaveDays",
                table: "Payrolls",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PayableDays",
                table: "Payrolls",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProrationBasisUsed",
                table: "Payrolls",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RecalculatedCount",
                table: "Payrolls",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "UnpaidLeaveDays",
                table: "Payrolls",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FixedWorkingDaysPerMonth",
                table: "AttendancePolicies",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "SalaryProrationBasis",
                table: "AttendancePolicies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "PayrollAuditLogs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PayrollId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OldNetSalary = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NewNetSalary = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    OldPayableDays = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NewPayableDays = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PerformedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PerformedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    table.PrimaryKey("PK_PayrollAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollAuditLogs_Payrolls_PayrollId",
                        column: x => x.PayrollId,
                        principalTable: "Payrolls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAuditLogs_PayrollId",
                table: "PayrollAuditLogs",
                column: "PayrollId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PayrollAuditLogs");

            migrationBuilder.DropColumn(
                name: "LastRecalculatedBy",
                table: "Payrolls");

            migrationBuilder.DropColumn(
                name: "LastRecalculatedOn",
                table: "Payrolls");

            migrationBuilder.DropColumn(
                name: "PaidLeaveDays",
                table: "Payrolls");

            migrationBuilder.DropColumn(
                name: "PayableDays",
                table: "Payrolls");

            migrationBuilder.DropColumn(
                name: "ProrationBasisUsed",
                table: "Payrolls");

            migrationBuilder.DropColumn(
                name: "RecalculatedCount",
                table: "Payrolls");

            migrationBuilder.DropColumn(
                name: "UnpaidLeaveDays",
                table: "Payrolls");

            migrationBuilder.DropColumn(
                name: "FixedWorkingDaysPerMonth",
                table: "AttendancePolicies");

            migrationBuilder.DropColumn(
                name: "SalaryProrationBasis",
                table: "AttendancePolicies");
        }
    }
}
