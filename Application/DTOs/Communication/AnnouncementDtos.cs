using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Communication
{
    public class AnnouncementDto
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Please enter a Title.")]
        [MaxLength(200)]
        [Display(Name = "Title")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Please enter a Message.")]
        [MaxLength(2000)]
        [Display(Name = "Message")]
        public string Message { get; set; }

        // AnnouncementType: 1 General / 2 Policy / 3 Holiday / 4 Urgent / 5 Achievement / 6 Important
        [Display(Name = "Type")]
        public int AnnouncementType { get; set; } = 1;
        public string? TypeText { get; set; }

        // AnnouncementPriority: 1 High / 2 Medium
        [Display(Name = "Priority")]
        public int Priority { get; set; } = 2;
        public string? PriorityText { get; set; }

        [Display(Name = "Publish Date")]
        [DataType(DataType.Date)]
        public DateTime PublishDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Expiry Date")]
        [DataType(DataType.Date)]
        public DateTime? ExpiryDate { get; set; }

        [Display(Name = "For Everyone")]
        public bool IsForAll { get; set; } = true;

        [Display(Name = "Department")]
        public string? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }

        [Display(Name = "Role")]
        public string? RoleId { get; set; }
        public string? RoleName { get; set; }

        [Display(Name = "Attachment")]
        public string? AttachmentUrl { get; set; }

        public string? CompanyId { get; set; }
        public string? BranchId { get; set; }

        public string? TenantId { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }
    }

    public class AnnouncementListDto
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string? Message { get; set; }
        public int AnnouncementType { get; set; }
        public string? TypeText { get; set; }
        public int Priority { get; set; }
        public string? PriorityText { get; set; }
        public DateTime PublishDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public bool IsForAll { get; set; }
        public string? DepartmentName { get; set; }
        public bool IsActive { get; set; }
    }
}
