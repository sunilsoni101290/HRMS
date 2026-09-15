using Application.Interfaces.Attendances;
using Application.Interfaces.ErrorLog;
using Domain.Entities;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static Domain.Enums.EnumExtensions;

namespace API.BackgroundServices
{
    // Comp Off DETECTION - the "System-detected" half of the
    // "System-detected, HR-approved" Comp Off design (see
    // Application/Services/Attendances/CompOffService.cs for the "HR-
    // approved" half). This job never credits an employee's leave balance
    // itself - it only creates a CompOffCandidate row at PendingReview for
    // HR to individually Approve or Reject via CompOffService.
    //
    // Each run scans Attendance rows from a rolling 7-day lookback window
    // (not the entire history - recently-punched days only, so re-runs stay
    // cheap) across every tenant. For each row, a day is "comp-off
    // eligible" if:
    //   a) Date falls on a Holiday (HolidayGroupDetail.HolidayDate,
    //      tenant-scoped) or a WeekOff (WeekOff.Day, tenant-scoped) - same
    //      lookup pattern as AttendanceService.GetCalendarAsync, AND
    //   b) TotalWorkingHours >= the employee's tenant/company's active
    //      AttendancePolicy.CompOffEligibleExtraHours (a tenant with no
    //      active policy, or CompOffEligibleExtraHours <= 0, has the
    //      feature effectively OFF - skipped entirely).
    //
    // Idempotency: before inserting, the job checks no CompOffCandidate
    // already exists for this exact (EmployeeId, AttendanceId) pair - this
    // is the primary safeguard against creating duplicate candidates when
    // the same Attendance row falls inside the lookback window on more than
    // one run. A matching UNIQUE (EmployeeId, AttendanceId) index on
    // CompOffCandidate (see ApplicationDbContext) is the DB-level backstop.
    //
    // Like LeaveAccrualService/LeaveEscalationService, this is a singleton
    // BackgroundService running outside any HTTP request scope, so every
    // scoped dependency is resolved from a fresh IServiceScope per run.
    public class CompOffDetectionService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<CompOffDetectionService> _logger;

        // Once-a-day housekeeping, same cadence as LeaveAccrualService -
        // Attendance data for a given day doesn't change enough intra-day to
        // justify scanning more often.
        private static readonly TimeSpan PollInterval = TimeSpan.FromHours(24);

        // Rolling lookback window - catches recently-punched/updated
        // Attendance rows without re-scanning the entire history every run.
        private const int LookbackDays = 7;

        public CompOffDetectionService(
            IServiceScopeFactory scopeFactory,
            ILogger<CompOffDetectionService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Give the app (and DbSeeder.SeedAsync in Program.cs) a moment
            // to finish starting up before the first run.
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunCycleAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    // A failed run must never crash the host - just log and
                    // try again on the next tick. Also recorded in the
                    // shared ErrorLog table (fresh scope - the cycle's own
                    // scope is already gone by the time this runs) so it
                    // shows up on the Error Log admin screen, not just in
                    // server logs.
                    _logger.LogError(ex, "Comp Off detection cycle failed.");

                    try
                    {
                        using var errorScope = _scopeFactory.CreateScope();
                        var errorLogService = errorScope.ServiceProvider.GetRequiredService<IErrorLogService>();

                        await errorLogService.LogAsync(
                            ex,
                            module: "HRMS",
                            feature: "Comp Off Detection",
                            controller: "CompOffDetectionService",
                            action: nameof(RunCycleAsync),
                            userId: "System",
                            userName: "System");
                    }
                    catch
                    {
                        // Logging must never itself take down the host.
                    }
                }

