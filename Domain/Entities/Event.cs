using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    public class Event : BaseEntity
    {
        [Required, MaxLength(200)]
        public string Title { get; set; }

        public string Description { get; set; }

        // Event Timing
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }

        // Location
        public string Location { get; set; }

        public EventType EventType { get; set; }
        // Meeting / Holiday / Training / Celebration

        // Organizer
        public string OrganizedBy { get; set; }

        // Target Audience
        public bool IsForAll { get; set; } = true;

        public string DepartmentId { get; set; }
        public virtual Department Department { get; set; }

        public string RoleId { get; set; }
        public virtual Role Role { get; set; }

        // Reminder
        public bool SendReminder { get; set; }
        public int? ReminderBeforeMinutes { get; set; }

        // Multi-Tenant
        public string CompanyId { get; set; }
        public string BranchId { get; set; }
        public ICollection<EventParticipant> Participants { get; set; }
        public override string GetSequencePrefix() => "EVT";
    }

    public class EventParticipant : BaseEntity
    {
        public string EventId { get; set; }
        public virtual Event Event { get; set; }

        public string EmployeeId { get; set; }
        public virtual Employee Employee { get; set; }

        public EventParticipantStatus Status { get; set; }
        // Invited / Accepted / Declined

        public override string GetSequencePrefix() => "EVP";
    }
}
