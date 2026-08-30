using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs.Employee;
using Application.DTOs.WorkTracking;
using Application.Interfaces.WorkTracking;
using Domain.Entities;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using static Domain.Enums.EnumExtensions;

namespace Application.Services.WorkTracking
{
    /// <summary>
    /// Reporting/analytics over DailyWorkLog+DailyWorkEntry - spec sections
    /// 20-25. By default (no explicit Status filter) only Pending and
    /// Approved days are counted as "real" work, matching how every other
    /// report in this codebase excludes Draft (not-yet-submitted, so not
    /// yet meaningful) - callers can still pass an explicit Status to see
    /// e.g. only Approved. Uses AsNoTracking + server-side aggregation
    /// throughout (spec section 40 - no loading entire tables into memory).
    /// </summary>
    public class WorkTrackingReportService : IWorkTrackingReportService
    {
        private readonly ApplicationDbContext _context;

        public WorkTrackingReportService(ApplicationDbContext context)
        {
            _context = context;
        }

        private IQueryable<DailyWorkLog> BaseHeaderQuery(WorkReportFilterDto filter, string tenantId)
        {
            var query = _context.DailyWorkLogs.AsNoTracking()
                .Include(x => x.Employee).ThenInclude(e => e!.Department)
                .Include(x => x.Employee).ThenInclude(e => e!.Designation)
                .Where(x => x.TenantId == tenantId);

            query = filter.Status.HasValue && Enum.IsDefined(typeof(ApprovalStatus), filter.Status.Value)
                ? query.Where(x => x.Status == (ApprovalStatus)filter.Status.Value)
                : query.Where(x => x.Status == ApprovalStatus.Pending || x.Status == ApprovalStatus.Approved);

            if (!string.IsNullOrEmpty(filter.EmployeeId)) query = query.Where(x => x.EmployeeId == filter.EmployeeId);
            if (!string.IsNullOrEmpty(filter.DepartmentId)) query = query.Where(x => x.Employee!.DepartmentId == filter.DepartmentId);
            if (filter.FromDate.HasValue) query = query.Where(x => x.WorkDate >= filter.FromDate.Value.Date);
            if (filter.ToDate.HasValue) query = query.Where(x => x.WorkDate <= filter.ToDate.Value.Date);

            return query;
        }

        // ==================================================================
        // Monthly Employee Work Report (Excel "Employ Work Report Monthly")
        // ==================================================================

        public async Task<PagedResult<MonthlyEmployeeWorkReportDto>> GetMonthlyEmployeeWorkReportAsync(WorkReportFilterDto filter, string tenantId)
        {
            var headerQuery = BaseHeaderQuery(filter, tenantId);

            var employeeIds = await headerQuery.Select(x => x.EmployeeId).Distinct().ToListAsync();

            var pageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
            var pageSize = filter.PageSize is < 1 or > 200 ? 20 : filter.PageSize;

            var pagedEmployeeIds = employeeIds
                .OrderBy(x => x)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var results = new List<MonthlyEmployeeWorkReportDto>();

            foreach (var employeeId in pagedEmployeeIds)
            {
                var headers = await headerQuery
                    .Where(x => x.EmployeeId == employeeId)
                    .Include(x => x.Entries!).ThenInclude(e => e.WorkActivity)
                    .Include(x => x.Entries!).ThenInclude(e => e.WorkJob).ThenInclude(j => j!.Client)
                    .Include(x => x.Entries!).ThenInclude(e => e.JobItem).ThenInclude(ji => ji!.DocumentStatus)
                    .ToListAsync();

                if (headers.Count == 0) continue;

                var employee = headers[0].Employee;
                var allEntries = headers.SelectMany(h => h.Entries ?? Enumerable.Empty<DailyWorkEntry>()).ToList();

                decimal SumCategory(WorkCategory cat) =>
                    allEntries.Where(e => e.WorkActivity?.WorkCategory == cat).Sum(e => e.Hours);

                var direct = SumCategory(WorkCategory.Direct);
                var clocked = headers.Sum(h => h.ClockedHours ?? 0);

                var dto = new MonthlyEmployeeWorkReportDto
                {
                    EmployeeId = employeeId,
                    EmployeeCode = employee?.EmployeeCode ?? "",
                    EmployeeName = employee != null ? $"{employee.FirstName} {employee.LastName}".Trim() : "",
                    Position = employee?.Designation?.Name,
                    ReportFrom = filter.FromDate ?? headers.Min(h => h.WorkDate),
                    ReportTo = filter.ToDate ?? headers.Max(h => h.WorkDate),
                    TotalClockedHours = clocked,
                    IdealHours = clocked,
                    DirectHours = direct,
                    IndirectHours = SumCategory(WorkCategory.Indirect),
                    IdleHours = SumCategory(WorkCategory.Idle),
                    DowntimeHours = SumCategory(WorkCategory.Downtime),
                    UtilizationPercent = clocked > 0 ? Math.Round(direct / clocked * 100, 1) : 0
                };

                dto.JobLines = allEntries
                    .Where(e => e.WorkJobId != null)
                    .GroupBy(e => new { e.WorkJobId, JobNumber = e.WorkJob!.JobNumber, ClientName = e.WorkJob.Client?.Name, JobItemCode = e.JobItem?.Code, DocStatus = e.JobItem?.DocumentStatus?.DisplayName })
                    .Select(g => new MonthlyEmployeeWorkJobLineDto
                    {
                        JobNumber = g.Key.JobNumber,
                        ClientName = g.Key.ClientName,
                        JobItemCode = g.Key.JobItemCode,
                        Hours = g.Sum(e => e.Hours),
                        DocumentStatusName = g.Key.DocStatus
                    })
                    .OrderBy(l => l.JobNumber)
                    .ToList();

                results.Add(dto);
            }

            return new PagedResult<MonthlyEmployeeWorkReportDto>
            {
                Data = results,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = employeeIds.Count
            };
        }

