using Application.DTOs.Attendances;
using Application.Interfaces.Attendances;
using Domain.Entities;
using Infrastructure;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Domain.Enums;

namespace Application.Services.Attendances
{
    public class BiometricSyncService
    : IBiometricSyncService
    {
        private readonly ApplicationDbContext _db;
        private readonly HttpClient _http;

        public BiometricSyncService(
            ApplicationDbContext db,
            HttpClient http)
        {
            _db = db;
            _http = http;
        }

        public async Task<bool>SyncDeviceLogsAsync(string deviceId)
        {
            var device =
                await _db.BiometricDevices
                .FirstOrDefaultAsync(
                    x => x.Id == deviceId);

            if (device == null)
                return false;

            var logs =
                await _http.GetFromJsonAsync
                <List<BiometricAttendanceLogDto>>
                ($"{device.ApiUrl}/logs");

            if (logs == null)
                return false;

            foreach (var item in logs)
            {
                bool exists =
                    await _db
                    .BiometricAttendanceLogs
                    .AnyAsync(x =>
                        x.EmployeeCode ==
                        item.BiometricEmployeeCode &&
                        x.PunchTime ==
                        item.PunchTime);

                if (!exists)
                {
                    _db.BiometricAttendanceLogs.Add(
                        new BiometricAttendanceLog
                        {
                            DeviceId = item.DeviceId,
                            EmployeeCode =
                                item.BiometricEmployeeCode,
                            PunchTime =
                                item.PunchTime,
                            PunchType = item.PunchType,
                            IsProcessed = false
                        });
                }
            }

            await _db.SaveChangesAsync();

            return true;
        }

        public async Task<bool>
            SyncAllDevicesAsync()
        {
            var devices =
                await _db.BiometricDevices
                .Where(x => x.IsActive)
                .ToListAsync();

            foreach (var item in devices)
            {
                await SyncDeviceLogsAsync(item.Id);
            }

            return true;
        }

        public async Task<List
            <BiometricAttendanceLogDto>>
            GetRawLogsAsync(string deviceId)
        {
            return await _db
                .BiometricAttendanceLogs
                .Where(x => x.DeviceId == deviceId)
                .Select(x =>
                    new BiometricAttendanceLogDto
                    {
                        Id = x.Id,
                        DeviceId = x.DeviceId,
                        BiometricEmployeeCode =
                            x.EmployeeCode,
                        PunchTime = x.PunchTime,
                        PunchType = x.PunchType,
                        IsProcessed = x.IsProcessed
                    })
                .ToListAsync();
        }
    }
}
