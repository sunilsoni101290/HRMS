using Application.DTOs.Attendances;

namespace Application.Interfaces.Attendances
{
    /// <summary>
    /// Development-only. Feeds hand-crafted punches through the exact same
    /// IBiometricSyncService.IngestPunchesAsync + IAttendanceProcessorService
    /// pipeline a real on-site BiometricAgent uses, so the eSSL machine is not
    /// required to exercise the full biometric-to-attendance workflow. Never
    /// call this from anything reachable in a production environment - see
    /// BiometricSimulatorController's environment gate.
    /// </summary>
    public interface IBiometricSimulatorService
    {
        Task<List<SimulatorMappedEmployeeDto>> GetMappedEmployeesAsync(string tenantId);

        Task<SimulatorResultDto> SimulatePunchAsync(SimulatePunchRequestDto request, string tenantId);

        Task<SimulatorResultDto> GenerateFullDayAsync(GenerateFullDayRequestDto request, string tenantId);

        Task<SimulatorResultDto> RunScenarioAsync(RunScenarioRequestDto request, string tenantId);

        Task<SimulatorResultDto> SimulateApiFailureAsync(string deviceId, string tenantId);

        Task<SimulatorResultDto> SimulateDeviceOfflineAsync(string deviceId, string tenantId);

        Task<int> ClearTestDataAsync(ClearTestDataRequestDto request, string tenantId);
    }
}
