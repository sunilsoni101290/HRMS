using System.ComponentModel.DataAnnotations;

namespace APP.Models.DTOs
{
    // ==============================
    // Support Ticket
    // ==============================

    public class SupportTicketListDto
    {
        public string Id { get; set; }

        public string EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public string? EmployeeCode { get; set; }

        public string Subject { get; set; }

        public int Category { get; set; }
        public string? CategoryText { get; set; }

        public int Priority { get; set; }
        public string? PriorityText { get; set; }

        public int Status { get; set; }
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

        [Required(ErrorMessage = "Please enter a Subject.")]
        [MaxLength(200)]
        [Display(Name = "Subject")]
        public string Subject { get; set; }

        [Required(ErrorMessage = "Please describe your issue.")]
        [Display(Name = "Description")]
        public string Description { get; set; }

        [Display(Name = "Category")]
        public int Category { get; set; } = 6; // General
        public string? CategoryText { get; set; }

        [Display(Name = "Priority")]
        public int Priority { get; set; } = 2; // Medium
        public string? PriorityText { get; set; }

        public int Status { get; set; }
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

        public bool IsSupportReply { get; set; }

        public string Message { get; set; }

        public DateTime CreatedOn { get; set; }
    }

    public class CreateSupportTicketRequestDto
    {
        public string? TenantId { get; set; }
        public string? CompanyId { get; set; }
        public string? BranchId { get; set; }

        public string EmployeeId { get; set; }

        [Required(ErrorMessage = "Please enter a Subject.")]
        [MaxLength(200)]
        [Display(Name = "Subject")]
        public string Subject { get; set; }

        [Required(ErrorMessage = "Please describe your issue.")]
        [Display(Name = "Description")]
        public string Description { get; set; }

        [Display(Name = "Category")]
        public int Category { get; set; } = 6;

        [Display(Name = "Priority")]
        public int Priority { get; set; } = 2;

        public string? AttachmentUrl { get; set; }

        public string? CreatedBy { get; set; }
    }

    public class AddReplyRequestDto
    {
        public string SupportTicketId { get; set; }

        [Required(ErrorMessage = "Please enter a message.")]
        [Display(Name = "Message")]
        public string Message { get; set; }

        public string RepliedByUserId { get; set; }
    }

    public class UpdateTicketStatusRequestDto
    {
        public string SupportTicketId { get; set; }
        public int Status { get; set; }
        public string? UpdatedByUserId { get; set; }
    }

    // ==============================
    // FAQ / Knowledge Base
    // ==============================

    public class FaqItemDto
    {
        public string? Id { get; set; }

        public string? CompanyId { get; set; }

        [Required(ErrorMessage = "Please enter a Category.")]
        [MaxLength(100)]
        [Display(Name = "Category")]
        public string Category { get; set; }

        [Required(ErrorMessage = "Please enter a Question.")]
        [MaxLength(300)]
        [Display(Name = "Question")]
        public string Question { get; set; }

        [Required(ErrorMessage = "Please enter an Answer.")]
        [Display(Name = "Answer")]
        public string Answer { get; set; }

        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; } = 0;

        [Display(Name = "Published")]
        public bool IsActive { get; set; } = true;

        public string? TenantId { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }
    }
}
