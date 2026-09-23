using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    /// <summary>
    /// Editable configuration for the "Database Management" module (Phase A
    /// of a multi-phase build - see the root "add DatabaseManagement module
    /// tables.sql" script). One row per tenant, same singleton-per-tenant
    /// convention as Domain/Entities/EsslIntegrationSetting.cs (upserted by
    /// TenantId, never duplicated - see
    /// ApplicationDbContext.OnModelCreating's unique index on TenantId).
    ///
    /// Phase A only reads/writes this table via the Settings tab - the
    /// actual Backup/Update/Restore/Swap behavior these settings will
    /// govern (auto-backup-before-update, risky-statement validation, the
    /// confirmation gate, etc.) ships in later phases. This entity holds
    /// the full field list from the start so later phases don't need
    /// repeated schema churn.
    /// </summary>
    public class DatabaseManagementSettings : BaseEntity
    {
        /// <summary>Automatically take a backup before any Database Update is executed (Phase B/C concern - Phase A only persists the flag).</summary>
        public bool AutoBackupBeforeUpdate { get; set; } = true;

        /// <summary>Largest .sql script file the Execute step will accept, in bytes.</summary>
        [Range(1024, 1024L * 1024 * 1024)]
        public long MaxScriptFileSizeBytes { get; set; } = 10 * 1024 * 1024; // 10 MB

        /// <summary>Comma-separated list of environment names this module is allowed to target (e.g. "Development,Local IIS").</summary>
        [Required]
        [MaxLength(500)]
        public string AllowedEnvironments { get; set; } = "Development,Local IIS";

        /// <summary>Command timeout (seconds) used for any SQL this module executes against the target database.</summary>
        [Range(5, 600)]
        public int SqlCommandTimeoutSeconds { get; set; } = 60;

        [Required]
        [MaxLength(500)]
        public string BackupDirectoryPath { get; set; } = @"C:\IIS\ERP\Backups";

        /// <summary>How many days a backup file/record is retained before later phases' cleanup may remove it.</summary>
        [Range(1, 365)]
        public int BackupRetentionDays { get; set; } = 30;

        /// <summary>
        /// Name of the policy later phases' script-validation step will
        /// enforce before allowing an Execute (e.g. "Strict" blocks
        /// DROP/TRUNCATE/DELETE-without-WHERE, "Warn" allows with an
        /// acknowledgement, "Off" disables validation). Free-text on
        /// purpose so a later phase can add policy names without a schema
        /// change; validated server-side against a small allowed set.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string RiskyStatementValidationPolicy { get; set; } = "Strict";

        /// <summary>Require an explicit confirmation step (e.g. typed database name) immediately before Execute runs.</summary>
        public bool RequireConfirmationBeforeExecution { get; set; } = true;

        /// <summary>Master switch for the Database Swap tab (Phase D). Off by default until that phase ships.</summary>
        public bool DatabaseSwapEnabled { get; set; } = false;

        /// <summary>How many days of DatabaseOperationHistory/DatabaseBackupHistory rows are retained before later phases' cleanup may purge them.</summary>
        [Range(1, 365)]
        public int ExecutionLogRetentionDays { get; set; } = 90;

        public override string GetSequencePrefix() => "DBM";
    }
}
