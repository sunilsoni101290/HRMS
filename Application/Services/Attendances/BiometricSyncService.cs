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
using Infrastructure.Data;
using Infrastructure.Interfaces;

namespace Application.Services.Attendances
{
    public class BiometricSyncService
    : IBiometricSyncService
    {
        private readonly ApplicationDbContext _db;
        private readonly HttpClient _http;
        private readonly ITenantService _tenantService;

        public BiometricSyncService(
            ApplicationDbContext db,
            HttpClient http,
            ITenantService tenantService)
        {
            _db = db;
            _http = http;
            _tenantService = tenantService;
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
            try
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
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<PunchIngestResultDto>
            IngestPunchesAsync(PunchIngestRequestDto request)
        {
            var result = new PunchIngestResultDto
            {
                ReceivedCount = request?.Punches?.Count ?? 0
            };

            if (request == null ||
                string.IsNullOrWhiteSpace(request.DeviceCode) ||
                string.IsNullOrWhiteSpace(request.DeviceKey))
            {
                result.Success = false;
                result.Message = "DeviceCode and DeviceKey are required.";
                return result;
            }

            // DeviceCode is only meant to be unique within a tenant, so scope by
            // the tenant resolved from the X-Tenant-ID header (TenantMiddleware)
            // to avoid one tenant's agent matching another tenant's device.
            var tenantId = _tenantService.GetTenantId();

            var device = await _db.BiometricDevices
                .FirstOrDefaultAsync(x =>
                    x.DeviceCode == request.DeviceCode &&
                    (string.IsNullOrEmpty(tenantId) || x.TenantId == tenantId));

            if (device == null || !device.IsActive)
            {
                result.Success = false;
                result.Message = "Unknown or inactive device.";
                return result;
            }

            // Constant-time-ish compare isn't critical here since this is a
            // shared secret over HTTPS, but avoid leaking timing on obvious mismatches.
            if (string.IsNullOrWhiteSpace(device.DeviceKey) ||
                device.DeviceKey != request.DeviceKey)
            {
                result.Success = false;
                result.Message = "Invalid device key.";
                return result;
            }

            foreach (var item in request.Punches ?? new List<PunchItemDto>())
            {
                if (string.IsNullOrWhiteSpace(item.EmployeeCode))
                    continue;

                bool exists = await _db.BiometricAttendanceLogs
                    .AnyAsync(x =>
                        x.DeviceId == device.Id &&
                        x.EmployeeCode == item.EmployeeCode &&
                        x.PunchTime == item.PunchTime);

                if (exists)
                {
                    result.DuplicateCount++;
                    continue;
                }

                _db.BiometricAttendanceLogs.Add(
                    new BiometricAttendanceLog
                    {
                        Id = IDManager.GetNewId(new BiometricAttendanceLog()),
                        DeviceId = device.Id,
                        EmployeeCode = item.EmployeeCode,
                        PunchTime = item.PunchTime,
                        PunchType = item.PunchType,
                        IsProcessed = false,
                        CreatedOn = DateTime.UtcNow,
                        CreatedBy = "BiometricAgent"
                    });

                result.InsertedCount++;
            }

            device.LastSyncDate = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            result.Success = true;
            result.Message = "Punches ingested.";

            return result;
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
