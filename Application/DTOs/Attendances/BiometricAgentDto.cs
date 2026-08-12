using System;

namespace Application.DTOs.Attendances
{
    /// <summary>
    /// CRUD/admin-facing view of a BiometricAgent, used by the MVC
    /// Agent management screens (create/edit/list) via the API.
    /// </summary>
    public class BiometricAgentDto
    {
        public string? Id { get; set; }

        public string TenantId { get; set; }

        public string AgentCode { get; set; }

        public string AgentName { get; set; }

        /// <summary>
        /// Shared secret the Windows Service uses to authenticate
        /// register/heartbeat/devices calls. Auto-generated on create if
        /// left blank. Only meaningful to display right after Create (same
        /// pattern as BiometricDeviceDto.DeviceKey).
        /// </summary>
        public string? AgentKey { get; set; }

        public string? MachineName { get; set; }

        public string? AgentVersion { get; set; }

        public bool IsActive { get; set; }

        public DateTime? LastHeartbeat { get; set; }

        /// <summary>
        /// True when LastHeartbeat is recent (within the configured
        /// heartbeat window). Same "real connectivity" pattern as
        /// BiometricDeviceDto.IsOnline.
        /// </summary>
        public bool IsOnline { get; set; }

        /// <summary>Count of BiometricDevices currently assigned to this agent. Convenience field for list screens - not persisted.</summary>
        public int DeviceCount { get; set; }

        public string CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }
    }
}
