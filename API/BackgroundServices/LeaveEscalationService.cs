using Application.Interfaces;
using Application.Interfaces.Communication;
using Application.Interfaces.ErrorLog;
using Application.Interfaces.Leaves;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using static Domain.Enums.EnumExtensions;

namespace API.BackgroundServices
{
    // Housekeeping sweep for Leave Applications that have been sitting
    // Pending at the same approval level too long.
    //
    // DESIGN DECISION - reminder + notification-widening, NOT auto-approval-
    // reassignment: after ReminderAfterHours, the current approver (and
    // their active out-of-office delegate, if any - see
    // ApprovalDelegation/IApprovalDelegationService) is reminded once. After
    // the longer EscalateAfterHours, the NEXT level approver is ALSO
    // notified as an FYI heads-up. Neither step ever changes
    // LeaveApplication.CurrentLevel/Status or reassigns approval authority -
    // a genuine "auto-escalate to the next level" (moving CurrentLevel
    // forward automatically, or letting the next level approve without the
    // current level ever acting) risks silently bypassing a manager's
    // legitimate authority over their own team's leave, which is a bigger
    // behavioral change than a background job should make unattended. A
    // human still has to actually act; this job only makes sure nobody
    // forgets, and that visibility widens the longer something sits
    // unresolved.
    //
    // Runs on a low-frequency timer (hourly) - this is housekeeping, not a
    // real-time channel. BackgroundService instances are singletons that run
    // outside any HTTP request scope, so every scoped dependency
    // (ApplicationDbContext, ILeaveApplicationService, INotificationService,
    // IEmailSender) must be resolved from a fresh IServiceScope per sweep
    // rather than injected directly into the constructor.
    public class LeaveEscalationService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LeaveEscalationService> _logger;
        private static readonly TimeSpan PollInterval = TimeSpan.FromHours(1);

        // Once a reminder/escalation notice has been sent for the current
        // pending state, don't repeat it more than once a day - avoids
        // spamming the approver every single hourly sweep once a threshold
        // has been crossed.
        private const double FollowUpIntervalHours = 24;

