using System;

namespace Application.DTOs.Attendances
{
    /// <summary>
    /// Result of (and status poll for) a real Test Connection attempt - see
    /// Domain.Entities.BiometricDeviceTestRequest for the full async
    /// request/response design. Returned by
    /// BiometricDeviceService.RequestTestConnectionAsync (immediately, with
    /// Status = Pending) and GetTestConnectionResultAsync (polled by the UI
    /// until Status is no longer Pending).
    /// </summary>
    public class DeviceTestConnectionResultDto
    {
        public string RequestId { get; set; }
        public string DeviceId { get; set; }
        public string DeviceCode { get; set; }

        /// <summary>Domain.Enums.EnumExtensions.TestConnectionStatus as an int, for simple JS comparison.</summary>
        public int Status { get; set; }

        /// <summary>"Pending" | "Success" | "Failed" | "TimedOut".</summary>
        public string StatusName { get; set; }

        /// <summary>"AgentUnavailable" | "Connecting" | "DeviceUnreachable" | "SdkConnectionFailed" | "Success". Null while still Pending.</summary>
        public string? Stage { get; set; }

        /// <summary>Safe, user-facing message - never a raw exception/stack trace.</summary>
        public string? Message { get; set; }

        /// <summary>Optional device serial/firmware string from a successful lightweight SDK probe. Absence does not imply failure.</summary>
        public string? DeviceInfo { get; set; }

        public DateTime RequestedOn { get; set; }
        public DateTime? CompletedOn { get; set; }

        /// <summary>Convenience for the UI - true once Status is Success/Failed/TimedOut (i.e. stop polling).</summary>
        public bool IsComplete { get; set; }
    }

    /// <summary>
    /// One pending test request handed to the agent via
    /// GET /api/BiometricAgent/pending-test-requests, carrying everything
    /// the agent needs to build a DeviceOptions and run the driver without
    /// a second round-trip. Mirrors BiometricAgent.Models.AgentTestRequest -
    /// keep the two in sync if either changes.
    /// </summary>
    public class AgentTestRequestDto
    {
        public string RequestId { get; set; }
        public string DeviceId { get; set; }
        public string DeviceCode { get; set; }
        public string DeviceKey { get; set; }
        public string DeviceType { get; set; }
        public string IPAddress { get; set; }
        public int Port { get; set; }
        public string CommunicationType { get; set; }
        public int CommKey { get; set; }
    }

    /// <summary>
    /// Body of POST /api/BiometricAgent/test-result - the agent's report
    /// back after actually attempting the ESSL SDK connection. Authenticated
    /// via AgentCode+AgentKey, same pattern as register/heartbeat.
    /// </summary>
    public class AgentTestResultSubmitDto
    {
        public string RequestId { get; set; }
        public string AgentCode { get; set; }
        public string AgentKey { get; set; }
        public bool Success { get; set; }

        /// <summary>"Connecting" | "DeviceUnreachable" | "SdkConnectionFailed" | "Success".</summary>
        public string Stage { get; set; }

        /// <summary>Safe, human-readable outcome - the agent must not send raw exception text/stack traces here.</summary>
        public string? Message { get; set; }

        public string? DeviceInfo { get; set; }
    }
}
