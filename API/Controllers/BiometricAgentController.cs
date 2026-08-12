using Application.Common.Responses;
using Application.DTOs.Attendances;
using Application.Interfaces.Attendances;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>
    /// Endpoints called by the on-site BiometricAgent Windows Service - never
    /// by a logged-in user. Authenticated via AgentCode + AgentKey (same
    /// shared-secret pattern as BiometricSyncController.Ingest's
    /// DeviceCode/DeviceKey), not a user JWT, so [AllowAnonymous] here is
    /// intentional. The agent must still send X-Tenant-ID (TenantMiddleware
    /// enforces this for every non-/api/auth request), which is what scopes
    /// every lookup below to the right tenant.
    ///
    /// CRUD for the BiometricAgent record itself (create/edit/list from the
    /// MVC) lives in the plain [Authorize] actions at the bottom of this
    /// controller, matching BiometricDeviceController's pattern.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class BiometricAgentController : ControllerBase
    {
        private readonly IBiometricAgentService _agentService;
        private readonly IBiometricDeviceService _deviceService;
        private readonly ITenantService _tenantService;
        private readonly ILogger<BiometricAgentController> _logger;

        public BiometricAgentController(
            IBiometricAgentService agentService,
            IBiometricDeviceService deviceService,
            ITenantService tenantService,
            ILogger<BiometricAgentController> logger)
        {
            _agentService = agentService;
            _deviceService = deviceService;
            _tenantService = tenantService;
            _logger = logger;
        }

        // ==================================================================
        // AGENT-FACING (AllowAnonymous, AgentCode/AgentKey authenticated)
        // ==================================================================

        /// <summary>
        /// First contact on service startup, and periodically thereafter.
        /// Confirms the agent is known/active for this tenant and refreshes
        /// machine/version metadata.
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] AgentRegisterRequestDto request)
        {
            var tenantId = _tenantService.GetTenantId();

            var result = await _agentService.RegisterAsync(request, tenantId);

            if (!result.Success)
            {
                _logger.LogWarning(
                    "BiometricAgent register rejected for AgentCode {AgentCode} (tenant {TenantId}): {Message}",
                    request?.AgentCode, tenantId, result.Message);

                return Unauthorized(result);
            }

            return Ok(result);
        }

        /// <summary>Lightweight liveness ping the agent sends every poll cycle (independent of whether it had any punches to push).</summary>
        [HttpPost("heartbeat")]
        [AllowAnonymous]
        public async Task<IActionResult> Heartbeat([FromBody] AgentHeartbeatRequestDto request)
        {
            var tenantId = _tenantService.GetTenantId();

            var ok = await _agentService.HeartbeatAsync(request, tenantId);

            if (!ok)
                return Unauthorized(new { Success = false, Message = "Unknown or inactive agent, or invalid agent key." });

            return Ok(new { Success = true });
        }

        /// <summary>Agent-level operating parameters (not per-device config - see /devices for that).</summary>
        [HttpGet("config")]
        [AllowAnonymous]
        public async Task<IActionResult> GetConfig([FromHeader(Name = "X-Agent-Code")] string agentCode,
                                                     [FromHeader(Name = "X-Agent-Key")] string agentKey)
        {
            var tenantId = _tenantService.GetTenantId();

            var agent = await _agentService.ValidateAsync(agentCode, agentKey, tenantId);

            if (agent == null)
                return Unauthorized(new { Success = false, Message = "Unknown or inactive agent, or invalid agent key." });

            return Ok(new AgentConfigResponseDto
            {
                AgentId = agent.Id,
                AgentCode = agent.AgentCode,
                AgentName = agent.AgentName,
                TenantId = agent.TenantId,
                IsActive = agent.IsActive
            });
        }

        /// <summary>
        /// Active BiometricDevice configurations assigned to this agent -
        /// the driver connection details the Worker feeds to
        /// DeviceDriverFactory each poll cycle.
        /// </summary>
        [HttpGet("devices")]
        [AllowAnonymous]
        public async Task<IActionResult> GetDevices([FromHeader(Name = "X-Agent-Code")] string agentCode,
                                                       [FromHeader(Name = "X-Agent-Key")] string agentKey)
        {
            var tenantId = _tenantService.GetTenantId();

            var agent = await _agentService.ValidateAsync(agentCode, agentKey, tenantId);

            if (agent == null)
                return Unauthorized(new { Success = false, Message = "Unknown or inactive agent, or invalid agent key." });

            var devices = await _deviceService.GetByAgentAsync(agent.Id, tenantId);

            return Ok(devices);
        }

        // ==================================================================
        // ADMIN-FACING (JWT [Authorize], called from MVC) - CRUD for the
        // BiometricAgent master record, matching BiometricDeviceController's shape.
        // ==================================================================

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Get()
        {
            return Ok(await _agentService.GetAllAsync());
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> Get(string id)
        {
            return Ok(await _agentService.GetByIdAsync(id));
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create(BiometricAgentDto dto)
        {
            try
            {
                var result = await _agentService.CreateAsync(dto);

                return Ok(new ApiResponse<BiometricAgentDto>
                {
                    Success = true,
                    Message = "Biometric agent created successfully.",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create biometric agent.");

                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Failed to create biometric agent."
                });
            }
        }

        [HttpPut]
        [Authorize]
        public async Task<IActionResult> Update(BiometricAgentDto dto)
        {
            var ok = await _agentService.UpdateAsync(dto);

            return Ok(new ApiResponse<BiometricAgentDto>
            {
                Success = ok,
                Message = ok ? "Biometric agent updated successfully." : "Agent not found.",
                Data = ok ? dto : null
            });
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(string id)
        {
            return Ok(await _agentService.DeleteAsync(id));
        }
    }
}
