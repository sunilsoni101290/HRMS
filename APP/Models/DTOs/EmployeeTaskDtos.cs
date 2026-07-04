using System.ComponentModel.DataAnnotations;

namespace APP.Models.DTOs
{
    public class EmployeeTaskDto
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Please enter a Title.")]
        [MaxLength(200)]
        [Display(Name = "Title")]
        public string Title { get; set; }

        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Please select an Employee.")]
        [Display(Name = "Assigned To")]
        public string EmployeeId { get; set; }
        public string? EmployeeName { get; set; }

        [Display(Name = "Due Date")]
        [DataType(DataType.Date)]
        public DateTime? DueDate { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; } = "Pending";

        [Display(Name = "Priority")]
        public string Priority { get; set; } = "Medium";

        public string? AssignedBy { get; set; }
        public DateTime? CompletedDate { get; set; }

        public string? CompanyId { get; set; }
        public string? BranchId { get; set; }

        public string? TenantId { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }
    }

    public class EmployeeTaskListDto
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string? EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public DateTime? DueDate { get; set; }
        public string Status { get; set; }
        public string Priority { get; set; }
    }
}
