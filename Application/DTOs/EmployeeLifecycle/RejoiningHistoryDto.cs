using System;

namespace Application.DTOs.EmployeeLifecycle
{
    // Full read/response shape for a RejoiningHistory record - see
    // Domain/Entities/RejoiningHistory.cs. No Maker/Checker fields (unlike
    // ProbationConfirmationDto/PipRecordDto/EmployeeTransferDto) - this
    // feature has no maker-checker workflow, same as EmployeeFeedbackDto.
    public class RejoiningHistoryDto
    {
        public string Id { get; set; }

        public string EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public string? EmployeeCode { get; set; }

        public DateTime PreviousRelievingDate { get; set; }
        public DateTime PreviousJoiningDate { get; set; }
        public DateTime NewJoiningDate { get; set; }

        public string? Reason { get; set; }

        public string ProcessedByUserId { get; set; }
        public string? ProcessedByName { get; set; }
        public DateTime ProcessedOn { get; set; }

        public string? TenantId { get; set; }
        public DateTime CreatedOn { get; set; }
        public string? CreatedBy { get; set; }
    }
}
