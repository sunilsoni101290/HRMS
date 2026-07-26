using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Attendances
{
    public class CreateShortLeaveRequestDto
    {
        [Required]
        [Display(Name = "Employee")]
        public string EmployeeId { get; set; }

        [Required]
        [Display(Name = "Leave Type")]
        public string LeaveTypeId { get; set; }

        [Required]
        [Display(Name = "Date")]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Required]
        [Display(Name = "From Time")]
        public TimeSpan FromTime { get; set; }

        [Required]
        [Display(Name = "To Time")]
        public TimeSpan ToTime { get; set; }

        [Required]
        public string Reason { get; set; }
    }
}
