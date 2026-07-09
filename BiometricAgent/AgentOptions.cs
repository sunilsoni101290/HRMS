namespace BiometricAgent
{
    public class AgentOptions
    {
        public const string SectionName = "Agent";

        public string TenantId { get; set; } = "";

        /// <summary>Base URL of the central ERP API, e.g. https://erp.example.com</summary>
        public string ApiBaseUrl { get; set; } = "";

        public int PollIntervalSeconds { get; set; } = 60;

        /// <summary>
        /// Where the agent keeps its local state (last synced punch time,
        /// offline queue file). Must be writable by the service account.
        /// </summary>
        public string StateFolder { get; set; } = @"C:\ProgramData\ERP\BiometricAgent";

        public DeviceOptions Device { get; set; } = new();
    }

    public class DeviceOptions
    {
        /// <summary>"Essl" for real hardware, "Mock" to run without a device for testing.</summary>
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
