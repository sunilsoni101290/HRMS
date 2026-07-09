using Application.DTOs.Attendances;
using Domain.Entities;
using Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Application.Interfaces.Attendances;
using Infrastructure.Data;


namespace Application.Services.Attendances
{
    public class BiometricDeviceService : IBiometricDeviceService
    {
        private readonly ApplicationDbContext _db;

        public BiometricDeviceService(
            ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<BiometricDeviceDto>>
            GetAllAsync()
        {
            try
            {
            return await _db.BiometricDevices
                .Select(x => new BiometricDeviceDto
                {
                    Id = x.Id,
                    TenantId = x.TenantId,
                    CompanyId = x.CompanyId,
                    BranchId = x.BranchId,
                    DeviceName = x.DeviceName,
                    DeviceCode = x.DeviceCode,
                    IPAddress = x.IPAddress,
                    Username = x.Username,
                    Password = x.Password,
                    Port = x.Port,
                    ApiUrl = x.ApiUrl,
                    SerialNumber = x.SerialNumber,
                    LastSyncDate = x.LastSyncDate,
                    DeviceKey = x.DeviceKey,
                    IsActive = x.IsActive
                }).ToListAsync();
            }
            catch (Exception)
            {
                return new List<BiometricDeviceDto>();
            }
        }

        public async Task<BiometricDeviceDto?>
            GetByIdAsync(string id)
        {
            try
            {
            return await _db.BiometricDevices
                .Where(x => x.Id == id)
                .Select(x => new BiometricDeviceDto
                {
                    Id = x.Id,
                    TenantId = x.TenantId,
                    CompanyId = x.CompanyId,
                    BranchId = x.BranchId,
                    DeviceName = x.DeviceName,
                    DeviceCode = x.DeviceCode,
                    IPAddress = x.IPAddress,
                    Username = x.Username,
                    Password = x.Password,
                    Port = x.Port,
                    ApiUrl = x.ApiUrl,
                    SerialNumber = x.SerialNumber,
                    LastSyncDate = x.LastSyncDate,
                    DeviceKey = x.DeviceKey,
                    IsActive = x.IsActive
                }).FirstOrDefaultAsync();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<BiometricDeviceDto>
            CreateAsync(BiometricDeviceDto dto)
        {
            try
            {
            var entity = new BiometricDevice
            {
                Id=IDManager.GetNewId(new BiometricDevice()),
                TenantId = dto.TenantId,
                CompanyId = dto.CompanyId,
                BranchId = dto.BranchId,
                DeviceName = dto.DeviceName,
                DeviceCode = dto.DeviceCode,
                IPAddress = dto.IPAddress,
                Port = dto.Port,
                ApiUrl = dto.ApiUrl,
                Username = dto.Username,
                Password = dto.Password,
                SerialNumber = dto.SerialNumber,
                // Auto-generate the agent auth secret if the caller didn't supply one.
                DeviceKey = string.IsNullOrWhiteSpace(dto.DeviceKey)
                    ? Guid.NewGuid().ToString("N")
                    : dto.DeviceKey,
                IsActive = dto.IsActive,
                CreatedBy = dto.CreatedBy,
                CreatedOn = DateTime.UtcNow,
            };

            _db.BiometricDevices.Add(entity);

            await _db.SaveChangesAsync();

            dto.Id = entity.Id;
            // Return the generated key so the caller can show/copy it once
            // (it's needed to configure the BiometricAgent at the client site).
            dto.DeviceKey = entity.DeviceKey;

            return dto;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool>
            UpdateAsync(BiometricDeviceDto dto)
        {
            try
            {
            var entity = await _db.BiometricDevices
                .FirstOrDefaultAsync(x => x.Id == dto.Id);

            if (entity == null)
                return false;

            entity.DeviceName = dto.DeviceName;
            entity.DeviceCode = dto.DeviceCode;
            entity.IPAddress = dto.IPAddress;
            entity.Port = dto.Port;
            entity.ApiUrl = dto.ApiUrl;
            entity.Username = dto.Username;
            entity.Password = dto.Password;
            entity.SerialNumber = dto.SerialNumber;
            // Only rotate the key when a new one is explicitly supplied, so a
            // plain "save" from the edit screen doesn't invalidate the agent's key.
            if (!string.IsNullOrWhiteSpace(dto.DeviceKey))
                entity.DeviceKey = dto.DeviceKey;
            entity.IsActive = dto.IsActive;
            entity.CreatedBy = dto.CreatedBy;
            entity.ModifiedBy = dto.ModifiedBy;
            entity.ModifiedOn = dto.ModifiedOn;

            await _db.SaveChangesAsync();

            return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool>
            DeleteAsync(string id)
        {
            try
            {
            var entity = await _db.BiometricDevices
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return false;

            _db.BiometricDevices.Remove(entity);

            await _db.SaveChangesAsync();

            return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        //public bool TestConnection(string ipAddress, int port)
        //{
        //    CZKEM device = new CZKEM();

        //    bool isConnected = device.Connect_Net(ipAddress, port);

        //    if (isConnected)
        //    {
        //        device.Disconnect();
        //        return true;
        //    }

        //    return false;
        //}

        public async Task<bool>
            TestConnectionAsync(string id)
        {
            try
            {
            var device = await _db.BiometricDevices
                .FirstOrDefaultAsync(x => x.Id == id);

            if (device == null)
                return false;

            try
            {
                using var ping =
                    new System.Net.NetworkInformation.Ping();

                var result =
                    await ping.SendPingAsync(
                        device.IPAddress);

                return result.Status ==
                       System.Net.NetworkInformation
                       .IPStatus.Success;
            }
            catch
            {
                return false;
            }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<List<BiometricDeviceHealthDto>> GetHealthSummaryAsync()
        {
            try
            {
                var onlineCutoff = DateTime.UtcNow.AddMinutes(-15);

                return await _db.BiometricDevices
                    .Select(x => new BiometricDeviceHealthDto
                    {
                        DeviceId = x.Id,
                        DeviceName = x.DeviceName,
                        DeviceCode = x.DeviceCode,
                        IPAddress = x.IPAddress,
                        IsActive = x.IsActive,
                        LastSyncDate = x.LastSyncDate,

                        IsOnline = x.IsActive &&
                            x.LastSyncDate != null &&
                            x.LastSyncDate >= onlineCutoff,

                        TotalPunchCount = _db.BiometricAttendanceLogs
                            .Count(l => l.DeviceId == x.Id),

                        PendingPunchCount = _db.BiometricAttendanceLogs
                            .Count(l => l.DeviceId == x.Id && !l.IsProcessed)
                    })
                    .ToListAsync();
            }
            catch (Exception)
            {
                return new List<BiometricDeviceHealthDto>();
            }
        }
    }
}
