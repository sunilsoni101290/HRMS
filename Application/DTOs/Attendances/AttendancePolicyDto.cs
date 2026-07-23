using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Attendances
{
    public class AttendancePolicyDto
    {
        public string? Id { get; set; }

        [Display(Name = "Company")]
        public string? CompanyId { get; set; }

        public string? CompanyName { get; set; }

        [Required]
        [Display(Name = "Policy Name")]
        public string PolicyName { get; set; }

        [Required]
        [Display(Name = "Effective From")]
        [DataType(DataType.Date)]
        public DateTime EffectiveFrom { get; set; }

        public bool IsActive { get; set; } = true;

        [Display(Name = "Max Correction Requests / Month")]
        public int MaxRegularizationRequestsPerMonth { get; set; }

        [Display(Name = "Max WFH Days / Month")]
        public int MaxWfhDaysPerMonth { get; set; }

        [Display(Name = "Late Mark Grace Count")]
        public int LateMarkGraceCount { get; set; }

        [Display(Name = "Late Mark Penalty")]
        public int LateMarkPenaltyType { get; set; }

        public string? LateMarkPenaltyTypeName { get; set; }

        [Display(Name = "Minimum Attendance % For Full Salary")]
        public decimal MinimumAttendancePercentForFullSalary { get; set; }

        [Display(Name = "Comp-Off Eligible Extra Hours")]
        public decimal CompOffEligibleExtraHours { get; set; }

        [MaxLength(500)]
        public string? Remarks { get; set; }

        public string? TenantId { get; set; }

        public DateTime CreatedOn { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }
    }
}
