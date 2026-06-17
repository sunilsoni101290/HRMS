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
    }
}
