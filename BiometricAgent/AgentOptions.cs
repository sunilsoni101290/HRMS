namespace BiometricAgent
{
    public class AgentOptions
    {
        public const string SectionName = "Agent";

        public string TenantId { get; set; } = "";

        /// <summary>Base URL of the central ERP API, e.g. https://erp.example.com/api/</summary>
        public string ApiBaseUrl { get; set; } = "";

        /// <summary>
        /// Must match BiometricAgent.AgentCode in the ERP database - the
        /// admin creates the BiometricAgent record first (MVC "Add Agent"),
        /// then copies AgentCode/AgentKey into this file. The agent never
        /// self-registers a new row; Register only authenticates an
        /// already-provisioned agent and refreshes its machine/version info.
        /// </summary>
        public string AgentCode { get; set; } = "";

        /// <summary>Must match BiometricAgent.AgentKey in the ERP database. Treat as a secret - see appsettings.json comment.</summary>
        public string AgentKey { get; set; } = "";

        /// <summary>How often (seconds) the worker polls every assigned device for new punches.</summary>
        public int PollIntervalSeconds { get; set; } = 60;

        /// <summary>How often (seconds) the worker pings /api/BiometricAgent/heartbeat, independent of the device poll cycle.</summary>
        public int HeartbeatIntervalSeconds { get; set; } = 60;

        /// <summary>
        /// Consecutive transient-failure retries (with exponential backoff)
        /// ApiClient attempts before giving up on a single call for this
        /// cycle. Permanent errors (401/403/404) are never retried.
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Where the agent keeps its local state (per-device last-synced
        /// time, per-device offline queue). Must be writable by the service account.
        /// </summary>
        public string StateFolder { get; set; } = @"C:\ProgramData\ERP\BiometricAgent";

        /// <summary>
        /// Legacy single-device override, retained only for local
        /// dev/testing without the API (e.g. running the Mock driver
        /// standalone). Production deployments leave this out entirely -
        /// devices are fetched from GET /api/BiometricAgent/devices instead,
        /// which is what supports multiple devices per agent.
        /// </summary>
        public DeviceOptions? Device { get; set; }
    }

    /// <summary>
    /// Per-device driver connection config. Populated dynamically from
    /// BiometricDeviceDto (GET /api/BiometricAgent/devices) in normal
    /// operation; AgentOptions.Device is only a static fallback for local testing.
    /// </summary>
    public class DeviceOptions
    {
        /// <summary>Server-side BiometricDevice.Id - used to key per-device SyncState/OfflineQueue files.</summary>
        public string Id { get; set; } = "";

        /// <summary>Driver selector - "Essl" for real hardware, "Mock" for testing. Matches BiometricDevice.DeviceType. See DeviceDriverFactory.</summary>
        public string DriverType { get; set; } = "Essl";

        /// <summary>Must match BiometricDevice.DeviceCode in the ERP database.</summary>
        public string DeviceCode { get; set; } = "";

        /// <summary>Must match BiometricDevice.DeviceKey in the ERP database.</summary>
        public string DeviceKey { get; set; } = "";

        public string IPAddress { get; set; } = "";

        public int Port { get; set; } = 4370;

        /// <summary>Device comm password/key, if one was set on the device (0 = none).</summary>
        public int CommKey { get; set; } = 0;

        /// <summary>COM ProgID of the vendor SDK - see appsettings.json comment.</summary>
        public string ProgId { get; set; } = "zkemkeeper.CZKEM";
    }
}
