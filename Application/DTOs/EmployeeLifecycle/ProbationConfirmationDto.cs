using System;

namespace Application.DTOs.EmployeeLifecycle
{
    // Full read/response shape for a ProbationConfirmation (Maker-Checker)
    // record - see Domain/Entities/ProbationConfirmation.cs. Later PIP
    // Outcome/Employee Transfer agents should mirror this DTO shape
    // (Maker*/Status/StatusName/Checker* fields) for their own equivalent
    // DTOs.
    public class ProbationConfirmationDto
    {
        public string Id { get; set; }

        public string EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public string? EmployeeCode { get; set; }
        public string? DepartmentName { get; set; }
        public string? DesignationName { get; set; }

        public DateTime ProbationStartDate { get; set; }
        public DateTime OriginalProbationEndDate { get; set; }

        public int Recommendation { get; set; }
        public string? RecommendationName { get; set; }

        public DateTime? ExtendedProbationEndDate { get; set; }

        // ---- Maker ----
        public string MakerId { get; set; }
        public string? MakerName { get; set; }
        public DateTime MakerActionOn { get; set; }
        public string? MakerRemarks { get; set; }

        public int Status { get; set; }
        public string? StatusName { get; set; }

        // ---- Checker ----
        public string? CheckerId { get; set; }
        public string? CheckerName { get; set; }
        public DateTime? CheckerActionOn { get; set; }
        public string? CheckerRemarks { get; set; }

        public DateTime? FinalConfirmationDate { get; set; }

        public string? TenantId { get; set; }
        public DateTime CreatedOn { get; set; }
        public string? CreatedBy { get; set; }
    }
}
