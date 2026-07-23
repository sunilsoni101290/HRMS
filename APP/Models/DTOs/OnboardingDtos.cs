using System.ComponentModel.DataAnnotations;
using static APP.Helpers.EnumExtensions;

namespace APP.Models.DTOs
{
    // Mirrors Application.DTOs.Onboarding.OnboardingCaseDto exactly (property
    // names/types) - see API/Controllers/OnboardingController.cs for the
    // endpoints this round-trips through. Enums are NOT
    // JsonStringEnumConverter'd by the API (matches
    // AttendanceRegularizationDto.Status), so they serialize as the
    // underlying int with a companion *Name string for display.
    public class OnboardingCaseDto
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Employee is required")]
        [Display(Name = "Employee")]
        public string EmployeeId { get; set; }

        public string? EmployeeName { get; set; }
        public string? EmployeeCode { get; set; }
        public string? DepartmentName { get; set; }
        public string? DesignationName { get; set; }

        public string? CandidateId { get; set; }

        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }

        [Display(Name = "Target Completion Date")]
        public DateTime? TargetCompletionDate { get; set; }

        public OnboardingCaseStatus Status { get; set; }
        public string? StatusName { get; set; }

        public DateTime? CompletedOn { get; set; }

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

    // POST api/onboarding body.
    public class CreateOnboardingCaseDto
    {
        [Required(ErrorMessage = "Please select an Employee")]
        [Display(Name = "Employee")]
        public string EmployeeId { get; set; }

        // Set only when this case is being started off the back of a
        // Recruitment Candidate conversion - mirrors Employee.CandidateId.
        public string? CandidateId { get; set; }

        [Display(Name = "Target Completion Date")]
        [DataType(DataType.Date)]
        public DateTime? TargetCompletionDate { get; set; }

        public string? Remarks { get; set; }
    }

    // PUT api/onboarding/checklist-item/{itemId}/status body.
    public class UpdateChecklistItemStatusDto
    {
        [Required]
        public OnboardingChecklistItemStatus Status { get; set; }

        public string? Remarks { get; set; }
    }

    // PUT api/onboarding/{id}/status body.
    public class UpdateOnboardingCaseStatusDto
    {
        [Required]
        public OnboardingCaseStatus Status { get; set; }

        public string? Remarks { get; set; }
    }

    // POST api/onboarding/{id}/checklist-item body.
    public class AddChecklistItemDto
    {
        [Required]
        [Display(Name = "Stage")]
        public OnboardingStageType StageType { get; set; }

        [Required(ErrorMessage = "Title is required")]
        public string Title { get; set; }

        public string? Description { get; set; }

        public bool IsMandatory { get; set; }

        // Optional - if omitted, the API appends after the highest
        // existing SortOrder for that stage within the case.
        public int? SortOrder { get; set; }
    }

    public class OnboardingChecklistTemplateItemDto
    {
        public string? Id { get; set; }

        [Display(Name = "Stage")]
        public OnboardingStageType StageType { get; set; }
        public string? StageTypeName { get; set; }

        [Required(ErrorMessage = "Title is required")]
        public string Title { get; set; }

        public string? Description { get; set; }

        public bool IsMandatory { get; set; }
        public int SortOrder { get; set; }

        public bool IsActive { get; set; }
    }

    // POST/PUT api/onboarding/templates[/{id}] body.
    public class UpsertOnboardingTemplateItemDto
    {
        [Required]
        [Display(Name = "Stage")]
        public OnboardingStageType StageType { get; set; }

        [Required(ErrorMessage = "Title is required")]
        public string Title { get; set; }

        public string? Description { get; set; }

        public bool IsMandatory { get; set; }
        public int SortOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
