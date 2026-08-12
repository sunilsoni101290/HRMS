namespace BiometricAgent.Models
{
    public enum PunchType
    {
        In = 1,
        Out = 2,
        BreakIn = 3,
        BreakOut = 4
    }

    /// <summary>
    /// One raw punch read off the device. Mirrors Application.DTOs.Attendances.PunchItemDto
    /// on the API side - keep the two in sync if either changes.
    /// </summary>
    public class PunchRecord
    {
        public string EmployeeCode { get; set; } = "";
        public DateTime PunchTime { get; set; }
        public PunchType PunchType { get; set; } = PunchType.In;

        /// <summary>The device's own unique transaction/log id for this punch, if the driver exposes one. Preferred idempotency key server-side when present.</summary>
        public string? DeviceTransactionId { get; set; }
    }

    /// <summary>Server-provided config for one assigned device, as returned by GET /api/BiometricAgent/devices. Mirrors Application.DTOs.Attendances.BiometricDeviceDto's agent-relevant fields.</summary>
    public class AgentDeviceInfo
    {
        public string Id { get; set; } = "";
        public string DeviceCode { get; set; } = "";
        public string DeviceName { get; set; } = "";
        public string DeviceKey { get; set; } = "";
        public string DeviceType { get; set; } = "Essl";
        public string IPAddress { get; set; } = "";
        public int Port { get; set; }
        public bool IsActive { get; set; }
    }
}