                try
                {
                    await Task.Delay(PollInterval, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    // Host is shutting down.
                }
            }
        }

        private async Task RunCycleAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var policyService = scope.ServiceProvider.GetRequiredService<IAttendancePolicyService>();

            var lookbackStart = DateTime.Today.AddDays(-LookbackDays);

            var attendances = await context.Attendances
                .Include(x => x.Employee)
                .Where(x => x.Date >= lookbackStart && !x.IsDeleted && x.TotalWorkingHours > 0)
                .ToListAsync(ct);

            // Group by tenant so the Holiday/WeekOff lookup sets (and the
            // active AttendancePolicy per company) only need to be resolved
            // once per tenant, not once per Attendance row.
            var byTenant = attendances
                .Where(x => x.Employee != null && !string.IsNullOrEmpty(x.TenantId))
                .GroupBy(x => x.TenantId);

            // Cache the active policy per (tenantId, companyId) within this
            // run - AttendancePolicyService.GetActiveForTenantAsync is cheap
            // but there is no reason to re-query it once per row when a
            // whole tenant's employees typically share a handful of
            // companies.
            var policyCache = new Dictionary<string, AttendancePolicy?>();

            foreach (var tenantGroup in byTenant)
            {
                var tenantId = tenantGroup.Key!;

                var minDate = tenantGroup.Min(x => x.Date.Date);
                var maxDate = tenantGroup.Max(x => x.Date.Date);

                // Same holiday-lookup pattern as
                // AttendanceService.GetCalendarAsync: tenant-scoped
                // HolidayGroupDetail rows within the date range touched by
                // this run's Attendance rows.
                var holidayMap = (await context.HolidayGroupDetails.AsNoTracking()
                        .Where(h => h.TenantId == tenantId && h.HolidayDate >= minDate && h.HolidayDate <= maxDate)
                        .Select(h => new { Date = h.HolidayDate.Date, h.HolidayName })
                        .ToListAsync(ct))
                    .GroupBy(h => h.Date)
                    .ToDictionary(g => g.Key, g => g.First().HolidayName);

                // Same WeekOff-lookup pattern as
                // AttendanceService.GetCalendarAsync: tenant-scoped WeekOff
                // days of week.
                var weekOffSet = (await context.WeekOffs.AsNoTracking()
                        .Where(w => w.TenantId == tenantId)
                        .Select(w => w.Day)
                        .ToListAsync(ct))
                    .ToHashSet();

                foreach (var attendance in tenantGroup)
                {
                    try
                    {
                        var employee = attendance.Employee;

                        if (employee == null)
                            continue;

                        var date = attendance.Date.Date;

                        bool isHoliday = holidayMap.ContainsKey(date);
                        bool isWeekOff = weekOffSet.Contains(date.DayOfWeek);

                        // Not a Holiday or WeekOff for this tenant - not a
                        // comp-off candidate day, regardless of hours worked.
                        if (!isHoliday && !isWeekOff)
                            continue;

                        var cacheKey = $"{tenantId}|{employee.CompanyId}";

                        if (!policyCache.TryGetValue(cacheKey, out var cachedPolicyEntity))
                        {
                            var policyDto = await policyService.GetActiveForTenantAsync(tenantId, employee.CompanyId);

                            cachedPolicyEntity = policyDto == null
                                ? null
                                : new AttendancePolicy { CompOffEligibleExtraHours = policyDto.CompOffEligibleExtraHours };

                            policyCache[cacheKey] = cachedPolicyEntity;
                        }

                        // No active policy for this tenant/company, or no
                        // threshold configured (<= 0) - Comp Off detection is
                        // effectively OFF for this employee.
                        if (cachedPolicyEntity == null || cachedPolicyEntity.CompOffEligibleExtraHours <= 0)
                            continue;

                        // TotalWorkingHours is used (not OvertimeHours) - on a
                        // Holiday/WeekOff there is no "expected" shift, so
                        // every hour actually worked that day is voluntary/
                        // extra, exactly what TotalWorkingHours already
                        // represents.
                        if (attendance.TotalWorkingHours < cachedPolicyEntity.CompOffEligibleExtraHours)
                            continue;

                        // Idempotency guard - the critical duplicate-
                        // prevention safeguard: skip if a candidate already
                        // exists for this exact (EmployeeId, AttendanceId).
                        bool alreadyExists = await context.CompOffCandidates
                            .AnyAsync(x => x.EmployeeId == employee.Id && x.AttendanceId == attendance.Id, ct);

                        if (alreadyExists)
                            continue;

                        string triggerReason = isHoliday
                            ? $"Worked {attendance.TotalWorkingHours:0.##} hrs on Holiday: {holidayMap[date]}"
                            : $"Worked {attendance.TotalWorkingHours:0.##} hrs on Week Off";

                        var candidate = new CompOffCandidate
                        {
                            Id = IDManager.GetNewId(new CompOffCandidate()),

                            EmployeeId = employee.Id,
                            AttendanceId = attendance.Id,

                            WorkedDate = date,
                            HoursWorked = attendance.TotalWorkingHours,

                            TriggerReason = triggerReason,

                            Status = CompOffCandidateStatus.PendingReview,
                            CreditedDays = 1,

                            TenantId = tenantId,
                            CreatedOn = DateTime.UtcNow,
                            CreatedBy = "System"
                        };

                        context.CompOffCandidates.Add(candidate);

                        await context.SaveChangesAsync(ct);
                    }
                    catch (Exception ex)
                    {
                        // One bad Attendance record must never stop the batch.
                        _logger.LogError(ex,
                            "Comp Off detection failed for attendance {AttendanceId}, employee {EmployeeId}.",
                            attendance.Id, attendance.EmployeeId);
                    }
                }
            }
        }
    }
}
