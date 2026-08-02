using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Pendingchanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PayslipRequests",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmployeeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PayrollId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PayrollYear = table.Column<int>(type: "int", nullable: false),
                    PayrollMonth = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ManagerRemarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ManagerActionBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ManagerActionOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FinanceRemarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FinanceActionBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FinanceActionOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DocumentUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DocumentFileName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GeneratedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GeneratedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompletedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_PayslipRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayslipRequests_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayslipRequests_Payrolls_PayrollId",
                        column: x => x.PayrollId,
                        principalTable: "Payrolls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaxDeclarations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmployeeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FinancialYearId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Regime = table.Column<int>(type: "int", nullable: false),
                    Section80C = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Section80CCD1B = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Section80D = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Section24B = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OtherDeductions = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AnnualRentPaid = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsMetroCity = table.Column<bool>(type: "bit", nullable: false),
                    LandlordPAN = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SubmittedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VerifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VerifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VerifierRemarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_TaxDeclarations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxDeclarations_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaxDeclarations_FinancialYears_FinancialYearId",
                        column: x => x.FinancialYearId,
                        principalTable: "FinancialYears",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaxSlabs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FinancialYearId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Regime = table.Column<int>(type: "int", nullable: false),
                    SlabOrder = table.Column<int>(type: "int", nullable: false),
                    MinIncome = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaxIncome = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    RatePercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
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
                    table.PrimaryKey("PK_TaxSlabs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxSlabs_FinancialYears_FinancialYearId",
                        column: x => x.FinancialYearId,
                        principalTable: "FinancialYears",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayslipRequestAudits",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PayslipRequestId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_PayslipRequestAudits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayslipRequestAudits_PayslipRequests_PayslipRequestId",
                        column: x => x.PayslipRequestId,
                        principalTable: "PayslipRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeTaxComputations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmployeeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FinancialYearId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TaxDeclarationId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Regime = table.Column<int>(type: "int", nullable: false),
                    AnnualGrossSalary = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    StandardDeduction = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    HraExemption = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalChapterVIADeductions = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxableIncome = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxBeforeCess = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Rebate87A = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    HealthEducationCess = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AnnualTaxLiability = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TdsDeductedTillDate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MonthlyTdsForRemainingMonths = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ComputedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ComputedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_EmployeeTaxComputations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeTaxComputations_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeTaxComputations_FinancialYears_FinancialYearId",
                        column: x => x.FinancialYearId,
                        principalTable: "FinancialYears",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeTaxComputations_TaxDeclarations_TaxDeclarationId",
                        column: x => x.TaxDeclarationId,
                        principalTable: "TaxDeclarations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeTaxComputations_EmployeeId_FinancialYearId",
                table: "EmployeeTaxComputations",
                columns: new[] { "EmployeeId", "FinancialYearId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeTaxComputations_FinancialYearId",
                table: "EmployeeTaxComputations",
                column: "FinancialYearId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeTaxComputations_TaxDeclarationId",
                table: "EmployeeTaxComputations",
                column: "TaxDeclarationId");

            migrationBuilder.CreateIndex(
                name: "IX_PayslipRequestAudits_PayslipRequestId",
                table: "PayslipRequestAudits",
                column: "PayslipRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_PayslipRequests_EmployeeId",
                table: "PayslipRequests",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PayslipRequests_PayrollId",
                table: "PayslipRequests",
                column: "PayrollId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxDeclarations_EmployeeId_FinancialYearId",
                table: "TaxDeclarations",
                columns: new[] { "EmployeeId", "FinancialYearId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxDeclarations_FinancialYearId",
                table: "TaxDeclarations",
                column: "FinancialYearId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxDeclarations_TenantId_Status",
                table: "TaxDeclarations",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxSlabs_FinancialYearId_Regime",
                table: "TaxSlabs",
                columns: new[] { "FinancialYearId", "Regime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeTaxComputations");

            migrationBuilder.DropTable(
                name: "PayslipRequestAudits");

            migrationBuilder.DropTable(
                name: "TaxSlabs");

            migrationBuilder.DropTable(
                name: "TaxDeclarations");

            migrationBuilder.DropTable(
                name: "PayslipRequests");
        }
    }
}
