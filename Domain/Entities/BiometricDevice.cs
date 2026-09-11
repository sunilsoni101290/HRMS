using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    public class BiometricDevice : BaseEntity
    {
        [Required]
        public string TenantId { get; set; }

        public virtual Tenant Tenant { get; set; }

        [Required]
        public string CompanyId { get; set; }

        public virtual Company Company { get; set; }

        public string? BranchId { get; set; }

        public virtual Branch Branch { get; set; }

        [Required]
        [MaxLength(100)]
        public string DeviceName { get; set; }

        [Required]
        [MaxLength(50)]
        public string DeviceCode { get; set; }

        [Required]
        public string IPAddress { get; set; }

        public int Port { get; set; }

        public string? ApiUrl { get; set; }

        public string? Username { get; set; }

        public string? Password { get; set; }

        public string? SerialNumber { get; set; }

        /// <summary>
        /// Timestamp of the last successful punch batch pushed by the
        /// BiometricAgent for this device (LastSyncTime). Updated by
        /// BiometricSyncService.IngestPunchesAsync on every accepted push.
        /// </summary>
        public DateTime? LastSyncDate { get; set; }

        /// <summary>
        /// Timestamp of the last time the assigned BiometricAgent reported
        /// this device in its device list / heartbeat cycle, regardless of
        /// whether any new punches existed. Distinct from LastSyncDate: a
        /// device can be "seen" by the agent (reachable, agent alive) with
        /// zero new punches to sync. Used for a more granular online signal
        /// than LastSyncDate alone.
        /// </summary>
        public DateTime? LastSeen { get; set; }

        /// <summary>
        /// Shared secret used by the on-site BiometricAgent (running on a machine
        /// at the client's location) to authenticate punch pushes to
        /// POST /api/BiometricSync/ingest. Not the same as Username/Password,
        /// which are for the device's own admin login.
        /// </summary>
        [MaxLength(100)]
        public string? DeviceKey { get; set; }

        /// <summary>
        /// Selects which IDeviceDriver the BiometricAgent uses to talk to this
        /// device (e.g. "Essl", "Mock", and later "ZKTeco"). Kept as a plain
        /// string (not an enum) so new drivers can be added without a schema
        /// change - see BiometricAgent.Drivers.DeviceDriverFactory.
        /// </summary>
        [MaxLength(30)]
        public string DeviceType { get; set; } = "Essl";

        /// <summary>
        /// How the agent physically reaches the device - "TCP/IP" today;
        /// reserved for "Serial"/"USB" if a future driver needs it.
        /// </summary>
        [MaxLength(30)]
        public string CommunicationType { get; set; } = "TCP/IP";

        /// <summary>
        /// The device's comm password/key (ZK-family "CommKey", set via
        /// SetCommPassword before Connect_Net if non-zero) - a device-level
        /// SDK setting, separate from DeviceKey (the agent's own auth secret
        /// to this API) and from Username/Password (the device's admin
        /// login). 0 (the default) means no comm password is set on the
        /// device. Only meaningful for drivers that use it - see
        /// BiometricAgent.Drivers.EsslDeviceDriver.ConnectAsync.
        /// </summary>
        public int CommKey { get; set; } = 0;

        /// <summary>
        /// The BiometricAgent (Windows Service) this device is assigned to.
        /// Null means the device is configured but not yet picked up by any
        /// agent. A device is only ever polled by the agent it is assigned to.
        /// </summary>
        public string? AgentId { get; set; }

        public virtual BiometricAgent? Agent { get; set; }

        public override string GetSequencePrefix()
            => "BDV";
    }

    public class BiometricAttendanceLog : BaseEntity
    {
        public string EmployeeCode { get; set; }

        public DateTime PunchTime { get; set; }

        public PunchType PunchType { get; set; }

        public string DeviceId { get; set; }

        /// <summary>
        /// The biometric device's own unique transaction/log id for this
        /// punch, when the driver/SDK exposes one. Preferred idempotency key
        /// over (DeviceId, EmployeeCode, PunchTime) when available, since
        /// device clocks can occasionally produce two genuinely different
        /// punches with the same second-level timestamp.
        /// </summary>
        [MaxLength(100)]
        public string? DeviceTransactionId { get; set; }

        /// <summary>
        /// How the punch was captured on the device - "Fingerprint", "Face",
        /// "Card", "Password", "Other". Informational/audit only; the
        /// attendance engine does not currently branch on this. Null for
        /// legacy rows and for drivers/devices that don't report it.
        /// </summary>
        [MaxLength(30)]
        public string? VerifyMode { get; set; }

        /// <summary>
        /// For eSSL-sourced rows only (DeviceId starts with "ESSL-"): which
        /// physical eTimeTrackLite1 table this punch was read from -
        /// "DeviceLogs" or a discovered "DeviceLogs_M_YYYY" monthly
        /// partition. Null for punches from any other source (agent-pushed
        /// devices via BiometricSyncService, manual/web punches). Audit only
        /// - "which biometric record generated this HRMS attendance entry"
        /// (requirement #23) - never used by attendance processing logic.
        /// </summary>
        [MaxLength(60)]
        public string? SourceTable { get; set; }

        /// <summary>
        /// For eSSL-sourced rows only: when eTimeTrackLite1 downloaded this
        /// punch from the device into its own database - NOT the punch time
        /// (PunchTime/LogDate is). Audit/troubleshooting only (e.g. "this
        /// August 4th punch wasn't visible to eTimeTrackLite1 until August
        /// 31st") - never used by attendance processing logic. Null for
        /// punches from any other source.
        /// </summary>
        public DateTime? DownloadDate { get; set; }

        public bool IsDuplicate { get; set; }

        public bool IsProcessed { get; set; }

        public DateTime? ProcessedOn { get; set; }

        public override string GetSequencePrefix()
        => "BAL";
    }

    /// <summary>
    /// A Windows Service instance (running on-site, on the same LAN as one or
    /// more physical biometric devices) that pulls punches from its assigned
    /// BiometricDevices and pushes them to POST /api/BiometricSync/ingest.
    /// One agent can serve many devices; one device belongs to at most one
    /// agent. The agent never talks to SQL Server directly - only to this API.
    /// </summary>
    public class BiometricAgent : BaseEntity
    {
        [Required]
        public string TenantId { get; set; }

        public virtual Tenant Tenant { get; set; }

        /// <summary>Stable identifier configured into the agent's appsettings (Agent:AgentCode). Unique per tenant.</summary>
        [Required]
        [MaxLength(50)]
        public string AgentCode { get; set; }

        [Required]
        [MaxLength(100)]
        public string AgentName { get; set; }

        /// <summary>
        /// Shared secret the agent sends (with AgentCode) to authenticate
        /// register/heartbeat/devices calls. Same pattern as
        /// BiometricDevice.DeviceKey - generated server-side, shown once.
        /// </summary>
        [MaxLength(100)]
        public string? AgentKey { get; set; }

        /// <summary>
        /// The branch this agent's machine physically sits at (its LAN is
        /// what the assigned BiometricDevices' IP addresses belong to).
        /// Purely organizational/informational for the ERP UI (e.g. "Mumbai
        /// Branch") - not used for any access-control or routing decision.
        /// </summary>
        public string? BranchId { get; set; }

        public virtual Branch Branch { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public string? MachineName { get; set; }

        public string? AgentVersion { get; set; }

        /// <summary>
        /// Last time this agent successfully called register or heartbeat.
        /// The agent-level equivalent of BiometricDevice.LastSeen - proves
        /// the Windows Service process itself is alive, independent of
        /// whether any of its devices currently have new punches.
        /// </summary>
        public DateTime? LastHeartbeat { get; set; }

        public virtual ICollection<BiometricDevice> Devices { get; set; } = new List<BiometricDevice>();

        public override string GetSequencePrefix()
            => "BAG";
    }

    /// <summary>
    /// A single "Test Connection" command/result round-trip for one device.
    /// Exists because BiometricAgent only ever calls OUT to this API (see
    /// Worker.cs's poll loop) - there is no reverse channel for the API to
    /// reach into the agent's process synchronously. So a Test Connection
    /// request is: (1) HRMS creates a Pending row here, (2) the assigned
    /// agent picks it up on its next poll of GET
    /// /api/BiometricAgent/pending-test-requests, actually calls
    /// IDeviceDriver.ConnectAsync/TryGetDeviceInfoAsync/DisconnectAsync
    /// against the real device, and (3) posts the outcome back to POST
    /// /api/BiometricAgent/test-result, which fills in Status/Stage/Message/
    /// DeviceInfo/CompletedOn here. The UI polls GET
    /// /api/BiometricDevice/{deviceId}/test-connection/{requestId} until
    /// Status is no longer Pending (or the API itself marks it TimedOut
    /// after ~90s with no result).
    ///
    /// Deliberately a separate row per attempt (not a handful of columns
    /// bolted onto BiometricDevice) - a Test Connection result is a
    /// point-in-time fact, not persistent device state, and keeping history
    /// here is useful for troubleshooting ("last 5 test attempts failed at
    /// the SDK Connect stage") without conflating it with
    /// BiometricDevice.IsActive/IsOnline.
    /// </summary>
    public class BiometricDeviceTestRequest : BaseEntity
    {
        [Required]
        public string TenantId { get; set; }

        public virtual Tenant Tenant { get; set; }

        [Required]
        public string DeviceId { get; set; }

        public virtual BiometricDevice Device { get; set; }

        /// <summary>The agent this request was routed to at creation time - a snapshot, so reassigning the device later doesn't orphan history.</summary>
        public string? AgentId { get; set; }

        public TestConnectionStatus Status { get; set; } = TestConnectionStatus.Pending;

        /// <summary>
        /// Where the attempt got to / failed at: "AgentUnavailable",
        /// "Connecting", "DeviceUnreachable", "SdkConnectionFailed",
        /// "Success". Drives which user-facing message the UI shows -
        /// see BiometricDeviceService.RequestTestConnectionAsync and
        /// BiometricAgentService.SubmitTestResultAsync.
        /// </summary>
        [MaxLength(50)]
        public string? Stage { get; set; }

        /// <summary>
        /// Safe, pre-composed message (never a raw exception/stack trace -
        /// both the API and the agent are responsible for sanitizing before
        /// this is written). Shown to the end user as-is.
        /// </summary>
        [MaxLength(500)]
        public string? Message { get; set; }

        /// <summary>Optional device serial/firmware string from a successful lightweight SDK probe after connect - see IDeviceDriver.TryGetDeviceInfoAsync. Null if unavailable/not supported; that does NOT mean the test failed.</summary>
        [MaxLength(200)]
        public string? DeviceInfo { get; set; }

        [Required]
        public string RequestedBy { get; set; }

        public DateTime RequestedOn { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedOn { get; set; }

        public override string GetSequencePrefix()
            => "BTR";
    }

    /// <summary>
    /// One row per synchronization run against a device - "SyncType" is
    /// "Ingest" (BiometricAgent/Simulator push via POST
    /// /api/BiometricSync/ingest), "Pull" (SyncDeviceLogsAsync/SyncAllDevicesAsync,
    /// the older HTTP-pull path), or "Process" (AttendanceProcessorService's
    /// batch run, which isn't per-device but is still worth a trail). Powers
    /// the device dashboard's "Records fetched/inserted/skipped/failed" and
    /// "Last successful sync" figures without having to infer them from
    /// BiometricAttendanceLog/BiometricDevice.LastSyncDate after the fact.
    /// </summary>
    public class BiometricSyncLog : BaseEntity
    {
        [Required]
        public string TenantId { get; set; }

        public virtual Tenant Tenant { get; set; }

        public string? DeviceId { get; set; }

        public virtual BiometricDevice? Device { get; set; }

        public string? AgentId { get; set; }

        [Required]
        [MaxLength(30)]
        public string SyncType { get; set; }

        public DateTime StartTime { get; set; } = DateTime.UtcNow;

        public DateTime? EndTime { get; set; }

        public int RecordsFetched { get; set; }

        public int RecordsInserted { get; set; }

        public int RecordsSkipped { get; set; }

        public int RecordsFailed { get; set; }

        /// <summary>
        /// Rows this run found already present in BiometricAttendanceLogs
        /// (matched via DeviceTransactionId) and therefore did not attempt
        /// to re-insert. Populated only for SyncType = "EsslDbPull" - see
        /// EsslAttendanceSyncService's reconciliation logging
        /// ("Source = AlreadyImported + Unmapped + Failed + Imported").
        /// Counted separately from RecordsSkipped so a duplicate (expected,
        /// harmless, re-scanning the overlap window every run) is never
        /// confused with a genuine skip/failure on the summary screen.
        /// </summary>
        public int DuplicateCount { get; set; }

        /// <summary>
        /// Rows this run imported into BiometricAttendanceLogs whose
        /// device-side employee code (UserId) had no active
        /// EmployeeBiometricMapping at import time - see
        /// EsslAttendanceSyncService's per-record unmapped logging and the
        /// existing "Unmapped Employees" screen
        /// (GetUnmappedEmployeesCoreAsync). These rows ARE imported (never
        /// silently dropped) but AttendanceProcessorService will keep
        /// leaving them IsProcessed=false until a matching mapping is
        /// created, which is the single biggest real-world cause of "eSSL
        /// source count" and "Attendance records created" diverging sharply.
        /// </summary>
        public int UnmappedCount { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "Success";

        [MaxLength(500)]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Populated only for SyncType = "EsslDbPull" (the eTimeTrackLite1
        /// direct-SQL integration - see EsslAttendanceSyncService). Null for
        /// every other SyncType. FromDate/ToDate record the actual query
        /// window used (manual/historical runs pass explicit dates;
        /// automatic runs record the computed incremental window so the
        /// history screen shows exactly what was scanned, not just when).
        /// </summary>
        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        /// <summary>"System" for the background service, or the acting user's name for a manual/historical sync - same convention as ErrorLog.ResolvedBy.</summary>
        [MaxLength(100)]
        public string? TriggeredBy { get; set; }

        /// <summary>
        /// Populated only for SyncType = "EsslDbPull" - a comma-separated
        /// list of the physical eTimeTrackLite1 tables this run actually
        /// scanned (e.g. "DeviceLogs, DeviceLogs_8_2026, DeviceLogs_9_2026"),
        /// as discovered by EsslAttendanceDataSource.DiscoverDeviceLogTablesAsync.
        /// Audit/troubleshooting only - lets an admin see exactly which
        /// monthly partition tables were (or, for a run that found none,
        /// were NOT) picked up for a given window, without needing direct
        /// SQL access to eTimeTrackLite1.
        /// </summary>
        [MaxLength(500)]
        public string? SourceTables { get; set; }

        public override string GetSequencePrefix()
            => "BSL";
    }

    public class EmployeeBiometricMapping : BaseEntity
    {
        [Required]
        public string EmployeeId { get; set; }

        public virtual Employee Employee { get; set; }

        [Required]
        public string BiometricEmployeeCode { get; set; }

        public string? CardNumber { get; set; }

        public string? FaceId { get; set; }

        public string? FingerTemplateId { get; set; }

        public override string GetSequencePrefix()
            => "EBM";
    }
}
