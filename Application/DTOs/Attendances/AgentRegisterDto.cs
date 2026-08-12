using System;
using System.Collections.Generic;

namespace Application.DTOs.Attendances
{
    /// <summary>
    /// Body of POST /api/BiometricAgent/register - the Windows Service's
    /// first contact with the API on startup, and periodically thereafter as
    /// a "am I still known/active" check. The BiometricAgent row itself is
    /// provisioned up-front by an admin from the MVC (same pattern as
    /// BiometricDevice), so Register never creates a new agent - it only
    /// authenticates AgentCode+AgentKey and refreshes machine/version info.
    /// </summary>
    public class AgentRegisterRequestDto
    {
        [System.ComponentModel.DataAnnotations.Required]
        public string AgentCode { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        public string AgentKey { get; set; }

        public string? MachineName { get; set; }

        public string? AgentVersion { get; set; }
    }

    public class AgentRegisterResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string? AgentId { get; set; }
        public string? AgentCode { get; set; }
        public string? AgentName { get; set; }
    }

    /// <summary>Body of POST /api/BiometricAgent/heartbeat - lightweight liveness ping sent every poll cycle.</summary>
    public class AgentHeartbeatRequestDto
    {
        [System.ComponentModel.DataAnnotations.Required]
        public string AgentCode { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        public string AgentKey { get; set; }

        public string? AgentVersion { get; set; }

        /// <summary>
        /// Optional last-cycle diagnostics so operators can see "last error"
        /// per agent from the MVC without needing server-side log access.
        /// </summary>
        public string? LastError { get; set; }

        public int? QueuedRecordCount { get; set; }
    }

    /// <summary>Response of GET /api/BiometricAgent/config - agent-level (not per-device) operating parameters.</summary>
    public class AgentConfigResponseDto
    {
        public string AgentId { get; set; }
        public string AgentCode { get; set; }
        public string AgentName { get; set; }
        public string TenantId { get; set; }
        public bool IsActive { get; set; }
    }
}
