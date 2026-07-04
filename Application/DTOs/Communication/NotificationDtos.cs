using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Communication
{
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

        // Info / Warning / Success / Error
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
