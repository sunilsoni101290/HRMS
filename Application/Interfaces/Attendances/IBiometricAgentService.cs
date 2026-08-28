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
        /// Rotates this agent's AgentKey to a fresh, randomly generated
        /// value and returns the full DTO with the new key populated -
        /// the only other place (besides CreateAsync) that ever returns
        /// AgentKey, since GetAllAsync/GetByIdAsync deliberately omit it.
        /// Invalidates whatever key any currently-running BiometricAgent
        /// Windows Service instance is using, so it will start failing
        /// register/heartbeat until reconfigured with the new key -
        /// callers (the "Download Agent Config" flow) should warn about
        /// this before invoking it.
        /// </summary>
        Task<BiometricAgentDto?> RegenerateKeyAsync(string id);

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

        /// <summary>
        /// Pending (Status = Pending) BiometricDeviceTestRequests routed to
        /// this agent - polled every cycle alongside GetAssignedDevicesAsync
        /// (see BiometricAgent.Worker). Does not flip them to any other
        /// status; the agent reports the outcome separately via
        /// SubmitTestResultAsync once it has actually tried the device.
        /// </summary>
        Task<List<AgentTestRequestDto>> GetPendingTestRequestsAsync(string agentId, string tenantId);

        /// <summary>
        /// Records the agent's outcome for one BiometricDeviceTestRequest.
        /// Idempotent-ish: a request that is no longer Pending (already
        /// completed, or timed out from the API side) is left alone and this
        /// returns false, since two different results racing in would be
        /// ambiguous and the first one to arrive should win.
        /// </summary>
        Task<bool> SubmitTestResultAsync(AgentTestResultSubmitDto dto, string tenantId);
    }
}
