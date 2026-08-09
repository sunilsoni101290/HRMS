# Loan & Advance Module - Reference Documentation (Phase 19)

Consolidated reference for everything built in Phases 1-16 and 18 (Phase 17,
Unit & Integration Testing, is deferred and not covered here - see
"Known Limitations" below). Intended audience: a developer picking up this
module for the first time, or an HR/Finance admin who needs to know what a
given screen or permission actually does.

## 1. Architecture

Clean Architecture, four layers:

```
Domain          Entities, enums (Domain.Enums.EnumExtensions), AppFeatureConstants
   ^
Application     DTOs, Interfaces, Services (business logic, validation, orchestration)
   ^
Infrastructure  ApplicationDbContext, IUnitOfWork/IRepository<T>, IDManager,
                LoanAdvanceAuditInterceptor, DbSeeder
   ^
API / APP       API = JWT-secured REST controllers (Swagger-visible).
                APP = server-rendered MVC (Bootstrap 4 + jQuery + DataTables),
                talks to API only via IApiService (never touches EF/DbContext).
```

Project reference chain is one-directional: `Application -> Infrastructure -> Domain`
(Infrastructure has no reference back to Application). A generic
`IRepository<T>`/`IUnitOfWork` pair scoped to this module lives in
`Infrastructure.Interfaces`/`Infrastructure.Repositories` for that reason,
even though the rest of the codebase injects `ApplicationDbContext` directly.

`APP.Models.DTOs` mirrors `Application.DTOs.LoanAdvance` field-for-field
(same convention used by every other module in this codebase, e.g.
TaxDeclaration, ProbationConfirmation) even though `APP.csproj` references
`Application.csproj` directly - APP controllers only ever talk to the API
over HTTP via `IApiService`, never to Application types directly.

## 2. Domain Model

| Entity | Purpose |
|---|---|
| `LoanType` | Master: loan product definition (interest method/rate, max tenure, guarantor/collateral flags). |
| `AdvanceType` | Master: advance product definition (max amount, max installments, interest-free flag). |
| `LoanPolicy` + `LoanPolicyApprovalLevel` | Versioned policy per LoanType (+ optional Company/Branch scope) with a configurable N-level approval matrix. Editing a policy creates a NEW version rather than mutating the old one. |
| `EmployeeLoan` | One row spans the full lifecycle: request -> approval -> disbursement -> active -> closed. See state machine below. |
| `EmployeeAdvance` | Mirror of EmployeeLoan, no interest/EMI - single-level approval (Reporting Manager, or Finance override). |
| `LoanEmiSchedule` / `AdvanceInstallment` | Amortization/installment rows generated at disbursement. |
| `LoanPaymentHistory` / `AdvancePaymentHistory` | Immutable ledger of every recovery (payroll deduction, manual receipt, pre-closure lump sum). |
| `LoanApprovalHistory` / `AdvanceApprovalHistory` | Per-level Approve/Reject audit trail, including delegate-acting-for tracking. |
| `LoanAdvanceAttachment` | Uploaded supporting documents, polymorphic via `EntityType`/`EntityId`. |
| `LoanAdvanceAuditLog` | Append-only field-level audit trail, written automatically by `LoanAdvanceAuditInterceptor` (Phase 16). |

### State machines

**Loan** (`LoanStatus`): `Draft -> Submitted -> PendingApproval -> Approved -> Disbursed -> Active -> [PreClosureRequested] -> SettlementPending -> Closed`, with `Rejected`/`Cancelled` as terminal off-ramps from `PendingApproval`. `PendingApproval` can loop across multiple `CurrentApprovalLevel`s before reaching `Approved`.

**Advance** (`AdvanceStatus`): `Draft -> Submitted -> PendingApproval -> Approved -> Disbursed -> Recovered/Settled`, with `Rejected`/`Cancelled` off-ramps. Single approval level only (`CurrentApprovalLevel` is always 0 or 1).

**Installments** (`InstallmentStatus`, shared by both): `Pending -> Recovered`, or `Skipped` (payroll net pay insufficient), `Waived` (manual settlement), `Cancelled` (pre-closure/settlement cancels remaining Pending rows).

### Maker-Checker invariant

Enforced identically for both Loan and Advance: `actingUserId != MakerId` is checked BEFORE any permission check, with no override - not even for HR/Admin. See `EnsureCheckerIsNotMaker` in both services.

## 3. Permissions / Feature Matrix

Nine `AppFeature` rows under a `LOAN_ADVANCE_MANAGEMENT` parent (seeded in `DbSeeder.ReconcileModulesAsync`, Phase 10):

