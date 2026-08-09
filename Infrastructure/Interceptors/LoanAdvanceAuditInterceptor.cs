using System.Text.Json;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Infrastructure.Interceptors
{
    /// <summary>
    /// Phase 16 - automatic, append-only audit trail for every tracked
    /// Loan &amp; Advance entity change (Create/Update/Delete), so individual
    /// service methods (EmployeeLoanService, EmployeeAdvanceService,
    /// LoanTypeService, ...) never need to call
    /// ILoanAdvanceAuditLogService.LogAsync by hand - see that interface's
    /// Phase 6/16 remarks. PayrollLoanRecoveryService's existing manual
    /// LogAsync calls are left as-is (they log a business-meaning summary -
    /// "recovered X for installment Y" - that this generic before/after
    /// field diff can't express), so a payroll recovery cycle now produces
    /// BOTH a hand-written business-summary row and this interceptor's
    /// automatic per-field-diff row for the same EmployeeLoan/EmployeeAdvance
    /// update; that overlap is intentional, not a bug.
    ///
    /// Design: EF Core interceptors are the only place in a Repository/
    /// UnitOfWork architecture that sees every SaveChanges call regardless
    /// of which service triggered it, without threading an
    /// ILoanAdvanceAuditLogService dependency through every single
    /// Loan/Advance service. Registered per-DbContext-scope (one instance
    /// per AddDbContext options build = one per request scope, see
    /// API/Program.cs), so the between-hooks `_pending` field is never
    /// shared across concurrent requests.
    /// </summary>
    public class LoanAdvanceAuditInterceptor : SaveChangesInterceptor
    {
        private static readonly HashSet<Type> WatchedTypes = new()
        {
            typeof(EmployeeLoan),
            typeof(EmployeeAdvance),
            typeof(LoanType),
            typeof(AdvanceType),
            typeof(LoanPolicy),
            typeof(LoanPolicyApprovalLevel),
            typeof(LoanEmiSchedule),
            typeof(AdvanceInstallment),
            typeof(LoanPaymentHistory),
            typeof(AdvancePaymentHistory),
            typeof(LoanApprovalHistory),
            typeof(AdvanceApprovalHistory),
            typeof(LoanAdvanceAttachment)
        };

        private List<PendingAuditEntry>? _pending;

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            Capture(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            Capture(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
        {
            FlushAsync(eventData.Context).GetAwaiter().GetResult();
            return base.SavedChanges(eventData, result);
        }

        public override async ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            await FlushAsync(eventData.Context, cancellationToken);
            return await base.SavedChangesAsync(eventData, result, cancellationToken);
        }

        /// <summary>
        /// Snapshots every watched, actually-changed entry BEFORE SaveChanges
        /// commits (original/current values are only meaningful pre-commit -
        /// after SaveChangesAsync, EF resets ChangeTracker state and
        /// OriginalValues would no longer reflect the pre-save row).
        /// </summary>
        private void Capture(DbContext? context)
        {
            _pending = null;

            if (context == null)
                return;

            var entries = context.ChangeTracker.Entries()
                .Where(e => WatchedTypes.Contains(e.Entity.GetType()) &&
                            (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted))
                .ToList();

            if (entries.Count == 0)
                return;

            var list = new List<PendingAuditEntry>();

            foreach (var entry in entries)
            {
                // A Modified entry with no actually-changed scalar property
                // (e.g. only a navigation collection was touched) would
                // otherwise produce a noisy, content-free audit row.
                if (entry.State == EntityState.Modified && !entry.Properties.Any(p => p.IsModified))
                    continue;

                var idValue = entry.Property("Id").CurrentValue as string;
                if (string.IsNullOrEmpty(idValue))
                    continue;

                var tenantId = entry.Property("TenantId").CurrentValue as string;
                if (string.IsNullOrEmpty(tenantId))
                    continue; // LoanAdvanceAuditLog.TenantId is required - skip the rare row with none.

                var performedBy = entry.State == EntityState.Added
                    ? entry.Property("CreatedBy").CurrentValue as string
                    : (entry.Property("ModifiedBy").CurrentValue as string) ?? (entry.Property("CreatedBy").CurrentValue as string);

                var action = entry.State switch
                {
                    EntityState.Added => "Create",
                    EntityState.Deleted => "Delete",
                    _ => "Update"
                };

                var oldJson = entry.State != EntityState.Added ? SerializeValues(entry.OriginalValues) : null;
                var newJson = entry.State != EntityState.Deleted ? SerializeValues(entry.CurrentValues) : null;

                list.Add(new PendingAuditEntry(
                    entry.Entity.GetType().Name, idValue, action, oldJson, newJson, tenantId, performedBy ?? "System"));
            }

            _pending = list.Count > 0 ? list : null;
        }

        /// <summary>
        /// Writes the captured rows via a SECOND SaveChanges after the
        /// caller's own save has already succeeded - never in the same
        /// save, so a business transaction's success is never blocked by
        /// (or rolled back for) an audit-write problem. This second save
        /// re-enters the interceptor, but LoanAdvanceAuditLog isn't in
        /// WatchedTypes, so Capture() finds nothing and it returns
        /// immediately - no infinite recursion.
        /// </summary>
        private async Task FlushAsync(DbContext? context, CancellationToken ct = default)
        {
            var pending = _pending;
            _pending = null;

            if (pending == null || pending.Count == 0 || context == null)
                return;

            var rows = pending.Select(p => new LoanAdvanceAuditLog
            {
                Id = IDManager.GetNewId(new LoanAdvanceAuditLog()),
                EntityType = p.EntityType,
                EntityId = p.EntityId,
                Action = p.Action,
                OldValuesJson = p.OldValuesJson,
                NewValuesJson = p.NewValuesJson,
                PerformedBy = p.PerformedBy,
                PerformedOn = DateTime.UtcNow,
                TenantId = p.TenantId!
            }).ToList();

            try
            {
                context.Set<LoanAdvanceAuditLog>().AddRange(rows);
                await context.SaveChangesAsync(ct);
            }
            catch
            {
                // Never let audit persistence break the caller's already-
                // succeeded business transaction - same "logging must never
                // break the app" convention as
                // LoanAdvanceAuditLogService.LogAsync's own try/catch.
            }
        }

        private static string SerializeValues(PropertyValues values)
        {
            var dict = new Dictionary<string, object?>();
            foreach (var prop in values.Properties)
                dict[prop.Name] = values[prop];

            return JsonSerializer.Serialize(dict);
        }

        private sealed record PendingAuditEntry(
            string EntityType, string EntityId, string Action,
            string? OldValuesJson, string? NewValuesJson, string? TenantId, string PerformedBy);
    }
}
