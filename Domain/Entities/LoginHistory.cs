using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Domain.Entities
{
    public class LoginHistory : BaseEntity
    {
        // Multi-Tenant
        [Required]
        public string TenantId { get; set; }
        public virtual Tenant Tenant { get; set; }

        // User
        [Required]
        public string UserId { get; set; }
        public virtual User User { get; set; }

        // Session Info
        public string SessionId { get; set; } // GUID for session tracking

        // Login Details
        public DateTime LoginTime { get; set; }
        public DateTime? LogoutTime { get; set; }

        public string LoginStatus { get; set; }
        // Success / Failed / Locked

        public string FailureReason { get; set; }

        // Network Info
        public string IPAddress { get; set; }
        public string DeviceInfo { get; set; } // Mobile / Desktop
        public string Browser { get; set; }    // Chrome, Edge
        public string OS { get; set; }         // Windows, Android

        // Location (Optional)
        public string Country { get; set; }
        public string City { get; set; }

        // Security
        public bool IsSuspicious { get; set; } // e.g. multiple failed attempts
        public override string GetSequencePrefix() => "LH";
    }
}
