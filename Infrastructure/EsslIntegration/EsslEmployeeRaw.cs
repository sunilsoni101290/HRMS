namespace Infrastructure.EsslIntegration
{
    /// <summary>
    /// Read-only mapping onto eTimeTrackLite1's own [dbo].[Employees] table
    /// (per the supplied esslEmployeeAdd.sql schema). Used only to show a
    /// human-friendly name/status for an eSSL device-side employee code that
    /// has no matching HRMS EmployeeBiometricMapping yet - the "Unknown
    /// Employee" / Unmapped Employees admin report (requirement #21).
    /// This integration never writes to this table.
    /// </summary>
    public class EsslEmployeeRaw
    {
        /// <summary>
        /// Declared PRIMARY KEY on the real table (per esslEmployeeAdd.sql -
        /// NOT EmployeeId, which is IDENTITY but not the PK there).
        /// </summary>
        public string EmployeeCode { get; set; } = "";

        public string EmployeeName { get; set; } = "";

        /// <summary>The field DeviceLogs.UserId is expected to match - see EsslDeviceLogRaw.UserId.</summary>
        public string EmployeeCodeInDevice { get; set; } = "";

        public string? Status { get; set; }

        public string? Designation { get; set; }
    }
}
