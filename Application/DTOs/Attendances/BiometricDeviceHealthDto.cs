using System;

namespace Application.DTOs.Attendances
{
    /// <summary>
    /// At-a-glance health summary for a biometric device, used by the
    /// "Device Health" dashboard so HR can spot a branch whose device/agent
    /// has stopped syncing without having to check the database directly.
    /// </summary>
    public class BiometricDeviceHealthDto
    {
        public string DeviceId { get; set; }
        public string DeviceName { get; set; }
        public string DeviceCode { get; set; }
        public string IPAddress { get; set; }
        public bool IsActive { get; set; }

        public DateTime? LastSyncDate { get; set; }

        /// <summary>
        /// True when the device is Active and has synced within the last 15
        /// minutes. A generous window since agents typically poll every
        /// 60 seconds by default - this just catches "stopped entirely",
        /// not brief network blips.
        /// </summary>
        public bool IsOnline { get; set; }

        public int TotalPunchCount { get; set; }

        /// <summary>Raw punches received but not yet turned into Attendance records.</summary>
        public int PendingPunchCount { get; set; }
    }
}
