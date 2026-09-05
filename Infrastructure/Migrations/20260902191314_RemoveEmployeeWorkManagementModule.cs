using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveEmployeeWorkManagementModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyWorkEntries");

            migrationBuilder.DropTable(
                name: "DailyWorkLogApprovalHistories");

            migrationBuilder.DropTable(
                name: "EmployeeWorkAssignmentHistories");

            migrationBuilder.DropTable(
                name: "WorkEntryReasons");

            migrationBuilder.DropTable(
                name: "DailyWorkLogs");

            migrationBuilder.DropTable(
                name: "EmployeeWorkAssignments");

            migrationBuilder.DropTable(
                name: "JobItems");

            migrationBuilder.DropTable(
                name: "WorkActivities");

            migrationBuilder.DropTable(
                name: "DocumentStatuses");

            migrationBuilder.DropTable(
                name: "WorkJobs");

            migrationBuilder.DropTable(
                name: "JobTypes");

            migrationBuilder.DropTable(
                name: "Clients");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clients",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DailyWorkLogs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmployeeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClockedHours = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubmittedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    WorkDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyWorkLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyWorkLogs_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DocumentStatuses",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JobTypes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    HasDisciplines = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkEntryReasons",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkEntryReasons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkJobs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClientId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    JobName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    JobNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkJobs_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DailyWorkLogApprovalHistories",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DailyWorkLogId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Action = table.Column<int>(type: "int", nullable: false),
                    ActionBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyWorkLogApprovalHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyWorkLogApprovalHistories_DailyWorkLogs_DailyWorkLogId",
                        column: x => x.DailyWorkLogId,
                        principalTable: "DailyWorkLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkActivities",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    JobTypeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AllowFreeTextOther = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RequiresReason = table.Column<bool>(type: "bit", nullable: false),
                    SkidsDiscipline = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WorkCategory = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkActivities_JobTypes_JobTypeId",
                        column: x => x.JobTypeId,
                        principalTable: "JobTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JobItems",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DocumentStatusId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    WorkJobId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ItemType = table.Column<int>(type: "int", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalWeightMT = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobItems_DocumentStatuses_DocumentStatusId",
                        column: x => x.DocumentStatusId,
                        principalTable: "DocumentStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JobItems_WorkJobs_WorkJobId",
                        column: x => x.WorkJobId,
                        principalTable: "WorkJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeWorkAssignments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmployeeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    JobItemId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    JobTypeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ReassignedFromId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    WorkActivityId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    WorkJobId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AssignedBy = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AssignmentType = table.Column<int>(type: "int", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EstimatedHours = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ExpectedEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Instructions = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    RejectedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
                name: "DailyWorkEntries",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AssignmentId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    DailyWorkLogId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    JobItemId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    JobTypeId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    WorkActivityId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    WorkEntryReasonId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    WorkJobId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    AdhocReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Hours = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WorkDoneToday = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyWorkEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyWorkEntries_DailyWorkLogs_DailyWorkLogId",
                        column: x => x.DailyWorkLogId,
                        principalTable: "DailyWorkLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DailyWorkEntries_EmployeeWorkAssignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "EmployeeWorkAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DailyWorkEntries_JobItems_JobItemId",
                        column: x => x.JobItemId,
                        principalTable: "JobItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DailyWorkEntries_JobTypes_JobTypeId",
                        column: x => x.JobTypeId,
                        principalTable: "JobTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DailyWorkEntries_WorkActivities_WorkActivityId",
                        column: x => x.WorkActivityId,
                        principalTable: "WorkActivities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DailyWorkEntries_WorkEntryReasons_WorkEntryReasonId",
                        column: x => x.WorkEntryReasonId,
                        principalTable: "WorkEntryReasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DailyWorkEntries_WorkJobs_WorkJobId",
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
                    Action = table.Column<int>(type: "int", nullable: false),
                    ActionBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
                name: "IX_DailyWorkEntries_DailyWorkLogId",
                table: "DailyWorkEntries",
                column: "DailyWorkLogId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyWorkEntries_JobItemId",
                table: "DailyWorkEntries",
                column: "JobItemId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyWorkEntries_JobTypeId",
                table: "DailyWorkEntries",
                column: "JobTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyWorkEntries_WorkActivityId",
                table: "DailyWorkEntries",
                column: "WorkActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyWorkEntries_WorkEntryReasonId",
                table: "DailyWorkEntries",
                column: "WorkEntryReasonId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyWorkEntries_WorkJobId_JobItemId",
                table: "DailyWorkEntries",
                columns: new[] { "WorkJobId", "JobItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_DailyWorkLogApprovalHistories_DailyWorkLogId",
                table: "DailyWorkLogApprovalHistories",
                column: "DailyWorkLogId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyWorkLogs_Employee_Date",
                table: "DailyWorkLogs",
                columns: new[] { "EmployeeId", "WorkDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyWorkLogs_TenantId_Status",
                table: "DailyWorkLogs",
                columns: new[] { "TenantId", "Status" });

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

            migrationBuilder.CreateIndex(
                name: "IX_JobItems_DocumentStatusId",
                table: "JobItems",
                column: "DocumentStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_JobItems_WorkJobId",
                table: "JobItems",
                column: "WorkJobId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkActivities_JobTypeId",
                table: "WorkActivities",
                column: "JobTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkJobs_ClientId",
                table: "WorkJobs",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkJobs_JobNumber",
                table: "WorkJobs",
                column: "JobNumber");
        }
    }
}
