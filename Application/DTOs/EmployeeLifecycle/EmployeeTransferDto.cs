using System;

namespace Application.DTOs.EmployeeLifecycle
{
    // Full read/response shape for an EmployeeTransfer (Maker-Checker)
    // record - see Domain/Entities/EmployeeTransfer.cs. Mirrors
    // ProbationConfirmationDto's/PipRecordDto's Maker*/Status/StatusName/
    // Checker* shape.
    public class EmployeeTransferDto
    {
        public string Id { get; set; }

        public string EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public string? EmployeeCode { get; set; }

        public DateTime EffectiveDate { get; set; }
        public string Reason { get; set; }

        // ---- From (snapshot) ----
        public string FromCompanyId { get; set; }
        public string? FromCompanyName { get; set; }

        public string? FromBranchId { get; set; }
        public string? FromBranchName { get; set; }

        public string FromDepartmentId { get; set; }
        public string? FromDepartmentName { get; set; }

        public string FromDesignationId { get; set; }
        public string? FromDesignationName { get; set; }

        public string? FromReportingManagerId { get; set; }
        public string? FromReportingManagerName { get; set; }

        // ---- To (proposed) ----
        public string? ToCompanyId { get; set; }
        public string? ToCompanyName { get; set; }

        public string? ToBranchId { get; set; }
        public string? ToBranchName { get; set; }

        public string? ToDepartmentId { get; set; }
        public string? ToDepartmentName { get; set; }

        public string? ToDesignationId { get; set; }
        public string? ToDesignationName { get; set; }

        public string? ToReportingManagerId { get; set; }
        public string? ToReportingManagerName { get; set; }

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

        public string? TenantId { get; set; }
        public DateTime CreatedOn { get; set; }
        public string? CreatedBy { get; set; }
    }
}
