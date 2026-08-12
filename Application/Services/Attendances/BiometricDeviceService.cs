using Application.DTOs.Attendances;
using Domain.Entities;
using Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Application.Interfaces.Attendances;
using Infrastructure.Data;
using Microsoft.Extensions.Logging;


namespace Application.Services.Attendances
{
    public class BiometricDeviceService : IBiometricDeviceService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<BiometricDeviceService> _logger;

        // Same window used everywhere in this feature (Device.IsOnline,
        // Agent.IsOnline, Health summary) so all "connected" badges agree.
        private static readonly TimeSpan OnlineWindow = TimeSpan.FromMinutes(15);

        public BiometricDeviceService(
            ApplicationDbContext db,
            ILogger<BiometricDeviceService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<List<BiometricDeviceDto>>
            GetAllAsync()
        {
            try
            {
            // Same "connected" signal as TestConnectionAsync/GetHealthSummaryAsync:
            // active AND a punch pushed by the agent within the last 15 minutes.
            var onlineCutoff = DateTime.UtcNow.Subtract(OnlineWindow);

            return await _db.BiometricDevices
                .Select(x => new BiometricDeviceDto
                {
                    Id = x.Id,
                    TenantId = x.TenantId,
                    CompanyId = x.CompanyId,
                    BranchId = x.BranchId,
                    DeviceName = x.DeviceName,
                    DeviceCode = x.DeviceCode,
                    IPAddress = x.IPAddress,
                    Username = x.Username,
                    Password = x.Password,
                    Port = x.Port,
                    ApiUrl = x.ApiUrl,
                    SerialNumber = x.SerialNumber,
                    LastSyncDate = x.LastSyncDate,
                    LastSeen = x.LastSeen,
                    DeviceKey = x.DeviceKey,
                    DeviceType = x.DeviceType,
                    CommunicationType = x.CommunicationType,
                    AgentId = x.AgentId,
                    AgentName = x.Agent != null ? x.Agent.AgentName : null,
                    IsActive = x.IsActive,
                    IsOnline = x.IsActive && x.LastSyncDate != null && x.LastSyncDate >= onlineCutoff
                }).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load biometric devices.");
                return new List<BiometricDeviceDto>();
            }
        }

        public async Task<BiometricDeviceDto?>
            GetByIdAsync(string id)
        {
            try
            {
            var onlineCutoff = DateTime.UtcNow.Subtract(OnlineWindow);

            return await _db.BiometricDevices
                .Where(x => x.Id == id)
                .Select(x => new BiometricDeviceDto
                {
                    Id = x.Id,
                    TenantId = x.TenantId,
                    CompanyId = x.CompanyId,
                    BranchId = x.BranchId,
                    DeviceName = x.DeviceName,
                    DeviceCode = x.DeviceCode,
                    IPAddress = x.IPAddress,
                    Username = x.Username,
                    Password = x.Password,
                    Port = x.Port,
                    ApiUrl = x.ApiUrl,
                    SerialNumber = x.SerialNumber,
                    LastSyncDate = x.LastSyncDate,
                    LastSeen = x.LastSeen,
                    DeviceKey = x.DeviceKey,
                    DeviceType = x.DeviceType,
                    CommunicationType = x.CommunicationType,
                    AgentId = x.AgentId,
                    AgentName = x.Agent != null ? x.Agent.AgentName : null,
                    IsActive = x.IsActive,
                    IsOnline = x.IsActive && x.LastSyncDate != null && x.LastSyncDate >= onlineCutoff
                }).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load biometric device {Id}.", id);
                return null;
            }
        }

        public async Task<BiometricDeviceDto>
            CreateAsync(BiometricDeviceDto dto)
        {
            try
            {
            // Guard against assigning to an agent from a different tenant -
            // TenantId isolation must hold even if the caller (a compromised
            // or buggy MVC session) sends a foreign AgentId.
            if (!string.IsNullOrWhiteSpace(dto.AgentId))
            {
                var agentOk = await _db.BiometricAgents.AnyAsync(a =>
                    a.Id == dto.AgentId && a.TenantId == dto.TenantId);

                if (!agentOk)
                    throw new InvalidOperationException("The selected agent does not belong to this tenant.");
            }

            var entity = new BiometricDevice
            {
                Id=IDManager.GetNewId(new BiometricDevice()),
                TenantId = dto.TenantId,
                CompanyId = dto.CompanyId,
                BranchId = dto.BranchId,
                DeviceName = dto.DeviceName,
                DeviceCode = dto.DeviceCode,
                IPAddress = dto.IPAddress,
                Port = dto.Port,
                ApiUrl = dto.ApiUrl,
                Username = dto.Username,
                Password = dto.Password,
                SerialNumber = dto.SerialNumber,
                DeviceType = string.IsNullOrWhiteSpace(dto.DeviceType) ? "Essl" : dto.DeviceType,
                CommunicationType = string.IsNullOrWhiteSpace(dto.CommunicationType) ? "TCP/IP" : dto.CommunicationType,
                AgentId = string.IsNullOrWhiteSpace(dto.AgentId) ? null : dto.AgentId,
                // Auto-generate the agent auth secret if the caller didn't supply one.
                DeviceKey = string.IsNullOrWhiteSpace(dto.DeviceKey)
                    ? Guid.NewGuid().ToString("N")
                    : dto.DeviceKey,
                IsActive = dto.IsActive,
                CreatedBy = dto.CreatedBy,
                CreatedOn = DateTime.UtcNow,
            };

            _db.BiometricDevices.Add(entity);

            await _db.SaveChangesAsync();

            dto.Id = entity.Id;
            // Return the generated key so the caller can show/copy it once
            // (it's needed to configure the BiometricAgent at the client site).
            dto.DeviceKey = entity.DeviceKey;
            // A brand-new device has never synced yet, so it's Disconnected
            // until the on-site BiometricAgent pushes its first punch.
            dto.IsOnline = false;

            return dto;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create biometric device {DeviceCode}.", dto?.DeviceCode);
                return null;
            }
        }

        public async Task<bool>
            UpdateAsync(BiometricDeviceDto dto)
        {
            try
            {
            var entity = await _db.BiometricDevices
                .FirstOrDefaultAsync(x => x.Id == dto.Id);

            if (entity == null)
                return false;

            if (!string.IsNullOrWhiteSpace(dto.AgentId) && dto.AgentId != entity.AgentId)
            {
                var agentOk = await _db.BiometricAgents.AnyAsync(a =>
                    a.Id == dto.AgentId && a.TenantId == entity.TenantId);

                if (!agentOk)
                    throw new InvalidOperationException("The selected agent does not belong to this tenant.");
            }

            entity.DeviceName = dto.DeviceName;
            entity.DeviceCode = dto.DeviceCode;
            entity.IPAddress = dto.IPAddress;
            entity.Port = dto.Port;
            entity.ApiUrl = dto.ApiUrl;
            entity.Username = dto.Username;
            entity.Password = dto.Password;
            entity.SerialNumber = dto.SerialNumber;
            entity.DeviceType = string.IsNullOrWhiteSpace(dto.DeviceType) ? entity.DeviceType : dto.DeviceType;
            entity.CommunicationType = string.IsNullOrWhiteSpace(dto.CommunicationType) ? entity.CommunicationType : dto.CommunicationType;
            entity.AgentId = string.IsNullOrWhiteSpace(dto.AgentId) ? null : dto.AgentId;
            // Only rotate the key when a new one is explicitly supplied, so a
            // plain "save" from the edit screen doesn't invalidate the agent's key.
            if (!string.IsNullOrWhiteSpace(dto.DeviceKey))
                entity.DeviceKey = dto.DeviceKey;
            entity.IsActive = dto.IsActive;
            entity.CreatedBy = dto.CreatedBy;
            entity.ModifiedBy = dto.ModifiedBy;
            entity.ModifiedOn = dto.ModifiedOn;

            await _db.SaveChangesAsync();

            return true;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update biometric device {Id}.", dto?.Id);
                return false;
            }
        }

        public async Task<bool>
            DeleteAsync(string id)
        {
            try
            {
            var entity = await _db.BiometricDevices
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return false;

            _db.BiometricDevices.Remove(entity);

            await _db.SaveChangesAsync();

            return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete biometric device {Id}.", id);
                return false;
            }
        }

        /// <summary>
        /// "Connection" here means: is the on-site BiometricAgent still
        /// successfully reaching this device and pushing punches to us?
        ///
        /// This deliberately does NOT ping/connect to device.IPAddress from
        /// the API server. That IP is on the client's private LAN, behind
        /// their router/firewall - the whole reason BiometricAgent exists is
        /// that the API generally *cannot* reach it directly. A ping from a
        /// cloud-hosted API would just time out for every device, even
        /// healthy ones, and report false negatives.
        ///
        /// Instead, use the same signal as GetHealthSummaryAsync: whether
        /// LastSyncDate (updated by BiometricSyncService.IngestPunchesAsync
        /// each time the agent successfully pushes) is recent. That proves
        /// the full chain - device, agent, client network, and API - is
        /// actually working end-to-end, which is a more meaningful test than
        /// a bare ping could ever be.
        /// </summary>
        public async Task<bool>
            TestConnectionAsync(string id)
        {
            try
            {
                var device = await _db.BiometricDevices
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (device == null || !device.IsActive)
                    return false;

                var onlineCutoff = DateTime.UtcNow.Subtract(OnlineWindow);

                return device.LastSyncDate != null &&
                       device.LastSyncDate >= onlineCutoff;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to test connection for biometric device {Id}.", id);
                return false;
            }
        }

        public async Task<List<BiometricDeviceHealthDto>> GetHealthSummaryAsync()
        {
            try
            {
                var onlineCutoff = DateTime.UtcNow.AddMinutes(-15);

                return await _db.BiometricDevices
                    .Select(x => new BiometricDeviceHealthDto
                    {
                        DeviceId = x.Id,
                        DeviceName = x.DeviceName,
                        DeviceCode = x.DeviceCode,
                        IPAddress = x.IPAddress,
                        IsActive = x.IsActive,
                        LastSyncDate = x.LastSyncDate,

                        IsOnline = x.IsActive &&
                            x.LastSyncDate != null &&
                            x.LastSyncDate >= onlineCutoff,

                        TotalPunchCount = _db.BiometricAttendanceLogs
                            .Count(l => l.DeviceId == x.Id),

                        PendingPunchCount = _db.BiometricAttendanceLogs
                            .Count(l => l.DeviceId == x.Id && !l.IsProcessed)
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load biometric device health summary.");
                return new List<BiometricDeviceHealthDto>();
            }
        }

        public async Task<BiometricDeviceStatusDto?> GetStatusAsync(string id)
        {
            try
            {
                var onlineCutoff = DateTime.UtcNow.Subtract(OnlineWindow);

                var status = await _db.BiometricDevices
                    .Where(x => x.Id == id)
                    .Select(x => new BiometricDeviceStatusDto
                    {
                        DeviceId = x.Id,
                        DeviceCode = x.DeviceCode,
                        DeviceName = x.DeviceName,
                        IsActive = x.IsActive,
                        LastSyncDate = x.LastSyncDate,
                        LastSeen = x.LastSeen,
                        IsOnline = x.IsActive && x.LastSyncDate != null && x.LastSyncDate >= onlineCutoff,
                        AgentId = x.AgentId,
                        AgentCode = x.Agent != null ? x.Agent.AgentCode : null,
                        AgentName = x.Agent != null ? x.Agent.AgentName : null,
                        AgentOnline = x.Agent != null
                            ? (bool?)(x.Agent.IsActive && x.Agent.LastHeartbeat != null && x.Agent.LastHeartbeat >= onlineCutoff)
                            : null,
                        AgentLastHeartbeat = x.Agent != null ? x.Agent.LastHeartbeat : null,
                        TotalPunchCount = _db.BiometricAttendanceLogs.Count(l => l.DeviceId == x.Id),
                        PendingPunchCount = _db.BiometricAttendanceLogs.Count(l => l.DeviceId == x.Id && !l.IsProcessed)
                    })
                    .FirstOrDefaultAsync();

                if (status == null)
                    return null;

                if (!status.IsActive)
                    status.LastError = "Device is disabled (IsActive = false).";
                else if (status.AgentId == null)
                    status.LastError = "No BiometricAgent is assigned to this device.";
                else if (status.AgentOnline == false)
                    status.LastError = "Assigned agent has not sent a heartbeat recently.";
                else if (!status.IsOnline)
                    status.LastError = "Agent is online but has not synced this device recently.";

                return status;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load status for biometric device {Id}.", id);
                return null;
            }
        }

        public async Task<List<BiometricDeviceDto>> GetByAgentAsync(string agentId, string tenantId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(agentId))
                    return new List<BiometricDeviceDto>();

                return await _db.BiometricDevices
                    .Where(x =>
                        x.AgentId == agentId &&
                        x.IsActive &&
                        (string.IsNullOrEmpty(tenantId) || x.TenantId == tenantId))
                    .Select(x => new BiometricDeviceDto
                    {
                        Id = x.Id,
                        TenantId = x.TenantId,
                        CompanyId = x.CompanyId,
                        BranchId = x.BranchId,
                        DeviceName = x.DeviceName,
                        DeviceCode = x.DeviceCode,
                        IPAddress = x.IPAddress,
                        Username = x.Username,
                        Password = x.Password,
                        Port = x.Port,
                        ApiUrl = x.ApiUrl,
                        SerialNumber = x.SerialNumber,
                        LastSyncDate = x.LastSyncDate,
                        LastSeen = x.LastSeen,
                        DeviceKey = x.DeviceKey,
                        DeviceType = x.DeviceType,
                        CommunicationType = x.CommunicationType,
                        AgentId = x.AgentId,
                        IsActive = x.IsActive
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load devices for agent {AgentId}.", agentId);
                return new List<BiometricDeviceDto>();
            }
        }
    }
}
