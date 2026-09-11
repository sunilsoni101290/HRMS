using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class pendingMigrationApply : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourceTemplateId",
                table: "SalaryStructures",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SalaryTemplates",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    table.PrimaryKey("PK_SalaryTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalaryTemplateDetails",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SalaryTemplateId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SalaryComponentId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CalculationType = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_SalaryTemplateDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalaryTemplateDetails_SalaryComponents_SalaryComponentId",
                        column: x => x.SalaryComponentId,
                        principalTable: "SalaryComponents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalaryTemplateDetails_SalaryTemplates_SalaryTemplateId",
                        column: x => x.SalaryTemplateId,
                        principalTable: "SalaryTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalaryStructures_SourceTemplateId",
                table: "SalaryStructures",
                column: "SourceTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryTemplateDetails_SalaryComponentId",
                table: "SalaryTemplateDetails",
                column: "SalaryComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryTemplateDetails_SalaryTemplateId",
                table: "SalaryTemplateDetails",
                column: "SalaryTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryTemplates_Name",
                table: "SalaryTemplates",
                column: "Name");

            migrationBuilder.AddForeignKey(
                name: "FK_SalaryStructures_SalaryTemplates_SourceTemplateId",
                table: "SalaryStructures",
                column: "SourceTemplateId",
                principalTable: "SalaryTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalaryStructures_SalaryTemplates_SourceTemplateId",
                table: "SalaryStructures");

            migrationBuilder.DropTable(
                name: "SalaryTemplateDetails");

            migrationBuilder.DropTable(
                name: "SalaryTemplates");

            migrationBuilder.DropIndex(
                name: "IX_SalaryStructures_SourceTemplateId",
                table: "SalaryStructures");

            migrationBuilder.DropColumn(
                name: "SourceTemplateId",
                table: "SalaryStructures");
        }
    }
}
