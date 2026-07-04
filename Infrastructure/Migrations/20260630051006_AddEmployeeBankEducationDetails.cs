using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeBankEducationDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BankDetails");

            migrationBuilder.DropColumn(
                name: "EmployeeContribution",
                table: "EmployeePFDetails");

            migrationBuilder.DropColumn(
                name: "EmployerContribution",
                table: "EmployeePFDetails");

            migrationBuilder.DropColumn(
                name: "EmployeeContribution",
                table: "EmployeeESICDetails");

            migrationBuilder.DropColumn(
                name: "EmployerContribution",
                table: "EmployeeESICDetails");

            migrationBuilder.RenameColumn(
                name: "IsPFApplicable",
                table: "EmployeePFDetails",
                newName: "IsInternationalWorker");

            migrationBuilder.RenameColumn(
                name: "IsESICApplicable",
                table: "EmployeeESICDetails",
                newName: "IsEligible");

            migrationBuilder.RenameColumn(
                name: "ESICJoiningDate",
                table: "EmployeeESICDetails",
                newName: "RegistrationDate");

            migrationBuilder.AddColumn<bool>(
                name: "EDLIApplicable",
                table: "EmployeePFDetails",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EPFApplicable",
                table: "EmployeePFDetails",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EPSApplicable",
                table: "EmployeePFDetails",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PFExitDate",
                table: "EmployeePFDetails",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PFOffice",
                table: "EmployeePFDetails",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Remarks",
                table: "EmployeePFDetails",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UANNumber",
                table: "EmployeePFDetails",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "EmployeeESICDetails",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ESICNumber",
                table: "EmployeeESICDetails",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<string>(
                name: "BranchOffice",
                table: "EmployeeESICDetails",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ESICDispensary",
                table: "EmployeeESICDetails",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExitDate",
                table: "EmployeeESICDetails",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Remarks",
                table: "EmployeeESICDetails",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "FilePath",
                table: "EmployeeDocuments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<int>(
                name: "DocumentType",
                table: "EmployeeDocuments",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "DocumentName",
                table: "EmployeeDocuments",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentNumber",
                table: "EmployeeDocuments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiryDate",
                table: "EmployeeDocuments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileExtension",
                table: "EmployeeDocuments",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "EmployeeDocuments",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "FileSize",
                table: "EmployeeDocuments",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsVerified",
                table: "EmployeeDocuments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "IssueDate",
                table: "EmployeeDocuments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IssuedBy",
                table: "EmployeeDocuments",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Remarks",
                table: "EmployeeDocuments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerifiedBy",
                table: "EmployeeDocuments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerifiedOn",
                table: "EmployeeDocuments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EmployeeBankDetails",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmployeeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BankName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    BranchName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    AccountNumber = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    AccountHolderName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IFSCCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MICRCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AccountType = table.Column<int>(type: "int", maxLength: 20, nullable: false),
                    CancelledChequeFilePath = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
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
                    table.PrimaryKey("PK_EmployeeBankDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeBankDetails_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeEducationDetails",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmployeeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Qualification = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Degree = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Specialization = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    InstituteName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    University = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Board = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PassingYear = table.Column<int>(type: "int", nullable: false),
                    PercentageOrCGPA = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Grade = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsHighestQualification = table.Column<bool>(type: "bit", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CertificateFilePath = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
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
                    table.PrimaryKey("PK_EmployeeEducationDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeEducationDetails_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeBankDetails_EmployeeId",
                table: "EmployeeBankDetails",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeEducationDetails_EmployeeId",
                table: "EmployeeEducationDetails",
                column: "EmployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeBankDetails");

            migrationBuilder.DropTable(
                name: "EmployeeEducationDetails");

            migrationBuilder.DropColumn(
                name: "EDLIApplicable",
                table: "EmployeePFDetails");

            migrationBuilder.DropColumn(
                name: "EPFApplicable",
                table: "EmployeePFDetails");

            migrationBuilder.DropColumn(
                name: "EPSApplicable",
                table: "EmployeePFDetails");

            migrationBuilder.DropColumn(
                name: "PFExitDate",
                table: "EmployeePFDetails");

            migrationBuilder.DropColumn(
                name: "PFOffice",
                table: "EmployeePFDetails");

            migrationBuilder.DropColumn(
                name: "Remarks",
                table: "EmployeePFDetails");

            migrationBuilder.DropColumn(
                name: "UANNumber",
                table: "EmployeePFDetails");

            migrationBuilder.DropColumn(
                name: "BranchOffice",
                table: "EmployeeESICDetails");

            migrationBuilder.DropColumn(
                name: "ESICDispensary",
                table: "EmployeeESICDetails");

            migrationBuilder.DropColumn(
                name: "ExitDate",
                table: "EmployeeESICDetails");

            migrationBuilder.DropColumn(
                name: "Remarks",
                table: "EmployeeESICDetails");

            migrationBuilder.DropColumn(
                name: "DocumentName",
                table: "EmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "DocumentNumber",
                table: "EmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "ExpiryDate",
                table: "EmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "FileExtension",
                table: "EmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "EmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "FileSize",
                table: "EmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "IsVerified",
                table: "EmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "IssueDate",
                table: "EmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "IssuedBy",
                table: "EmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "Remarks",
                table: "EmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "VerifiedBy",
                table: "EmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "VerifiedOn",
                table: "EmployeeDocuments");

            migrationBuilder.RenameColumn(
                name: "IsInternationalWorker",
                table: "EmployeePFDetails",
                newName: "IsPFApplicable");

            migrationBuilder.RenameColumn(
                name: "RegistrationDate",
                table: "EmployeeESICDetails",
                newName: "ESICJoiningDate");

            migrationBuilder.RenameColumn(
                name: "IsEligible",
                table: "EmployeeESICDetails",
                newName: "IsESICApplicable");

            migrationBuilder.AddColumn<decimal>(
                name: "EmployeeContribution",
                table: "EmployeePFDetails",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EmployerContribution",
                table: "EmployeePFDetails",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "EmployeeESICDetails",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ESICNumber",
                table: "EmployeeESICDetails",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<decimal>(
                name: "EmployeeContribution",
                table: "EmployeeESICDetails",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EmployerContribution",
                table: "EmployeeESICDetails",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FilePath",
                table: "EmployeeDocuments",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "DocumentType",
                table: "EmployeeDocuments",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateTable(
                name: "BankDetails",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmployeeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AccountHolderName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AccountNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BankName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IFSCCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankDetails_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BankDetails_EmployeeId",
                table: "BankDetails",
                column: "EmployeeId");
        }
    }
}
