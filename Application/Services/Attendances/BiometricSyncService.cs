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
                            Id = IDManager.GetNewId(new BiometricAttendanceLog()),
                            TenantId = device.TenantId,
                            DeviceId = item.DeviceId,
                            EmployeeCode =
                                item.BiometricEmployeeCode,
                            PunchTime =
                                item.PunchTime,
                            PunchType = item.PunchType,
                            IsProcessed = false,
                            CreatedOn = DateTime.UtcNow,
                            CreatedBy = "BiometricSync"
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

            // Defense in depth: if the request identifies the pushing agent,
            // confirm this device is actually assigned to it. Legacy/older
            // agents that don't send AgentCode are still accepted on
            // DeviceKey alone (see PunchIngestRequestDto.AgentCode).
            BiometricAgent? agent = null;

            if (!string.IsNullOrWhiteSpace(request.AgentCode))
            {
                agent = await _db.BiometricAgents.FirstOrDefaultAsync(a =>
                    a.AgentCode == request.AgentCode && a.TenantId == device.TenantId);

                if (agent == null || !agent.IsActive)
                {
                    result.Success = false;
                    result.Message = "Unknown or inactive agent.";
                    return result;
                }

                if (string.IsNullOrEmpty(device.AgentId) || device.AgentId != agent.Id)
                {
                    result.Success = false;
                    result.Message = "This device is not assigned to the requesting agent.";
                    return result;
                }
            }

            foreach (var item in request.Punches ?? new List<PunchItemDto>())
            {
                if (string.IsNullOrWhiteSpace(item.EmployeeCode))
                    continue;

                // Prefer the device's own transaction id for idempotency when
                // supplied - it survives even if two genuine punches land in
                // the same second. Falls back to (Device, Employee, PunchTime).
                bool exists = !string.IsNullOrWhiteSpace(item.DeviceTransactionId)
                    ? await _db.BiometricAttendanceLogs.AnyAsync(x =>
                        x.DeviceId == device.Id &&
                        x.DeviceTransactionId == item.DeviceTransactionId)
                    : await _db.BiometricAttendanceLogs.AnyAsync(x =>
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
                        TenantId = device.TenantId,
                        DeviceId = device.Id,
                        EmployeeCode = item.EmployeeCode,
                        PunchTime = item.PunchTime,
                        PunchType = item.PunchType,
                        DeviceTransactionId = item.DeviceTransactionId,
                        IsProcessed = false,
                        CreatedOn = DateTime.UtcNow,
                        CreatedBy = "BiometricAgent"
                    });

                result.InsertedCount++;
            }

            device.LastSyncDate = DateTime.UtcNow;
            device.LastSeen = DateTime.UtcNow;

            if (agent != null)
                agent.LastHeartbeat = DateTime.UtcNow;

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // The unique indexes on BiometricAttendanceLogs are the final
                // backstop against a race between two concurrent ingest calls
                // for the same device (e.g. agent retry overlapping with a
                // manual Sync Now) slipping past the AnyAsync checks above.
                result.Success = false;
                result.Message = "One or more punches were rejected as duplicates by the database.";
                result.DuplicateCount += result.InsertedCount;
                result.InsertedCount = 0;
                return result;
            }

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
