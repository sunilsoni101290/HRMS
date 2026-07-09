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

        public List<PunchItemDto> Punches { get; set; } = new();
    }

    public class PunchItemDto
    {
        /// <summary>Employee code/enrollment ID as stored on the biometric device.</summary>
        public string EmployeeCode { get; set; }

        public DateTime PunchTime { get; set; }

        public PunchType PunchType { get; set; }
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
