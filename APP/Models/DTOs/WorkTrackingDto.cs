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
}
