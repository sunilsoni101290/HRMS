using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Application.Services.Attendances
{
    /// <summary>
    /// One fully-formed row ready to be bulk-staged. Everything business-
    /// relevant (Id, EmployeeCode, PunchType resolution, DeviceTransactionId
    /// = SourceTable+DeviceLogId identity, employee-mapping lookup) is
    /// computed in EsslAttendanceSyncService EXACTLY as it is today - this
    /// class never re-derives any of that in T-SQL, it only bulk-writes the
    /// finished result and lets the database do the set-based dedupe/insert.
    /// </summary>
    public sealed class EsslBulkStageRow
    {
        public BiometricAttendanceLog Entity { get; init; } = null!;

        /// <summary>The raw eTimeTrackLite1 DeviceLogId - audit/troubleshooting only, NEVER the dedupe key (Entity.DeviceTransactionId, built from SourceTable+DeviceLogId, is).</summary>
        public int SourceDeviceLogId { get; init; }

        /// <summary>True when this punch's employee code had no matching EmployeeBiometricMapping at staging time - still staged/inserted, just flagged for the reconciliation count and the Unmapped Employees screen.</summary>
        public bool IsUnmapped { get; init; }
    }

    /// <summary>DB-verified (not C#-counted) reconciliation counts for one StageAndMergeAsync call. StagedCount == InsertedCount + DuplicateCount always holds on success (see EsslBulkStaging.sql's usp_EsslStaging_MergeAttendanceLogs).</summary>
    public sealed class EsslBulkStageResult
    {
        public int StagedCount { get; init; }
        public int InsertedCount { get; init; }
        public int DuplicateCount { get; init; }
        public int UnmappedInsertedCount { get; init; }
        public int FailedCount { get; init; }
    }

    public interface IEsslBulkAttendanceLogWriter
    {
        /// <summary>
        /// Bulk-copies <paramref name="rows"/> into dbo.EsslDeviceLogStaging
        /// then calls dbo.usp_EsslStaging_MergeAttendanceLogs to merge them
        /// into BiometricAttendanceLogs in one set-based operation. Throws
        /// on any failure (bulk copy or the stored procedure) rather than
        /// returning a partial/best-guess result - the caller
        /// (EsslAttendanceSyncService) is expected to catch and fall back to
        /// its existing EF AddRange/SaveChangesAsync path for this same
        /// batch, exactly like it already falls back to row-by-row when a
        /// bulk EF SaveChangesAsync fails. Requires
        /// Infrastructure/Migrations-adjacent SQL objects created by
        /// EsslBulkStaging.sql to already exist - if they don't (not yet
        /// run on this environment), the stored-procedure call throws and
        /// the same fallback applies, so this can be deployed safely before
        /// that script has been run everywhere.
        /// </summary>
        Task<EsslBulkStageResult> StageAndMergeAsync(
            string connectionString,
            string tenantId,
            IReadOnlyList<EsslBulkStageRow> rows,
            CancellationToken ct = default);
    }

    public sealed class EsslBulkAttendanceLogWriter : IEsslBulkAttendanceLogWriter
    {
        private readonly ILogger<EsslBulkAttendanceLogWriter> _logger;

        public EsslBulkAttendanceLogWriter(ILogger<EsslBulkAttendanceLogWriter> logger)
        {
            _logger = logger;
        }

        public async Task<EsslBulkStageResult> StageAndMergeAsync(
            string connectionString,
            string tenantId,
            IReadOnlyList<EsslBulkStageRow> rows,
            CancellationToken ct = default)
        {
            if (rows == null || rows.Count == 0)
                return new EsslBulkStageResult();

            var batchRunId = Guid.NewGuid();

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(ct);

            // Step 1: bulk-copy the fully-formed rows into staging - one TDS
            // bulk-insert stream instead of N parameterized INSERT
            // statements (or N EF change-tracked entities). This table is
            // scoped per BatchRunId so concurrent tenants can never
            // interleave, and a failed run's rows are left behind for
            // inspection (see EsslBulkStaging.sql).
            using (var table = BuildStagingTable(batchRunId, tenantId, rows))
            using (var bulkCopy = new SqlBulkCopy(connection)
            {
                DestinationTableName = "dbo.EsslDeviceLogStaging",
                BulkCopyTimeout = 120
            })
            {
                foreach (DataColumn col in table.Columns)
                    bulkCopy.ColumnMappings.Add(col.ColumnName, col.ColumnName);

                await bulkCopy.WriteToServerAsync(table, ct);
            }

            // Step 2: one set-based merge, entirely in SQL Server. Any
            // exception here (including the deliberate THROW inside the
            // stored procedure's CATCH block on a whole-batch failure)
            // propagates to the caller unmodified - it must NOT be swallowed
            // here, since the caller's fallback logic depends on seeing it.
            using var command = new SqlCommand("dbo.usp_EsslStaging_MergeAttendanceLogs", connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 120
            };

            command.Parameters.Add(new SqlParameter("@BatchRunId", SqlDbType.UniqueIdentifier) { Value = batchRunId });

            var stagedOut = AddOutputParam(command, "@StagedCount");
            var insertedOut = AddOutputParam(command, "@InsertedCount");
            var duplicateOut = AddOutputParam(command, "@DuplicateCount");
            var unmappedOut = AddOutputParam(command, "@UnmappedInsertedCount");
            var failedOut = AddOutputParam(command, "@FailedCount");

            await command.ExecuteNonQueryAsync(ct);

            var result = new EsslBulkStageResult
            {
                StagedCount = (int)(stagedOut.Value ?? 0),
                InsertedCount = (int)(insertedOut.Value ?? 0),
                DuplicateCount = (int)(duplicateOut.Value ?? 0),
                UnmappedInsertedCount = (int)(unmappedOut.Value ?? 0),
                FailedCount = (int)(failedOut.Value ?? 0)
            };

            _logger.LogInformation(
                "eSSL bulk staging: tenant={TenantId}, batchRunId={BatchRunId}, staged={Staged}, inserted={Inserted} (unmapped={Unmapped}), duplicate={Duplicate}, failed={Failed}.",
                tenantId, batchRunId, result.StagedCount, result.InsertedCount, result.UnmappedInsertedCount, result.DuplicateCount, result.FailedCount);

            return result;
        }

        private static SqlParameter AddOutputParam(SqlCommand command, string name)
        {
            var p = new SqlParameter(name, SqlDbType.Int) { Direction = ParameterDirection.Output };
            command.Parameters.Add(p);
            return p;
        }

        private static DataTable BuildStagingTable(Guid batchRunId, string tenantId, IReadOnlyList<EsslBulkStageRow> rows)
        {
            var table = new DataTable();
            table.Columns.Add("BatchRunId", typeof(Guid));
            table.Columns.Add("TenantId", typeof(string));
            table.Columns.Add("Id", typeof(string));
            table.Columns.Add("EmployeeCode", typeof(string));
            table.Columns.Add("PunchTime", typeof(DateTime));
            table.Columns.Add("PunchType", typeof(int));
            table.Columns.Add("DeviceId", typeof(string));
            table.Columns.Add("DeviceTransactionId", typeof(string));
            table.Columns.Add("SourceTable", typeof(string));
            table.Columns.Add("SourceDeviceLogId", typeof(int));
            table.Columns.Add("DownloadDate", typeof(DateTime));
            table.Columns.Add("IsUnmapped", typeof(bool));
            table.Columns.Add("CreatedBy", typeof(string));

            foreach (var row in rows)
            {
                var e = row.Entity;
                table.Rows.Add(
                    batchRunId,
                    tenantId,
                    e.Id,
                    e.EmployeeCode,
                    e.PunchTime,
                    (int)e.PunchType,
                    e.DeviceId,
                    e.DeviceTransactionId,
                    e.SourceTable ?? "",
                    row.SourceDeviceLogId,
                    (object?)e.DownloadDate ?? DBNull.Value,
                    row.IsUnmapped,
                    (object?)e.CreatedBy ?? DBNull.Value);
            }

            return table;
        }
    }
}
