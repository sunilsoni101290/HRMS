using Application.DTOs.Leaves;
using Application.Interfaces.ErrorLog;
using Application.Interfaces.Leaves;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using static Domain.Enums.EnumExtensions;

namespace API.BackgroundServices
{
    // Leave Policy Engine - foundational background automation.
    //
    // Two responsibilities, both driven off LeaveType master data added in
    // this phase (Domain/Entities/LeaveType.cs: AccrualFrequency,
    // AccrualDaysPerCycle, MinServiceDaysRequired):
    //
    //   a) ACCRUAL - on each LeaveType's cycle-boundary date (1st of the
    //      month for Monthly, 1st of Jan/Apr/Jul/Oct for Quarterly, 1st of
    //      January for Yearly), credit AccrualDaysPerCycle days to every
    //      eligible employee's current-year balance, capped so the
    //      employee's Allocated+Credited total for the year never exceeds
    //      LeaveType.MaxDaysPerYear.
    //
    //   b) CARRY-FORWARD - on January 1st, roll unused balance from the
    //      previous year into the new year for every LeaveType with
    //      AllowCarryForward = true, capped at MaxCarryForwardDays.
    //
    // DESIGN DECISION - reuse, don't duplicate: both operations delegate the
    // actual balance mutation to the existing ILeaveBalanceService
    // (CreditLeaveAsync / CarryForwardLeaveAsync) rather than writing to
    // LeaveBalance/LeaveBalanceTransaction directly here, so this job can
    // never drift from the balance-calculation logic already used by the
    // manual HR-driven flows.
    //
    // Like LeaveEscalationService, this is a singleton BackgroundService
    // that runs outside any HTTP request scope, so every scoped dependency
    // (ApplicationDbContext, ILeaveBalanceService) is resolved from a fresh
    // IServiceScope per run rather than injected into the constructor.
    public class LeaveAccrualService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<LeaveAccrualService> _logger;

        // Once-a-day housekeeping - there is no benefit to checking more
        // often, since cycle-boundary dates are whole calendar days.
        private static readonly TimeSpan PollInterval = TimeSpan.FromHours(24);

        public LeaveAccrualService(
            IServiceScopeFactory scopeFactory,
            ILogger<LeaveAccrualService> logger)
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
                    _logger.LogError(ex, "Leave accrual/carry-forward cycle failed.");

