using Application.DTOs.Attendances;
using Application.DTOs.Employee;
using Application.Interfaces.Attendances;
using Application.Interfaces.ErrorLog;
using Domain.Entities;
using Domain.Helper;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Services.Attendances
{
    /// <summary>
    /// Standalone Historical Attendance Sync engine - see
    /// IHistoricalAttendanceSyncService.cs's remarks. This class NEVER
    /// references IEsslAttendanceSyncService, EsslAttendanceSyncState,
    /// IEsslSyncJobQueue, or any eSSL device-fetching code - it only ever
    /// reads EXISTING dbo.BiometricAttendanceLogs rows (via the
    /// dbo.ProcessHistoricalBiometricAttendance stored procedure - see the
    /// root-level "Create HistoricalAttendanceSyncJob table and
    /// ProcessHistoricalBiometricAttendance SP.sql" script) and turns them
    /// into AttendanceLogs/Attendances for an admin-picked historical
    /// window.
    /// </summary>
    public class HistoricalAttendanceSyncService : IHistoricalAttendanceSyncService
    {
        private readonly ApplicationDbContext _db;
        private readonly IErrorLogService _errorLogService;
        private readonly ILogger<HistoricalAttendanceSyncService> _logger;

        // Batch-level failures inside the SP already keep the whole job
        // moving (see the SP's per-batch TRY/CATCH); a per-job maximum
        // execution time still bounds one call to the SP itself, mirroring
        // EsslAttendanceSyncBackgroundService's ConsumeManualJobsAsync
        // per-job timeout pattern.
        private const int DefaultCommandTimeoutSeconds = 3 * 60 * 60; // 3 hours - a very large historical range can legitimately take a long time; this runs in the background, never blocking an HTTP request.

        public HistoricalAttendanceSyncService(
            ApplicationDbContext db,
            IErrorLogService errorLogService,
            ILogger<HistoricalAttendanceSyncService> logger)
        {
            _db = db;
            _errorLogService = errorLogService;
            _logger = logger;
        }

        public async Task<HistoricalSyncPreviewDto> PreviewAsync(HistoricalSyncRequestDto request, string tenantId)
        {
            var fromDate = request.FromDate.Date;
            var toDate = request.ToDate.Date.AddDays(1); // exclusive upper bound, matches the SP's own window

            var result = new HistoricalSyncPreviewDto
            {
                FromDate = request.FromDate.Date,
                ToDate = request.ToDate.Date,
                EmployeeId = request.EmployeeId
            };

            if (!string.IsNullOrWhiteSpace(request.EmployeeId))
            {
                var emp = await _db.Employees.AsNoTracking()
                    .Where(e => e.Id == request.EmployeeId && e.TenantId == tenantId && !e.IsDeleted)
                    .Select(e => new { e.FirstName, e.LastName })
                    .FirstOrDefaultAsync();

                result.EmployeeName = emp != null ? $"{emp.FirstName} {emp.LastName}".Trim() : null;
            }

            var rangeQuery = _db.BiometricAttendanceLogs.AsNoTracking()
                .Where(x => x.TenantId == tenantId && !x.IsDeleted && x.PunchTime >= fromDate && x.PunchTime < toDate);

            result.TotalBiometricLogsInRange = await rangeQuery.CountAsync();
            result.FlaggedDuplicateCount = await rangeQuery.CountAsync(x => x.IsDuplicate);

            var linkedRawIds = await _db.AttendanceLogs.AsNoTracking()
                .Where(x => x.BiometricAttendanceLogId != null)
                .Select(x => x.BiometricAttendanceLogId!)
                .ToListAsync();

            var linkedSet = linkedRawIds.ToHashSet();

            var rangeIds = await rangeQuery.Where(x => !x.IsDuplicate).Select(x => new { x.Id, x.EmployeeCode }).ToListAsync();

            result.AlreadyLinkedCount = rangeIds.Count(x => linkedSet.Contains(x.Id));
            result.UnprocessedLogsInRange = rangeIds.Count(x => !linkedSet.Contains(x.Id));

            var activeMappingCodes = (await _db.EmployeeBiometricMappings.AsNoTracking()
                    .Where(m => m.IsActive && !m.IsDeleted)
                    .Select(m => m.BiometricEmployeeCode)
                    .ToListAsync())
                .Select(BiometricEmployeeCodeNormalizer.Normalize)
                .ToHashSet();

            var unmappedCodes = rangeIds
                .Where(x => !linkedSet.Contains(x.Id))
                .Select(x => BiometricEmployeeCodeNormalizer.Normalize(x.EmployeeCode))
                .Where(c => !activeMappingCodes.Contains(c))
                .Distinct()
                .ToList();

            result.UnmappedEmployeeCodeCount = unmappedCodes.Count;
            result.SampleUnmappedCodes = unmappedCodes.Take(10).ToList();

            var overlapping = await FindOverlappingActiveJobAsync(tenantId, request.FromDate.Date, request.ToDate.Date);
            result.HasOverlappingActiveJob = overlapping != null;
            result.OverlappingJobId = overlapping?.Id;

            return result;
        }

        public async Task<(bool Claimed, string? Message, string? JobId)> ClaimAndCreateJobAsync(
            HistoricalSyncRequestDto request, string tenantId, string requestedBy)
        {
            if (request.FromDate.Date > request.ToDate.Date)
                return (false, "From Date cannot be after To Date.", null);

            // Serializable isolation for the atomic "no overlapping
            // Queued/Running job already exists, then insert" check - this
            // feature's OWN lock, entirely separate from
            // EsslAttendanceSyncState.IsSyncRunning (spec section 5).
            //
            // This whole claim-check-then-insert sequence has to run inside
            // the DbContext's own execution strategy
            // (Database.CreateExecutionStrategy().ExecuteAsync(...)), not a
            // bare "using var transaction = await
            // Database.BeginTransactionAsync(...)". The project's
            // SqlServerRetryingExecutionStrategy (EnableRetryOnFailure) can
            // transparently retry a failed operation, but it can only safely
            // do that when it owns the whole retriable unit - a manually
            // opened transaction outside its control throws
            // "SqlServerRetryingExecutionStrategy does not support
            // user-initiated transactions" the moment anything inside it
            // touches the database, exactly as EF Core's own exception
            // message says to fix. ExecuteAsync's delegate re-runs from
            // scratch on a transient failure, so it opens its own fresh
            // transaction on each attempt rather than sharing one across
            // retries.
            var strategy = _db.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

                try
                {
                    var overlapping = await FindOverlappingActiveJobAsync(tenantId, request.FromDate.Date, request.ToDate.Date);

                    if (overlapping != null)
                    {
                        await transaction.RollbackAsync();
                        return (false,
                            $"A Historical Attendance Sync job (status: {overlapping.Status}) is already queued/running for an overlapping date range " +
                            $"({overlapping.FromDate:dd-MMM-yyyy} to {overlapping.ToDate:dd-MMM-yyyy}) for this tenant. Wait for it to finish or pick a non-overlapping range.",
                            (string?)null);
                    }

                    var job = new HistoricalAttendanceSyncJob
                    {
                        TenantId = tenantId,
                        FromDate = request.FromDate.Date,
                        ToDate = request.ToDate.Date,
                        EmployeeId = string.IsNullOrWhiteSpace(request.EmployeeId) ? null : request.EmployeeId,
                        BatchSize = request.BatchSize < 1 ? 1000 : request.BatchSize,
                        RequestedBy = requestedBy,
                        Status = "Queued",
                        IsRunning = false,
                        CreatedBy = requestedBy
                    };
                    job.Id = IDManager.GetNewId(job);

                    await _db.HistoricalAttendanceSyncJobs.AddAsync(job);
                    await _db.SaveChangesAsync();

                    await transaction.CommitAsync();

                    return (true, "Historical Attendance Sync job queued.", job.Id);
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        private async Task<HistoricalAttendanceSyncJob?> FindOverlappingActiveJobAsync(string tenantId, DateTime fromDate, DateTime toDate)
        {
            return await _db.HistoricalAttendanceSyncJobs
                .Where(j => j.TenantId == tenantId && !j.IsDeleted &&
                            (j.Status == "Queued" || j.Status == "Running") &&
                            j.FromDate.Date <= toDate && j.ToDate.Date >= fromDate)
                .OrderByDescending(j => j.CreatedOn)
                .FirstOrDefaultAsync();
        }

        public async Task RunJobAsync(string jobId, CancellationToken ct)
        {
            var job = await _db.HistoricalAttendanceSyncJobs.FirstOrDefaultAsync(j => j.Id == jobId, ct);

            if (job == null)
            {
                _logger.LogWarning("Historical Attendance Sync: job {JobId} not found - skipping.", jobId);
                return;
            }

            if (job.Status != "Queued")
            {
                _logger.LogWarning("Historical Attendance Sync: job {JobId} is not Queued (Status={Status}) - skipping to avoid double-processing.", jobId, job.Status);
                return;
            }

            _logger.LogInformation(
                "Historical Attendance Sync: starting job {JobId} for tenant {TenantId}, range {From:dd-MMM-yyyy} to {To:dd-MMM-yyyy}, EmployeeId={EmployeeId}.",
                jobId, job.TenantId, job.FromDate, job.ToDate, job.EmployeeId ?? "(all)");

            var connectionString = _db.Database.GetConnectionString();

            try
            {
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync(ct);

                await using var command = connection.CreateCommand();
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = "dbo.ProcessHistoricalBiometricAttendance";
                command.CommandTimeout = DefaultCommandTimeoutSeconds;

                command.Parameters.Add(new SqlParameter("@TenantId", SqlDbType.NVarChar, 450) { Value = job.TenantId });
                command.Parameters.Add(new SqlParameter("@FromDate", SqlDbType.DateTime2) { Value = job.FromDate });
                command.Parameters.Add(new SqlParameter("@ToDate", SqlDbType.DateTime2) { Value = job.ToDate });
                command.Parameters.Add(new SqlParameter("@EmployeeId", SqlDbType.NVarChar, 450) { Value = (object?)job.EmployeeId ?? DBNull.Value });
                command.Parameters.Add(new SqlParameter("@BatchSize", SqlDbType.Int) { Value = job.BatchSize });
                command.Parameters.Add(new SqlParameter("@RequestedBy", SqlDbType.NVarChar, 450) { Value = job.RequestedBy });
                command.Parameters.Add(new SqlParameter("@JobId", SqlDbType.NVarChar, 450) { Value = job.Id });

                SqlParameter OutParam(string name, SqlDbType type, int size = 0)
                {
                    var p = new SqlParameter(name, type) { Direction = ParameterDirection.Output };
                    // size > 0 is a real fixed length (e.g. NVARCHAR(450));
                    // size == -1 is ADO.NET's convention for NVARCHAR(MAX)
                    // and has to be set explicitly too - SqlParameter.Size
                    // defaults to 0 for an output parameter with no Value
                    // assigned yet, and 0 is not a valid size for either
                    // case, which is exactly what "the Size property has an
                    // invalid size of 0" was complaining about for
                    // @ErrorSummary (the only -1/MAX output param here).
                    // size == 0 (the default) means "let SqlParameter infer
                    // it" - only true for the plain Int output params below.
                    if (size > 0 || size == -1) p.Size = size;
                    command.Parameters.Add(p);
                    return p;
                }

                var pTotal = OutParam("@TotalRecords", SqlDbType.Int);
                var pProcessed = OutParam("@ProcessedCount", SqlDbType.Int);
                var pFailed = OutParam("@FailedCount", SqlDbType.Int);
                var pUnmapped = OutParam("@UnmappedCount", SqlDbType.Int);
                var pDuplicate = OutParam("@DuplicateCount", SqlDbType.Int);
                var pLogsCreated = OutParam("@AttendanceLogsCreated", SqlDbType.Int);
                var pAttCreated = OutParam("@AttendancesCreated", SqlDbType.Int);
                var pAttUpdated = OutParam("@AttendancesUpdated", SqlDbType.Int);
                var pErrorSummary = OutParam("@ErrorSummary", SqlDbType.NVarChar, -1);

                await command.ExecuteNonQueryAsync(ct);

                _logger.LogInformation(
                    "Historical Attendance Sync: job {JobId} finished. Total={Total}, Processed={Processed}, Failed={Failed}, Unmapped={Unmapped}, Duplicate={Duplicate}, LogsCreated={LogsCreated}, AttCreated={AttCreated}, AttUpdated={AttUpdated}.",
                    jobId, pTotal.Value, pProcessed.Value, pFailed.Value, pUnmapped.Value, pDuplicate.Value, pLogsCreated.Value, pAttCreated.Value, pAttUpdated.Value);

                // The SP itself already wrote the final Status/counters/EndTime
                // straight onto the job row (see Step 10 / "Final status" in
                // the SP) - this is only a defensive re-read/verify, useful
                // if the connection dropped between the SP finishing and this
                // call returning.
                _db.ChangeTracker.Clear();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Historical Attendance Sync: job {JobId} failed with an unhandled exception.", jobId);

                await _errorLogService.LogAsync(
                    ex,
                    module: "Attendance",
                    feature: "Historical Attendance Sync",
                    controller: "HistoricalAttendanceSyncService",
                    action: nameof(RunJobAsync),
                    userId: job.RequestedBy,
                    tenantId: job.TenantId);

                // The SP could not run at all (e.g. connection failure before
                // it even started) - the job row must still be released from
                // Running/IsRunning so it isn't stuck forever, and marked
                // Failed rather than silently left Queued/Running.
                var freshJob = await _db.HistoricalAttendanceSyncJobs.FirstOrDefaultAsync(j => j.Id == jobId);

                if (freshJob != null)
                {
                    freshJob.Status = "Failed";
                    freshJob.IsRunning = false;
                    freshJob.EndTime = DateTime.UtcNow;
                    freshJob.ErrorSummary = (freshJob.ErrorSummary ?? "") + "Job could not run: " + ex.Message;
                    freshJob.ModifiedOn = DateTime.UtcNow;
                    await _db.SaveChangesAsync();
                }
            }
        }

        public async Task<HistoricalSyncJobDto?> GetJobStatusAsync(string jobId, string tenantId)
        {
            var job = await _db.HistoricalAttendanceSyncJobs.AsNoTracking()
                .FirstOrDefaultAsync(j => j.Id == jobId && j.TenantId == tenantId && !j.IsDeleted);

            if (job == null) return null;

            return await MapAsync(job);
        }

        public async Task<PagedResult<HistoricalSyncJobDto>> GetJobHistoryAsync(HistoricalSyncJobFilterDto filter, string tenantId)
        {
            var query = _db.HistoricalAttendanceSyncJobs.AsNoTracking()
                .Where(j => j.TenantId == tenantId && !j.IsDeleted);

            if (filter.DateFrom.HasValue)
                query = query.Where(j => j.CreatedOn.Date >= filter.DateFrom.Value.Date);

            if (filter.DateTo.HasValue)
                query = query.Where(j => j.CreatedOn.Date <= filter.DateTo.Value.Date);

            if (!string.IsNullOrWhiteSpace(filter.Status))
                query = query.Where(j => j.Status == filter.Status);

            var total = await query.CountAsync();

            var pageSize = filter.PageSize < 1 ? 20 : filter.PageSize;
            var pageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;

            var jobs = await query
                .OrderByDescending(j => j.CreatedOn)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = new List<HistoricalSyncJobDto>();
            foreach (var job in jobs)
                items.Add(await MapAsync(job));

            return new PagedResult<HistoricalSyncJobDto>
            {
                Data = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = total
            };
        }

        private async Task<HistoricalSyncJobDto> MapAsync(HistoricalAttendanceSyncJob job)
        {
            string? employeeName = null;

            if (!string.IsNullOrWhiteSpace(job.EmployeeId))
            {
                var emp = await _db.Employees.AsNoTracking()
                    .Where(e => e.Id == job.EmployeeId)
                    .Select(e => new { e.FirstName, e.LastName })
                    .FirstOrDefaultAsync();

                employeeName = emp != null ? $"{emp.FirstName} {emp.LastName}".Trim() : null;
            }

            return new HistoricalSyncJobDto
            {
                Id = job.Id,
                FromDate = job.FromDate,
                ToDate = job.ToDate,
                EmployeeId = job.EmployeeId,
                EmployeeName = employeeName,
                BatchSize = job.BatchSize,
                RequestedBy = job.RequestedBy,
                Status = job.Status,
                StartTime = job.StartTime,
                EndTime = job.EndTime,
                DurationSeconds = job.StartTime.HasValue && job.EndTime.HasValue
                    ? (job.EndTime.Value - job.StartTime.Value).TotalSeconds
                    : null,
                TotalRecords = job.TotalRecords,
                ProcessedCount = job.ProcessedCount,
                FailedCount = job.FailedCount,
                UnmappedCount = job.UnmappedCount,
                DuplicateCount = job.DuplicateCount,
                AttendanceLogsCreated = job.AttendanceLogsCreated,
                AttendancesCreated = job.AttendancesCreated,
                AttendancesUpdated = job.AttendancesUpdated,
                ErrorSummary = job.ErrorSummary,
                CreatedOn = job.CreatedOn
            };
        }

        public async Task ReconcileStaleLocksAsync()
        {
            try
            {
                var stuck = await _db.HistoricalAttendanceSyncJobs
                    .Where(j => j.IsRunning || j.Status == "Running")
                    .ToListAsync();

                if (stuck.Count == 0) return;

                var now = DateTime.UtcNow;

                foreach (var job in stuck)
                {
                    job.IsRunning = false;
                    job.Status = "Failed";
                    job.EndTime = now;
                    job.ErrorSummary = (job.ErrorSummary ?? "") +
                        "Interrupted by an application restart and automatically reset - re-run this date range if it did not complete.";
                    job.ModifiedOn = now;
                }

                await _db.SaveChangesAsync();

                _logger.LogWarning(
                    "Historical Attendance Sync: reconciled {Count} job(s) left Running by a previous app restart.", stuck.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Historical Attendance Sync: failed to reconcile stale locks on startup.");
            }
        }
    }
}
