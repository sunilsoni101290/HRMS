# Phase 18 - Performance Optimization

Scope: the Loan & Advance module as it stands after Phases 1-16 (Phase 17,
Testing, was explicitly deferred by the user's request and is not covered
here). Three areas were reviewed: database indexes, N+1 query patterns, and
tracking/read-query hygiene.

## 1. Database indexes

`Infrastructure/ApplicationDbContext.cs` (`OnModelCreating`) and the
companion script `docs/LoanAdvanceModule/18-Performance-Indexes.sql` add:

| Index | Table | Why |
|---|---|---|
| `IX_EmployeeLoans_Tenant_Status` | EmployeeLoans | Dashboard/report aggregates (Phase 14) filter by `TenantId` then `Status`; the existing `(EmployeeId, Status)` index doesn't serve a tenant-wide scan. |
| `IX_EmployeeAdvances_Tenant_Status` | EmployeeAdvances | Same reason, Advance side. |
| `IX_LoanPaymentHistories_PaymentDate` | LoanPaymentHistories | `GetPaymentHistoryReportAsync`'s tenant-wide date-range scan doesn't filter by `EmployeeLoanId`, so the per-loan composite index below doesn't help it. |
| `IX_AdvancePaymentHistories_PaymentDate` | AdvancePaymentHistories | Same, Advance side. |
| `IX_Notifications_Reference_CreatedOn` | Notifications | `LoanAdvanceReminderService.AlreadyNotifiedTodayAsync` (Phase 15) runs `ReferenceId IN (...) AND CreatedOn >= today` on every 6-hourly sweep, against a table that grows every day. |

While reviewing the original Phase 4 design script
(`04-SQL-Scripts.sql`) against the actual EF model, five indexes specified
there had never been carried into `ApplicationDbContext.cs`
(`IX_EmployeeLoans_Tenant_Company_Branch`,
`IX_EmployeeAdvances_Tenant_Company_Branch`,
`IX_LoanPaymentHistories_Loan_Date`,
`IX_AdvancePaymentHistories_Advance_Date`,
`IX_LoanApprovalHistories_Loan`, `IX_AdvanceApprovalHistories_Advance`) -
these were added back so the design doc and the running schema agree again.

No `dotnet ef` tooling is available in this environment, so these are
expressed as both EF Fluent API configuration (applies automatically on the
next `dotnet ef migrations add` + `database update` cycle) and a
guarded, idempotent raw SQL script for a database that's managed outside
migrations.

## 2. N+1 query fixes

Three genuine N+1 patterns were found and fixed, all in code that runs once
per tenant/user rather than once per record - the kind of query that looks
fine in dev with a handful of rows and degrades linearly as real data
accumulates:

- **`EmployeeLoanService.GetPendingOnMeAsync`** - previously resolved each
  candidate PendingApproval loan's approver set (2-5 queries per loan:
  primary-approver lookup by `ApproverType`, plus an active-delegate check
  per resolved primary) inside a `foreach`. Rewritten to batch-resolve
  reporting managers, role members, and active delegations across *all*
  candidates in a fixed ~4 queries total, independent of how many loans are
  pending. `ResolveLevelApproverUserIdsAsync` itself (used by
  `ApproveAsync`/`RejectAsync`/`ResolveCurrentApproverUserIdsAsync`, which
  only ever resolve *one* loan per call) was left untouched - no N+1 there.
- **`EmployeeAdvanceService.GetPendingOnMeAsync`** - same shape, simpler
  (single-level): batches the Reporting-Manager-to-User lookup into one
  query instead of two round trips per candidate advance.
- **`LoanAdvanceReminderService`** (`RemindLoanInstallmentsAsync` /
  `RemindAdvanceInstallmentsAsync`) - the Employee-to-User lookup used to
  resolve who to notify was inside the per-installment loop; batched into
  one dictionary built before the loop.

## 3. Read-query hygiene

- `Repository<T>.Query()` already defaults to `AsNoTracking()` (opt-in
  tracking only via `Query(asNoTracking: false)`, used deliberately at the
  handful of call sites that go on to mutate and `SaveChangesAsync`) - this
  was already correct across every Loan & Advance service and required no
  changes.
- Phase 14's `LoanReportService` and Phase 8's `LoanCalculationService`
  were re-checked for accidental tracked queries or repeated per-row
  round trips; both already batch-load before looping in C# (no additional
  N+1 found there).

## Deliberately not done

- **Response/output caching for LoanType/AdvanceType lookups** - considered,
  rejected: both tables are small per tenant (tens of rows), already
  `AsNoTracking`, and adding a cache layer risks serving a stale row right
  after an HR edit for very little query-cost benefit at this scale.
- **Server-side pagination on `GetAllAsync` (Loan/Advance/report lists)** -
  the UI already paginates client-side via DataTables (Phase 12); at
  realistic per-tenant volumes this is fine. Flagged as the first thing to
  revisit if a tenant's Loan/Advance history grows into the tens of
  thousands of rows - `GetAllAsync`'s `tenantId`/`status`/`employeeId`
  filters are already index-friendly per the table above, so adding
  `skip`/`take` later is additive, not a rework.
