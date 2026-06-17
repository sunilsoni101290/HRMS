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
                    IsActive = x.IsActive
                }).ToListAsync();
        }

        public async Task<BiometricDeviceDto?>
            GetByIdAsync(string id)
        {
            return await _db.BiometricDevices
                .Where(x => x.Id == id)
                .Select(x => new BiometricDeviceDto
                {
                    Id = x.Id,
                    DeviceName = x.DeviceName,
                    DeviceCode = x.DeviceCode,
                    IPAddress = x.IPAddress,
                    Port = x.Port
                }).FirstOrDefaultAsync();
        }

        public async Task<BiometricDeviceDto>
            CreateAsync(BiometricDeviceDto dto)
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
                IsActive = dto.IsActive,
                CreatedBy = dto.CreatedBy,
                CreatedOn = DateTime.UtcNow,
            };

            _db.BiometricDevices.Add(entity);

            await _db.SaveChangesAsync();

            dto.Id = entity.Id;

            return dto;
        }

        public async Task<bool>
            UpdateAsync(BiometricDeviceDto dto)
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
            entity.IsActive = dto.IsActive;
            entity.CreatedBy = dto.CreatedBy;
            entity.ModifiedBy = dto.ModifiedBy;
            entity.ModifiedOn = dto.ModifiedOn;

            await _db.SaveChangesAsync();

            return true;
        }

        public async Task<bool>
            DeleteAsync(string id)
        {
            var entity = await _db.BiometricDevices
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return false;

            _db.BiometricDevices.Remove(entity);

            await _db.SaveChangesAsync();

            return true;
        }

        public async Task<bool>
            TestConnectionAsync(string id)
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
    }
}