| Feature | Controller | Type | Notes |
|---|---|---|---|
| `LOAN_TYPE` | LoanType | Master | HR Manager: full CRUD. |
| `ADVANCE_TYPE` | AdvanceType | Master | HR Manager: full CRUD. |
| `LOAN_POLICY` | LoanPolicy | Master | HR Manager: full CRUD. |
| `EMPLOYEE_LOAN` | EmployeeLoan | Transaction | HR Manager: full + Approve ("Finance"). Employee: self-service View/Create only. `Approve` action on this SAME feature is what makes someone "Finance" for Disburse/Settle - no separate feature. |
| `EMPLOYEE_ADVANCE` | EmployeeAdvance | Transaction | Same dual grant shape as EMPLOYEE_LOAN. |
| `LOAN_ADVANCE_DASHBOARD` | LoanAdvanceDashboard | Dashboard | HR Manager: View only. |
| `LOAN_ADVANCE_REPORT` | LoanAdvanceReport | Report | HR Manager: View (+Export/Print where granted). |
| `LOAN_ADVANCE_AUDIT_LOG` | LoanAdvanceAuditLog | Report | HR Manager: View only. No dedicated "Auditor" role exists yet - Super Admin/System Configurator cover that gap via the full-access-roles loop. |

Super Admin and System Configurator roles receive every one of these
automatically. All permission checks follow the same `UserRole -> RolePermission -> Permission` join shown in every service's `HasPermissionAsync`/`EnsurePermissionAsync`.

## 4. API Reference (`API` project, JWT `[Authorize]`, base route `api/<controller>`)

### `api/loantype`, `api/advancetype` (master CRUD, identical shape)
| Method | Route | Notes |
|---|---|---|
| GET | `/` | `?includeInactive=false` |
| GET | `/{id}` | |
| POST | `/` | Create |
| PUT | `/{id}` | Update |
| DELETE | `/{id}` | Soft-delete; blocked if open (non-Closed/Rejected/Cancelled) loans/advances of this type exist. |

### `api/loanpolicy`
| Method | Route | Notes |
|---|---|---|
| GET | `/` | `?loanTypeId&companyId` |
| GET | `/{id}` | |
| POST | `/` | Create a new version (with `LoanPolicyApprovalLevel` rows). |
| PUT | `/{id}` | Also creates a new version - policies are never mutated in place. |
| POST | `/{id}/deactivate` | |

### `api/employeeloan`
| Method | Route | Notes |
|---|---|---|
| GET | `/` | `?status&employeeId` - self-scoped unless caller has View permission. |
| GET | `/{id}` | |
| GET | `/pending-on-me` | Batch-resolved, see Phase 18. |
| GET | `/eligibility` | Pre-submission affordability check. |
| POST | `/preview-emi` | Live, non-persisted EMI schedule preview. |
| POST | `/submit` | MAKER. |
| PUT | `/approve` | CHECKER, current level only. |
| PUT | `/reject` | CHECKER, current level only, remarks required. |
| POST | `/disburse` | Finance (Approve permission); generates the EMI schedule. |
| GET | `/{id}/pre-closure-quote` | Live-computed accrued interest + penalty. |
| POST | `/{id}/request-pre-closure` | Active -> PreClosureRequested. |
| POST | `/settle` | Finance; lump-sum settlement, cancels remaining Pending EMIs. |

