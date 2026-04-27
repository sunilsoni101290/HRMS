using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class NotificationRecipient : BaseEntity
    {
        public string NotificationId { get; set; }
        public virtual Notification Notification { get; set; }

        public string UserId { get; set; }
        public virtual User User { get; set; }

        // Read Tracking
        public bool IsRead { get; set; } = false;
        public DateTime? ReadDate { get; set; }

        // Delivery Tracking
        public bool IsDelivered { get; set; }
        public DateTime? DeliveredDate { get; set; }
        public override string GetSequencePrefix() => "NR";
    }
}
