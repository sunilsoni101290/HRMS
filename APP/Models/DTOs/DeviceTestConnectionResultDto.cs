using System;

namespace APP.Models.DTOs
{
    /// <summary>Mirrors Application.DTOs.Attendances.DeviceTestConnectionResultDto - the shape returned by the API's test-connection request/status endpoints.</summary>
    public class DeviceTestConnectionResultDto
    {
        public string RequestId { get; set; }
        public string DeviceId { get; set; }
        public string DeviceCode { get; set; }
        public int Status { get; set; }
        public string StatusName { get; set; }
        public string? Stage { get; set; }
        public string? Message { get; set; }
        public string? DeviceInfo { get; set; }
        public DateTime RequestedOn { get; set; }
        public DateTime? CompletedOn { get; set; }
        public bool IsComplete { get; set; }
    }
}
