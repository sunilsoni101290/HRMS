using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AssignDailyWorkentry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdhocReason",
                table: "DailyWorkEntries",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssignmentId",
                table: "DailyWorkEntries",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkDoneToday",
                table: "DailyWorkEntries",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EmployeeWorkAssignments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmployeeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AssignedBy = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    WorkJobId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    JobTypeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    JobItemId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    WorkActivityId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    AssignmentType = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpectedEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EstimatedHours = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Instructions = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReassignedFromId = table.Column<string>(type: "nvarchar(450)", nullable: true),
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
                    table.PrimaryKey("PK_EmployeeWorkAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeWorkAssignments_EmployeeWorkAssignments_ReassignedFromId",
                        column: x => x.ReassignedFromId,
                        principalTable: "EmployeeWorkAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeWorkAssignments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeWorkAssignments_JobItems_JobItemId",
                        column: x => x.JobItemId,
                        principalTable: "JobItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeWorkAssignments_JobTypes_JobTypeId",
                        column: x => x.JobTypeId,
                        principalTable: "JobTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeWorkAssignments_WorkActivities_WorkActivityId",
                        column: x => x.WorkActivityId,
                        principalTable: "WorkActivities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeWorkAssignments_WorkJobs_WorkJobId",
                        column: x => x.WorkJobId,
                        principalTable: "WorkJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeWorkAssignmentHistories",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmployeeWorkAssignmentId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ActionBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Action = table.Column<int>(type: "int", nullable: false),
                    ActionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_EmployeeWorkAssignmentHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeWorkAssignmentHistories_EmployeeWorkAssignments_EmployeeWorkAssignmentId",
                        column: x => x.EmployeeWorkAssignmentId,
                        principalTable: "EmployeeWorkAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyWorkEntries_AssignmentId",
                table: "DailyWorkEntries",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeWorkAssignmentHistories_EmployeeWorkAssignmentId",
                table: "EmployeeWorkAssignmentHistories",
                column: "EmployeeWorkAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeWorkAssignments_AssignedBy",
                table: "EmployeeWorkAssignments",
                column: "AssignedBy");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeWorkAssignments_EmployeeId_Status",
                table: "EmployeeWorkAssignments",
                columns: new[] { "EmployeeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeWorkAssignments_JobItemId",
                table: "EmployeeWorkAssignments",
                column: "JobItemId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeWorkAssignments_JobTypeId",
                table: "EmployeeWorkAssignments",
                column: "JobTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeWorkAssignments_ReassignedFromId",
                table: "EmployeeWorkAssignments",
                column: "ReassignedFromId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeWorkAssignments_WorkActivityId",
                table: "EmployeeWorkAssignments",
                column: "WorkActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeWorkAssignments_WorkJobId_JobItemId",
                table: "EmployeeWorkAssignments",
                columns: new[] { "WorkJobId", "JobItemId" });

            migrationBuilder.AddForeignKey(
                name: "FK_DailyWorkEntries_EmployeeWorkAssignments_AssignmentId",
                table: "DailyWorkEntries",
                column: "AssignmentId",
                principalTable: "EmployeeWorkAssignments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DailyWorkEntries_EmployeeWorkAssignments_AssignmentId",
                table: "DailyWorkEntries");

            migrationBuilder.DropTable(
                name: "EmployeeWorkAssignmentHistories");

            migrationBuilder.DropTable(
                name: "EmployeeWorkAssignments");

            migrationBuilder.DropIndex(
                name: "IX_DailyWorkEntries_AssignmentId",
                table: "DailyWorkEntries");

            migrationBuilder.DropColumn(
                name: "AdhocReason",
                table: "DailyWorkEntries");

            migrationBuilder.DropColumn(
                name: "AssignmentId",
                table: "DailyWorkEntries");

            migrationBuilder.DropColumn(
                name: "WorkDoneToday",
                table: "DailyWorkEntries");
        }
    }
}