        // ==================================================================
        // Job-wise Work Report
        // ==================================================================

        public async Task<List<JobWiseWorkReportDto>> GetJobWiseWorkReportAsync(WorkReportFilterDto filter, string tenantId)
        {
            var headerQuery = BaseHeaderQuery(filter, tenantId);
            var headerIds = headerQuery.Select(x => x.Id);

            var entriesQuery = _context.DailyWorkEntries.AsNoTracking()
                .Include(x => x.WorkJob).ThenInclude(j => j!.Client)
                .Include(x => x.WorkActivity)
                .Where(x => headerIds.Contains(x.DailyWorkLogId) && x.WorkJobId != null);

            if (!string.IsNullOrEmpty(filter.WorkJobId)) entriesQuery = entriesQuery.Where(x => x.WorkJobId == filter.WorkJobId);
            if (!string.IsNullOrEmpty(filter.ClientId)) entriesQuery = entriesQuery.Where(x => x.WorkJob!.ClientId == filter.ClientId);
            if (!string.IsNullOrEmpty(filter.JobTypeId)) entriesQuery = entriesQuery.Where(x => x.JobTypeId == filter.JobTypeId);

            var entries = await entriesQuery.ToListAsync();

            return entries
                .GroupBy(e => new { e.WorkJobId, e.WorkJob!.JobNumber, e.WorkJob.JobName, ClientName = e.WorkJob.Client?.Name })
                .Select(g => new JobWiseWorkReportDto
                {
                    WorkJobId = g.Key.WorkJobId!,
                    JobNumber = g.Key.JobNumber,
                    JobName = g.Key.JobName,
                    ClientName = g.Key.ClientName,
                    DirectHours = g.Where(e => e.WorkActivity?.WorkCategory == WorkCategory.Direct).Sum(e => e.Hours),
                    IndirectHours = g.Where(e => e.WorkActivity?.WorkCategory == WorkCategory.Indirect).Sum(e => e.Hours),
                    TotalHours = g.Sum(e => e.Hours)
                })
                .OrderByDescending(x => x.TotalHours)
                .ToList();
        }

        // ==================================================================
        // Structure/Equipment Work Report (Excel "Reports-Only for assigned")
        // ==================================================================

