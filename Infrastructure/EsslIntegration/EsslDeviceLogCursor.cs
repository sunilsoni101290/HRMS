using System;

namespace Infrastructure.EsslIntegration
{
    /// <summary>
    /// Keyset-pagination marker for GetDeviceLogsAsync when reading is spread
    /// across MULTIPLE physical source tables (DeviceLogs plus zero or more
    /// DeviceLogs_M_YYYY tables) unioned together. A plain "DeviceLogId >
    /// last id" cursor is not safe here: DeviceLogId is only an IDENTITY
    /// column WITHIN one physical table, so two different monthly tables can
    /// (and in real eTimeTrackLite1 deployments, do) both contain a row with
    /// DeviceLogId = 91. Ordering/paginating by (LogDate, SourceTable,
    /// DeviceLogId) instead - all three together - stays correct regardless
    /// of per-table id collisions, and LogDate-first ordering is also exactly
    /// what "process punches chronologically" requires.
    /// </summary>
    public sealed class EsslDeviceLogCursor
    {
        public DateTime LogDate { get; set; }

        public string SourceTable { get; set; } = "";

        public int DeviceLogId { get; set; }

        public static EsslDeviceLogCursor FromRow(EsslDeviceLogRaw row) => new()
        {
            LogDate = row.LogDate,
            SourceTable = row.SourceTable,
            DeviceLogId = row.DeviceLogId
        };
    }
}