        public LeaveEscalationService(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            ILogger<LeaveEscalationService> logger)
        {
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Give the app (and DbSeeder.SeedAsync in Program.cs) a moment
            // to finish starting up before the first sweep.
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
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
                    // and try again on the next tick. Logged both ways: the
                    // ILogger line for local/console diagnostics, plus the
                    // shared ErrorLog table (via a fresh scope - the sweep's
                    // own scope is already gone by the time an exception
                    // unwinds up to here) so this shows up on the Error Log
                    // admin screen like every other logged failure in the
                    // app, not just in server logs nobody is watching.
                    _logger.LogError(ex, "Leave escalation sweep failed.");

                    try
                    {
                        using var errorScope = _scopeFactory.CreateScope();
                        var errorLogService = errorScope.ServiceProvider.GetRequiredService<IErrorLogService>();

                        await errorLogService.LogAsync(
                            ex,
                            module: "HRMS",
                            feature: "Leave Escalation",
                            controller: "LeaveEscalationService",
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
            double reminderAfterHours = _configuration.GetValue<double?>("LeaveEscalation:ReminderAfterHours") ?? 24;
            double escalateAfterHours = _configuration.GetValue<double?>("LeaveEscalation:EscalateAfterHours") ?? 72;

            using var scope = _scopeFactory.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var leaveService = scope.ServiceProvider.GetRequiredService<ILeaveApplicationService>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
            var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

            var now = DateTime.UtcNow;

            var stalePending = await context.LeaveApplications
                .Include(x => x.Employee)
                .Where(x => x.Status == ApprovalStatus.Pending)
                .ToListAsync(ct);

            foreach (var leave in stalePending)
            {
                var pendingSince = leave.ModifiedOn ?? leave.CreatedOn;
                var pendingHours = (now - pendingSince).TotalHours;

                if (pendingHours < reminderAfterHours)
                    continue;

                bool alreadyReminded = leave.LastReminderSentOn.HasValue;

                bool dueForFollowUp = leave.LastReminderSentOn.HasValue &&
                    (now - leave.LastReminderSentOn.Value).TotalHours >= FollowUpIntervalHours;

                if (!alreadyReminded)
                {
                    // First time crossing the reminder threshold for this
                    // pending state - notify the current approver(s).
                    await SendReminderAsync(leaveService, notificationService, emailSender, context, leave, isEscalationNotice: false);

                    leave.LastReminderSentOn = now;
                    await context.SaveChangesAsync(ct);

                    continue;
                }

                if (pendingHours >= escalateAfterHours && dueForFollowUp)
                {
                    // Long overdue and we already reminded once (at least a
                    // day ago) - widen visibility to the next level as an
                    // FYI, without touching CurrentLevel/Status.
                    await SendReminderAsync(leaveService, notificationService, emailSender, context, leave, isEscalationNotice: true);

                    leave.LastReminderSentOn = now;
                    await context.SaveChangesAsync(ct);
                }
            }
        }

        private async Task SendReminderAsync(
            ILeaveApplicationService leaveService,
            INotificationService notificationService,
            IEmailSender emailSender,
            ApplicationDbContext context,
            Domain.Entities.LeaveApplication leave,
            bool isEscalationNotice)
        {
            var applicantName = $"{leave.Employee?.FirstName} {leave.Employee?.LastName}".Trim();
            var levelName = GetLevelName(leave.CurrentLevel);

            // Reuses the exact same approver-resolution logic the live
            // workflow notifications use (LeaveApplicationService's
            // NotifyLevelAsync/ResolveApproverUserIdsAsync under the hood) -
            // so a delegate covering for the primary approver right now is
            // correctly included, never bypassed.
            var recipientUserIds = isEscalationNotice
                ? await leaveService.ResolveNextLevelApproverUserIdsAsync(leave.Id)
                : await leaveService.ResolveCurrentApproverUserIdsAsync(leave.Id);

            if (recipientUserIds == null || recipientUserIds.Count == 0)
            {
                _logger.LogInformation(
                    "Leave {LeaveApplicationId} is stale-pending at level {Level} but resolved to no recipient - skipping {Kind}.",
                    leave.Id, leave.CurrentLevel, isEscalationNotice ? "escalation notice" : "reminder");

                return;
            }

            string title = isEscalationNotice
                ? "Leave Request Overdue - Escalation Notice"
                : "Reminder: Leave Request Awaiting Your Approval";

            string message = isEscalationNotice
                ? $"{applicantName}'s leave from {leave.FromDate:dd-MMM-yyyy} to {leave.ToDate:dd-MMM-yyyy} ({leave.TotalDays} day(s)) has been awaiting {levelName} approval for an extended period. You are next in the approval chain - please follow up if needed."
                : $"{applicantName}'s leave from {leave.FromDate:dd-MMM-yyyy} to {leave.ToDate:dd-MMM-yyyy} ({leave.TotalDays} day(s)) is still awaiting your approval as {levelName}.";

            foreach (var userId in recipientUserIds.Where(x => !string.IsNullOrEmpty(x)).Distinct())
            {
                try
                {
                    await notificationService.CreateDirectAsync(
                        userId,
                        title,
                        message,
                        isEscalationNotice ? "Warning" : "Info",
                        redirectUrl: $"/LeaveApplication/Details/{leave.Id}",
                        featureId: "LEAVE",
                        referenceId: leave.Id,
                        tenantId: leave.TenantId,
                        createdBy: "System");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "In-app escalation notification failed for leave {LeaveApplicationId}, user {UserId}.",
                        leave.Id, userId);
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
                    _logger.LogWarning(ex,
                        "Email escalation notification failed for leave {LeaveApplicationId}, user {UserId}.",
                        leave.Id, userId);
                }
            }
        }

        private static string GetLevelName(int level) => level switch
        {
            1 => "Reporting Manager",
            2 => "Department Head",
            3 => "HR",
            _ => "Unknown"
        };
    }
}