        public async Task<List<StructureWorkReportDto>> GetStructureWorkReportAsync(WorkReportFilterDto filter, string tenantId)
        {
            var headerQuery = BaseHeaderQuery(filter, tenantId);
            var headerIds = headerQuery.Select(x => x.Id);

            var entriesQuery = _context.DailyWorkEntries.AsNoTracking()
                .Include(x => x.WorkJob).ThenInclude(j => j!.Client)
                .Include(x => x.JobItem).ThenInclude(ji => ji!.DocumentStatus)
                .Include(x => x.WorkActivity)
                .Where(x => headerIds.Contains(x.DailyWorkLogId) && x.JobItemId != null);

            if (!string.IsNullOrEmpty(filter.WorkJobId)) entriesQuery = entriesQuery.Where(x => x.WorkJobId == filter.WorkJobId);
            if (!string.IsNullOrEmpty(filter.JobTypeId)) entriesQuery = entriesQuery.Where(x => x.JobTypeId == filter.JobTypeId);

            var entries = await entriesQuery.ToListAsync();

            return entries
                .GroupBy(e => e.JobItemId)
                .Select(g =>
                {
                    var first = g.First();
                    var directLines = g.Where(e => e.WorkActivity?.WorkCategory == WorkCategory.Direct).ToList();
                    var indirectLines = g.Where(e => e.WorkActivity?.WorkCategory == WorkCategory.Indirect).ToList();

                    var totalDirect = directLines.Sum(e => e.Hours);
                    var weight = first.JobItem?.TotalWeightMT;

                    return new StructureWorkReportDto
                    {
                        JobNumber = first.WorkJob?.JobNumber ?? "",
                        ClientName = first.WorkJob?.Client?.Name,
                        JobItemId = g.Key!,
                        StructureId = first.JobItem?.Code ?? "",
                        DirectActivityHours = directLines
                            .GroupBy(e => e.WorkActivity!.Name)
                            .ToDictionary(x => x.Key, x => x.Sum(e => e.Hours)),
                        TotalDirectHours = totalDirect,
                        IndirectActivityHours = indirectLines
                            .GroupBy(e => e.WorkActivity!.Name)
                            .ToDictionary(x => x.Key, x => x.Sum(e => e.Hours)),
                        TotalIndirectHours = indirectLines.Sum(e => e.Hours),
                        TotalHours = g.Sum(e => e.Hours),
                        TotalWeightMT = weight,
                        // Engineering Hours / MT (spec section 25) - only
                        // when a weight is actually recorded; never forced
                        // on job types where it doesn't apply.
                        HoursPerMT = weight.HasValue && weight.Value > 0 ? Math.Round(totalDirect / weight.Value, 2) : null,
                        DocumentStatusName = first.JobItem?.DocumentStatus?.DisplayName
                    };
                })
                .OrderBy(x => x.JobNumber).ThenBy(x => x.StructureId)
                .ToList();
        }

        // ==================================================================
        // Work Utilization Report
        // ==================================================================

        public async Task<List<UtilizationReportDto>> GetUtilizationReportAsync(WorkReportFilterDto filter, string tenantId)
        {
            var headers = await BaseHeaderQuery(filter, tenantId)
                .Include(x => x.Entries!).ThenInclude(e => e.WorkActivity)
                .ToListAsync();

            return headers
                .GroupBy(h => h.EmployeeId)
                .Select(g =>
                {
                    var employee = g.First().Employee;
                    var entries = g.SelectMany(h => h.Entries ?? Enumerable.Empty<DailyWorkEntry>()).ToList();
                    var clocked = g.Sum(h => h.ClockedHours ?? 0);
                    var direct = entries.Where(e => e.WorkActivity?.WorkCategory == WorkCategory.Direct).Sum(e => e.Hours);

                    return new UtilizationReportDto
                    {
                        EmployeeId = g.Key,
                        EmployeeCode = employee?.EmployeeCode ?? "",
                        EmployeeName = employee != null ? $"{employee.FirstName} {employee.LastName}".Trim() : "",
                        DepartmentName = employee?.Department?.Name,
                        ClockedHours = clocked,
                        DirectHours = direct,
                        IndirectHours = entries.Where(e => e.WorkActivity?.WorkCategory == WorkCategory.Indirect).Sum(e => e.Hours),
                        IdleHours = entries.Where(e => e.WorkActivity?.WorkCategory == WorkCategory.Idle).Sum(e => e.Hours),
                        DowntimeHours = entries.Where(e => e.WorkActivity?.WorkCategory == WorkCategory.Downtime).Sum(e => e.Hours),
                        UtilizationPercent = clocked > 0 ? Math.Round(direct / clocked * 100, 1) : 0
                    };
                })
                .OrderByDescending(x => x.UtilizationPercent)
                .ToList();
        }
    }
}
