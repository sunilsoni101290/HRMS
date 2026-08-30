using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.WorkTracking
{
    /// <summary>
    /// One activity line as submitted from the Daily Work Entry form.
    /// EmployeeId/Status/approval fields deliberately do NOT appear here -
    /// they belong on the header and are never accepted from the client
    /// (spec section 35).
    /// </summary>
    public class DailyWorkEntryLineInputDto
    {
        /// <summary>Existing line Id when editing a Draft; null for a new line.</summary>
        public string? Id { get; set; }

        /// <summary>Null for an Idle/Downtime-only line.</summary>
        public string? WorkJobId { get; set; }

        [Required(ErrorMessage = "Job type is required.")]
        public string JobTypeId { get; set; } = "";

        public string? JobItemId { get; set; }

        public string? WorkActivityId { get; set; }

        /// <summary>Required when the selected activity's WorkCategory is Idle or Downtime.</summary>
        public string? WorkEntryReasonId { get; set; }

        [Required(ErrorMessage = "Hours are required.")]
        [Range(0.01, 24, ErrorMessage = "Hours must be greater than 0 and no more than 24.")]
        public decimal Hours { get; set; }

        [MaxLength(500)]
        public string? Remarks { get; set; }
    }

    /// <summary>Save Draft / Submit request body - EmployeeId is deliberately absent; the server always resolves it from the acting user (spec section 35).</summary>
    public class SaveDailyWorkLogDto
    {
        [Required(ErrorMessage = "Work date is required.")]
        public DateTime WorkDate { get; set; }

        [Required(ErrorMessage = "At least one activity line is required.")]
        [MinLength(1, ErrorMessage = "At least one activity line is required.")]
        public List<DailyWorkEntryLineInputDto> Lines { get; set; } = new();
    }

    public class DailyWorkEntryLineDto
    {
        public string Id { get; set; } = "";

        public string? WorkJobId { get; set; }
        public string? JobNumber { get; set; }
        public string? ClientName { get; set; }

        public string? JobTypeId { get; set; }
        public string? JobTypeName { get; set; }

        public string? JobItemId { get; set; }
        public string? JobItemCode { get; set; }

        public string? WorkActivityId { get; set; }
        public string? WorkActivityName { get; set; }
        public int? WorkCategory { get; set; }
        public string? WorkCategoryName { get; set; }

        public string? WorkEntryReasonId { get; set; }
        public string? WorkEntryReasonName { get; set; }

        public decimal Hours { get; set; }
        public string? Remarks { get; set; }
    }

    public class DailyWorkLogDto
    {
        public string Id { get; set; } = "";

        public string EmployeeId { get; set; } = "";
        public string? EmployeeCode { get; set; }
        public string? EmployeeName { get; set; }
        public string? DepartmentName { get; set; }
        public string? DesignationName { get; set; }
        public string? ReportingManagerName { get; set; }

        public DateTime WorkDate { get; set; }

        public int Status { get; set; }
        public string? StatusName { get; set; }

        public decimal? ClockedHours { get; set; }
        public decimal TotalHours { get; set; }
        public decimal DirectHours { get; set; }
        public decimal IndirectHours { get; set; }
        public decimal IdleHours { get; set; }
        public decimal DowntimeHours { get; set; }

        public DateTime? SubmittedAt { get; set; }
        public string? SubmittedByName { get; set; }

        public DateTime? ApprovedAt { get; set; }
        public string? ApprovedByName { get; set; }

        public DateTime? RejectedAt { get; set; }
        public string? RejectedByName { get; set; }
        public string? RejectionReason { get; set; }

        public List<DailyWorkEntryLineDto> Lines { get; set; } = new();
    }

    /// <summary>One row in the Team Leader Approval queue list (spec section 19's mockup) - header-level summary only; drill-down uses GetById for the full DailyWorkLogDto with lines.</summary>
    public class DailyWorkLogSummaryDto
    {
        public string Id { get; set; } = "";
        public string EmployeeCode { get; set; } = "";
        public string EmployeeName { get; set; } = "";
        public DateTime WorkDate { get; set; }
        public decimal TotalHours { get; set; }
        public int Status { get; set; }
        public string StatusName { get; set; } = "";
        public DateTime? SubmittedAt { get; set; }
    }

    public class RejectDailyWorkLogDto
    {
        [Required(ErrorMessage = "Rejection reason is required.")]
        [MaxLength(500)]
        public string Reason { get; set; } = "";
    }
}
