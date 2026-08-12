using System;

namespace Application.DTOs.Attendances
{
    /// <summary>
    /// Response of GET /api/BiometricDevice/{id}/status - the operational
    /// snapshot the MVC "Device Health"/Details screens poll, combining
    /// connection state, sync recency and assigned-agent liveness in one call.
    /// </summary>
    public class BiometricDeviceStatusDto
    {
        public string DeviceId { get; set; }
        public string DeviceCode { get; set; }
        public string DeviceName { get; set; }
        public bool IsActive { get; set; }

        /// <summary>True when Active and LastSyncDate is within the online window.</summary>
        public bool IsOnline { get; set; }

        public DateTime? LastSyncDate { get; set; }
        public DateTime? LastSeen { get; set; }

        public string? AgentId { get; set; }
        public string? AgentCode { get; set; }
        public string? AgentName { get; set; }

        /// <summary>True when the assigned agent's own LastHeartbeat is recent. Null when no agent is assigned.</summary>
        public bool? AgentOnline { get; set; }
        public DateTime? AgentLastHeartbeat { get; set; }

        public int TotalPunchCount { get; set; }
        public int PendingPunchCount { get; set; }

        /// <summary>Best-effort human-readable reason the device is offline, for operator triage.</summary>
        public string? LastError { get; set; }
    }
}
