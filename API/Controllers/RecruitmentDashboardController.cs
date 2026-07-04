using Application.Interfaces.Recruitment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    #region Recruitment Dashboard API

    [ApiController]
    [Route("api/recruitment-dashboard")]
    [Authorize]
    public class RecruitmentDashboardController : ControllerBase
    {
        private readonly IRecruitmentDashboardService _service;

        public RecruitmentDashboardController(IRecruitmentDashboardService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var data = await _service.GetDashboardAsync();
            return Ok(data);
        }
    }

    #endregion
}
