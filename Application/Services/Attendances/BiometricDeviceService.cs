using Application.Common.Exceptions;
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
using static Domain.Enums.EnumExtensions;


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
                    CommKey = x.CommKey,
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
                    CommKey = x.CommKey,
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
                CommKey = dto.CommKey,
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
            entity.CommKey = dto.CommKey;
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
        /// How long the UI keeps polling for a result before this service
        /// gives up and reports TimedOut. Comfortably longer than the
        /// agent's default 60s PollIntervalSeconds (see AgentOptions) so a
        /// normally-operating agent always finishes well within this window;
        /// exceeding it is itself diagnostic ("agent isn't polling").
        /// </summary>
        private static readonly TimeSpan TestRequestTimeout = TimeSpan.FromSeconds(90);

        /// <summary>
        /// Kicks off a REAL Test Connection. This deliberately does NOT
        /// ping/connect to device.IPAddress from the API server - that IP is
        /// on the client's private LAN, behind their router/firewall, which
        /// is the whole reason BiometricAgent exists (the API generally
        /// *cannot* reach it directly; a ping from a cloud-hosted API would
        /// just time out for every device, even healthy ones).
        ///
        /// Instead this validates what it can synchronously (device exists/
        /// active, has an assigned agent, that agent is online), then hands
        /// the actual device-level check off to the assigned BiometricAgent
        /// via a Pending BiometricDeviceTestRequest row - see
        /// GetTestConnectionResultAsync for how the caller learns the
        /// outcome, and BiometricAgent.Worker for how the agent picks it up.
        /// </summary>
        public async Task<DeviceTestConnectionResultDto> RequestTestConnectionAsync(string deviceId, string tenantId, string requestedBy)
        {
            var device = await _db.BiometricDevices
                .FirstOrDefaultAsync(x => x.Id == deviceId && (string.IsNullOrEmpty(tenantId) || x.TenantId == tenantId));

            if (device == null)
                throw new NotFoundException("Device not found.", "DEVICE_NOT_FOUND");

            if (!device.IsActive)
                throw new BadRequestException("The biometric device is inactive.", "DEVICE_INACTIVE");

            if (string.IsNullOrWhiteSpace(device.IPAddress) || device.Port <= 0)
                throw new BadRequestException("The device's IP address/port are not configured.", "DEVICE_MISCONFIGURED");

            if (string.IsNullOrWhiteSpace(device.AgentId))
                throw new BadRequestException("No biometric agent is assigned to this device.", "AGENT_NOT_ASSIGNED");

            var agent = await _db.BiometricAgents.FirstOrDefaultAsync(a => a.Id == device.AgentId);

            // Agent record missing entirely (e.g. deleted out from under an
            // assigned device) is distinct from "exists but disabled" - both
            // are still pre-flight, non-retryable failures though, so both
            // map to the same "unavailable" business outcome.
            if (agent == null)
                throw new BadRequestException("The biometric agent is unavailable.", "AGENT_UNAVAILABLE");

            if (!agent.IsActive)
                throw new BadRequestException("The assigned biometric agent is inactive.", "AGENT_INACTIVE");

            var agentOnlineCutoff = DateTime.UtcNow.Subtract(OnlineWindow);
            var agentOnline = agent.LastHeartbeat != null && agent.LastHeartbeat >= agentOnlineCutoff;

            if (!agentOnline)
                throw new BadRequestException(
                    "The biometric agent is unavailable (no heartbeat in the last 15 minutes). " +
                    "Confirm the ERP Biometric Agent Windows Service is running on the branch machine.",
                    "AGENT_UNAVAILABLE");

            var request = new BiometricDeviceTestRequest
            {
                Id = IDManager.GetNewId(new BiometricDeviceTestRequest()),
                TenantId = device.TenantId,
                DeviceId = device.Id,
                AgentId = device.AgentId,
                Status = TestConnectionStatus.Pending,
                RequestedBy = requestedBy,
                RequestedOn = DateTime.UtcNow,
                CreatedBy = requestedBy,
                CreatedOn = DateTime.UtcNow
            };

            _db.BiometricDeviceTestRequests.Add(request);
            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Test connection requested. DeviceId: {DeviceId}, DeviceCode: {DeviceCode}, DeviceType: {DeviceType}, " +
                "Agent: {AgentName}, IP: {Ip}, Port: {Port}, RequestId: {RequestId}, RequestedBy: {RequestedBy}",
                device.Id, device.DeviceCode, device.DeviceType, agent.AgentName, device.IPAddress, device.Port, request.Id, requestedBy);

            return MapTestResult(request, device.DeviceCode);
        }

        public async Task<DeviceTestConnectionResultDto?> GetTestConnectionResultAsync(string requestId, string tenantId)
        {
            var request = await _db.BiometricDeviceTestRequests
                .Include(r => r.Device)
                .FirstOrDefaultAsync(r => r.Id == requestId && (string.IsNullOrEmpty(tenantId) || r.TenantId == tenantId));

            if (request == null)
                return null;

            // Still Pending but past TestRequestTimeout since request was
            // made -> the assigned agent never picked it up (offline,
            // service stopped, or polling a different set of devices).
            // Mark TimedOut here (API-side, on read) rather than requiring
            // a background sweep - this endpoint is only ever hit while the
            // UI is actively polling, so it's always checked promptly.
            if (request.Status == TestConnectionStatus.Pending &&
                DateTime.UtcNow - request.RequestedOn > TestRequestTimeout)
            {
                request.Status = TestConnectionStatus.TimedOut;
                request.Stage = "AgentUnavailable";
                request.Message = "The biometric agent did not respond in time. Confirm the Windows Service is running and can reach the ERP API.";
                request.CompletedOn = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                _logger.LogWarning(
                    "Test connection timed out waiting for agent. DeviceCode: {DeviceCode}, RequestId: {RequestId}",
                    request.Device?.DeviceCode, request.Id);
            }

            return MapTestResult(request, request.Device?.DeviceCode ?? "");
        }

        private static DeviceTestConnectionResultDto MapTestResult(BiometricDeviceTestRequest request, string deviceCode) => new()
        {
            RequestId = request.Id,
            DeviceId = request.DeviceId,
            DeviceCode = deviceCode,
            Status = (int)request.Status,
            StatusName = request.Status.ToString(),
            Stage = request.Stage,
            Message = request.Message,
            DeviceInfo = request.DeviceInfo,
            RequestedOn = request.RequestedOn,
            CompletedOn = request.CompletedOn,
            IsComplete = request.Status != TestConnectionStatus.Pending
        };

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

        /// <summary>Aggregate figures for the Device Health dashboard's summary cards - see BiometricDashboardSummaryDto.</summary>
        public async Task<BiometricDashboardSummaryDto> GetDashboardSummaryAsync()
        {
            try
            {
                var onlineCutoff = DateTime.UtcNow.AddMinutes(-15);
                var todayUtc = DateTime.UtcNow.Date;
                var last24h = DateTime.UtcNow.AddHours(-24);

                var devices = await _db.BiometricDevices
                    .Select(x => new { x.IsActive, x.LastSyncDate })
                    .ToListAsync();

                var lastSuccessfulSync = await _db.BiometricSyncLogs
                    .Where(x => x.Status == "Success")
                    .OrderByDescending(x => x.StartTime)
                    .Select(x => (DateTime?)x.StartTime)
                    .FirstOrDefaultAsync();

                return new BiometricDashboardSummaryDto
                {
                    TotalDevices = devices.Count,

                    OnlineDevices = devices.Count(x =>
                        x.IsActive && x.LastSyncDate != null && x.LastSyncDate >= onlineCutoff),

                    OfflineDevices = devices.Count(x =>
                        x.IsActive && (x.LastSyncDate == null || x.LastSyncDate < onlineCutoff)),

                    DisabledDevices = devices.Count(x => !x.IsActive),

                    TodaysPunchCount = await _db.BiometricAttendanceLogs
                        .CountAsync(x => x.PunchTime >= todayUtc && x.PunchTime < todayUtc.AddDays(1)),

                    PendingPunchCount = await _db.BiometricAttendanceLogs
                        .CountAsync(x => !x.IsProcessed),

                    FailedSyncCount24h = await _db.BiometricSyncLogs
                        .CountAsync(x => x.Status == "Failed" && x.StartTime >= last24h),

                    LastSuccessfulSync = lastSuccessfulSync
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load biometric dashboard summary.");
                return new BiometricDashboardSummaryDto();
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
                        CommKey = x.CommKey,
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
