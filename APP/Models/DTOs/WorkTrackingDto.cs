using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace APP.Models.DTOs
{
    // Mirrors Application/DTOs/WorkTracking/*.cs - kept as separate
    // APP-side types per this codebase's existing convention (see
    // EsslIntegrationDto/ErrorLogDto etc.), not shared across the
    // API/APP project boundary.

    public class ClientDto
    {
        public string? Id { get; set; }
        [Required] public string Name { get; set; } = "";
        public string? Code { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class WorkJobDto
    {
        public string? Id { get; set; }
        [Required] public string JobNumber { get; set; } = "";
        [Required] public string JobName { get; set; } = "";
        [Required] public string ClientId { get; set; } = "";
        public string? ClientName { get; set; }
        public int Status { get; set; } = 1;
        public string? StatusName { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class JobTypeDto
    {
        public string? Id { get; set; }
        [Required] public string Name { get; set; } = "";
        public string? Code { get; set; }
        public int DisplayOrder { get; set; }
        public bool HasDisciplines { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class JobItemDto
    {
        public string? Id { get; set; }
        [Required] public string WorkJobId { get; set; } = "";
        public string? JobNumber { get; set; }
        [Required] public string Code { get; set; } = "";
        public string? Description { get; set; }
        public int ItemType { get; set; } = 3;
        public string? ItemTypeName { get; set; }
        public decimal? TotalWeightMT { get; set; }
        public string? DocumentStatusId { get; set; }
        public string? DocumentStatusName { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class WorkActivityDto
    {
        public string? Id { get; set; }
        [Required] public string JobTypeId { get; set; } = "";
        public string? JobTypeName { get; set; }
        public int? SkidsDiscipline { get; set; }
        public string? SkidsDisciplineName { get; set; }
        [Required] public string Name { get; set; } = "";
        public int WorkCategory { get; set; } = 1;
        public string? WorkCategoryName { get; set; }
        public int DisplayOrder { get; set; }
        public bool RequiresReason { get; set; }
        public bool AllowFreeTextOther { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class WorkEntryReasonDto
    {
        public string? Id { get; set; }
        public int Category { get; set; }
        public string? CategoryName { get; set; }
        [Required] public string Name { get; set; } = "";
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class DocumentStatusDto
    {
        public string? Id { get; set; }
        [Required] public string Code { get; set; } = "";
        [Required] public string DisplayName { get; set; } = "";
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class DailyWorkEntryLineInputDto
    {
        public string? Id { get; set; }
        public string? WorkJobId { get; set; }
        [Required] public string JobTypeId { get; set; } = "";
        public string? JobItemId { get; set; }
        public string? WorkActivityId { get; set; }
        public string? WorkEntryReasonId { get; set; }
        [Range(0.01, 24)] public decimal Hours { get; set; }
        public string? Remarks { get; set; }
        public string? WorkDoneToday { get; set; }
        public string? AssignmentId { get; set; }
        public string? AdhocReason { get; set; }
    }

    public class SaveDailyWorkLogDto
    {
        [Required] public DateTime WorkDate { get; set; }
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
        public string? WorkDoneToday { get; set; }
        public string? AssignmentId { get; set; }
        public string? AdhocReason { get; set; }
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
        [Required] public string Reason { get; set; } = "";
    }

    public class WorkReportFilterDto
    {
        public string? EmployeeId { get; set; }
        public string? DepartmentId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? WorkJobId { get; set; }
        public string? ClientId { get; set; }
        public string? JobTypeId { get; set; }
        public int? Status { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class MonthlyEmployeeWorkJobLineDto
    {
        public string JobNumber { get; set; } = "";
        public string? ClientName { get; set; }
        public string? JobItemCode { get; set; }
        public decimal Hours { get; set; }
        public string? DocumentStatusName { get; set; }
    }

    public class MonthlyEmployeeWorkReportDto
    {
        public string EmployeeId { get; set; } = "";
        public string EmployeeCode { get; set; } = "";
        public string EmployeeName { get; set; } = "";
        public string? Position { get; set; }
        public DateTime ReportFrom { get; set; }
        public DateTime ReportTo { get; set; }
        public decimal TotalClockedHours { get; set; }
        public decimal IdealHours { get; set; }
        public decimal DirectHours { get; set; }
        public decimal IndirectHours { get; set; }
        public decimal IdleHours { get; set; }
        public decimal DowntimeHours { get; set; }
        public decimal UtilizationPercent { get; set; }
        public List<MonthlyEmployeeWorkJobLineDto> JobLines { get; set; } = new();
    }

    public class JobWiseWorkReportDto
    {
        public string WorkJobId { get; set; } = "";
        public string JobNumber { get; set; } = "";
        public string JobName { get; set; } = "";
        public string? ClientName { get; set; }
        public decimal DirectHours { get; set; }
        public decimal IndirectHours { get; set; }
        public decimal TotalHours { get; set; }
    }

    public class StructureWorkReportDto
    {
        public string JobNumber { get; set; } = "";
        public string? ClientName { get; set; }
        public string JobItemId { get; set; } = "";
        public string StructureId { get; set; } = "";
        public Dictionary<string, decimal> DirectActivityHours { get; set; } = new();
        public decimal TotalDirectHours { get; set; }
        public Dictionary<string, decimal> IndirectActivityHours { get; set; } = new();
        public decimal TotalIndirectHours { get; set; }
        public decimal TotalHours { get; set; }
        public decimal? TotalWeightMT { get; set; }
        public decimal? HoursPerMT { get; set; }
        public string? DocumentStatusName { get; set; }
    }

    public class UtilizationReportDto
    {
        public string EmployeeId { get; set; } = "";
        public string EmployeeCode { get; set; } = "";
        public string EmployeeName { get; set; } = "";
        public string? DepartmentName { get; set; }
        public decimal ClockedHours { get; set; }
        public decimal DirectHours { get; set; }
        public decimal IndirectHours { get; set; }
        public decimal IdleHours { get; set; }
        public decimal DowntimeHours { get; set; }
        public decimal UtilizationPercent { get; set; }
    }

    // ==============================
    // Employee Job/Work Assignment - mirrors Application/DTOs/WorkTracking/AssignmentDtos.cs
    // ==============================

    public class SaveEmployeeWorkAssignmentDto
    {
        [Required] public string EmployeeId { get; set; } = "";
        [Required] public string WorkJobId { get; set; } = "";
        [Required] public string JobTypeId { get; set; } = "";
        public string? JobItemId { get; set; }
        public string? WorkActivityId { get; set; }
        public int AssignmentType { get; set; } = 1;
        public int Priority { get; set; } = 2;
        public DateTime? StartDate { get; set; }
        public DateTime? ExpectedEndDate { get; set; }
        [Range(0, 100000)] public decimal? EstimatedHours { get; set; }
        public string? Instructions { get; set; }
    }

    public class UpdateAssignmentStatusDto
    {
        [Required] public int Status { get; set; }
        public string? Remarks { get; set; }
    }

    public class ReassignEmployeeWorkAssignmentDto
    {
        [Required] public string NewEmployeeId { get; set; } = "";
        public string? Remarks { get; set; }
    }

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
