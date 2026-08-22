using Application.DTOs.Attendances;
using Application.Interfaces.Attendances;
using Domain.Entities;
using Infrastructure;
using Infrastructure.Data;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static Domain.Enums.EnumExtensions;

namespace Application.Services.Attendances
{
    public class BiometricAgentService : IBiometricAgentService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<BiometricAgentService> _logger;

        // Same "recent contact" window used for BiometricDevice.IsOnline, so
        // Agent/Device online signals stay consistent with each other.
        private static readonly TimeSpan OnlineWindow = TimeSpan.FromMinutes(15);

        public BiometricAgentService(
            ApplicationDbContext db,
            ILogger<BiometricAgentService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<List<BiometricAgentDto>> GetAllAsync()
        {
            try
            {
                var cutoff = DateTime.UtcNow.Subtract(OnlineWindow);

                return await _db.BiometricAgents
                    .Select(a => new BiometricAgentDto
                    {
                        Id = a.Id,
                        TenantId = a.TenantId,
                        AgentCode = a.AgentCode,
                        AgentName = a.AgentName,
                        BranchId = a.BranchId,
                        BranchName = a.Branch != null ? a.Branch.Name : null,
                        Description = a.Description,
                        MachineName = a.MachineName,
                        AgentVersion = a.AgentVersion,
                        IsActive = a.IsActive,
                        LastHeartbeat = a.LastHeartbeat,
                        IsOnline = a.IsActive && a.LastHeartbeat != null && a.LastHeartbeat >= cutoff,
                        DeviceCount = _db.BiometricDevices.Count(d => d.AgentId == a.Id),
                        CreatedBy = a.CreatedBy,
                        ModifiedOn = a.ModifiedOn,
                        ModifiedBy = a.ModifiedBy
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load biometric agents.");
                return new List<BiometricAgentDto>();
            }
        }

        public async Task<BiometricAgentDto?> GetByIdAsync(string id)
        {
            try
            {
                var cutoff = DateTime.UtcNow.Subtract(OnlineWindow);

                return await _db.BiometricAgents
                    .Where(a => a.Id == id)
                    .Select(a => new BiometricAgentDto
                    {
                        Id = a.Id,
                        TenantId = a.TenantId,
                        AgentCode = a.AgentCode,
                        AgentName = a.AgentName,
                        BranchId = a.BranchId,
                        BranchName = a.Branch != null ? a.Branch.Name : null,
                        Description = a.Description,
                        MachineName = a.MachineName,
                        AgentVersion = a.AgentVersion,
                        IsActive = a.IsActive,
                        LastHeartbeat = a.LastHeartbeat,
                        IsOnline = a.IsActive && a.LastHeartbeat != null && a.LastHeartbeat >= cutoff,
                        DeviceCount = _db.BiometricDevices.Count(d => d.AgentId == a.Id),
                        CreatedBy = a.CreatedBy,
                        ModifiedOn = a.ModifiedOn,
                        ModifiedBy = a.ModifiedBy
                    })
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load biometric agent {Id}.", id);
                return null;
            }
        }

        public async Task<BiometricAgentDto> CreateAsync(BiometricAgentDto dto)
        {
            var entity = new BiometricAgent
            {
                Id = IDManager.GetNewId(new BiometricAgent()),
                TenantId = dto.TenantId,
                AgentCode = dto.AgentCode,
                AgentName = dto.AgentName,
                BranchId = string.IsNullOrWhiteSpace(dto.BranchId) ? null : dto.BranchId,
                Description = dto.Description,
                MachineName = dto.MachineName,
                AgentVersion = dto.AgentVersion,
                // Auto-generate the agent auth secret if the caller didn't supply one -
                // same convention as BiometricDeviceService.CreateAsync/DeviceKey.
                AgentKey = string.IsNullOrWhiteSpace(dto.AgentKey)
                    ? Guid.NewGuid().ToString("N")
                    : dto.AgentKey,
                IsActive = dto.IsActive,
                CreatedBy = dto.CreatedBy,
                CreatedOn = DateTime.UtcNow,
            };

            _db.BiometricAgents.Add(entity);

            await _db.SaveChangesAsync();

            dto.Id = entity.Id;
            // Return the generated key so the caller can show/copy it once
            // (needed to configure the BiometricAgent Windows Service).
            dto.AgentKey = entity.AgentKey;
            dto.IsOnline = false;
            dto.DeviceCount = 0;

            return dto;
        }

        public async Task<bool> UpdateAsync(BiometricAgentDto dto)
        {
            try
            {
                var entity = await _db.BiometricAgents
                    .FirstOrDefaultAsync(a => a.Id == dto.Id);

                if (entity == null)
                    return false;

                entity.AgentName = dto.AgentName;
                entity.BranchId = string.IsNullOrWhiteSpace(dto.BranchId) ? null : dto.BranchId;
                entity.Description = dto.Description;
                // Only rotate the key when a new one is explicitly supplied,
                // so a plain "save" from the edit screen doesn't invalidate
                // the running Windows Service's credentials.
                if (!string.IsNullOrWhiteSpace(dto.AgentKey))
                    entity.AgentKey = dto.AgentKey;
                entity.IsActive = dto.IsActive;
                entity.ModifiedBy = dto.ModifiedBy;
                entity.ModifiedOn = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update biometric agent {Id}.", dto.Id);
                return false;
            }
        }

        public async Task<bool> DeleteAsync(string id)
        {
            try
            {
                var entity = await _db.BiometricAgents
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (entity == null)
                    return false;

                var hasDevices = await _db.BiometricDevices.AnyAsync(d => d.AgentId == id);
                if (hasDevices)
                {
                    // The FK is DeleteBehavior.Restrict at the DB level too,
                    // but failing fast here gives a clean message instead of
                    // a raw SqlException bubbling up.
                    _logger.LogWarning("Refused to delete agent {Id} - still has devices assigned.", id);
                    return false;
                }

                _db.BiometricAgents.Remove(entity);

                await _db.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete biometric agent {Id}.", id);
                return false;
            }
        }

        public async Task<BiometricAgent?> ValidateAsync(string agentCode, string agentKey, string tenantId)
        {
            if (string.IsNullOrWhiteSpace(agentCode) || string.IsNullOrWhiteSpace(agentKey))
                return null;

            var agent = await _db.BiometricAgents
                .FirstOrDefaultAsync(a =>
                    a.AgentCode == agentCode &&
                    (string.IsNullOrEmpty(tenantId) || a.TenantId == tenantId));

            if (agent == null || !agent.IsActive)
                return null;

            if (string.IsNullOrWhiteSpace(agent.AgentKey) || agent.AgentKey != agentKey)
                return null;

            return agent;
        }

        public async Task<AgentRegisterResponseDto> RegisterAsync(AgentRegisterRequestDto request, string tenantId)
        {
            if (request == null ||
                string.IsNullOrWhiteSpace(request.AgentCode) ||
                string.IsNullOrWhiteSpace(request.AgentKey))
            {
                return new AgentRegisterResponseDto
                {
                    Success = false,
                    Message = "AgentCode and AgentKey are required."
                };
            }

            var agent = await ValidateAsync(request.AgentCode, request.AgentKey, tenantId);

            if (agent == null)
            {
                // Deliberately generic - do not reveal whether the code
                // exists, belongs to another tenant, or the key is wrong.
                return new AgentRegisterResponseDto
                {
                    Success = false,
                    Message = "Unknown or inactive agent, or invalid agent key."
                };
            }

            agent.MachineName = string.IsNullOrWhiteSpace(request.MachineName) ? agent.MachineName : request.MachineName;
            agent.AgentVersion = string.IsNullOrWhiteSpace(request.AgentVersion) ? agent.AgentVersion : request.AgentVersion;
            agent.LastHeartbeat = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "BiometricAgent {AgentCode} registered from {MachineName} (v{Version}).",
                agent.AgentCode, agent.MachineName, agent.AgentVersion);

            return new AgentRegisterResponseDto
            {
                Success = true,
                Message = "Registered.",
                AgentId = agent.Id,
                AgentCode = agent.AgentCode,
                AgentName = agent.AgentName
            };
        }

        public async Task<bool> HeartbeatAsync(AgentHeartbeatRequestDto request, string tenantId)
        {
            if (request == null)
                return false;

            var agent = await ValidateAsync(request.AgentCode, request.AgentKey, tenantId);

            if (agent == null)
                return false;

            agent.LastHeartbeat = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(request.AgentVersion))
                agent.AgentVersion = request.AgentVersion;

            await _db.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(request.LastError))
            {
                // Never log secrets - LastError is an agent-supplied
                // free-text diagnostic message, not a credential.
                _logger.LogWarning(
                    "BiometricAgent {AgentCode} reported an error on its last cycle: {Error} (queued: {Queued})",
                    agent.AgentCode, request.LastError, request.QueuedRecordCount);
            }

            return true;
        }

