using System;

namespace Application.DTOs.EmployeeLifecycle
{
    // Shared Create/Update shape - this codebase's convention varies
    // between separate Create/Update DTOs per feature, but a single shared
    // shape is fine here since there's no workflow-state complexity (no
    // maker-checker, no Status/Recommendation fields that differ between
    // create and update). EmployeeId is only meaningful on Create (Update
    // never moves feedback to a different employee - EmployeeFeedbackService
    // ignores it on update).
    public class CreateUpdateEmployeeFeedbackDto
    {
        public string EmployeeId { get; set; }

        public DateTime FeedbackDate { get; set; }

        public int Category { get; set; }

        public int? Rating { get; set; }

        public string? Strengths { get; set; }
        public string? AreasOfImprovement { get; set; }
        public string? Comments { get; set; }

        public bool IsVisibleToEmployee { get; set; } = true;
    }
}
