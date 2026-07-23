using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using static Domain.Enums.EnumExtensions;

namespace Application.DTOs.Onboarding
{
    public class OnboardingCaseDto
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Employee is required")]
        public string EmployeeId { get; set; }

        public string? EmployeeName { get; set; }
        public string? EmployeeCode { get; set; }
        public string? DepartmentName { get; set; }
        public string? DesignationName { get; set; }

        public string? CandidateId { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime? TargetCompletionDate { get; set; }

        // Enums are NOT converted to JsonStringEnumConverter in this API
        // (see API/Program.cs AddJsonOptions) - matches
        // AttendanceRegularizationDto.Status / LeaveApplicationDto.Status,
        // serializes as the underlying int with a companion *Name string for
        // display.
        public OnboardingCaseStatus Status { get; set; }
        public string? StatusName { get; set; }

        public DateTime? CompletedOn { get; set; }

        // Completed-or-NotApplicable mandatory items / total mandatory items
        // * 100, rounded. 100 when there are no mandatory items at all.
        public int ProgressPercent { get; set; }

        public string? Remarks { get; set; }

        public string? CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }

        public List<OnboardingChecklistItemDto> ChecklistItems { get; set; } = new();
    }

    public class OnboardingChecklistItemDto
    {
        public string? Id { get; set; }

        public string OnboardingCaseId { get; set; }

        public OnboardingStageType StageType { get; set; }
        public string? StageTypeName { get; set; }

        [Required(ErrorMessage = "Title is required")]
        public string Title { get; set; }

        public string? Description { get; set; }

        public bool IsMandatory { get; set; }
        public int SortOrder { get; set; }

        public OnboardingChecklistItemStatus Status { get; set; }
        public string? StatusName { get; set; }

        public DateTime? CompletedOn { get; set; }
        public string? CompletedBy { get; set; }
        public string? CompletedByName { get; set; }

        public string? Remarks { get; set; }
    }

    public class CreateOnboardingCaseDto
    {
        [Required(ErrorMessage = "Employee is required")]
        public string EmployeeId { get; set; }

        // Set only when this case is being started off the back of a
        // Recruitment Candidate conversion - mirrors Employee.CandidateId.
        public string? CandidateId { get; set; }

        public DateTime? TargetCompletionDate { get; set; }

        public string? Remarks { get; set; }
    }

    public class OnboardingChecklistTemplateItemDto
    {
        public string? Id { get; set; }

        public OnboardingStageType StageType { get; set; }
        public string? StageTypeName { get; set; }

        public string Title { get; set; }
        public string? Description { get; set; }

        public bool IsMandatory { get; set; }
        public int SortOrder { get; set; }

        public bool IsActive { get; set; }
    }

    public class UpsertOnboardingTemplateItemDto
    {
        [Required]
        public OnboardingStageType StageType { get; set; }

        [Required(ErrorMessage = "Title is required")]
        public string Title { get; set; }

        public string? Description { get; set; }

        public bool IsMandatory { get; set; }
        public int SortOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class UpdateChecklistItemStatusDto
    {
        [Required]
        public OnboardingChecklistItemStatus Status { get; set; }

        public string? Remarks { get; set; }
    }

    public class AddChecklistItemDto
    {
        [Required]
        public OnboardingStageType StageType { get; set; }

        [Required(ErrorMessage = "Title is required")]
        public string Title { get; set; }

        public string? Description { get; set; }

        public bool IsMandatory { get; set; }

        // Optional - if omitted, the service appends after the highest
        // existing SortOrder for that stage within the case.
        public int? SortOrder { get; set; }
    }

    // PUT /api/onboarding/{id}/status body.
    public class UpdateOnboardingCaseStatusDto
    {
        [Required]
        public OnboardingCaseStatus Status { get; set; }

        public string? Remarks { get; set; }
    }
}
