using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Communication
{
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

        // EventType: 1 Meeting / 2 Holiday / 3 Training / 4 Celebration
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
}
