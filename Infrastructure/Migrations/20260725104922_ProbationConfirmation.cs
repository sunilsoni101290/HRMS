using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ProbationConfirmation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ProbationEndDate",
                table: "Employees",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProbationPeriodMonths",
                table: "Designations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "EmployeeFeedbacks",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmployeeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    GivenByUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FeedbackDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: true),
                    Strengths = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AreasOfImprovement = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsVisibleToEmployee = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeFeedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeFeedbacks_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeTransfers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmployeeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FromCompanyId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FromBranchId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FromDepartmentId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FromDesignationId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FromReportingManagerId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ToCompanyId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ToBranchId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ToDepartmentId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ToDesignationId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ToReportingManagerId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MakerId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MakerActionOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MakerRemarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CheckerId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CheckerActionOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CheckerRemarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeTransfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeTransfers_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProbationConfirmations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmployeeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProbationStartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OriginalProbationEndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Recommendation = table.Column<int>(type: "int", nullable: false),
                    ExtendedProbationEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MakerId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MakerActionOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MakerRemarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CheckerId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CheckerActionOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CheckerRemarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FinalConfirmationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProbationConfirmations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProbationConfirmations_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RejoiningHistories",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmployeeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PreviousRelievingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PreviousJoiningDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NewJoiningDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProcessedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProcessedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    table.PrimaryKey("PK_RejoiningHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RejoiningHistories_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PipRecords",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmployeeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProbationConfirmationId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Goals = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MidReviewDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MidReviewNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    FinalOutcome = table.Column<int>(type: "int", nullable: false),
                    ProposedFinalOutcome = table.Column<int>(type: "int", nullable: true),
                    MakerId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MakerActionOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MakerRemarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CheckerId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CheckerActionOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CheckerRemarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PipRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PipRecords_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PipRecords_ProbationConfirmations_ProbationConfirmationId",
                        column: x => x.ProbationConfirmationId,
                        principalTable: "ProbationConfirmations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeFeedbacks_EmployeeId",
                table: "EmployeeFeedbacks",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeFeedbacks_TenantId_EmployeeId",
                table: "EmployeeFeedbacks",
                columns: new[] { "TenantId", "EmployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeTransfers_EmployeeId",
                table: "EmployeeTransfers",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeTransfers_TenantId_Status",
                table: "EmployeeTransfers",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PipRecords_EmployeeId",
                table: "PipRecords",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PipRecords_ProbationConfirmationId",
                table: "PipRecords",
                column: "ProbationConfirmationId");

            migrationBuilder.CreateIndex(
                name: "IX_PipRecords_TenantId_FinalOutcome",
                table: "PipRecords",
                columns: new[] { "TenantId", "FinalOutcome" });

            migrationBuilder.CreateIndex(
                name: "IX_ProbationConfirmations_EmployeeId",
                table: "ProbationConfirmations",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProbationConfirmations_TenantId_Status",
                table: "ProbationConfirmations",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RejoiningHistories_EmployeeId",
                table: "RejoiningHistories",
                column: "EmployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeFeedbacks");

            migrationBuilder.DropTable(
                name: "EmployeeTransfers");

            migrationBuilder.DropTable(
                name: "PipRecords");

            migrationBuilder.DropTable(
                name: "RejoiningHistories");

            migrationBuilder.DropTable(
                name: "ProbationConfirmations");

            migrationBuilder.DropColumn(
                name: "ProbationEndDate",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "ProbationPeriodMonths",
                table: "Designations");
        }
    }
}
