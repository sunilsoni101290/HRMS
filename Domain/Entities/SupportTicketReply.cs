using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    public class SupportTicketReply : BaseEntity
    {
        [Required]
        public string SupportTicketId { get; set; }
        public SupportTicket? SupportTicket { get; set; }

        // The user who posted this message - could be the employee who
        // raised the ticket, or an admin/HR responder. Resolved
        // server-side from the caller's own session, never trusted from
        // client input.
        [Required]
        public string RepliedByUserId { get; set; }

        [Required]
        public string Message { get; set; }

        public override string GetSequencePrefix() => "TRP";
    }
}
