using System;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    public class EmployeeTask : BaseEntity
    {
        [Required, MaxLength(200)]
        public string Title { get; set; }

        public string? Description { get; set; }

        // Assigned employee
        public string EmployeeId { get; set; }
        public virtual Employee Employee { get; set; }

        public DateTime? DueDate { get; set; }

        // Pending / InProgress / Completed
        [MaxLength(50)]
        public string Status { get; set; } = "Pending";

        // High / Medium / Low
        [MaxLength(50)]
        public string Priority { get; set; } = "Medium";

        public string? AssignedBy { get; set; }

        public DateTime? CompletedDate { get; set; }

        public string CompanyId { get; set; }
        public string? BranchId { get; set; }

        public override string GetSequencePrefix() => "TSK";
    }
}
