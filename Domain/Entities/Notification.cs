using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Domain.Entities
{
    public class Notification : BaseEntity
    {
        [Required, MaxLength(200)]
        public string Title { get; set; }

        [Required, MaxLength(1000)]
        public string Message { get; set; }

        // Feature / Module
        public string? FeatureId { get; set; }   // e.g. LEAVE, PAYROLL
        public string? ReferenceId { get; set; } // e.g. LeaveApplicationId

        // Type
        public string NotificationType { get; set; }
        // Info / Warning / Success / Error

        public string NotificationModule { get; set; }
        // HRMS / Billing / Inventory

        // Navigation (Frontend redirect)
        public string? RedirectUrl { get; set; }

        // Broadcast
        public bool IsBroadcast { get; set; }

        // Priority
        public int Priority { get; set; } // 1 = High

        // Expiry
        public DateTime? ExpiryDate { get; set; }
        
        // Navigation
        public ICollection<NotificationRecipient> Recipients { get; set; }
        public override string GetSequencePrefix() => "N";
    }
}
