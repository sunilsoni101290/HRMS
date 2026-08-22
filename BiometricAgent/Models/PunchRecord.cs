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
        /// <summary>"TCP/IP" today - reserved for future Serial/USB drivers, not yet used to pick a driver.</summary>
        public string CommunicationType { get; set; } = "TCP/IP";
        /// <summary>Device SDK comm password (ZK-family "CommKey"), from the ERP - NOT hard-coded here. 0 = none.</summary>
        public int CommKey { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// One pending "please test this device right now" command, as returned
    /// by GET /api/BiometricAgent/pending-test-requests. Mirrors
    /// Application.DTOs.Attendances.AgentTestRequestDto - keep in sync.
    /// </summary>
    public class AgentTestRequest
    {
        public string RequestId { get; set; } = "";
        public string DeviceId { get; set; } = "";
        public string DeviceCode { get; set; } = "";
        public string DeviceKey { get; set; } = "";
        public string DeviceType { get; set; } = "Essl";
        public string IPAddress { get; set; } = "";
        public int Port { get; set; }
        public string CommunicationType { get; set; } = "TCP/IP";
        public int CommKey { get; set; }
    }

    /// <summary>
    /// Body posted to POST /api/BiometricAgent/test-result. Mirrors
    /// Application.DTOs.Attendances.AgentTestResultSubmitDto - keep in sync.
    /// Message/DeviceInfo must always be safe, pre-composed text - never a
    /// raw exception/stack trace (see Worker.ProcessTestRequestAsync).
    /// </summary>
    public class AgentTestResultSubmission
    {
        public string RequestId { get; set; } = "";
        public string AgentCode { get; set; } = "";
        public string AgentKey { get; set; } = "";
        public bool Success { get; set; }
        public string Stage { get; set; } = "";
        public string? Message { get; set; }
        public string? DeviceInfo { get; set; }
    }
}
