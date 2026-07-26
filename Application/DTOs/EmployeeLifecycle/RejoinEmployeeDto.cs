using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.EmployeeLifecycle
{
    // Input for RejoiningService.RejoinAsync - EmployeeId is the FORMER
    // employee being rehired (Employee.RelievingDate != null, IsDeleted ==
    // false, validated server-side). NewJoiningDate must be on/after the
    // employee's current RelievingDate - validated server-side, never
    // trusted from client input alone. ProcessedByUserId/ProcessedOn are
    // NOT supplied here - resolved server-side from actingUserId /
    // DateTime.UtcNow, same reasoning as MakerId/GivenByUserId elsewhere in
    // this module.
    public class RejoinEmployeeDto
    {
        [Required]
        [Display(Name = "Employee")]
        public string EmployeeId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "New Joining Date")]
        public DateTime NewJoiningDate { get; set; }

        public string? Reason { get; set; }
    }
}
