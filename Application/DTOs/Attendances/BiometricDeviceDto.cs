using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Attendances
{
    public class BiometricDeviceDto
    {
        public string? Id { get; set; }

        public string TenantId { get; set; }

        public string CompanyId { get; set; }

        public string? BranchId { get; set; }

        public string DeviceName { get; set; }

        public string DeviceCode { get; set; }

        public string IPAddress { get; set; }

        public int Port { get; set; }

        public string? ApiUrl { get; set; }

        public string? Username { get; set; }

        public string? Password { get; set; }

        public string? SerialNumber { get; set; }

        public DateTime? LastSyncDate { get; set; }

        /// <summary>Last time the assigned BiometricAgent reported this device in its device list/heartbeat cycle.</summary>
        public DateTime? LastSeen { get; set; }

        /// <summary>
        /// Shared secret the on-site BiometricAgent sends when pushing punches
        /// to /api/BiometricSync/ingest. Auto-generated on create if left blank.
        /// </summary>
        public string? DeviceKey { get; set; }

        /// <summary>Selects the IDeviceDriver the agent uses ("Essl", "Mock", ...). See DeviceDriverFactory.</summary>
        public string DeviceType { get; set; } = "Essl";

        /// <summary>"TCP/IP" today; reserved for future Serial/USB drivers.</summary>
        public string CommunicationType { get; set; } = "TCP/IP";

        /// <summary>The BiometricAgent this device is assigned to. Null = not yet assigned to any agent.</summary>
        public string? AgentId { get; set; }

        /// <summary>Convenience field for list/detail screens - not persisted.</summary>
        public string? AgentName { get; set; }

        public bool IsActive { get; set; }

        /// <summary>
        /// Real connectivity signal - true when the on-site BiometricAgent
        /// has pushed a punch (updating LastSyncDate) within the last 15
        /// minutes. Distinct from IsActive (an admin on/off toggle): a
        /// device can be Active but Disconnected if the agent has stopped
        /// reporting. See BiometricDeviceService.TestConnectionAsync for why
        /// this can't be a direct ping from the API.
        /// </summary>
        public bool IsOnline { get; set; }

        public string CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }
    }
}
