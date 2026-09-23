using System;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    /// <summary>
    /// One row per database backup taken through the Database Management
    /// module. Schema-only in Phase A - no writer exists yet (Phase B adds
    /// the actual "Backup Current DB" step); the Database Info panel's
    /// "Last Backup" value reads MAX(CompletedAt) WHERE Status = 'Success'
    /// from this table and correctly shows "Never" until a row exists. See
    /// the root "add DatabaseManagement module tables.sql" script.
    /// </summary>
    public class DatabaseBackupHistory : BaseEntity
    {
        [MaxLength(300)]
        public string? ServerName { get; set; }

        [MaxLength(200)]
        public string? DatabaseName { get; set; }

        [MaxLength(1000)]
        public string? BackupFilePath { get; set; }

        public long? BackupSizeBytes { get; set; }

        /// <summary>"Pending" | "Running" | "Success" | "Failed".</summary>
        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "Pending";

        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        [MaxLength(450)]
        public string? InitiatedBy { get; set; }

        /// <summary>"Manual" | "AutoBeforeUpdate" | "Scheduled" - what triggered this backup.</summary>
        [MaxLength(50)]
        public string? TriggerType { get; set; }

        [MaxLength(1000)]
        public string? Message { get; set; }

        public string? ErrorDetails { get; set; }

        /// <summary>Days this backup is retained for, copied from DatabaseManagementSettings.BackupRetentionDays at the time it was taken (so a later settings change never retroactively changes an existing backup's retention).</summary>
        public int? RetentionDays { get; set; }

        public override string GetSequencePrefix() => "DBH";
    }
}
