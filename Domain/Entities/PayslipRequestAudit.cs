using System;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    // One row per state transition on a PayslipRequest - who did it, when,
    // and with what remarks. Written alongside every mutation in
    // PayslipRequestService (Requested / ApprovedByManager /
    // RejectedByManager / ForwardedToFinance / PayslipGenerated /
    // Completed / RejectedByFinance / Downloaded), and surfaced as
    // PayslipRequestDto.Timeline on the Details view - same purpose as an
    // approval-chain history table elsewhere in this codebase, but kept as
    // its own simple append-only entity since PayslipRequest has no
    // multi-level chain to reuse.
    public class PayslipRequestAudit : BaseEntity
    {
        [Required]
        public string PayslipRequestId { get; set; }
        public virtual PayslipRequest PayslipRequest { get; set; }

        // "Requested" / "ApprovedByManager" / "RejectedByManager" /
        // "ForwardedToFinance" / "PayslipGenerated" / "Completed" /
        // "RejectedByFinance" / "Downloaded".
        [Required]
        public string Action { get; set; }

        // The acting User.Id (not EmployeeId) - resolved to a display name
        // the same way PayslipRequest.ManagerActionBy/FinanceActionBy are.
        [Required]
        public string PerformedBy { get; set; }

        [MaxLength(500)]
        public string? Remarks { get; set; }

        public DateTime PerformedOn { get; set; } = DateTime.UtcNow;

        public override string GetSequencePrefix() => "PRA";
    }
}
