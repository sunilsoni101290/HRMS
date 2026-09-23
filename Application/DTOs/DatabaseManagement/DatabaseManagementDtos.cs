using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Application.DTOs.Employee;

namespace Application.DTOs.DatabaseManagement
{
    // ==========================================================================
    // Settings tab (Phase A - fully functional CRUD against
    // DatabaseManagementSettings, one row per tenant).
    // ==========================================================================

    public class DatabaseManagementSettingsDto
    {
        public string? Id { get; set; }

        public bool AutoBackupBeforeUpdate { get; set; } = true;

        [Range(1024, 1024L * 1024 * 1024, ErrorMessage = "Max script file size must be between 1 KB and 1 GB.")]
        public long MaxScriptFileSizeBytes { get; set; } = 10 * 1024 * 1024;

        [Required(ErrorMessage = "Allowed environments is required.")]
        [MaxLength(500)]
        public string AllowedEnvironments { get; set; } = "Development,Local IIS";

        [Range(5, 600, ErrorMessage = "SQL command timeout must be between 5 and 600 seconds.")]
        public int SqlCommandTimeoutSeconds { get; set; } = 60;

        [Required(ErrorMessage = "Backup directory path is required.")]
        [MaxLength(500)]
        public string BackupDirectoryPath { get; set; } = "";

        [Range(1, 365, ErrorMessage = "Backup retention must be between 1 and 365 days.")]
        public int BackupRetentionDays { get; set; } = 30;

        [Required]
        [MaxLength(50)]
        public string RiskyStatementValidationPolicy { get; set; } = "Strict";

        public bool RequireConfirmationBeforeExecution { get; set; } = true;

        public bool DatabaseSwapEnabled { get; set; } = false;

        [Range(1, 365, ErrorMessage = "Execution log retention must be between 1 and 365 days.")]
        public int ExecutionLogRetentionDays { get; set; } = 90;

        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }
    }

    // ==========================================================================
    // Database Info panel (Phase A - real, read-only, live SQL Server data)
    // ==========================================================================

    public class DatabaseInfoDto
    {
        public bool Available { get; set; }
        public string? UnavailableReason { get; set; }

        public string? ServerName { get; set; }
        public string? DatabaseName { get; set; }

        /// <summary>Database size in MB, null when unavailable.</summary>
        public double? DatabaseSizeMb { get; set; }

        /// <summary>MAX(CompletedAt) from DatabaseBackupHistory WHERE Status = 'Success' - null/"Never" until a later phase writes a backup row.</summary>
        public DateTime? LastBackupOn { get; set; }
    }

    // ==========================================================================
    // Execution History tab (Phase A - reads DatabaseOperationHistory, always
    // empty for now; no writer exists until later phases).
    // ==========================================================================

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

    // ==========================================================================
    // Quick Actions - "Open SSMS" connection-info workflow (server/database
    // name only - never a password or the raw connection string).
    // ==========================================================================

    public class DatabaseConnectionInfoDto
    {
        public string? ServerName { get; set; }
        public string? DatabaseName { get; set; }
    }
}
