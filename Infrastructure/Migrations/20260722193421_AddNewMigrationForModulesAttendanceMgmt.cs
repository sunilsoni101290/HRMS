using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNewMigrationForModulesAttendanceMgmt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AttendancePolicies",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CompanyId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    PolicyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    MaxRegularizationRequestsPerMonth = table.Column<int>(type: "int", nullable: false),
                    LateMarkGraceCount = table.Column<int>(type: "int", nullable: false),
                    LateMarkPenaltyType = table.Column<int>(type: "int", nullable: false),
                    MinimumAttendancePercentForFullSalary = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CompOffEligibleExtraHours = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendancePolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendancePolicies_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttendancePolicies_CompanyId",
                table: "AttendancePolicies",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendancePolicies_TenantId",
                table: "AttendancePolicies",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendancePolicies_TenantId_CompanyId_IsActive_EffectiveFrom",
                table: "AttendancePolicies",
                columns: new[] { "TenantId", "CompanyId", "IsActive", "EffectiveFrom" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendancePolicies");
        }
    }
}
