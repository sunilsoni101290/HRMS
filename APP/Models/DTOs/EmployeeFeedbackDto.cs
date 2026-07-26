using System.ComponentModel.DataAnnotations;

namespace APP.Models.DTOs
{
    // Mirrors Application.DTOs.EmployeeLifecycle.EmployeeFeedbackDto exactly -
    // property names/types must match the API's JSON 1:1 or model binding
    // silently breaks. Backed by API/Controllers/EmployeeFeedbackController.cs
    // (api/employeefeedback). Category is an int on the wire - see
    // EnumExtensions.FeedbackCategory for the values (General=1,
    // Performance=2, Behavioral=3, Skill=4, Attendance=5, Other=6). Phase 4
    // of the "Probation & Confirmation" (Employee Lifecycle) module - NO
    // maker-checker workflow, plain CRUD.
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

    // Shared Create/Update body - mirrors
    // Application.DTOs.EmployeeLifecycle.CreateUpdateEmployeeFeedbackDto
    // exactly. EmployeeId is only meaningful on Create (Update never moves
    // feedback to a different employee - the API ignores it on update).
    public class CreateUpdateEmployeeFeedbackDto
    {
        [Required]
        [Display(Name = "Employee")]
        public string EmployeeId { get; set; }

        [Required]
        [Display(Name = "Feedback Date")]
        [DataType(DataType.Date)]
        public DateTime FeedbackDate { get; set; }

        [Required]
        [Display(Name = "Category")]
        public int Category { get; set; }

        [Range(1, 5)]
        [Display(Name = "Rating")]
        public int? Rating { get; set; }

        [Display(Name = "Strengths")]
        public string? Strengths { get; set; }

        [Display(Name = "Areas Of Improvement")]
        public string? AreasOfImprovement { get; set; }

        [Display(Name = "Comments")]
        public string? Comments { get; set; }

        [Display(Name = "Visible To Employee")]
        public bool IsVisibleToEmployee { get; set; } = true;
    }
}
