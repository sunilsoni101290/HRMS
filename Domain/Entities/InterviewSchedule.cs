using System;
using System.Collections.Generic;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    public class InterviewSchedule : BaseEntity
    {
        public string CandidateApplicationId { get; set; }
        public CandidateApplication CandidateApplication { get; set; }

        public DateTime InterviewDate { get; set; }

        public string InterviewerId { get; set; }
        public Employee Interviewer { get; set; }

        public InterviewScheduleMode Mode { get; set; } // Online / Offline

        public InterviewScheduleStatus Status { get; set; } // Scheduled / Completed / Cancelled

        public string? Feedback { get; set; }
        public override string GetSequencePrefix() => "INT";
    }
}
