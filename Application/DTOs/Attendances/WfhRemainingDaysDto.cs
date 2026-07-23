namespace Application.DTOs.Attendances
{
    // Used by the create-request form to show "X of Y WFH days used this
    // month" - see IWfhRequestService.GetRemainingWfhDaysAsync.
    public class WfhRemainingDaysDto
    {
        public string EmployeeId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }

        // Null/0 = no active policy or no limit configured (unlimited).
        public int? MaxWfhDaysPerMonth { get; set; }

        // Already-Approved WFH days for this employee that fall within
        // this calendar month.
        public int UsedDays { get; set; }

        // Null when MaxWfhDaysPerMonth is null/0 (unlimited); otherwise
        // Max - Used, floored at 0.
        public int? RemainingDays { get; set; }
    }
}
