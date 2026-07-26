using System;

namespace Application.DTOs.EmployeeLifecycle
{
    // Full read/response shape for a PipRecord (Maker-Checker on the final
    // outcome resolution only) - see Domain/Entities/PipRecord.cs. Mirrors
    // ProbationConfirmationDto's Maker*/Status/StatusName/Checker* shape.
    public class PipRecordDto
    {
        public string Id { get; set; }

        public string EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public string? EmployeeCode { get; set; }
        public string? DepartmentName { get; set; }
        public string? DesignationName { get; set; }

        public string ProbationConfirmationId { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Goals { get; set; }

        public DateTime? MidReviewDate { get; set; }
        public string? MidReviewNotes { get; set; }

        public int FinalOutcome { get; set; }
        public string? FinalOutcomeName { get; set; }

        // ---- Maker ----
        public string? MakerId { get; set; }
        public string? MakerName { get; set; }
        public DateTime? MakerActionOn { get; set; }
        public string? MakerRemarks { get; set; }

        public int Status { get; set; }
        public string? StatusName { get; set; }

        // ---- Checker ----
        public string? CheckerId { get; set; }
        public string? CheckerName { get; set; }
        public DateTime? CheckerActionOn { get; set; }
        public string? CheckerRemarks { get; set; }

        public string? TenantId { get; set; }
        public DateTime CreatedOn { get; set; }
        public string? CreatedBy { get; set; }
    }
}
