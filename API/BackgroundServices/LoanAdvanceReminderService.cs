using Application.Interfaces;
using Application.Interfaces.Communication;
using Application.Interfaces.ErrorLog;
using Application.Interfaces.LoanAdvance;
using Domain.Helper;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using static Domain.Enums.EnumExtensions;

namespace API.BackgroundServices
{
    // Phase 15 - housekeeping sweep for the Loan & Advance module, mirroring
    // LeaveEscalationService's design exactly (see that file's remarks for
    // the full rationale): reminder-only, never auto-approves/reassigns/
    // auto-recovers anything - a human still has to act, this job just makes
    // sure nobody forgets.
    //
    // Two independent concerns, both best-effort and de-duplicated to at
    // most once per calendar day per target:
    //   1. Stale PendingApproval loans/advances - reminds the CURRENT
    //      approver(s) (reusing IEmployeeLoanService/IEmployeeAdvanceService.
    //      ResolveCurrentApproverUserIdsAsync, so a delegate covering right
    //      now is correctly included).
    //   2. Upcoming (due within DueSoonDays) and overdue Pending EMI/
    //      Advance installments - reminds the employee themselves.
    //
    // Neither EmployeeLoan/EmployeeAdvance nor LoanEmiSchedule/
    // AdvanceInstallment carries a "LastReminderSentOn" column (unlike
    // LeaveApplication), so instead of a schema change, de-duplication
    // checks the Notifications table directly for an existing row with the
    // same ReferenceId created since local midnight - cheap, self-contained,
    // and needs no migration.
    public class LoanAdvanceReminderService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LoanAdvanceReminderService> _logger;
        private static readonly TimeSpan PollInterval = TimeSpan.FromHours(6);

