using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    public class Announcement : BaseEntity
    {
        [Required, MaxLength(200)]
        public string Title { get; set; }

        [Required, MaxLength(2000)]
        public string Message { get; set; }

        // Type
        public AnnouncementType AnnouncementType { get; set; }
        // General / Policy / Holiday / Urgent

        // Schedule
        public DateTime PublishDate { get; set; }
        public DateTime? ExpiryDate { get; set; }

        // Target Audience
        public bool IsForAll { get; set; } = true;

        public string? DepartmentId { get; set; }
        public virtual Department Department { get; set; }

        public string? RoleId { get; set; }
        public virtual Role Role { get; set; }

        // Attachment
        public string? AttachmentUrl { get; set; }

        // Priority
        public AnnouncementPriority Priority { get; set; }

        public string CompanyId { get; set; }
        public string? BranchId { get; set; }

        public override string GetSequencePrefix() => "ANN";
    }
}
