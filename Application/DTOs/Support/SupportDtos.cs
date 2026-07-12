using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using static Domain.Enums.EnumExtensions;

namespace Application.DTOs.Support
{
    public class SupportTicketListDto
    {
        public string Id { get; set; }

        public string EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public string? EmployeeCode { get; set; }

        public string Subject { get; set; }

        public TicketCategory Category { get; set; }
        public string? CategoryText { get; set; }

        public TicketPriority Priority { get; set; }
        public string? PriorityText { get; set; }

        public TicketStatus Status { get; set; }
        public string? StatusText { get; set; }

        public int ReplyCount { get; set; }

        public DateTime CreatedOn { get; set; }
        public DateTime? ResolvedDate { get; set; }
    }

    public class SupportTicketDto
    {
        public string Id { get; set; }

        public string EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public string? EmployeeCode { get; set; }

        [Required(ErrorMessage = "Subject is required.")]
        [MaxLength(200)]
        public string Subject { get; set; }

        [Required(ErrorMessage = "Please describe your issue.")]
        public string Description { get; set; }

        public TicketCategory Category { get; set; } = TicketCategory.General;
        public string? CategoryText { get; set; }

        public TicketPriority Priority { get; set; } = TicketPriority.Medium;
        public string? PriorityText { get; set; }

        public TicketStatus Status { get; set; }
        public string? StatusText { get; set; }

        public string? AttachmentUrl { get; set; }

        public string? ResolvedBy { get; set; }
        public DateTime? ResolvedDate { get; set; }

        public DateTime CreatedOn { get; set; }
        public string? CreatedBy { get; set; }

        public List<SupportTicketReplyDto> Replies { get; set; } = new();

        public string? TenantId { get; set; }
        public string? CompanyId { get; set; }
        public string? BranchId { get; set; }
    }

    public class SupportTicketReplyDto
    {
        public string Id { get; set; }
        public string SupportTicketId { get; set; }

        public string RepliedByUserId { get; set; }
        public string? RepliedByName { get; set; }

        // True when the reply came from an admin/HR/support user, rather
        // than the employee who raised the ticket - lets the UI style the
        // thread like a two-sided conversation.
        public bool IsSupportReply { get; set; }

        public string Message { get; set; }

        public DateTime CreatedOn { get; set; }
    }

    public class CreateSupportTicketRequestDto
    {
        public string? TenantId { get; set; }
        public string? CompanyId { get; set; }
        public string? BranchId { get; set; }

        [Required(ErrorMessage = "Employee is required.")]
        public string EmployeeId { get; set; }

        [Required(ErrorMessage = "Subject is required.")]
        [MaxLength(200)]
        public string Subject { get; set; }

        [Required(ErrorMessage = "Please describe your issue.")]
        public string Description { get; set; }

        public TicketCategory Category { get; set; } = TicketCategory.General;
        public TicketPriority Priority { get; set; } = TicketPriority.Medium;

        public string? AttachmentUrl { get; set; }

        public string? CreatedBy { get; set; }
    }

    public class AddReplyRequestDto
    {
        [Required(ErrorMessage = "Ticket is required.")]
        public string SupportTicketId { get; set; }

        [Required(ErrorMessage = "Please enter a message.")]
        public string Message { get; set; }

        // Never trusted from the client for authorization purposes - the
        // API resolves who's actually replying from the caller's own
        // session/JWT, this is only carried through the APP layer.
        public string RepliedByUserId { get; set; }
    }

    public class UpdateTicketStatusRequestDto
    {
        [Required(ErrorMessage = "Ticket is required.")]
        public string SupportTicketId { get; set; }

        public TicketStatus Status { get; set; }

        public string? UpdatedByUserId { get; set; }
    }

    public class FaqItemDto
    {
        public string? Id { get; set; }

        public string? CompanyId { get; set; }

        [Required(ErrorMessage = "Category is required.")]
        [MaxLength(100)]
        public string Category { get; set; }

        [Required(ErrorMessage = "Question is required.")]
        [MaxLength(300)]
        public string Question { get; set; }

        [Required(ErrorMessage = "Answer is required.")]
        public string Answer { get; set; }

        public int DisplayOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public string? TenantId { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }
    }
}
