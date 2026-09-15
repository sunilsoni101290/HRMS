using Application.DTOs.ErrorLogs;
using Application.Interfaces.ErrorLog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>
    /// Deliberately a SEPARATE controller from ErrorLogController (not an
    /// extra action on it) - ErrorLogController carries a class-level
    /// [Authorize(Roles = "System Configurator")], and ASP.NET Core
    /// combines class-level and method-level [Authorize] attributes with
    /// AND, not override, so a plain [Authorize] action added there would
    /// still be restricted to System Configurator only. This endpoint has
    /// to be reachable by ANY logged-in user, since it exists to report
    /// exceptions that occurred in APP (MVC) - which has no direct
    /// database access - for any user's session, not just admins'.
    ///
    /// Called by APP.Attributes.ClientErrorLoggingFilter (a global
    /// IAsyncExceptionFilter) whenever an unhandled exception surfaces in
    /// an APP controller action that never touched the API at all (so it
    /// would otherwise never reach the ErrorLog table). Best-effort only -
    /// IErrorLogService.LogClientErrorAsync never throws, so this always
    /// returns 200 once authenticated.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ClientErrorLogController : ControllerBase
    {
        private readonly IErrorLogService _service;

        public ClientErrorLogController(IErrorLogService service)
        {
            _service = service;
        }

        // POST api/ClientErrorLog
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ClientErrorLogDto dto)
        {
            await _service.LogClientErrorAsync(
                dto,
                userId: User.FindFirst("UserId")?.Value,
                userName: User.Identity?.Name,
                tenantId: User.FindFirst("CompanyId")?.Value);

            return Ok(new { success = true });
        }
    }
}
