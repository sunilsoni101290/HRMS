using System;
using System.Collections.Generic;

namespace Application.DTOs.WorkTracking
{
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

    /// <summary>Excel "Employ Work Report Monthly" sheet, converted into a proper report (spec section 20).</summary>
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

        /// <summary>Direct Hours / Ideal Hours x 100 (spec section 21) - Ideal Hours here is Total Clocked Hours from Attendance unless a separate configured standard is confirmed.</summary>
        public decimal UtilizationPercent { get; set; }

        public List<MonthlyEmployeeWorkJobLineDto> JobLines { get; set; } = new();
    }

    public class MonthlyEmployeeWorkJobLineDto
    {
        public string JobNumber { get; set; } = "";
        public string? ClientName { get; set; }
        public string? JobItemCode { get; set; }
        public decimal Hours { get; set; }
        public string? DocumentStatusName { get; set; }
    }

    /// <summary>Job-wise Work Report (spec section: navigation "Job-wise Work Report") - total hours booked against one Job across all employees/dates in range.</summary>
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

    /// <summary>Excel "Reports-Only for assigned" sheet - per-structure engineering effort breakdown (spec section 24), plus MT-based productivity (spec section 25) when the JobItem has a weight.</summary>
    public class StructureWorkReportDto
    {
        public string JobNumber { get; set; } = "";
        public string? ClientName { get; set; }
        public string JobItemId { get; set; } = "";
        public string StructureId { get; set; } = "";

        /// <summary>Hours per Work Activity name (Direct activities) - dynamic, since the activity list is configurable per Job Type.</summary>
        public Dictionary<string, decimal> DirectActivityHours { get; set; } = new();
        public decimal TotalDirectHours { get; set; }

        public Dictionary<string, decimal> IndirectActivityHours { get; set; } = new();
        public decimal TotalIndirectHours { get; set; }

        public decimal TotalHours { get; set; }

        public decimal? TotalWeightMT { get; set; }

        /// <summary>Total Hours / Total Weight (MT) - only populated when TotalWeightMT is set (spec section 25, never forced elsewhere).</summary>
        public decimal? HoursPerMT { get; set; }

        public string? DocumentStatusName { get; set; }
    }

    /// <summary>Work Utilization Report - one row per employee for the selected range.</summary>
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
