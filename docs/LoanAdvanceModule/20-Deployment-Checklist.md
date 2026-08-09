# Loan & Advance Module - Deployment Checklist (Phase 20)

Final phase of the 20-phase plan. This module ships as part of the main
HRMS API + APP deployment (no separate service/container) - this checklist
covers the module-specific steps on top of whatever the team's normal HRMS
release process already does. **Phase 17 (Unit & Integration Testing) was
explicitly deferred and has not been done** - treat that as an open risk
against this checklist, not an oversight.

This codebase manages its schema via manually-run, idempotent SQL scripts,
NOT `dotnet ef database update` - confirmed by `API/Program.cs` calling
neither `Database.Migrate()` nor `Database.EnsureCreated()`, and by the
single `Migrations/20260804154741_InitialMigration.cs` predating (and not
containing) any Loan & Advance table. Every script below already guards
itself with `IF NOT EXISTS`, so re-running a script that's already been
applied is always safe.

## 1. Database changes (run in this order, once per environment)

- [ ] `docs/LoanAdvanceModule/04-SQL-Scripts.sql` - full Phase 3/4 schema
      (13 tables + their original indexes/constraints). Skip if this
      environment's database already has these tables (check for
      `dbo.EmployeeLoans`).
- [ ] `docs/LoanAdvanceModule/18-Performance-Indexes.sql` - the Phase 18
      performance indexes (`IX_EmployeeLoans_Tenant_Status`,
      `IX_Notifications_Reference_CreatedOn`, etc.) plus the five indexes
      reconciled back from the original Phase 4 design. Safe to run even if
      some/all of these already exist.
- [ ] Confirm no other in-flight schema script from this codebase's root
      (e.g. `add ... column ....sql` files) targets a table this module's
      FKs depend on (`Employees`, `Users`, `Payrolls`, `Companies`,
      `Branches`, `Tenants`) - run those first if so.
- [ ] Take a schema/data backup before running any script against a
      production database, per your normal DB-change process.

## 2. Configuration (`API/appsettings.json` or environment-specific overrides)

- [ ] `ConnectionStrings:ERPConnection` points at the target database.
- [ ] `Smtp` section - set `Host`/`Port`/credentials if email notifications
      should actually send in this environment. Leaving `Host` blank is
      valid and intentional for environments without SMTP access - email
      becomes a silent no-op (logged), never a failure; the in-app
      Notification is the channel that must work.
- [ ] `LoanAdvanceReminder` section - defaults are
      `ApprovalReminderAfterHours: 48`, `DueSoonDays: 3`. Adjust only if
      the business wants a different reminder cadence; both are read live
      from config on every sweep (no restart needed to change them again
      later).
- [ ] `Jwt` section (Key/Issuer/Audience/expiry) matches what APP expects
      for the session-cookie JWT it forwards - shared with the rest of the
      HRMS API, not module-specific, but confirm it's set for this
      environment.

## 3. Application startup - what happens automatically (no manual step)

- [ ] `DbSeeder.ReconcileModulesAsync` / `ReconcilePermissionsAsync` run on
      every startup (idempotent upsert-by-Code) - this is what seeds/
      updates the 9 `LOAN_ADVANCE_*` `AppFeature` rows and their
      `RolePermission` grants (HR Manager, Employee self-service, full-
      access roles). No manual permission-seeding step is required; just
      confirm the deploy actually restarted the API process so this ran.
- [ ] `LoanAdvanceReminderService` (hosted service) starts automatically,
      waits 2 minutes, then sweeps every 6 hours - no manual start needed.
- [ ] `LoanAdvanceAuditInterceptor` is wired into every
      `ApplicationDbContext` instance automatically via `AddDbContext`'s
      options builder - no manual step.

## 4. Post-deploy permission spot-check

- [ ] Log in as a Super Admin / System Configurator user - confirm the
      "Loan & Advance Management" menu group appears with all 8 child
      items (Loan Type, Advance Type, Loan Policy, Employee Loan, Employee
      Advance, Dashboard, Reports, Audit Log).
- [ ] Log in as an HR Manager - confirm the same menu appears (minus
      anything HR Manager isn't granted - none currently, HR Manager has
      full access across this module).
