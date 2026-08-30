using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs.Attendances;
using Application.DTOs.Employee;

namespace Application.Interfaces.Attendances
{
    /// <summary>
    /// The one core sync engine for the eSSL eTimeTrackLite1 direct-SQL
    /// integration - used by BOTH the automatic background service and the
    /// manual/historical "Sync Now" admin action (requirement: "Manual
    /// synchronization must use the same core service as automatic
    /// synchronization. Do not duplicate synchronization logic.").
    /// </summary>
    public interface IEsslAttendanceSyncService
    {
        /// <summary>
        /// Runs one sync pass. request.FromDate/ToDate both null = automatic
        /// incremental mode (uses the persisted EsslAttendanceSyncState
        /// cursor). Either set = manual/historical mode over that explicit
        /// window. Safe to call concurrently - internally serialized per
        /// tenant via EsslAttendanceSyncState.IsSyncRunning (requirement
        /// #12's "appropriate locking").
        /// </summary>
        Task<EsslSyncResultDto> SyncAsync(
            EsslSyncRequestDto request,
            string tenantId,
            string triggeredBy,
            CancellationToken ct = default);

        /// <summary>Card 1 "Integration Status" - runtime/status fields only, never configuration.</summary>
        Task<EsslSyncSettingsDto> GetSettingsAsync(string tenantId);

        /// <summary>Card 2 "Database Configuration" GET - never includes the password, only HasPasswordConfigured.</summary>
        Task<EsslDatabaseConfigViewDto> GetConfigurationAsync(string tenantId);

        /// <summary>
        /// Validates and persists the Database Configuration form. Blank/null
        /// dto.Password keeps the existing saved password unchanged. Does
        /// NOT test the connection itself and does NOT start a sync - the
        /// UI is expected to call TestConnectionAsync separately if desired.
        /// </summary>
        Task<(bool Success, string Message)> SaveConfigurationAsync(EsslDatabaseConfigDto dto, string tenantId, string modifiedBy);

        /// <summary>
        /// Tests the exact values currently in the form (requirement #6) -
        /// never the saved configuration. Blank/null dto.Password falls back
        /// to the tenant's already-saved password (if any), so a user
        /// testing connectivity right after opening the page (without
        /// retyping a password they already saved) still gets a meaningful
        /// result. Never saves anything, never starts a sync.
        /// </summary>
        Task<(bool Success, string Message)> TestConnectionAsync(EsslDatabaseConfigDto dto, string tenantId);

        Task<PagedResult<EsslSyncHistoryDto>> GetSyncHistoryAsync(EsslSyncHistoryFilterDto filter, string tenantId);

        Task<List<EsslUnmappedEmployeeDto>> GetUnmappedEmployeesAsync(string tenantId);
    }
}
