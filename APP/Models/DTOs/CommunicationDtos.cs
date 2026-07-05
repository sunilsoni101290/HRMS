using System.ComponentModel.DataAnnotations;

namespace APP.Models.DTOs
{
    // ==============================
    // Announcement
    // ==============================

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

        [Display(Name = "Type")]
        public int AnnouncementType { get; set; } = 1;
        public string? TypeText { get; set; }

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

    // ==============================
    // Event
    // ==============================

    public class EventDto
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Please enter a Title.")]
        [MaxLength(200)]
        [Display(Name = "Title")]
        public string Title { get; set; }

        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Please select a Start Date.")]
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        [Required(ErrorMessage = "Please select an End Date.")]
        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Start Time")]
        public TimeSpan? StartTime { get; set; }

        [Display(Name = "End Time")]
        public TimeSpan? EndTime { get; set; }

        [Display(Name = "Location")]
        public string? Location { get; set; }

        [Display(Name = "Event Type")]
        public int EventType { get; set; } = 1;
        public string? TypeText { get; set; }

        [Display(Name = "Organized By")]
        public string? OrganizedBy { get; set; }

        [Display(Name = "For Everyone")]
        public bool IsForAll { get; set; } = true;

        [Display(Name = "Department")]
        public string? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }

        [Display(Name = "Role")]
        public string? RoleId { get; set; }
        public string? RoleName { get; set; }

        [Display(Name = "Send Reminder")]
        public bool SendReminder { get; set; }

        [Display(Name = "Reminder (minutes before)")]
        public int? ReminderBeforeMinutes { get; set; }

        public string? CompanyId { get; set; }
        public string? BranchId { get; set; }

        public int ParticipantCount { get; set; }

        public string? TenantId { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }
    }

    public class EventListDto
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public int EventType { get; set; }
        public string? TypeText { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Location { get; set; }
        public string? OrganizedBy { get; set; }
        public bool IsForAll { get; set; }
        public string? DepartmentName { get; set; }
        public int ParticipantCount { get; set; }
    }

    // ==============================
    // Notification
    // ==============================

    public class NotificationCreateDto
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Please enter a Title.")]
        [MaxLength(200)]
        [Display(Name = "Title")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Please enter a Message.")]
        [MaxLength(1000)]
        [Display(Name = "Message")]
        public string Message { get; set; }

        [Display(Name = "Type")]
        public string NotificationType { get; set; } = "Info";

        [Display(Name = "Module")]
        public string? NotificationModule { get; set; } = "HRMS";

        [Display(Name = "Redirect URL")]
        public string? RedirectUrl { get; set; }

        [Display(Name = "Priority (1 = High)")]
        public int Priority { get; set; } = 2;

        [Display(Name = "Expiry Date")]
        [DataType(DataType.Date)]
        public DateTime? ExpiryDate { get; set; }

        public string? FeatureId { get; set; }
        public string? ReferenceId { get; set; }

        public string? TenantId { get; set; }
        public string CreatedBy { get; set; }
    }

    public class MyNotificationDto
    {
        public string RecipientId { get; set; }
        public string NotificationId { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string? NotificationType { get; set; }
        public int Priority { get; set; }
        public string? RedirectUrl { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadDate { get; set; }
        public DateTime CreatedOn { get; set; }
    }

    public class NotificationListDto
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string? Message { get; set; }
        public string? NotificationType { get; set; }
        public int Priority { get; set; }
        public bool IsBroadcast { get; set; }
        public int RecipientCount { get; set; }
        public int ReadCount { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}
