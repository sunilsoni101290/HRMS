using System.ComponentModel.DataAnnotations;

namespace APP.Models.DTOs
{
    // Mirrors Application.DTOs.Attendances.AttendancePolicyDto exactly -
    // property names/types must match the API's JSON 1:1 or model binding
    // silently breaks. Backed by API/Controllers/AttendancePolicyController.cs
    // (api/attendancepolicy).
    public class AttendancePolicyDto
    {
        public string? Id { get; set; }

        [Display(Name = "Company")]
        public string? CompanyId { get; set; }

        public string? CompanyName { get; set; }

        [Required(ErrorMessage = "Policy name is required")]
        [Display(Name = "Policy Name")]
        public string PolicyName { get; set; }

        [Required(ErrorMessage = "Effective From date is required")]
        [Display(Name = "Effective From")]
        [DataType(DataType.Date)]
        public DateTime EffectiveFrom { get; set; } = DateTime.Today;

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Max Correction Requests / Month")]
        public int MaxRegularizationRequestsPerMonth { get; set; }

        [Display(Name = "Max WFH Days / Month")]
        public int MaxWfhDaysPerMonth { get; set; }

        [Display(Name = "Late Mark Grace Count")]
        public int LateMarkGraceCount { get; set; }

        // Int enum on the wire - see EnumExtensions.LateMarkPenaltyType for
        // the dropdown values (None=1, WarningOnly=2, HalfDayDeduction=3,
        // FullDayDeduction=4).
        [Display(Name = "Late Mark Penalty")]
        public int LateMarkPenaltyType { get; set; }

        public string? LateMarkPenaltyTypeName { get; set; }

        [Display(Name = "Minimum Attendance % For Full Salary")]
        [Range(0, 100, ErrorMessage = "Must be between 0 and 100")]
        public decimal MinimumAttendancePercentForFullSalary { get; set; }

        [Display(Name = "Comp-Off Eligible Extra Hours")]
        public decimal CompOffEligibleExtraHours { get; set; }

        // Used by ShortLeaveRequestController's Create form to show a live
        // "X hours = Y.YY days" hint as the employee picks From/To time -
        // see Application.DTOs.Attendances.AttendancePolicyDto and
        // Domain/Entities/AttendancePolicy.cs.ShortLeaveHoursPerDay. The real
        // calculation always happens server-side in ShortLeaveRequestService.
        [Display(Name = "Short Leave Hours / Day")]
        public decimal ShortLeaveHoursPerDay { get; set; }

        [Display(Name = "Salary Proration Basis")]
        public int SalaryProrationBasis { get; set; } = 2;
        public string? SalaryProrationBasisName { get; set; }

        [Display(Name = "Fixed Working Days / Month")]
        public decimal FixedWorkingDaysPerMonth { get; set; } = 26m;

        [MaxLength(500)]
        public string? Remarks { get; set; }

        public string? TenantId { get; set; }

        public DateTime CreatedOn { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }
    }
}
