using Application.Interfaces.Leaves;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
   
    [ApiController]
    [Route("api/leave-dashboard")]
    [Authorize]
    public class LeaveDashboardController : ControllerBase
    {
        private readonly ILeaveDashboardService _service;

        public LeaveDashboardController(ILeaveDashboardService service)
        {
            _service = service;
        }

        [HttpGet("summary/{employeeId}/{year}")]
        public async Task<IActionResult> Summary(string employeeId, int year)
        {
            return Ok(await _service.GetSummary(employeeId, year));
        }

        [HttpGet("balance/{employeeId}/{year}")]
        public async Task<IActionResult> Balance(string employeeId, int year)
        {
            return Ok(await _service.GetBalances(employeeId, year));
        }

        [HttpGet("pending/{managerId}")]
        public async Task<IActionResult> Pending(string managerId)
        {
            return Ok(await _service.GetPendingApprovals(managerId));
        }

        [HttpGet("calendar/{employeeId}/{year}/{month}")]
        public async Task<IActionResult> Calendar(string employeeId, int year, int month)
        {
            return Ok(await _service.GetCalendar(employeeId, year, month));
        }

        [HttpGet("stats/{employeeId}/{year}")]
        public async Task<IActionResult> Stats(string employeeId, int year)
        {
            return Ok(await _service.GetStats(employeeId, year));
        }
    }
}
