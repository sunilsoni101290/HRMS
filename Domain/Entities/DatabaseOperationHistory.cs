using System;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    /// <summary>
    /// One row per database operation (script update / restore / swap) run
    /// through the Database Management module. Schema-only in Phase A - the
    /// Execution History tab reads this table (currently always empty; no
    /// writer exists yet), and later phases (B "Execute on Local DB", C
    /// "Restore", D "Database Swap") will start inserting rows here instead
    /// of requiring a second schema change. See the root
    /// "add DatabaseManagement module tables.sql" script.
    /// </summary>
    public class DatabaseOperationHistory : BaseEntity
    {
        /// <summary>"ScriptUpdate" | "Restore" | "Swap" - free-text, validated server-side by whichever later phase writes it.</summary>
        [Required]
        [MaxLength(50)]
        public string OperationType { get; set; } = "";

        [MaxLength(300)]
        public string? ScriptFileName { get; set; }

        /// <summary>SHA-256 of the executed script's content, for tamper-evidence / re-run detection.</summary>
        [MaxLength(100)]
        public string? ScriptHash { get; set; }

        [MaxLength(300)]
        public string? TargetServer { get; set; }

        [MaxLength(200)]
        public string? TargetDatabase { get; set; }

        /// <summary>Must match one of DatabaseManagementSettings.AllowedEnvironments at the time the operation ran.</summary>
        [MaxLength(100)]
        public string? Environment { get; set; }

        /// <summary>Human-readable summary of what step/action this row represents (e.g. "Execute Script", "Restore from Backup").</summary>
        [MaxLength(200)]
        public string? Action { get; set; }

        /// <summary>"Pending" | "Running" | "Success" | "Failed" | "Cancelled".</summary>
        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "Pending";

        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        [MaxLength(450)]
        public string? ExecutedBy { get; set; }

        /// <summary>FK (by convention, not a DB-enforced FK - see DatabaseBackupHistory.Id) to the backup taken immediately before this operation, when AutoBackupBeforeUpdate applied.</summary>
        [MaxLength(450)]
        public string? BackupId { get; set; }

        [MaxLength(1000)]
        public string? Message { get; set; }

        public string? ErrorDetails { get; set; }

        public int? AffectedObjectsCount { get; set; }

        /// <summary>Wall-clock duration of the operation, in seconds.</summary>
        public double? ExecutionDuration { get; set; }

        public override string GetSequencePrefix() => "DOH";
    }
}
