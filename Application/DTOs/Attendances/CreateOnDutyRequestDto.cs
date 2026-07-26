using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Attendances
{
    public class CreateOnDutyRequestDto
    {
        [Required]
        [Display(Name = "Employee")]
        public string EmployeeId { get; set; }

        [Required]
        [Display(Name = "From Date")]
        [DataType(DataType.Date)]
        public DateTime FromDate { get; set; }

        [Required]
        [Display(Name = "To Date")]
        [DataType(DataType.Date)]
        public DateTime ToDate { get; set; }

        [Required]
        public string Purpose { get; set; }

        [Display(Name = "Location")]
        public string? Location { get; set; }
    }
}
