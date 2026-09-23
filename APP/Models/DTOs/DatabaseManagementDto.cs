using System;
using System.ComponentModel.DataAnnotations;

namespace APP.Models.DTOs
{
    /// <summary>Mirrors Application.DTOs.DatabaseManagement.DatabaseManagementSettingsDto (API) - see that file's remarks.</summary>
    public class DatabaseManagementSettingsDto
    {
        public string? Id { get; set; }

        [Display(Name = "Auto-Backup Before Update")]
        public bool AutoBackupBeforeUpdate { get; set; } = true;

        [Range(1024, 1024L * 1024 * 1024, ErrorMessage = "Max script file size must be between 1 KB and 1 GB.")]
        [Display(Name = "Max Script File Size (bytes)")]
        public long MaxScriptFileSizeBytes { get; set; } = 10 * 1024 * 1024;

        [Required(ErrorMessage = "Allowed environments is required.")]
        [MaxLength(500)]
        [Display(Name = "Allowed Environments (comma-separated)")]
        public string AllowedEnvironments { get; set; } = "Development,Local IIS";

        [Range(5, 600, ErrorMessage = "SQL command timeout must be between 5 and 600 seconds.")]
        [Display(Name = "SQL Command Timeout (seconds)")]
        public int SqlCommandTimeoutSeconds { get; set; } = 60;

        [Required(ErrorMessage = "Backup directory path is required.")]
        [MaxLength(500)]
        [Display(Name = "Backup Directory Path")]
        public string BackupDirectoryPath { get; set; } = "";

        [Range(1, 365, ErrorMessage = "Backup retention must be between 1 and 365 days.")]
        [Display(Name = "Backup Retention (days)")]
        public int BackupRetentionDays { get; set; } = 30;

        [Required]
        [Display(Name = "Risky Statement Validation Policy")]
        public string RiskyStatementValidationPolicy { get; set; } = "Strict";

        [Display(Name = "Require Confirmation Before Execution")]
        public bool RequireConfirmationBeforeExecution { get; set; } = true;

        [Display(Name = "Database Swap Enabled")]
        public bool DatabaseSwapEnabled { get; set; } = false;

        [Range(1, 365, ErrorMessage = "Execution log retention must be between 1 and 365 days.")]
        [Display(Name = "Execution Log Retention (days)")]
        public int ExecutionLogRetentionDays { get; set; } = 90;

        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }
    }

    public class DatabaseInfoDto
    {
        public bool Available { get; set; }
        public string? UnavailableReason { get; set; }
        public string? ServerName { get; set; }
        public string? DatabaseName { get; set; }
        public double? DatabaseSizeMb { get; set; }
        public DateTime? LastBackupOn { get; set; }
    }

    public class DatabaseOperationHistoryDto
    {
        public string Id { get; set; } = "";
        public string? OperationType { get; set; }
        public string? ScriptFileName { get; set; }
        public string? TargetServer { get; set; }
        public string? TargetDatabase { get; set; }
        public string? Environment { get; set; }
        public string? Action { get; set; }
        public string Status { get; set; } = "";
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? ExecutedBy { get; set; }
        public string? Message { get; set; }
        public int? AffectedObjectsCount { get; set; }
        public double? ExecutionDuration { get; set; }
    }

    public class DatabaseOperationHistoryFilterDto
    {
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string? OperationType { get; set; }
        public string? Status { get; set; }

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class DatabaseConnectionInfoDto
    {
        public string? ServerName { get; set; }
        public string? DatabaseName { get; set; }
    }
}
