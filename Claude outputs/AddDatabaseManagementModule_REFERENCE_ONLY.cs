using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDatabaseManagementModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DatabaseManagementSettings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AutoBackupBeforeUpdate = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    MaxScriptFileSizeBytes = table.Column<long>(type: "bigint", nullable: false, defaultValue: 10485760L),
                    AllowedEnvironments = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false, defaultValue: "Development,Local IIS"),
                    SqlCommandTimeoutSeconds = table.Column<int>(type: "int", nullable: false, defaultValue: 60),
                    BackupDirectoryPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false, defaultValue: "C:\\IIS\\ERP\\Backups"),
                    BackupRetentionDays = table.Column<int>(type: "int", nullable: false, defaultValue: 30),
                    RiskyStatementValidationPolicy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Strict"),
                    RequireConfirmationBeforeExecution = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    DatabaseSwapEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ExecutionLogRetentionDays = table.Column<int>(type: "int", nullable: false, defaultValue: 90),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatabaseManagementSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DatabaseOperationHistories",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OperationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ScriptFileName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ScriptHash = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TargetServer = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    TargetDatabase = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Environment = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Action = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Pending"),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExecutedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    BackupId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ErrorDetails = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AffectedObjectsCount = table.Column<int>(type: "int", nullable: true),
                    ExecutionDuration = table.Column<double>(type: "float", nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatabaseOperationHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DatabaseBackupHistories",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ServerName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    DatabaseName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    BackupFilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    BackupSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Pending"),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InitiatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    TriggerType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ErrorDetails = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RetentionDays = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatabaseBackupHistories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DatabaseManagementSettings_Tenant",
                table: "DatabaseManagementSettings",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DatabaseOperationHistories_Tenant_StartedAt",
                table: "DatabaseOperationHistories",
                columns: new[] { "TenantId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DatabaseOperationHistories_Tenant_Status",
                table: "DatabaseOperationHistories",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_DatabaseBackupHistories_Tenant_Status_CompletedAt",
                table: "DatabaseBackupHistories",
                columns: new[] { "TenantId", "Status", "CompletedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DatabaseBackupHistories");

            migrationBuilder.DropTable(
                name: "DatabaseOperationHistories");

            migrationBuilder.DropTable(
                name: "DatabaseManagementSettings");
        }
    }
}
