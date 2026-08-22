using System;
using System.Collections.Generic;
using static Domain.Enums.EnumExtensions;

namespace Application.DTOs.Attendances
{
    /// <summary>
    /// Payload the on-site BiometricAgent posts to
    /// POST /api/BiometricSync/ingest. The agent talks to the physical
    /// device on the client's LAN and forwards whatever punches it collected
    /// since the last successful push.
    /// </summary>
    public class PunchIngestRequestDto
    {
        [System.ComponentModel.DataAnnotations.Required]
        public string DeviceCode { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        public string DeviceKey { get; set; }

        /// <summary>
        /// Optional - identifies which agent is pushing, so the server can
        /// confirm the device is actually assigned to this agent (defense in
        /// depth on top of the DeviceKey check). Older/legacy agents that
        /// don't send it are still accepted via DeviceKey alone.
        /// </summary>
        public string? AgentCode { get; set; }

        public List<PunchItemDto> Punches { get; set; } = new();
    }

    public class PunchItemDto
    {
        /// <summary>Employee code/enrollment ID as stored on the biometric device.</summary>
        public string EmployeeCode { get; set; }

        public DateTime PunchTime { get; set; }

        public PunchType PunchType { get; set; }

        /// <summary>The device's own unique transaction/log id for this punch, if the driver exposes one. See BiometricAttendanceLog.DeviceTransactionId.</summary>
        public string? DeviceTransactionId { get; set; }

        /// <summary>How the punch was captured - "Fingerprint", "Face", "Card", "Password", "Other". Optional/informational.</summary>
        public string? VerifyMode { get; set; }
    }

    public class PunchIngestResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int ReceivedCount { get; set; }
        public int InsertedCount { get; set; }
        public int DuplicateCount { get; set; }
    }
}
