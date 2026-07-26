using System;
using System.ComponentModel.DataAnnotations;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    // Company-wide attendance RULES (grace/regularization limits, late-mark
    // penalty, minimum attendance % for full salary, comp-off eligibility
    // threshold) - deliberately separate from Shift, which only holds
    // per-shift TIMING (StartTime/EndTime/Grace*/HalfDayMinutes/etc). A
    // tenant can have several Shifts but only needs one (or a handful,
    // per-company) AttendancePolicy governing the rules layered on top of
    // whichever shift an employee is on.
    public class AttendancePolicy : BaseEntity
    {
        // Null = tenant-wide default policy (fallback when no
        // company-specific policy is active) - see
        // IAttendancePolicyService.GetActiveForTenantAsync.
        public string? CompanyId { get; set; }
        public virtual Company? Company { get; set; }

        [Required, MaxLength(200)]
        public string PolicyName { get; set; }

        [Required]
        public DateTime EffectiveFrom { get; set; }

        public bool IsActive { get; set; } = true;

        public int MaxRegularizationRequestsPerMonth { get; set; }

        // Cap on approved Work From Home days per calendar month per
        // employee - 0 (or unset) means unlimited, see
        // WfhRequestService's monthly-limit check.
        public int MaxWfhDaysPerMonth { get; set; }

        // Number of late arrivals allowed per month before
        // LateMarkPenaltyType kicks in.
        public int LateMarkGraceCount { get; set; }

        public LateMarkPenaltyType LateMarkPenaltyType { get; set; } = LateMarkPenaltyType.None;

        // e.g. 90.00 = employee must maintain 90% attendance in the month to
        // draw full salary (payroll-facing rule, not enforced by this
        // module itself).
        public decimal MinimumAttendancePercentForFullSalary { get; set; }

        // Extra hours worked beyond the shift's full-day requirement before
        // a Comp-Off is earned.
        public decimal CompOffEligibleExtraHours { get; set; }

        // Conversion factor used to turn a Short Leave request's requested
        // hours into a fraction of a day (FractionalDays = RequestedHours /
        // ShortLeaveHoursPerDay) - see ShortLeaveRequestService.CreateAsync.
        public decimal ShortLeaveHoursPerDay { get; set; } = 8.0m;

        [MaxLength(500)]
        public string? Remarks { get; set; }

        public override string GetSequencePrefix() => "APOL";
    }
}