        /// <summary>GET /api/BiometricAgent/pending-test-requests - polled by the Worker alongside GetAssignedDevicesAsync each cycle.</summary>
        public async Task<List<AgentTestRequestDto>> GetPendingTestRequestsAsync(string agentId, string tenantId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(agentId))
                    return new List<AgentTestRequestDto>();

                return await _db.BiometricDeviceTestRequests
                    .Where(r =>
                        r.AgentId == agentId &&
                        r.Status == TestConnectionStatus.Pending &&
                        (string.IsNullOrEmpty(tenantId) || r.TenantId == tenantId))
                    .OrderBy(r => r.RequestedOn)
                    .Select(r => new AgentTestRequestDto
                    {
                        RequestId = r.Id,
                        DeviceId = r.DeviceId,
                        DeviceCode = r.Device.DeviceCode,
                        DeviceKey = r.Device.DeviceKey,
                        DeviceType = r.Device.DeviceType,
                        IPAddress = r.Device.IPAddress,
                        Port = r.Device.Port,
                        CommunicationType = r.Device.CommunicationType,
                        CommKey = r.Device.CommKey
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load pending test requests for agent {AgentId}.", agentId);
                return new List<AgentTestRequestDto>();
            }
        }

        /// <summary>POST /api/BiometricAgent/test-result - the agent's report after actually attempting the ESSL SDK connection.</summary>
        public async Task<bool> SubmitTestResultAsync(AgentTestResultSubmitDto dto, string tenantId)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.RequestId))
                return false;

            var agent = await ValidateAsync(dto.AgentCode, dto.AgentKey, tenantId);
            if (agent == null)
                return false;

            var request = await _db.BiometricDeviceTestRequests
                .Include(r => r.Device)
                .FirstOrDefaultAsync(r =>
                    r.Id == dto.RequestId &&
                    r.AgentId == agent.Id &&
                    (string.IsNullOrEmpty(tenantId) || r.TenantId == tenantId));

            if (request == null)
            {
                _logger.LogWarning(
                    "Test result submitted for unknown/foreign request {RequestId} by agent {AgentCode}.",
                    dto.RequestId, dto.AgentCode);
                return false;
            }

            // Already completed (e.g. the API-side timeout already fired
            // while this result was in flight) - first result wins, don't
            // overwrite it with a possibly-stale second one.
            if (request.Status != TestConnectionStatus.Pending)
            {
                _logger.LogInformation(
                    "Ignored test result for request {RequestId} - already {Status}.",
                    request.Id, request.Status);
                return false;
            }

            request.Status = dto.Success ? TestConnectionStatus.Success : TestConnectionStatus.Failed;
            request.Stage = dto.Stage;
            // Defense in depth: the agent should already be sending a safe,
            // pre-composed message, never a raw exception - truncate hard in
            // case a future agent build regresses on that.
            request.Message = string.IsNullOrWhiteSpace(dto.Message)
                ? null
                : dto.Message.Length > 500 ? dto.Message[..500] : dto.Message;
            request.DeviceInfo = string.IsNullOrWhiteSpace(dto.DeviceInfo)
                ? null
                : dto.DeviceInfo.Length > 200 ? dto.DeviceInfo[..200] : dto.DeviceInfo;
            request.CompletedOn = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            if (dto.Success)
            {
                _logger.LogInformation(
                    "Test connection successful. DeviceCode: {DeviceCode}, Agent: {AgentCode}, RequestId: {RequestId}, DeviceInfo: {DeviceInfo}",
                    request.Device?.DeviceCode, agent.AgentCode, request.Id, request.DeviceInfo);
            }
            else
            {
                _logger.LogWarning(
                    "Test connection failed. DeviceCode: {DeviceCode}, Agent: {AgentCode}, RequestId: {RequestId}, Stage: {Stage}, Error: {Message}",
                    request.Device?.DeviceCode, agent.AgentCode, request.Id, request.Stage, request.Message);
            }

            return true;
        }
    }
}
