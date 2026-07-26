using System;

namespace Application.DTOs.EmployeeLifecycle
{
    // Full read/response shape for an EmployeeFeedback record - see
    // Domain/Entities/EmployeeFeedback.cs. No Maker/Checker fields (unlike
    // ProbationConfirmationDto/PipRecordDto/EmployeeTransferDto) - this
    // feature has no maker-checker workflow.
    public class EmployeeFeedbackDto
    {
        public string Id { get; set; }

        public string EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public string? EmployeeCode { get; set; }

        public string GivenByUserId { get; set; }
        public string? GivenByName { get; set; }

        public DateTime FeedbackDate { get; set; }

        public int Category { get; set; }
        public string? CategoryName { get; set; }

        public int? Rating { get; set; }

        public string? Strengths { get; set; }
        public string? AreasOfImprovement { get; set; }
        public string? Comments { get; set; }

        public bool IsVisibleToEmployee { get; set; }

        public string? TenantId { get; set; }
        public DateTime CreatedOn { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }
    }
}
