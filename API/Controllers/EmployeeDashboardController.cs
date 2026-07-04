using Application.Interfaces.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    #region Employee Dashboard API

    [ApiController]
    [Route("api/employee-dashboard")]
    [Authorize]
    public class EmployeeDashboardController : ControllerBase
    {
        private readonly IEmployeeDashboardService _service;

        public EmployeeDashboardController(IEmployeeDashboardService service)
        {
            _service = service;
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> Get(string userId)
        {
            var data = await _service.GetAsync(userId);
            return Ok(data);
        }
    }

    #endregion
}
