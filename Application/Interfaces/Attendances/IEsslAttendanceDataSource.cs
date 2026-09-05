using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Infrastructure.EsslIntegration;

namespace Application.Interfaces.Attendances
{
    /// <summary>
    /// Ad-hoc connection parameters for testing a set of values BEFORE they
    /// are saved (requirement: "Test Connection must read the values
    /// currently entered in the form... do NOT save the settings"). Deliberately
    /// separate from EsslDatabaseConfigDto (the Application.DTOs.Attendances
    /// full save/view DTO) so this Infrastructure-facing interface doesn't
    /// need to reference DataAnnotations-decorated request DTOs - just the
    /// bare values needed to build a connection string.
    /// </summary>
    public class EsslConnectionParameters
    {
        public string DatabaseServer { get; set; } = "";
        public string DatabaseName { get; set; } = "";

        /// <summary>"Sql" or "Windows" - see Domain.Helper.EsslAuthenticationTypes.</summary>
        public string AuthenticationType { get; set; } = "Sql";

        public string? Username { get; set; }

        /// <summary>Plain text here is fine - this object is never persisted, only used to build an in-memory SqlConnectionStringBuilder for one test attempt.</summary>
        public string? Password { get; set; }

        public int ConnectionTimeout { get; set; } = 15;
    }

    /// <summary>
    /// Abstraction over the eTimeTrackLite1 SQL Server database (requirement
    /// #11 - "Prefer a dedicated IEsslAttendanceDataSource / EsslAttendanceDataSource").
    /// Every method here is a plain SELECT - this interface has no
    /// Insert/Update/Delete member on purpose, so a caller can never
    /// accidentally write to the vendor database through this abstraction.
    ///
    /// Every real query is tenant-scoped (tenantId) - the connection is now
    /// built from that tenant's persisted EsslIntegrationSetting row (falling
    /// back to the legacy "EsslDatabase:*" appsettings.json section only if
    /// no row has been saved yet for that tenant), NOT a single fixed
    /// connection string resolved once at app startup.
    /// </summary>
    public interface IEsslAttendanceDataSource
    {
        /// <summary>Whether the integration is enabled for this tenant (persisted EsslIntegrationSetting.IntegrationEnabled, falling back to EsslDatabase:Enabled in appsettings.json if no row exists yet).</summary>
        Task<bool> IsEnabledAsync(string tenantId, CancellationToken ct = default);

        /// <summary>
        /// Tests the CURRENTLY SAVED connection settings for this tenant -
        /// used only where testing the persisted configuration (rather than
        /// unsaved form values) is actually what's wanted. The Settings
        /// page's "Test Connection" button uses the other overload instead.
        /// </summary>
        Task<(bool Success, string Message)> TestConnectionAsync(string tenantId, CancellationToken ct = default);

        /// <summary>
        /// Tests an AD-HOC set of connection parameters - never persisted,
        /// never used to start a sync. Backs the Settings page's "Test
        /// Connection" button, which must validate exactly what's currently
        /// typed into the form (requirement #6).
        /// </summary>
        Task<(bool Success, string Message)> TestConnectionAsync(EsslConnectionParameters parameters, CancellationToken ct = default);

        /// <summary>
        /// Safely discovers which physical DeviceLogs tables actually exist
        /// right now for this tenant's configured database, restricted to
        /// names matching EsslDeviceLogTableName's whitelist (the bare
        /// "DeviceLogs" table, plus any "DeviceLogs_M_YYYY" monthly partition
        /// whose calendar month overlaps [fromDateInclusive, toDateExclusive)).
        /// Queries sys.tables/sys.schemas only - never touches row data, never
        /// creates/alters anything. Returns an empty list (never throws) if
        /// eSSL is disabled/unreachable, so a discovery failure degrades to
        /// "nothing to read this run" rather than crashing the sync.
        /// </summary>
        Task<List<string>> DiscoverDeviceLogTablesAsync(
            string tenantId,
            DateTime fromDateInclusive,
            DateTime toDateExclusive,
            CancellationToken ct = default);

        /// <summary>
        /// Reads one batch of raw punches strictly after (LogDate >=
        /// fromDateInclusive) and (LogDate &lt; toDateExclusive), UNIONed
        /// across every table in sourceTables (each of which MUST already be
        /// EsslDeviceLogTableName-valid - see DiscoverDeviceLogTablesAsync),
        /// ordered by (LogDate, SourceTable, DeviceLogId) ascending - the
        /// composite keyset that stays correct even though DeviceLogId alone
        /// can collide across two different physical tables (requirement
        /// #27's "ORDER BY on an indexed/monotonic column" plus requirement
        /// #9's "process punches chronologically"). afterCursor (null on a
        /// run's first call) skips rows already returned earlier in THIS run
        /// so a resumed/overlapping window doesn't re-walk rows the caller
        /// already has in hand (cross-run duplicate prevention is the DB
        /// unique index on BiometricAttendanceLog, not this parameter).
        /// batchSize caps how many rows come back - never "SELECT * FROM
        /// DeviceLogs" (requirement #26).
        /// </summary>
        Task<List<EsslDeviceLogRaw>> GetDeviceLogsAsync(
            string tenantId,
            IReadOnlyList<string> sourceTables,
            DateTime fromDateInclusive,
            DateTime toDateExclusive,
            EsslDeviceLogCursor? afterCursor,
            int batchSize,
            CancellationToken ct = default);

        /// <summary>
        /// Best-effort lookup of eTimeTrackLite1's own employee name/status
        /// for a batch of device-side UserId/EmployeeCodeInDevice values -
        /// used only to make the "Unmapped Employees" admin report
        /// (requirement #21) readable (show a real name, not just a raw
        /// code). Returns an empty dictionary (never throws) if eSSL is
        /// disabled/unreachable - the unmapped-employee report still works,
        /// it just falls back to showing the raw code.
        /// </summary>
        Task<Dictionary<string, EsslEmployeeRaw>> GetEmployeeNamesAsync(
            string tenantId,
            IEnumerable<string> deviceUserIds,
            CancellationToken ct = default);
    }
}
