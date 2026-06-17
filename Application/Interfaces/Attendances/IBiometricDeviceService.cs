using Application.DTOs.Attendances;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces.Attendances
{
    public interface IBiometricDeviceService
    {
        Task<List<BiometricDeviceDto>> GetAllAsync();
        Task<BiometricDeviceDto?> GetByIdAsync(string id);
        Task<BiometricDeviceDto> CreateAsync(BiometricDeviceDto dto);
        Task<bool> UpdateAsync(BiometricDeviceDto dto);
        Task<bool> DeleteAsync(string id);
        Task<bool> TestConnectionAsync(string id);
    }
}
