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