- [ ] Log in as a plain Employee - confirm they see "Employee Loan"/
      "Employee Advance" (self-service, Create + View own only) and NOT
      the masters (Loan Type/Advance Type/Loan Policy) or Reports/Audit
      Log/Dashboard.

## 5. Functional smoke test (run once per environment, in order)

- [ ] Create a `LoanType` and an `AdvanceType`.
- [ ] Create a `LoanPolicy` for that Loan Type with at least one approval
      level; confirm the Company->Branch cascade dropdown works.
- [ ] As an employee, submit a loan request; confirm the eligibility check
      and live EMI preview both respond on the Create form.
- [ ] Confirm the resolved approver receives an in-app notification (bell
      icon) and, if SMTP is configured, an email.
- [ ] Approve the loan (as a different user than the maker - confirm the
      maker-cannot-approve-own-request guard actually blocks self-
      approval if you try it).
- [ ] Disburse the loan; confirm the EMI schedule renders on Details and
      the Sanction Letter / Statement print views open correctly (no PDF
      library involved - these are `window.print()`-based HTML views).
- [ ] Repeat submit -> approve -> disburse for an Advance request (single-
      level approval, no EMI schedule - flat installments instead).
- [ ] Run (or wait for) a Payroll generation cycle for an employee with an
      Active loan/advance; confirm `PayrollLoanRecoveryService` recovered
      the due installment(s) and `OutstandingPrincipal`/`OutstandingAmount`
      decreased accordingly. `POST api/payrollloanrecovery/{payrollId}/recover`
      can force this manually if you don't want to wait for a real cycle.
- [ ] Open the Loan & Advance Dashboard - confirm the KPI cards render
      real numbers (not zeros unless genuinely empty).
- [ ] Open Loan & Advance Reports - confirm Outstanding Balance and
      Payment History both populate and Excel export downloads correctly
      for each.
- [ ] Open the Audit Log page - confirm the Create/Approve/Disburse
      actions just performed above all appear as rows with correct
      before/after JSON in the diff viewer.
- [ ] Excel Import/Export on Loan Type and Advance Type masters - export,
      edit, re-import, confirm the `_ExcelImportResult` partial reports
      success/failure per row correctly.

## 6. Background job verification

- [ ] Check application logs ~2-3 minutes after startup for the first
      `LoanAdvanceReminderService` sweep (no errors logged is success -
      it's silent when there's nothing to remind).
- [ ] If a loan/advance has been sitting `PendingApproval` for longer than
      `ApprovalReminderAfterHours`, confirm a reminder notification/email
      actually fires on the next sweep (or temporarily lower the config
      value in a non-prod environment to test faster).
- [ ] Confirm a sweep failure would be caught and logged rather than
      crashing the host - `RunSweepAsync` is wrapped in try/catch inside
      `ExecuteAsync`'s loop by design; no action needed, just know this is
      the expected behavior if you see an error logged (the job retries on
      its next tick, not immediately).

## 7. Rollback plan

- [ ] All Phase 20 database changes are strictly additive (new tables,
      new indexes) - there is no destructive migration to reverse. Rolling
      back the application code to a pre-module version is safe with the
      new schema still in place (the extra tables/indexes are simply
      unused by older code).
- [ ] If a specific new index is suspected of causing a write-performance
      regression (unlikely at this table size, but checkable), it can be
      dropped individually (`DROP INDEX <name> ON <table>`) without
      affecting correctness - only read performance.
- [ ] If `LoanAdvanceReminderService` needs to be disabled without a full
      redeploy, comment out its `AddHostedService` registration in
      `API/Program.cs` and redeploy - notifications on workflow
      transitions (Submit/Approve/Reject/...) are unaffected either way,
      since those are wired directly into the services, not the
      background job.

## 8. Known risks carried into this deployment

- **No automated test coverage** (Phase 17 deferred) - the smoke test in
  section 5 is the only verification this module has had beyond manual
  code review across Phases 1-16/18.
- **Schema managed by hand-run SQL scripts**, not EF migrations - a future
  contributor adding fields via `dotnet ef migrations add` against this
  `DbContext` will generate a migration that doesn't match the real
  database history; keep using the SQL-script convention for this module
  until/unless the whole codebase moves to migration-managed schema.
- **No dedicated "Auditor" role** - Audit Log access is HR Manager +
  full-access roles only; revisit if a real segregated audit function is
  needed later.
