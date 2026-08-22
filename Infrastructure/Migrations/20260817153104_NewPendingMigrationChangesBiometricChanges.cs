using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NewPendingMigrationChangesBiometricChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CommKey",
                table: "BiometricDevices",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "BranchId",
                table: "BiometricAgents",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "BiometricAgents",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BiometricAgents_BranchId",
                table: "BiometricAgents",
                column: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_BiometricAgents_Branches_BranchId",
                table: "BiometricAgents",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BiometricAgents_Branches_BranchId",
                table: "BiometricAgents");

            migrationBuilder.DropIndex(
                name: "IX_BiometricAgents_BranchId",
                table: "BiometricAgents");

            migrationBuilder.DropColumn(
                name: "CommKey",
                table: "BiometricDevices");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "BiometricAgents");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "BiometricAgents");
        }
    }
}
