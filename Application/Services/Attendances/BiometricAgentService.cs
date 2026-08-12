using Application.DTOs.Attendances;
using Application.Interfaces.Attendances;
using Domain.Entities;
using Infrastructure;
using Infrastructure.Data;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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
    }
}
