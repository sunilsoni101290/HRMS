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

        /// <summary>
        /// Kicks off a REAL Test Connection: validates the device/agent are
        /// usable, creates a Pending BiometricDeviceTestRequest, and returns
        /// immediately (Status = Pending). The assigned agent picks the
        /// request up on its next poll cycle and actually attempts the ESSL
        /// SDK connection - see GetTestConnectionResultAsync for polling the
        /// outcome. Throws NotFoundException / BadRequestException for
        /// pre-flight failures (device missing, inactive, no agent assigned,
        /// agent offline) so the controller can map each to a precise message.
        /// </summary>
        Task<DeviceTestConnectionResultDto> RequestTestConnectionAsync(string deviceId, string tenantId, string requestedBy);

        /// <summary>Polled by the UI after RequestTestConnectionAsync until IsComplete is true. Returns null if the request doesn't exist (wrong id/tenant).</summary>
        Task<DeviceTestConnectionResultDto?> GetTestConnectionResultAsync(string requestId, string tenantId);

        Task<List<BiometricDeviceHealthDto>> GetHealthSummaryAsync();

        Task<BiometricDashboardSummaryDto> GetDashboardSummaryAsync();

        /// <summary>Single-device status snapshot for GET /api/BiometricDevice/{id}/status.</summary>
        Task<BiometricDeviceStatusDto?> GetStatusAsync(string id);

        /// <summary>
        /// Active devices assigned to the given agent, scoped to the
        /// tenant the agent belongs to. Used by GET /api/BiometricAgent/devices.
        /// </summary>
        Task<List<BiometricDeviceDto>> GetByAgentAsync(string agentId, string tenantId);
    }
}