        public LoanAdvanceReminderService(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            ILogger<LoanAdvanceReminderService> logger)
        {
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
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
                    await RunSweepAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    // A failed sweep must never crash the host - just log
                    // and try again on the next tick. Also recorded in the
                    // shared ErrorLog table (fresh scope - the sweep's own
                    // scope is already gone by the time this runs) so it
                    // shows up on the Error Log admin screen, not just in
                    // server logs.
                    _logger.LogError(ex, "Loan & Advance reminder sweep failed.");

                    try
                    {
                        using var errorScope = _scopeFactory.CreateScope();
                        var errorLogService = errorScope.ServiceProvider.GetRequiredService<IErrorLogService>();

                        await errorLogService.LogAsync(
                            ex,
                            module: "HRMS",
                            feature: "Loan & Advance Reminders",
                            controller: "LoanAdvanceReminderService",
                            action: nameof(RunSweepAsync),
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

        private async Task RunSweepAsync(CancellationToken ct)
        {
            double approvalReminderAfterHours = _configuration.GetValue<double?>("LoanAdvanceReminder:ApprovalReminderAfterHours") ?? 48;
            int dueSoonDays = _configuration.GetValue<int?>("LoanAdvanceReminder:DueSoonDays") ?? 3;

            using var scope = _scopeFactory.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var loanService = scope.ServiceProvider.GetRequiredService<IEmployeeLoanService>();
            var advanceService = scope.ServiceProvider.GetRequiredService<IEmployeeAdvanceService>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
            var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

            var todayStart = DateTime.UtcNow.Date;

            await RemindStalePendingLoansAsync(context, loanService, notificationService, emailSender, approvalReminderAfterHours, todayStart, ct);
            await RemindStalePendingAdvancesAsync(context, advanceService, notificationService, emailSender, approvalReminderAfterHours, todayStart, ct);
            await RemindLoanInstallmentsAsync(context, notificationService, emailSender, dueSoonDays, todayStart, ct);
            await RemindAdvanceInstallmentsAsync(context, notificationService, emailSender, dueSoonDays, todayStart, ct);
        }

        #region Stale Pending Approvals

        private async Task RemindStalePendingLoansAsync(
            ApplicationDbContext context, IEmployeeLoanService loanService,
            INotificationService notificationService, IEmailSender emailSender,
            double reminderAfterHours, DateTime todayStart, CancellationToken ct)
        {
            var now = DateTime.UtcNow;

            var stalePending = await context.EmployeeLoans
                .Include(x => x.Employee)
                .Where(x => x.Status == LoanStatus.PendingApproval && !x.IsDeleted)
                .Where(x => (x.ModifiedOn ?? x.CreatedOn) <= now.AddHours(-reminderAfterHours))
                .ToListAsync(ct);

            if (stalePending.Count == 0)
                return;

            var alreadyRemindedIds = await AlreadyNotifiedTodayAsync(context, stalePending.Select(x => x.Id), todayStart, ct);

            foreach (var loan in stalePending)
            {
                if (alreadyRemindedIds.Contains(loan.Id))
                    continue;

                var recipients = await loanService.ResolveCurrentApproverUserIdsAsync(loan.Id);
                if (recipients.Count == 0)
                    continue;

                var applicantName = $"{loan.Employee?.FirstName} {loan.Employee?.LastName}".Trim();
                var title = "Reminder: Loan Request Awaiting Your Approval";
                var message = $"{applicantName}'s loan request of {loan.RequestedAmount:N0} has been awaiting your approval for over {reminderAfterHours:N0} hours.";

                await DispatchAsync(notificationService, emailSender, context, recipients, title, message, "Warning",
                    $"/EmployeeLoan/Details/{loan.Id}", AppFeatureConstants.EMPLOYEE_LOAN, loan.Id, loan.TenantId);
            }
        }

        private async Task RemindStalePendingAdvancesAsync(
            ApplicationDbContext context, IEmployeeAdvanceService advanceService,
            INotificationService notificationService, IEmailSender emailSender,
            double reminderAfterHours, DateTime todayStart, CancellationToken ct)
        {
            var now = DateTime.UtcNow;

            var stalePending = await context.EmployeeAdvances
                .Include(x => x.Employee)
                .Where(x => x.Status == AdvanceStatus.PendingApproval && !x.IsDeleted)
                .Where(x => (x.ModifiedOn ?? x.CreatedOn) <= now.AddHours(-reminderAfterHours))
                .ToListAsync(ct);

            if (stalePending.Count == 0)
                return;

            var alreadyRemindedIds = await AlreadyNotifiedTodayAsync(context, stalePending.Select(x => x.Id), todayStart, ct);

            foreach (var advance in stalePending)
            {
                if (alreadyRemindedIds.Contains(advance.Id))
                    continue;

                var recipients = await advanceService.ResolveCurrentApproverUserIdsAsync(advance.Id);
                if (recipients.Count == 0)
                    continue;

                var applicantName = $"{advance.Employee?.FirstName} {advance.Employee?.LastName}".Trim();
                var title = "Reminder: Advance Request Awaiting Your Approval";
                var message = $"{applicantName}'s advance request of {advance.RequestedAmount:N0} has been awaiting your approval for over {reminderAfterHours:N0} hours.";

                await DispatchAsync(notificationService, emailSender, context, recipients, title, message, "Warning",
                    $"/EmployeeAdvance/Details/{advance.Id}", AppFeatureConstants.EMPLOYEE_ADVANCE, advance.Id, advance.TenantId);
            }
        }

        #endregion

        #region Upcoming / Overdue Installments

        private async Task RemindLoanInstallmentsAsync(
            ApplicationDbContext context, INotificationService notificationService, IEmailSender emailSender,
            int dueSoonDays, DateTime todayStart, CancellationToken ct)
        {
            var horizon = todayStart.AddDays(dueSoonDays);

            var pending = await context.LoanEmiSchedules
                .Include(x => x.EmployeeLoan).ThenInclude(l => l.Employee)
                .Where(x => x.Status == InstallmentStatus.Pending && x.DueDate <= horizon)
                .ToListAsync(ct);

            if (pending.Count == 0)
                return;

            var alreadyRemindedIds = await AlreadyNotifiedTodayAsync(context, pending.Select(x => x.Id), todayStart, ct);

            // PHASE 18 - one batched Employee->User lookup instead of a
            // per-installment query (the original N+1: up to one Users
            // query per Pending EMI row on every 6-hourly sweep).
            var employeeIds = pending.Select(x => x.EmployeeLoan?.EmployeeId).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
            var employeeUserIdMap = employeeIds.Count == 0
                ? new Dictionary<string, string>()
                : await context.Users.Where(u => employeeIds.Contains(u.EmployeeId)).ToDictionaryAsync(u => u.EmployeeId, u => u.Id, ct);

            foreach (var emi in pending)
            {
                if (alreadyRemindedIds.Contains(emi.Id))
                    continue;

                var loan = emi.EmployeeLoan;
                if (loan == null) continue;

                if (!employeeUserIdMap.TryGetValue(loan.EmployeeId, out var employeeUserId) || string.IsNullOrEmpty(employeeUserId))
                    continue;

                var isOverdue = emi.DueDate < todayStart;
                var title = isOverdue ? "Loan EMI Overdue" : "Upcoming Loan EMI Due";
                var message = isOverdue
                    ? $"Your EMI of {emi.EmiAmount:N2} for installment #{emi.InstallmentNumber} was due on {emi.DueDate:dd-MMM-yyyy} and is still pending."
                    : $"Your EMI of {emi.EmiAmount:N2} for installment #{emi.InstallmentNumber} is due on {emi.DueDate:dd-MMM-yyyy}.";

                await DispatchAsync(notificationService, emailSender, context, new List<string> { employeeUserId },
                    title, message, isOverdue ? "Error" : "Info",
                    $"/EmployeeLoan/Details/{loan.Id}", AppFeatureConstants.EMPLOYEE_LOAN, emi.Id, loan.TenantId);
            }
        }

        private async Task RemindAdvanceInstallmentsAsync(
            ApplicationDbContext context, INotificationService notificationService, IEmailSender emailSender,
            int dueSoonDays, DateTime todayStart, CancellationToken ct)
        {
            var horizon = todayStart.AddDays(dueSoonDays);

            var pending = await context.AdvanceInstallments
                .Include(x => x.EmployeeAdvance).ThenInclude(a => a.Employee)
                .Where(x => x.Status == InstallmentStatus.Pending && x.DueDate <= horizon)
                .ToListAsync(ct);

            if (pending.Count == 0)
                return;

            var alreadyRemindedIds = await AlreadyNotifiedTodayAsync(context, pending.Select(x => x.Id), todayStart, ct);

            // PHASE 18 - same batched-lookup fix as RemindLoanInstallmentsAsync above.
            var employeeIds = pending.Select(x => x.EmployeeAdvance?.EmployeeId).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
            var employeeUserIdMap = employeeIds.Count == 0
                ? new Dictionary<string, string>()
                : await context.Users.Where(u => employeeIds.Contains(u.EmployeeId)).ToDictionaryAsync(u => u.EmployeeId, u => u.Id, ct);

            foreach (var installment in pending)
            {
                if (alreadyRemindedIds.Contains(installment.Id))
                    continue;

                var advance = installment.EmployeeAdvance;
                if (advance == null) continue;

                if (!employeeUserIdMap.TryGetValue(advance.EmployeeId, out var employeeUserId) || string.IsNullOrEmpty(employeeUserId))
                    continue;

                var isOverdue = installment.DueDate < todayStart;
                var title = isOverdue ? "Advance Installment Overdue" : "Upcoming Advance Installment Due";
                var message = isOverdue
                    ? $"Your advance installment of {installment.InstallmentAmount:N2} (#{installment.InstallmentNumber}) was due on {installment.DueDate:dd-MMM-yyyy} and is still pending."
                    : $"Your advance installment of {installment.InstallmentAmount:N2} (#{installment.InstallmentNumber}) is due on {installment.DueDate:dd-MMM-yyyy}.";

                await DispatchAsync(notificationService, emailSender, context, new List<string> { employeeUserId },
                    title, message, isOverdue ? "Error" : "Info",
                    $"/EmployeeAdvance/Details/{advance.Id}", AppFeatureConstants.EMPLOYEE_ADVANCE, installment.Id, advance.TenantId);
            }
        }

        #endregion

        #region Shared Helpers

        /// <summary>Reference.Ids (loan/advance/installment) that already have a Notification row created since local midnight - the once-per-day de-dup check.</summary>
        private static async Task<HashSet<string>> AlreadyNotifiedTodayAsync(ApplicationDbContext context, IEnumerable<string> referenceIds, DateTime todayStart, CancellationToken ct)
        {
            var ids = referenceIds.Distinct().ToList();
            if (ids.Count == 0)
                return new HashSet<string>();

            var sentRefIds = await context.Notifications
                .Where(n => !n.IsDeleted && n.CreatedOn >= todayStart && n.ReferenceId != null && ids.Contains(n.ReferenceId!))
                .Select(n => n.ReferenceId!)
                .ToListAsync(ct);

            return sentRefIds.ToHashSet();
        }

        private async Task DispatchAsync(
            INotificationService notificationService, IEmailSender emailSender, ApplicationDbContext context,
            List<string> userIds, string title, string message, string severity,
            string redirectUrl, string featureId, string referenceId, string? tenantId)
        {
            foreach (var userId in userIds.Where(x => !string.IsNullOrEmpty(x)).Distinct())
            {
                try
                {
                    await notificationService.CreateDirectAsync(
                        userId, title, message, severity,
                        redirectUrl: redirectUrl,
                        featureId: featureId,
                        referenceId: referenceId,
                        tenantId: tenantId,
                        createdBy: "System");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "In-app reminder failed for reference {ReferenceId}, user {UserId}.", referenceId, userId);
                }

                try
                {
                    var email = await context.Users
                        .Where(u => u.Id == userId)
                        .Select(u => u.Email)
                        .FirstOrDefaultAsync();

                    if (!string.IsNullOrWhiteSpace(email))
                        await emailSender.SendAsync(email, title, message);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Email reminder failed for reference {ReferenceId}, user {UserId}.", referenceId, userId);
                }
            }
        }

        #endregion
    }
}
