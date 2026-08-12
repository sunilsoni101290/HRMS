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

        Task<List<BiometricDeviceHealthDto>> GetHealthSummaryAsync();

        /// <summary>Single-device status snapshot for GET /api/BiometricDevice/{id}/status.</summary>
        Task<BiometricDeviceStatusDto?> GetStatusAsync(string id);

        /// <summary>
        /// Active devices assigned to the given agent, scoped to the
        /// tenant the agent belongs to. Used by GET /api/BiometricAgent/devices.
        /// </summary>
        Task<List<BiometricDeviceDto>> GetByAgentAsync(string agentId, string tenantId);
    }
}
