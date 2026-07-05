using Application.DTOs.Attendances;
using Application.Interfaces.Attendances;
using Domain.Entities;
using Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;


namespace Application.Services.Attendances
{
    public class BiometricService : IBiometricDeviceService
    {
        private readonly ApplicationDbContext _db;
        private readonly HttpClient _http;

        public BiometricService(ApplicationDbContext db,
                                HttpClient http)
        {
            _db = db;
            _http = http;
        }

        public async Task<IEnumerable<BiometricDeviceDto>> GetDevices()
        {
            try
            {
            return await _db.BiometricDevices
                .Select(x => new BiometricDeviceDto
                {
                    Id = x.Id,
                    DeviceName = x.DeviceName,
                    DeviceCode = x.DeviceCode,
                    IPAddress = x.IPAddress,
                    Port = x.Port,
                    ApiUrl = x.ApiUrl,
                    IsActive = x.IsActive
                }).ToListAsync();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<BiometricDeviceDto> AddDevice(BiometricDeviceDto dto)
        {
            try
            {
            var entity = new BiometricDevice
            {
                DeviceName = dto.DeviceName,
                DeviceCode = dto.DeviceCode,
                IPAddress = dto.IPAddress,
                Port = dto.Port,
                ApiUrl = dto.ApiUrl,
                Username = dto.Username,
                Password = dto.Password,
                IsActive = dto.IsActive
            };

            _db.BiometricDevices.Add(entity);

            await _db.SaveChangesAsync();

            dto.Id = entity.Id;

            return dto;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<List<BiometricAttendanceLogDto>> FetchAttendanceLogs(string deviceId)
        {
            try
            {
            var device = await _db.BiometricDevices
                .FirstOrDefaultAsync(x => x.Id == deviceId);

            if (device == null)
                return new List<BiometricAttendanceLogDto>();

            var response = await _http.GetAsync(
                $"{device.ApiUrl}/attendance/logs");

            if (!response.IsSuccessStatusCode)
                return new List<BiometricAttendanceLogDto>();

            var result = await response.Content
                .ReadFromJsonAsync<List<BiometricAttendanceLogDto>>();

            return result ?? new List<BiometricAttendanceLogDto>();
            }
            catch (Exception)
            {
                return new List<BiometricAttendanceLogDto>();
            }
        }

        public async Task<bool> SyncAttendance(string deviceId)
        {
            try
            {
            var logs = await FetchAttendanceLogs(deviceId);

            foreach (var item in logs)
            {
                bool exists = await _db.BiometricAttendanceLogs
                    .AnyAsync(x =>
                        x.EmployeeCode == item.EmployeeCode &&
                        x.PunchTime == item.PunchTime);

                if (!exists)
                {
                    _db.BiometricAttendanceLogs.Add(
                        new BiometricAttendanceLog
                        {
                            EmployeeCode = item.EmployeeCode,
                            PunchTime = item.PunchTime,
                            PunchType = item.PunchType,
                            DeviceId = item.DeviceId,
                            IsProcessed = false
                        });
                }
            }

            await _db.SaveChangesAsync();

            return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<BiometricDeviceDto> GetDeviceById(string id)
        {
            try
            {
            return await _db.BiometricDevices
                .Where(x => x.Id == id)
                .Select(x => new BiometricDeviceDto
                {
                    Id = x.Id,
                    DeviceName = x.DeviceName,
                    DeviceCode = x.DeviceCode,
                    IPAddress = x.IPAddress,
                    Port = x.Port,
                    ApiUrl = x.ApiUrl,
                    Username = x.Username,
                    Password = x.Password,
                    IsActive = x.IsActive
                }).FirstOrDefaultAsync();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool> UpdateDevice(BiometricDeviceDto dto)
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
            entity.IsActive = dto.IsActive;

            await _db.SaveChangesAsync();

            return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> DeleteDevice(string id)
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
    }
}
