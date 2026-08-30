using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    /// <summary>
    /// Persisted, editable configuration for the eSSL eTimeTrackLite1
    /// direct-SQL integration - one row per tenant (same convention as
    /// EsslAttendanceSyncState). Before this entity existed, connection
    /// settings lived only in appsettings.json ("EsslDatabase:*"), which
    /// is why EsslAttendanceDataSource still falls back to that section
    /// when no row exists yet for a tenant (keeps an existing deployment
    /// working unmodified until someone saves via the new Settings form).
    ///
    /// The password is NEVER stored in plain text - see EncryptedPassword,
    /// protected/unprotected via ASP.NET Core's built-in Data Protection API
    /// (IDataProtectionProvider - already part of the shared framework, no
    /// new package). It is never serialized to any DTO returned to the
    /// browser (see EsslDatabaseConfigViewDto, which has no password
    /// property at all, only a HasPasswordConfigured bool).
    /// </summary>
    public class EsslIntegrationSetting : BaseEntity
    {
        public bool IntegrationEnabled { get; set; }

        [Required]
        [MaxLength(300)]
        public string DatabaseServer { get; set; } = "";

        [Required]
        [MaxLength(200)]
        public string DatabaseName { get; set; } = "etimetracklite1";

        /// <summary>"Sql" or "Windows" - see Domain.Helper.EsslAuthenticationType constants.</summary>
        [Required]
        [MaxLength(20)]
        public string AuthenticationType { get; set; } = "Sql";

        [MaxLength(200)]
        public string? Username { get; set; }

        /// <summary>Data-Protection-encrypted at rest - never the raw password. Null/empty when AuthenticationType = "Windows".</summary>
        public string? EncryptedPassword { get; set; }

        [Range(1, 300)]
        public int ConnectionTimeout { get; set; } = 15;

        [Range(1, 1440)]
        public int SyncIntervalMinutes { get; set; } = 5;

        [Range(1, 5000)]
        public int BatchSize { get; set; } = 500;

        public override string GetSequencePrefix() => "EIS";
    }
}
