using Application.Common.Responses;
using Application.DTOs.Employee;
using Application.DTOs.WorkTracking;
using Application.Interfaces.WorkTracking;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>Daily Work Entry reports - Monthly Employee, Job-wise, Structure/Equipment, Utilization (spec sections 20-25).</summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WorkTrackingReportController : ControllerBase
    {
        private readonly IWorkTrackingReportService _service;

        private string? TenantId => User.FindFirst("TenantId")?.Value;

        public WorkTrackingReportController(IWorkTrackingReportService service)
        {
            _service = service;
        }

        [HttpGet("monthly-employee")]
        public async Task<IActionResult> GetMonthlyEmployeeReport([FromQuery] WorkReportFilterDto filter)
        {
            var result = await _service.GetMonthlyEmployeeWorkReportAsync(filter ?? new WorkReportFilterDto(), TenantId!);
            return Ok(new ApiResponse<PagedResult<MonthlyEmployeeWorkReportDto>> { Success = true, Data = result });
        }

        [HttpGet("job-wise")]
        public async Task<IActionResult> GetJobWiseReport([FromQuery] WorkReportFilterDto filter)
        {
            var result = await _service.GetJobWiseWorkReportAsync(filter ?? new WorkReportFilterDto(), TenantId!);
            return Ok(new ApiResponse<List<JobWiseWorkReportDto>> { Success = true, Data = result });
        }

        [HttpGet("structure-wise")]
        public async Task<IActionResult> GetStructureReport([FromQuery] WorkReportFilterDto filter)
        {
            var result = await _service.GetStructureWorkReportAsync(filter ?? new WorkReportFilterDto(), TenantId!);
            return Ok(new ApiResponse<List<StructureWorkReportDto>> { Success = true, Data = result });
        }

        [HttpGet("utilization")]
        public async Task<IActionResult> GetUtilizationReport([FromQuery] WorkReportFilterDto filter)
        {
            var result = await _service.GetUtilizationReportAsync(filter ?? new WorkReportFilterDto(), TenantId!);
            return Ok(new ApiResponse<List<UtilizationReportDto>> { Success = true, Data = result });
        }
    }
}
