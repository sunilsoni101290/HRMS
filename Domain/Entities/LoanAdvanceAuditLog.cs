using Domain.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    /// <summary>
    /// Append-only audit trail (FR-18 / Phase 16) covering every
    /// create/update/status-transition across every Loan &amp; Advance
    /// module entity - deliberately does NOT inherit
    /// <see cref="BaseEntity"/> (no IsDeleted/IsActive/soft-delete;
    /// audit rows are immutable and never removed), matching the existing
    /// <see cref="ApiRequestLog"/>/<see cref="ErrorLog"/> convention of a
    /// plain <see cref="IEntity"/> implementation for tenant-wide log
    /// tables. Written by LoanAuditInterceptor (Phase 16), read by
    /// ILoanAuditLogService.
    /// </summary>
    public class LoanAdvanceAuditLog : IEntity
    {
        [Key]
        public string Id { get; set; }

        /// <summary>e.g. "EmployeeLoan", "LoanType", "LoanPolicy" - the CLR entity name, not a table name.</summary>
        [Required]
        public string EntityType { get; set; }

        [Required]
        public string EntityId { get; set; }

        /// <summary>Create / Update / StatusChange / Approve / Reject / Disburse / Settle / PreClose.</summary>
        [Required]
        public string Action { get; set; }

        public string? OldValuesJson { get; set; }
        public string? NewValuesJson { get; set; }

        [Required]
        public string PerformedBy { get; set; }
        public virtual User PerformedByUser { get; set; }

        public DateTime PerformedOn { get; set; } = DateTime.UtcNow;

        public string? IpAddress { get; set; }

        [Required]
        public string TenantId { get; set; }
        public virtual Tenant Tenant { get; set; }

        public string GetSequencePrefix() => "LAL";

        public string GetKeyPrefix() => GetSequencePrefix();
    }
}