                    try
                    {
                        using var errorScope = _scopeFactory.CreateScope();
                        var errorLogService = errorScope.ServiceProvider.GetRequiredService<IErrorLogService>();

                        await errorLogService.LogAsync(
                            ex,
                            module: "HRMS",
                            feature: "Leave Accrual",
                            controller: "LeaveAccrualService",
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
            var leaveBalanceService = scope.ServiceProvider.GetRequiredService<ILeaveBalanceService>();

            var today = DateTime.UtcNow.Date;

            await RunAccrualAsync(context, leaveBalanceService, today, ct);
            await RunCarryForwardAsync(context, leaveBalanceService, today, ct);
        }

        // =====================================================
        // a) ACCRUAL
        // =====================================================
        private async Task RunAccrualAsync(
            ApplicationDbContext context,
            ILeaveBalanceService leaveBalanceService,
            DateTime today,
            CancellationToken ct)
        {
            int year = today.Year;

            var leaveTypes = await context.LeaveTypes
                .Where(x =>
                    !x.IsDeleted &&
                    x.IsActive &&
                    x.AccrualFrequency != LeaveAccrualFrequency.None &&
                    x.AccrualDaysPerCycle > 0)
                .ToListAsync(ct);

            foreach (var leaveType in leaveTypes)
            {
                if (!IsAccrualCycleBoundary(leaveType.AccrualFrequency, today))
                    continue;

                var employeesQuery = context.Employees
                    .Where(x =>
                        !x.IsDeleted &&
                        x.IsActive &&
                        x.RelievingDate == null);

                // LeaveType.TenantId is nullable (shared/global master data
                // when null) - only scope to a tenant when the LeaveType
                // actually belongs to one, matching how the rest of this
                // codebase treats nullable TenantId on master data.
                if (!string.IsNullOrEmpty(leaveType.TenantId))
                    employeesQuery = employeesQuery.Where(x => x.TenantId == leaveType.TenantId);

                var employees = await employeesQuery.ToListAsync(ct);

                foreach (var employee in employees)
                {
                    try
                    {
                        int serviceDays = (today - ((DateTime)employee.JoiningDate).Date).Days;

                        if (serviceDays < leaveType.MinServiceDaysRequired)
                            continue;

                        // Idempotency guard: CreditLeaveAsync itself has no
                        // built-in duplicate protection (unlike
                        // CarryForwardLeaveAsync below), so skip if a Credit
                        // transaction already exists for this
                        // employee+leavetype today - covers app restarts
                        // re-triggering the same day's cycle.
                        bool alreadyCredited = await context.LeaveBalanceTransactions
                            .AnyAsync(x =>
                                x.EmployeeId == employee.Id &&
                                x.LeaveTypeId == leaveType.Id &&
                                x.TransactionType == LeaveTransactionType.Credit &&
                                x.TransactionDate.Date == today, ct);

                        if (alreadyCredited)
                            continue;

                        var balance = await context.LeaveBalances
                            .FirstOrDefaultAsync(x =>
                                x.EmployeeId == employee.Id &&
                                x.LeaveTypeId == leaveType.Id &&
                                x.Year == year, ct);

                        if (balance == null)
                        {
                            // No balance row for this employee/leave type/
                            // year yet (annual allocation - AllocateLeaveAsync
                            // - hasn't run for them). Accrual only tops up an
                            // existing balance; it deliberately does not
                            // create one out-of-band, so skip rather than
                            // guess an allocation.
                            _logger.LogInformation(
                                "Leave accrual skipped for employee {EmployeeId}, leave type {LeaveTypeId}: no {Year} balance row exists yet.",
                                employee.Id, leaveType.Id, year);

                            continue;
                        }

                        decimal cap = leaveType.MaxDaysPerYear;
                        decimal currentTotal = balance.Allocated + balance.Credited;
                        decimal headroom = cap - currentTotal;

                        if (headroom <= 0)
                            continue; // Already at/over the annual cap - credit 0, never throw.

                        decimal creditDays = Math.Min(leaveType.AccrualDaysPerCycle, headroom);

                        if (creditDays <= 0)
                            continue;

                        bool ok = await leaveBalanceService.CreditLeaveAsync(new LeaveAdjustmentRequestDto
                        {
                            EmployeeId = employee.Id,
                            LeaveTypeId = leaveType.Id,
                            Days = creditDays
                        });

                        if (!ok)
                        {
                            _logger.LogWarning(
                                "Leave accrual credit returned false for employee {EmployeeId}, leave type {LeaveTypeId}.",
                                employee.Id, leaveType.Id);
                        }
                    }
                    catch (Exception ex)
                    {
                        // One bad employee record must never stop the batch.
                        _logger.LogError(ex,
                            "Leave accrual failed for employee {EmployeeId}, leave type {LeaveTypeId}.",
                            employee.Id, leaveType.Id);
                    }
                }
            }
        }

        private static bool IsAccrualCycleBoundary(LeaveAccrualFrequency frequency, DateTime today)
        {
            // Deliberately simple/deterministic - "1st of the cycle" - no
            // calendar/holiday-shifting logic. If the app happens to be
            // down on the 1st, the per-transaction idempotency guard above
            // does NOT retroactively fire the missed cycle on a later day;
            // the next run just waits for the next boundary date. That's an
            // acceptable trade-off for this foundational phase (documented
            // as a known limitation below).
            return frequency switch
            {
                LeaveAccrualFrequency.Monthly => today.Day == 1,
                LeaveAccrualFrequency.Quarterly => today.Day == 1 &&
                    (today.Month == 1 || today.Month == 4 || today.Month == 7 || today.Month == 10),
                LeaveAccrualFrequency.Yearly => today.Day == 1 && today.Month == 1,
                _ => false
            };
        }

        // =====================================================
        // b) CARRY-FORWARD (January 1st only)
        // =====================================================
        private async Task RunCarryForwardAsync(
            ApplicationDbContext context,
            ILeaveBalanceService leaveBalanceService,
            DateTime today,
            CancellationToken ct)
        {
            if (today.Month != 1 || today.Day != 1)
                return;

            int previousYear = today.Year - 1;
            int currentYear = today.Year;

            // CarryForwardLeaveAsync loops over ALL of an employee's
            // AllowCarryForward=true balances for FromYear internally (see
            // Application/Services/Leaves/LeaveBalanceService.cs), so it is
            // called once per employee, not once per LeaveType.
            var employeeIds = await context.LeaveBalances
                .Where(x =>
                    x.Year == previousYear &&
                    !x.IsDeleted &&
                    x.LeaveType.AllowCarryForward &&
                    !x.LeaveType.IsDeleted)
                .Select(x => x.EmployeeId)
                .Distinct()
                .ToListAsync(ct);

            foreach (var employeeId in employeeIds)
            {
                try
                {
                    // CarryForwardLeaveAsync already has its own built-in
                    // idempotency: for each LeaveType it skips creating a
                    // new LeaveBalance row if one already exists for
                    // ToYear, so re-running it for the same employee/year
                    // within the same day (e.g. an app restart) cannot
                    // double-carry-forward. No extra guard needed here.
                    bool ok = await leaveBalanceService.CarryForwardLeaveAsync(new CarryForwardLeaveRequestDto
                    {
                        EmployeeId = employeeId,
                        FromYear = previousYear,
                        ToYear = currentYear
                    });

                    if (!ok)
                    {
                        _logger.LogWarning(
                            "Leave carry-forward returned false for employee {EmployeeId} ({FromYear} -> {ToYear}).",
                            employeeId, previousYear, currentYear);
                    }
                }
                catch (Exception ex)
                {
                    // One bad employee record must never stop the batch.
                    _logger.LogError(ex,
                        "Leave carry-forward failed for employee {EmployeeId} ({FromYear} -> {ToYear}).",
                        employeeId, previousYear, currentYear);
                }
            }
        }
    }
}
