using Application.DTOs.Attendances;

namespace Application.Interfaces.Attendances
{
    /// <summary>
    /// Admin-facing CRUD (MVC/API, JWT-authenticated) plus the agent-facing
    /// register/heartbeat/validate operations (AgentCode+AgentKey
    /// authenticated, called by the Windows Service - never by SQL directly).
    /// </summary>
    public interface IBiometricAgentService
    {
        Task<List<BiometricAgentDto>> GetAllAsync();
        Task<BiometricAgentDto?> GetByIdAsync(string id);
        Task<BiometricAgentDto> CreateAsync(BiometricAgentDto dto);
        Task<bool> UpdateAsync(BiometricAgentDto dto);
        Task<bool> DeleteAsync(string id);

        /// <summary>
        /// Validates AgentCode+AgentKey scoped to the given tenant. Returns
        /// null if the agent doesn't exist, is inactive, the key doesn't
        /// match, or it belongs to a different tenant - callers must treat
        /// all of those identically (no distinguishing error detail) to
        /// avoid leaking which part of the credential pair was wrong.
        /// </summary>
        Task<Domain.Entities.BiometricAgent?> ValidateAsync(string agentCode, string agentKey, string tenantId);

        Task<AgentRegisterResponseDto> RegisterAsync(AgentRegisterRequestDto request, string tenantId);

        Task<bool> HeartbeatAsync(AgentHeartbeatRequestDto request, string tenantId);
    }
}