### `api/employeeadvance`
Same shape as `api/employeeloan` minus `eligibility`/`preview-emi`/pre-closure (advances don't amortize): `GET /`, `GET /{id}`, `GET /pending-on-me`, `POST /submit`, `PUT /approve`, `PUT /reject`, `POST /disburse`, `POST /settle`.

### `api/loanadvanceattachment`
`GET /?entityType&entityId`, `POST /` (metadata only - actual file upload handled by the existing file-storage convention), `DELETE /{id}`.

### `api/loanadvancereport` (Phase 14)
`GET /dashboard`, `GET /outstanding-balance?departmentId`, `GET /payment-history?fromDate&toDate`.

### `api/loanadvanceauditlog` (Phase 16, read-only)
`GET /?entityType&entityId` (single-record drill-down), `GET /recent?entityType&take=200` (tenant-wide feed).

### `api/payrollloanrecovery` (Phase 8)
`POST /{payrollId}/recover` - manual re-trigger of the payroll recovery engine (normally invoked automatically by `PayrollBusinessService.GenerateAsync`).

## 5. UI Map (`APP` project, session-cookie `[JwtAuthorize]`)

| Controller | Views | Notes |
|---|---|---|
| `LoanType`, `AdvanceType` | Index, Create/Edit, Import | DataTables list, Excel import/export (Phase 13). |
| `LoanPolicy` | Index, Create/Edit | Dynamic JS-driven approval-matrix table, Company->Branch cascade. |
| `EmployeeLoan` | Index, PendingOnMe, Create, Details, SanctionLetter, Statement | Details has status-driven action buttons/modals (Approve/Reject/Disburse/RequestPreClosure/Settle) and an "Audit Trail" link (Phase 16). SanctionLetter/Statement are print-friendly (`Layout = null`) views, no PDF library dependency. |
| `EmployeeAdvance` | Index, PendingOnMe, Create, Details | Mirror of EmployeeLoan minus EMI-specific views. |
| `LoanAdvanceDashboard` | Index | KPI cards backed by `api/loanadvancereport/dashboard` + a "Recent Activity" panel from the plain list endpoints. |
| `LoanAdvanceReport` | Index | Outstanding Balance + Payment History tables, filters, Excel export. |
| `LoanAdvanceAuditLog` | Index, ForEntity | Tenant-wide feed with modal JSON diff viewer; per-record timeline. |

Select2 is deliberately NOT used anywhere in this module (confirmed absent
from the whole codebase) - dropdowns are plain Bootstrap `<select>` +
`SelectList`.

## 6. Business Logic Highlights

- **EMI calculation** (`LoanCalculationService`): standard reducing-balance formula `EMI = P x r x (1+r)^n / ((1+r)^n - 1)`, or flat-rate (`interest = P x rate% x months/12`, split evenly). Both handle the zero-interest edge case without dividing by zero.
- **Eligibility check**: policy min/max amount & tenure, active-loan-count cap, minimum service tenure, and a TRUE aggregate of every currently-Pending EMI/installment obligation across ALL loan types (not just the one being requested) compared against `MaxDeductionPercentOfNetSalary` of the latest payroll's net salary.
- **Pre-closure quote**: daily-accrued interest (`OutstandingPrincipal x dailyRate x daysSinceLastPayment`) from the later of the last Recovered installment or DisbursedOn, plus a policy-configured pre-closure penalty percentage.
- **Payroll recovery** (`PayrollLoanRecoveryService`, invoked automatically from `PayrollBusinessService.GenerateAsync`): idempotent per-employee sweep, caps deductions at the employee's `NetSalary`, writes payment history, auto-closes accounts at zero balance. Failures are logged and never block payroll generation.
- **Approval resolution**: Loan uses a configurable N-level matrix (`ReportingManager` / `SpecificRole` / `SpecificUser` per level, each with active-delegation fallback via `ApprovalDelegation`). Advance uses a single level (Reporting Manager, or a Finance override via Approve permission) - a deliberate simplification since the original schema never had a separate AdvancePolicy table.

## 7. Notifications (Phase 15)

In-app (`Notification`/`NotificationRecipient`) + best-effort email fan-out
at every workflow transition (Submit/Approve/Reject/Disburse/PreClosure
Request/Settle), following the same pattern as `LeaveApplicationService`.
Background reminders (`LoanAdvanceReminderService`, hosted service, 6-hour
sweep): stale `PendingApproval` nudges to the current approver, and
upcoming/overdue EMI & installment reminders to the employee - both capped
at once per calendar day via a Notifications-table de-dup check (no schema
change needed).

## 8. Audit Trail (Phase 16)

`LoanAdvanceAuditInterceptor` (an EF Core `SaveChangesInterceptor`,
registered per-request-scope in `API/Program.cs`) automatically writes a
`LoanAdvanceAuditLog` row for every Create/Update/Delete across all 13
watched entities - no service needs to call `ILoanAdvanceAuditLogService.LogAsync`
by hand. `PayrollLoanRecoveryService`'s existing manual business-summary
logging is intentionally kept alongside it (different, complementary
information - a generic field diff can't express "recovered X for
installment Y").

## 9. Configuration Reference (`API/appsettings.json`)

```jsonc
"Smtp": { "Host": "", ... }               // optional; blank Host = email no-ops (logged), never breaks a workflow
"LoanAdvanceReminder": {
  "ApprovalReminderAfterHours": 48,       // how long PendingApproval sits before the current approver is reminded
  "DueSoonDays": 3                        // how many days ahead of DueDate an "upcoming" EMI/installment reminder fires
}
```

## 10. Performance Notes (Phase 18)

See `docs/LoanAdvanceModule/18-Performance-Optimization.md` and
`18-Performance-Indexes.sql` for the full writeup: tenant-scoped indexes for
the Phase 14 dashboard/report queries, a `Notifications` index for the
Phase 15 reminder de-dup check, and three N+1 query fixes
(`GetPendingOnMeAsync` on both services, and the reminder sweep's
employee-to-user lookup).

## 11. Known Limitations / Deferred Items

- **Phase 17 (Unit & Integration Testing)** has not been done - explicitly
  deferred at the user's direction to do Phase 18/19 first. No automated
  test project exists yet for this module.
- **No PDF library** anywhere in this codebase; Sanction Letter / Statement
  use dependency-free print-friendly HTML views (`window.print()`) rather
  than server-generated PDFs, by explicit choice (see Phase 13) over the
  paid-above-$1M-revenue QuestPDF alternative.
- **Server-side pagination** is not implemented on `GetAllAsync` list
  endpoints - the UI paginates client-side via DataTables (Phase 12), which
  is adequate at realistic per-tenant volumes but is the first thing to
  revisit if a tenant's history grows very large (see Phase 18 doc).
- **No dedicated "Auditor" role** exists in this codebase - the Audit Log
  feature is granted to HR Manager plus the full-access roles (Super
  Admin/System Configurator) instead.
