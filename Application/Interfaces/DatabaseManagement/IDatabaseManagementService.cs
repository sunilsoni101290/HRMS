using System.Threading.Tasks;
using Application.DTOs.DatabaseManagement;
using Application.DTOs.Employee;

namespace Application.Interfaces.DatabaseManagement
{
    /// <summary>
    /// Phase A of the Database Management module - scaffold, Settings CRUD,
    /// a real read-only Database Info panel, and an Execution History
    /// reader (the table it reads is always empty until a later phase
    /// starts writing to it). Every method is System Configurator ONLY -
    /// see EnsurePermissionAsync's remarks - same permission-check
    /// convention as IErrorLogService.
    ///
    /// Deliberately does NOT expose anything for Backup/Execute/Restore/
    /// Swap yet (Phase B/C/D concern) - the APP-side controller/view build
    /// that UI as a visibly-disabled "coming soon" shell instead of calling
    /// into a fake/stub API method here.
    /// </summary>
    public interface IDatabaseManagementService
    {
        Task<DatabaseManagementSettingsDto> GetSettingsAsync(string tenantId, string actingUserId);

        Task<(bool Success, string Message)> SaveSettingsAsync(DatabaseManagementSettingsDto dto, string tenantId, string modifiedBy);

        /// <summary>Read-only, degrades to Available=false with a clear reason on any SQL/connectivity failure - never throws.</summary>
        Task<DatabaseInfoDto> GetDatabaseInfoAsync(string tenantId, string actingUserId);

        Task<PagedResult<DatabaseOperationHistoryDto>> GetOperationHistoryAsync(DatabaseOperationHistoryFilterDto filter, string tenantId, string actingUserId);

        /// <summary>Server name + database name only - never the password or raw connection string.</summary>
        Task<DatabaseConnectionInfoDto> GetConnectionInfoAsync(string tenantId, string actingUserId);
    }
}
