using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Employee;
using Application.DTOs.WorkTracking;

namespace Application.Interfaces.WorkTracking
{
    /// <summary>Reporting/analytics over approved (and, where noted, all) DailyWorkLog data - spec sections 20-25.</summary>
    public interface IWorkTrackingReportService
    {
        Task<PagedResult<MonthlyEmployeeWorkReportDto>> GetMonthlyEmployeeWorkReportAsync(WorkReportFilterDto filter, string tenantId);

        Task<List<JobWiseWorkReportDto>> GetJobWiseWorkReportAsync(WorkReportFilterDto filter, string tenantId);

        Task<List<StructureWorkReportDto>> GetStructureWorkReportAsync(WorkReportFilterDto filter, string tenantId);

        Task<List<UtilizationReportDto>> GetUtilizationReportAsync(WorkReportFilterDto filter, string tenantId);
    }
}
