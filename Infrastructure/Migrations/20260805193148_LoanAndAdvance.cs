using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LoanAndAdvance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ReferenceId",
                table: "Notifications",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "AdvanceTypes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MaxAmountSalaryMultiplier = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MaxInstallments = table.Column<int>(type: "int", nullable: false),
                    IsInterestFree = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdvanceTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdvanceTypes_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LoanAdvanceAttachments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EntityType = table.Column<int>(type: "int", nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UploadedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
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
                    table.PrimaryKey("PK_LoanAdvanceAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoanAdvanceAttachments_Users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LoanAdvanceAuditLogs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OldValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PerformedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PerformedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    PerformedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoanAdvanceAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoanAdvanceAuditLogs_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoanAdvanceAuditLogs_Users_PerformedByUserId",
                        column: x => x.PerformedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LoanTypes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    InterestMethod = table.Column<int>(type: "int", nullable: false),
                    DefaultInterestRatePercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaxTenureMonths = table.Column<int>(type: "int", nullable: false),
                    RequiresGuarantor = table.Column<bool>(type: "bit", nullable: false),
                    RequiresCollateral = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoanTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoanTypes_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeAdvances",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CompanyId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    BranchId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    EmployeeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AdvanceTypeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RequestedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ApprovedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    InstallmentCount = table.Column<int>(type: "int", nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CurrentApprovalLevel = table.Column<int>(type: "int", nullable: false),
                    MakerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MakerActionOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MakerRemarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DisbursedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DisbursedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DisbursementMode = table.Column<int>(type: "int", nullable: true),
                    DisbursementReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OutstandingAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ClosedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeAdvances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeAdvances_AdvanceTypes_AdvanceTypeId",
                        column: x => x.AdvanceTypeId,
                        principalTable: "AdvanceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeAdvances_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeAdvances_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeAdvances_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeAdvances_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeAdvances_Users_MakerId",
                        column: x => x.MakerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LoanPolicies",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CompanyId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    BranchId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    LoanTypeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MinAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MinTenureMonths = table.Column<int>(type: "int", nullable: false),
                    MaxTenureMonths = table.Column<int>(type: "int", nullable: false),
                    InterestRatePercent = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MinServiceMonthsRequired = table.Column<int>(type: "int", nullable: false),
                    MaxActiveLoans = table.Column<int>(type: "int", nullable: false),
                    MaxDeductionPercentOfNetSalary = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EligibilitySalaryMultiplier = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PreClosurePenaltyPercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoanPolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoanPolicies_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoanPolicies_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoanPolicies_LoanTypes_LoanTypeId",
                        column: x => x.LoanTypeId,
                        principalTable: "LoanTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoanPolicies_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AdvanceApprovalHistories",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmployeeAdvanceId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LevelNumber = table.Column<int>(type: "int", nullable: false),
                    CheckerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ActedAsDelegateForUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Decision = table.Column<int>(type: "int", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ActionOn = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    table.PrimaryKey("PK_AdvanceApprovalHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdvanceApprovalHistories_EmployeeAdvances_EmployeeAdvanceId",
                        column: x => x.EmployeeAdvanceId,
                        principalTable: "EmployeeAdvances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdvanceApprovalHistories_Users_ActedAsDelegateForUserId",
                        column: x => x.ActedAsDelegateForUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdvanceApprovalHistories_Users_CheckerId",
                        column: x => x.CheckerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AdvanceInstallments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmployeeAdvanceId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    InstallmentNumber = table.Column<int>(type: "int", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InstallmentAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RecoveredOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PayrollId = table.Column<string>(type: "nvarchar(450)", nullable: true),
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
                    table.PrimaryKey("PK_AdvanceInstallments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdvanceInstallments_EmployeeAdvances_EmployeeAdvanceId",
                        column: x => x.EmployeeAdvanceId,
                        principalTable: "EmployeeAdvances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdvanceInstallments_Payrolls_PayrollId",
                        column: x => x.PayrollId,
                        principalTable: "Payrolls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeLoans",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CompanyId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    BranchId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    EmployeeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoanTypeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoanPolicyId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RequestedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ApprovedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TenureMonths = table.Column<int>(type: "int", nullable: false),
                    InterestRatePercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    InterestMethod = table.Column<int>(type: "int", nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CurrentApprovalLevel = table.Column<int>(type: "int", nullable: false),
                    MakerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MakerActionOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MakerRemarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DisbursedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DisbursedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DisbursementMode = table.Column<int>(type: "int", nullable: true),
                    DisbursementReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OutstandingPrincipal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ClosedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosureReason = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeLoans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeLoans_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeLoans_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeLoans_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeLoans_LoanPolicies_LoanPolicyId",
                        column: x => x.LoanPolicyId,
                        principalTable: "LoanPolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeLoans_LoanTypes_LoanTypeId",
                        column: x => x.LoanTypeId,
                        principalTable: "LoanTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeLoans_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeLoans_Users_MakerId",
                        column: x => x.MakerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LoanPolicyApprovalLevels",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoanPolicyId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LevelNumber = table.Column<int>(type: "int", nullable: false),
                    ApproverType = table.Column<int>(type: "int", nullable: false),
                    ApproverRoleId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ApproverUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    MinAmountThreshold = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
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
                    table.PrimaryKey("PK_LoanPolicyApprovalLevels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoanPolicyApprovalLevels_LoanPolicies_LoanPolicyId",
                        column: x => x.LoanPolicyId,
                        principalTable: "LoanPolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoanPolicyApprovalLevels_Roles_ApproverRoleId",
                        column: x => x.ApproverRoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoanPolicyApprovalLevels_Users_ApproverUserId",
                        column: x => x.ApproverUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AdvancePaymentHistories",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmployeeAdvanceId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AdvanceInstallmentId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    PaymentSource = table.Column<int>(type: "int", nullable: false),
                    AmountPaid = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PayrollId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ReceiptReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
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
                    table.PrimaryKey("PK_AdvancePaymentHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdvancePaymentHistories_AdvanceInstallments_AdvanceInstallmentId",
                        column: x => x.AdvanceInstallmentId,
                        principalTable: "AdvanceInstallments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdvancePaymentHistories_EmployeeAdvances_EmployeeAdvanceId",
                        column: x => x.EmployeeAdvanceId,
                        principalTable: "EmployeeAdvances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdvancePaymentHistories_Payrolls_PayrollId",
                        column: x => x.PayrollId,
                        principalTable: "Payrolls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LoanApprovalHistories",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmployeeLoanId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LevelNumber = table.Column<int>(type: "int", nullable: false),
                    CheckerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ActedAsDelegateForUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Decision = table.Column<int>(type: "int", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ActionOn = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    table.PrimaryKey("PK_LoanApprovalHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoanApprovalHistories_EmployeeLoans_EmployeeLoanId",
                        column: x => x.EmployeeLoanId,
                        principalTable: "EmployeeLoans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoanApprovalHistories_Users_ActedAsDelegateForUserId",
                        column: x => x.ActedAsDelegateForUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoanApprovalHistories_Users_CheckerId",
                        column: x => x.CheckerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LoanEmiSchedules",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmployeeLoanId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    InstallmentNumber = table.Column<int>(type: "int", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OpeningBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PrincipalComponent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    InterestComponent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EmiAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ClosingBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RecoveredOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PayrollId = table.Column<string>(type: "nvarchar(450)", nullable: true),
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
                    table.PrimaryKey("PK_LoanEmiSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoanEmiSchedules_EmployeeLoans_EmployeeLoanId",
                        column: x => x.EmployeeLoanId,
                        principalTable: "EmployeeLoans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoanEmiSchedules_Payrolls_PayrollId",
                        column: x => x.PayrollId,
                        principalTable: "Payrolls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LoanPaymentHistories",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmployeeLoanId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoanEmiScheduleId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    PaymentSource = table.Column<int>(type: "int", nullable: false),
                    AmountPaid = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PrincipalPaid = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    InterestPaid = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PayrollId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ReceiptReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
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
                    table.PrimaryKey("PK_LoanPaymentHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoanPaymentHistories_EmployeeLoans_EmployeeLoanId",
                        column: x => x.EmployeeLoanId,
                        principalTable: "EmployeeLoans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoanPaymentHistories_LoanEmiSchedules_LoanEmiScheduleId",
                        column: x => x.LoanEmiScheduleId,
                        principalTable: "LoanEmiSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoanPaymentHistories_Payrolls_PayrollId",
                        column: x => x.PayrollId,
                        principalTable: "Payrolls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Reference_CreatedOn",
                table: "Notifications",
                columns: new[] { "ReferenceId", "CreatedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_AdvanceApprovalHistories_ActedAsDelegateForUserId",
                table: "AdvanceApprovalHistories",
                column: "ActedAsDelegateForUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AdvanceApprovalHistories_Advance",
                table: "AdvanceApprovalHistories",
                columns: new[] { "EmployeeAdvanceId", "LevelNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_AdvanceApprovalHistories_CheckerId",
                table: "AdvanceApprovalHistories",
                column: "CheckerId");

            migrationBuilder.CreateIndex(
                name: "IX_AdvanceInstallments_DueTracking",
                table: "AdvanceInstallments",
                columns: new[] { "Status", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AdvanceInstallments_PayrollId",
                table: "AdvanceInstallments",
                column: "PayrollId");

            migrationBuilder.CreateIndex(
                name: "UX_AdvanceInstallments",
                table: "AdvanceInstallments",
                columns: new[] { "EmployeeAdvanceId", "InstallmentNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdvancePaymentHistories_Advance_Date",
                table: "AdvancePaymentHistories",
                columns: new[] { "EmployeeAdvanceId", "PaymentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AdvancePaymentHistories_AdvanceInstallmentId",
                table: "AdvancePaymentHistories",
                column: "AdvanceInstallmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AdvancePaymentHistories_PaymentDate",
                table: "AdvancePaymentHistories",
                column: "PaymentDate");

            migrationBuilder.CreateIndex(
                name: "IX_AdvancePaymentHistories_PayrollId",
                table: "AdvancePaymentHistories",
                column: "PayrollId");

            migrationBuilder.CreateIndex(
                name: "UX_AdvanceTypes_Tenant_Code",
                table: "AdvanceTypes",
                columns: new[] { "TenantId", "Code" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAdvances_AdvanceTypeId",
                table: "EmployeeAdvances",
                column: "AdvanceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAdvances_BranchId",
                table: "EmployeeAdvances",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAdvances_CompanyId",
                table: "EmployeeAdvances",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAdvances_Employee_Status",
                table: "EmployeeAdvances",
                columns: new[] { "EmployeeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAdvances_MakerId",
                table: "EmployeeAdvances",
                column: "MakerId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAdvances_Tenant_Company_Branch",
                table: "EmployeeAdvances",
                columns: new[] { "TenantId", "CompanyId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAdvances_Tenant_Status",
                table: "EmployeeAdvances",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeLoans_BranchId",
                table: "EmployeeLoans",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeLoans_CompanyId",
                table: "EmployeeLoans",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeLoans_Employee_Status",
                table: "EmployeeLoans",
                columns: new[] { "EmployeeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeLoans_LoanPolicyId",
                table: "EmployeeLoans",
                column: "LoanPolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeLoans_LoanTypeId",
                table: "EmployeeLoans",
                column: "LoanTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeLoans_MakerId",
                table: "EmployeeLoans",
                column: "MakerId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeLoans_Tenant_Company_Branch",
                table: "EmployeeLoans",
                columns: new[] { "TenantId", "CompanyId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeLoans_Tenant_Status",
                table: "EmployeeLoans",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_LoanAdvanceAttachments_Entity",
                table: "LoanAdvanceAttachments",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_LoanAdvanceAttachments_UploadedByUserId",
                table: "LoanAdvanceAttachments",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanAdvanceAuditLogs_Entity",
                table: "LoanAdvanceAuditLogs",
                columns: new[] { "EntityType", "EntityId", "PerformedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_LoanAdvanceAuditLogs_PerformedByUserId",
                table: "LoanAdvanceAuditLogs",
                column: "PerformedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanAdvanceAuditLogs_TenantId",
                table: "LoanAdvanceAuditLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanApprovalHistories_ActedAsDelegateForUserId",
                table: "LoanApprovalHistories",
                column: "ActedAsDelegateForUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanApprovalHistories_CheckerId",
                table: "LoanApprovalHistories",
                column: "CheckerId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanApprovalHistories_Loan",
                table: "LoanApprovalHistories",
                columns: new[] { "EmployeeLoanId", "LevelNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_LoanEmiSchedules_DueTracking",
                table: "LoanEmiSchedules",
                columns: new[] { "Status", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_LoanEmiSchedules_PayrollId",
                table: "LoanEmiSchedules",
                column: "PayrollId");

            migrationBuilder.CreateIndex(
                name: "UX_LoanEmiSchedules",
                table: "LoanEmiSchedules",
                columns: new[] { "EmployeeLoanId", "InstallmentNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoanPaymentHistories_Loan_Date",
                table: "LoanPaymentHistories",
                columns: new[] { "EmployeeLoanId", "PaymentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_LoanPaymentHistories_LoanEmiScheduleId",
                table: "LoanPaymentHistories",
                column: "LoanEmiScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanPaymentHistories_PaymentDate",
                table: "LoanPaymentHistories",
                column: "PaymentDate");

            migrationBuilder.CreateIndex(
                name: "IX_LoanPaymentHistories_PayrollId",
                table: "LoanPaymentHistories",
                column: "PayrollId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanPolicies_BranchId",
                table: "LoanPolicies",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanPolicies_CompanyId",
                table: "LoanPolicies",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanPolicies_LoanTypeId",
                table: "LoanPolicies",
                column: "LoanTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanPolicies_TenantId",
                table: "LoanPolicies",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanPolicyApprovalLevels_ApproverRoleId",
                table: "LoanPolicyApprovalLevels",
                column: "ApproverRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanPolicyApprovalLevels_ApproverUserId",
                table: "LoanPolicyApprovalLevels",
                column: "ApproverUserId");

            migrationBuilder.CreateIndex(
                name: "UX_LoanPolicyApprovalLevels",
                table: "LoanPolicyApprovalLevels",
                columns: new[] { "LoanPolicyId", "LevelNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_LoanTypes_Tenant_Code",
                table: "LoanTypes",
                columns: new[] { "TenantId", "Code" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdvanceApprovalHistories");

            migrationBuilder.DropTable(
                name: "AdvancePaymentHistories");

            migrationBuilder.DropTable(
                name: "LoanAdvanceAttachments");

            migrationBuilder.DropTable(
                name: "LoanAdvanceAuditLogs");

            migrationBuilder.DropTable(
                name: "LoanApprovalHistories");

            migrationBuilder.DropTable(
                name: "LoanPaymentHistories");

            migrationBuilder.DropTable(
                name: "LoanPolicyApprovalLevels");

            migrationBuilder.DropTable(
                name: "AdvanceInstallments");

            migrationBuilder.DropTable(
                name: "LoanEmiSchedules");

            migrationBuilder.DropTable(
                name: "EmployeeAdvances");

            migrationBuilder.DropTable(
                name: "EmployeeLoans");

            migrationBuilder.DropTable(
                name: "AdvanceTypes");

            migrationBuilder.DropTable(
                name: "LoanPolicies");

            migrationBuilder.DropTable(
                name: "LoanTypes");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_Reference_CreatedOn",
                table: "Notifications");

            migrationBuilder.AlterColumn<string>(
                name: "ReferenceId",
                table: "Notifications",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);
        }
    }
}
