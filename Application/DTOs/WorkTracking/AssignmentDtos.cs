using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.WorkTracking
{
    /// <summary>
    /// "Assign Job/Work to Employee" request body. EmployeeId here IS
    /// client-supplied (unlike the self-service DailyWorkEntry DTOs) -
    /// that's the whole point of this screen - but WorkAssignmentService
    /// re-validates server-side that the acting user is actually allowed to
    /// assign work to this specific employee (spec section 23) before ever
    /// trusting it.
    /// </summary>
    public class SaveEmployeeWorkAssignmentDto
    {
        [Required(ErrorMessage = "Employee is required.")]
        public string EmployeeId { get; set; } = "";

        [Required(ErrorMessage = "Job is required.")]
        public string WorkJobId { get; set; } = "";

        [Required(ErrorMessage = "Job type is required.")]
        public string JobTypeId { get; set; } = "";

        public string? JobItemId { get; set; }

        public string? WorkActivityId { get; set; }

        /// <summary>1=Job, 2=Task, 3=Activity - see EnumExtensions.AssignmentType.</summary>
        public int AssignmentType { get; set; } = 1;

        /// <summary>1=Low, 2=Normal, 3=High, 4=Urgent.</summary>
        public int Priority { get; set; } = 2;

        public DateTime? StartDate { get; set; }
        public DateTime? ExpectedEndDate { get; set; }

        [Range(0, 100000)]
        public decimal? EstimatedHours { get; set; }

        [MaxLength(1000)]
        public string? Instructions { get; set; }
    }

    public class UpdateAssignmentStatusDto
    {
        /// <summary>2=Accepted, 3=InProgress, 4=Completed, 5=Rejected, 6=Returned - see EnumExtensions.AssignmentStatus. Only forward-legal transitions are accepted; enforced server-side, never trusted as-is.</summary>
        [Required]
        public int Status { get; set; }

        [MaxLength(500)]
        public string? Remarks { get; set; }
    }

    public class ReassignEmployeeWorkAssignmentDto
    {
        [Required(ErrorMessage = "New employee is required.")]
        public string NewEmployeeId { get; set; } = "";

        [MaxLength(500)]
        public string? Remarks { get; set; }
    }

    /// <summary>Full detail view - backs both the manager's "view assignment" and the employee's "My Assigned Jobs -> open" drill-down (spec section 7).</summary>
    public class EmployeeWorkAssignmentDto
    {
        public string Id { get; set; } = "";

        public string EmployeeId { get; set; } = "";
        public string? EmployeeCode { get; set; }
        public string? EmployeeName { get; set; }
        public string? DepartmentName { get; set; }

        public string? AssignedByName { get; set; }

        public string WorkJobId { get; set; } = "";
        public string? JobNumber { get; set; }
        public string? JobName { get; set; }
        public string? ClientName { get; set; }

        public string? JobTypeId { get; set; }
        public string? JobTypeName { get; set; }

        public string? JobItemId { get; set; }
        public string? JobItemCode { get; set; }

        public string? WorkActivityId { get; set; }
        public string? WorkActivityName { get; set; }

        public int AssignmentType { get; set; }
        public string? AssignmentTypeName { get; set; }

        public int Priority { get; set; }
        public string? PriorityName { get; set; }

        public int Status { get; set; }
        public string? StatusName { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? ExpectedEndDate { get; set; }

        public decimal? EstimatedHours { get; set; }
        public decimal ActualHours { get; set; }
        public decimal? RemainingHours { get; set; }
        public bool IsOverUtilized { get; set; }

        public string? Instructions { get; set; }

        public DateTime? AcceptedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? RejectedAt { get; set; }
        public string? RejectionReason { get; set; }

        public string? ReassignedFromId { get; set; }

        public DateTime CreatedOn { get; set; }
    }

    /// <summary>One row in "My Assigned Jobs" / a manager's assignment list (spec section 7's mockup table).</summary>
    public class EmployeeWorkAssignmentSummaryDto
    {
        public string Id { get; set; } = "";
        public string EmployeeCode { get; set; } = "";
        public string EmployeeName { get; set; } = "";
        public string JobNumber { get; set; } = "";
        public string? ClientName { get; set; }
        public string? JobItemCode { get; set; }
        public string? WorkActivityName { get; set; }
        public decimal? EstimatedHours { get; set; }
        public decimal ActualHours { get; set; }
        public decimal? RemainingHours { get; set; }
        public bool IsOverUtilized { get; set; }
        public int Status { get; set; }
        public string StatusName { get; set; } = "";
        public DateTime? ExpectedEndDate { get; set; }
        public bool IsOverdue { get; set; }
    }

    /// <summary>
    /// One assignable Job/JobItem/Activity combo for the CURRENT employee -
    /// backs the assignment-scoped cascading dropdowns in Daily Work Entry
    /// (spec section 9). Deliberately flat/denormalized so the client-side
    /// cascade (Job -> Structure -> Activity) can filter this one small
    /// list in-memory rather than round-tripping to the server at every
    /// step.
    /// </summary>
    public class AssignedWorkComboDto
    {
        public string AssignmentId { get; set; } = "";
        public string WorkJobId { get; set; } = "";
        public string JobNumber { get; set; } = "";
        public string JobTypeId { get; set; } = "";
        public string? JobItemId { get; set; }
        public string? JobItemCode { get; set; }
        public string? WorkActivityId { get; set; }
        public string? WorkActivityName { get; set; }
        public int AssignmentType { get; set; }
        public int Status { get; set; }
    }

    /// <summary>"My Work" summary cards (spec section 18) - assembled from the caller's own assignments/entries only.</summary>
    public class MyWorkDashboardDto
    {
        public int AssignedJobs { get; set; }
        public int InProgress { get; set; }
        public int Completed { get; set; }
        public int PendingDailyEntries { get; set; }
        public decimal TodayHours { get; set; }
        public decimal ThisWeekHours { get; set; }
        public decimal ThisMonthHours { get; set; }
        public int PendingSubmission { get; set; }
        public int RejectedEntries { get; set; }
        public List<EmployeeWorkAssignmentSummaryDto> TodaysAssignedWork { get; set; } = new();
    }

    /// <summary>Manager/Team "Team Work Overview" (spec section 19).</summary>
    public class TeamWorkOverviewDto
    {
        public int TeamMembers { get; set; }
        public int ActiveAssignments { get; set; }
        public int PendingDailyApprovals { get; set; }
        public int OverdueAssignments { get; set; }
        public decimal EstimatedHours { get; set; }
        public decimal ActualHours { get; set; }
        public decimal DirectHours { get; set; }
        public decimal IndirectHours { get; set; }
        public decimal IdleHours { get; set; }
        public decimal DowntimeHours { get; set; }
    }

    /// <summary>Employee Assignment Report row (spec section 26).</summary>
    public class EmployeeAssignmentReportRowDto
    {
        public string EmployeeCode { get; set; } = "";
        public string EmployeeName { get; set; } = "";
        public string JobNumber { get; set; } = "";
        public string? JobItemCode { get; set; }
        public string? WorkActivityName { get; set; }
        public string? AssignedByName { get; set; }
        public DateTime AssignedDate { get; set; }
        public decimal? EstimatedHours { get; set; }
        public decimal ActualHours { get; set; }
        public decimal? RemainingHours { get; set; }
        public string StatusName { get; set; } = "";
    }
}
