using Application.DTOs.Attendances;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces.Attendances
{
    public interface IBiometricSyncService
    {
        Task<bool> SyncDeviceLogsAsync(string deviceId);
        Task<bool> SyncAllDevicesAsync();
        Task<List<BiometricAttendanceLogDto>>GetRawLogsAsync(string deviceId);

        /// <summary>
        /// Accepts a batch of punches pushed by the on-site BiometricAgent,
        /// after validating the device code + device key. This is the path
        /// real deployments use, since the central API normally cannot reach
        /// a device sitting on the client's private LAN directly.
        /// </summary>
        Task<PunchIngestResultDto> IngestPunchesAsync(PunchIngestRequestDto request);
    }
}
