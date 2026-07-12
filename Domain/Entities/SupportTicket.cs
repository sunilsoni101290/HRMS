using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    public class SupportTicket : BaseEntity
    {
        public string? CompanyId { get; set; }
        public string? BranchId { get; set; }

        // Who raised it - resolved server-side from the caller's own
        // session at creation time, never trusted from client input.
        [Required]
        public string EmployeeId { get; set; }
        public Employee? Employee { get; set; }

        [Required, MaxLength(200)]
        public string Subject { get; set; }

        [Required]
        public string Description { get; set; }

        public TicketCategory Category { get; set; } = TicketCategory.General;
        public TicketPriority Priority { get; set; } = TicketPriority.Medium;
        public TicketStatus Status { get; set; } = TicketStatus.Open;

        public string? AttachmentUrl { get; set; }

        // The admin/HR user currently handling this ticket, if any -
        // optional, set when an admin picks it up.
        public string? AssignedToUserId { get; set; }

        public DateTime? ResolvedDate { get; set; }
        public string? ResolvedBy { get; set; }

        public ICollection<SupportTicketReply>? Replies { get; set; }

        public override string GetSequencePrefix() => "TCK";
    }
}
