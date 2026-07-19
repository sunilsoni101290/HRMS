using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDelegationAndReminders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastReminderSentOn",
                table: "LeaveApplications",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ApprovalDelegations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DelegatorEmployeeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DelegateEmployeeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalDelegations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalDelegations_Employees_DelegateEmployeeId",
                        column: x => x.DelegateEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApprovalDelegations_Employees_DelegatorEmployeeId",
                        column: x => x.DelegatorEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalDelegations_DelegateEmployeeId",
                table: "ApprovalDelegations",
                column: "DelegateEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalDelegations_DelegatorEmployeeId_StartDate_EndDate",
                table: "ApprovalDelegations",
                columns: new[] { "DelegatorEmployeeId", "StartDate", "EndDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApprovalDelegations");

            migrationBuilder.DropColumn(
                name: "LastReminderSentOn",
                table: "LeaveApplications");
        }
    }
}
