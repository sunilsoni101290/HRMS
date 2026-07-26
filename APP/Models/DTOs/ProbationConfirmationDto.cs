using System.ComponentModel.DataAnnotations;

namespace APP.Models.DTOs
{
    // Mirrors Application.DTOs.EmployeeLifecycle.ProbationConfirmationDto
    // exactly - property names/types must match the API's JSON 1:1 or model
    // binding silently breaks. Backed by
    // API/Controllers/ProbationConfirmationController.cs
    // (api/probationconfirmation). Status/Recommendation are ints on the
    // wire - see EnumExtensions.ProbationConfirmationStatus/
    // ProbationRecommendation for the values. The Application-side source
    // DTO carries no data annotations at all, so none are added here either
    // - this is a read/response shape, never itself posted as a form body
    // (CreateProbationConfirmationDto/CheckerActionDto below are the actual
    // form-bound DTOs).
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

    // Body for POST api/probationconfirmation - the Maker's proposal input.
    // Mirrors Application.DTOs.EmployeeLifecycle.CreateProbationConfirmationDto
    // exactly, annotations included (matches WfhRequestDto's/
    // CreateWfhRequestDto's annotation style in this codebase).
    public class CreateProbationConfirmationDto
    {
        [Required]
        [Display(Name = "Employee")]
        public string EmployeeId { get; set; }
        public string TenantId { get; set; }
        public string ActingUserId { get; set; }

        // EnumExtensions.ProbationRecommendation.
        [Required]
        public int Recommendation { get; set; }

        // Required only when Recommendation == Extend (2) - enforced
        // server-side; also enforced client-side via simple show/hide JS on
        // the Create view.
        [DataType(DataType.Date)]
        public DateTime? ExtendedProbationEndDate { get; set; }

        [MaxLength(1000)]
        public string? MakerRemarks { get; set; }
    }

    // Result row for GET api/probationconfirmation/due-for-review - mirrors
    // Application.DTOs.EmployeeLifecycle.ProbationDueForReviewDto exactly.
    // Employee records (not ProbationConfirmation records) listing who HR
    // should review next.
    public class ProbationDueForReviewDto
    {
        public string EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public string? EmployeeCode { get; set; }
        public string? DepartmentName { get; set; }
        public string? DesignationName { get; set; }

        public DateTime JoiningDate { get; set; }

        // Null if the employee's ProbationEndDate was never explicitly set.
        public DateTime? ProbationEndDate { get; set; }

        // (ProbationEndDate - today).Days - null when ProbationEndDate is
        // null. Negative means already overdue.
        public int? DaysRemaining { get; set; }
        public bool IsOverdue { get; set; }
    }
}
