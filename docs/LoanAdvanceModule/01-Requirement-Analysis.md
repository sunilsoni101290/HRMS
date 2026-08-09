# Loan & Advance Module — Phase 1: Requirement Analysis

> Part of the HRMS ERP suite. This module plugs into the existing Clean
> Architecture solution (`Domain` / `Application` / `Infrastructure` / `API`
> / `APP`) and reuses existing masters (`Tenant`, `Company`, `Branch`,
> `Employee`, `Payroll`) and platform services (JWT auth, `SessionHelper`,
> `IApiService`, `AppFeature`/permission model, `NotificationService`,
> `SequenceService`) rather than re-inventing them.

## 1.1 Purpose

Give HR/Finance a controlled, auditable way to run employee **Loans**
(long-tenure, interest-bearing, EMI-recovered) and **Advances**
(short-term, typically interest-free, single or few-installment recovery)
from request through payroll recovery to closure — replacing manual
spreadsheet tracking.

## 1.2 Actors / Roles

| Role | Description | Typical Permission |
|---|---|---|
| **Employee** | Raises a Loan/Advance request, views own outstanding balance, payment history, uploads supporting attachments. | `LoanRequest.Create/View(own)` |
| **Reporting Manager** | First-level approver (Level 1) for their direct reports' requests. | `LoanRequest.Approve(L1)` |
| **HR Admin** | Configures Loan/Advance Type masters and Loan Policy; acts as an approval level (typically L2); can view/act on all requests in their Company/Branch scope. | `LoanType.*`, `LoanPolicy.*`, `LoanRequest.Approve(L2)` |
| **Finance/Payroll Admin** | Final approver where required, triggers Disbursement, monitors payroll deduction batches, performs settlement/pre-closure, reconciles outstanding balances. | `LoanDisbursement.*`, `PayrollRecovery.*`, `LoanSettlement.*` |
| **Maker (any role above with Create rights)** | Proposes an action (request, disbursement, settlement). | — |
| **Checker (any role above with Approve rights, ≠ Maker)** | Approves/Rejects a Maker's proposal at their assigned level. | — |
| **Auditor / Super Admin** | Read-only access to all records, audit trail, reports across tenants (support/compliance). | `LoanAudit.View` |

Roles map onto the existing `Role` / `Permission` / `RolePermission` /
`AppFeature` tables — no new auth model is introduced (see Phase 10).

## 1.3 Functional Requirements

### FR-1 Loan Type Master
- CRUD for Loan Types (e.g. Personal Loan, Vehicle Loan, Emergency Loan).
- Fields: Code, Name, Description, Default Interest Rate %, Interest Method
  (Flat/Reducing), Max Tenure (months), Requires Collateral/Guarantor flag,
  Is Active.
- Soft delete only (`IsDeleted`), never hard delete once referenced by a
  request.

### FR-2 Advance Type Master
- CRUD for Advance Types (e.g. Salary Advance, Festival Advance, Medical
  Advance).
- Fields: Code, Name, Description, Max Amount (flat or × of gross salary),
  Max Installments, Is Interest Free, Is Active.

### FR-3 Loan Policy
- Company/Branch (or Tenant-wide) scoped policy per Loan Type:
  Min/Max amount, Min/Max tenure, Interest rate override, Max active
  loans per employee, Min tenure-of-service eligibility (months),
  Max % of net salary deductible (EMI + existing deductions), Eligibility
  formula (multiple of gross/net salary), Approval Matrix reference
  (which levels apply, in what order, amount-based thresholds).
- Versioned — a Policy change must not silently alter EMI schedules
  already generated for approved/disbursed loans.

### FR-4 Employee Loan Request
- Employee selects Loan Type, requested amount, tenure, purpose, uploads
  attachments; system shows live eligibility (Phase 8) before submit.
- Draft → Submitted → (multi-level approval) → Approved/Rejected →
  Disbursed → Active → Closed/Settled/Foreclosed.

### FR-5 Employee Advance Request
- Simpler lifecycle: Draft → Submitted → Approved/Rejected → Disbursed →
  Recovered (single/few deductions) → Settled.

### FR-6 Multi-Level Approval Workflow
- Configurable N sequential levels, resolved at submit-time from the
  Loan Policy's Approval Matrix (can vary by amount threshold, e.g.
  ≤ ₹50,000 = 2 levels, > ₹50,000 = 3 levels).
- Maker-Checker invariant reused from `ProbationConfirmation`: the
  acting Checker's `User.Id` must never equal the Maker's, at every
  level. Any level's Reject stops the workflow (terminal Rejected state)
  with mandatory remarks.
- Delegation: reuses existing `ApprovalDelegation` entity/service so an
  approver on leave can delegate.

### FR-7 Loan Disbursement
- Finance marks an Approved loan as Disbursed (full or, for large loans,
  tranche-based), captures disbursement date, mode (Bank Transfer/
  Cheque/Payroll credit), reference number, generates the EMI Schedule.

### FR-8 EMI Schedule
- Auto-generated reducing-balance (default; flat supported per Loan
  Policy config, per clarified requirement) amortization schedule:
  Installment #, Due Date (aligned to payroll cycle), Principal
  component, Interest component, Total EMI, Opening/Closing Balance,
  Status (Pending/Recovered/Skipped/Waived).
- Recalculable on Pre-Closure/part-payment.

### FR-9 Payroll Deduction
- Each payroll run picks up all `Pending`, due-this-cycle EMI/Advance
  installments for the employee, deducts via a `LoanDeductionComponent`
  wired into the existing `SalaryComponent`/`PayrollDetail` calculation,
  marks the installment `Recovered`, and posts a `LoanPaymentHistory`
  row. Failed/skipped deductions (e.g. insufficient net pay) roll to
  next cycle with an audit note, never silently dropped.

### FR-10 Interest Calculation
- Reducing-balance interest engine (Phase 8) is the system default;
  flat-rate supported as a per-Loan-Type policy switch. Must be unit
  tested against hand-calculated amortization tables (Phase 17).

### FR-11 Loan Pre-Closure
- Employee/Finance can request full pre-closure at any time; system
  computes outstanding principal + accrued interest to date (+
  optional pre-closure penalty from Policy), employee pays/deducts the
  lump sum, remaining EMIs are cancelled, loan moves to `Closed`.

### FR-12 Loan Settlement
- Covers pre-closure settlement above **and** the full-and-final /
  resignation scenario: on employee exit, any outstanding loan balance
  is settled against F&F dues (integration point flagged, F&F engine
  itself out of scope — see §1.6) or written off with approval.

### FR-13 Advance Settlement
- Marks an Advance fully recovered (all installments deducted or a
  manual settlement entry), closes the Advance.

### FR-14 Outstanding Balance
- Real-time view per employee / per loan/advance: principal
  disbursed, principal paid, interest paid, outstanding principal,
  next due date & amount, days-past-due if any.

### FR-15 Payment History
- Full ledger of every recovery (payroll-deducted, manual, or
  pre-closure), with source (Payroll Run ID / Manual receipt).

### FR-16 Attachments
- Generic attachment support (ID proof, purpose justification,
  quotation, medical bill, etc.) on Request, reused across Loan and
  Advance, stored the same way `EmployeeDocument` already is (path +
  metadata row), with size/type validation.

### FR-17 Remarks
- Free-text remarks at every workflow transition (Maker remark on
  submit, Checker remark on approve/reject, Finance remark on
  disbursement/settlement) — all persisted, never overwritten, shown
  as a timeline on Details.

### FR-18 Audit Logs
- Every create/update/status-transition on every Loan/Advance-module
  entity is written to a shared `LoanAuditLog` (who, when, entity,
  action, before/after snapshot) — see Phase 16.

### FR-19 Reports & Dashboard
- HR/Finance dashboard: total disbursed, total outstanding, overdue
  count, this-month recoveries, approvals pending on me. Drill-down
  reports: Loan Register, Advance Register, EMI Due list (next payroll
  cycle), Employee-wise Outstanding, Aging/overdue.

## 1.4 Non-Functional Requirements

| Category | Requirement |
|---|---|
| **Multi-tenancy** | Every table carries `TenantId` (+ `CompanyId`/`BranchId` where applicable) exactly like existing entities; all queries scoped via `ITenantService`, same as `ApplicationDbContext` already enforces. |
| **Security** | JWT-authenticated APIs (`[JwtAuthorize]`), role/permission-gated actions, no direct DB access from `APP` layer (always via `IApiService` → API), passwords/PII never logged. |
| **Auditability** | Immutable audit trail (Phase 16); Maker ≠ Checker enforced server-side, never trusted from client. |
| **Data integrity** | EMI schedule and interest figures computed server-side only; client never sends computed amounts that are trusted as-is. |
| **Performance** | List endpoints paginated + indexed (Phase 3/18); dashboard aggregates cached short-TTL. |
| **Availability/Resilience** | Payroll deduction batch must be idempotent and re-runnable without double-deducting (Phase 8/18). |
| **Usability** | Bootstrap 5 responsive UI, works down to tablet width; DataTables + Select2 consistent with rest of HRMS (Phase 11). |
| **Extensibility** | Interest method, approval levels, and eligibility formula are policy-driven, not hard-coded, so new Loan Types don't need code changes. |
| **Testability** | Interest/EMI/eligibility engines are pure, side-effect-free domain services — unit-testable without DB (Phase 17). |

## 1.5 Assumptions

1. Reuses existing `Employee`, `Company`, `Branch`, `Tenant`, `Payroll`,
   `PayrollDetail`, `SalaryComponent`, `User`, `Role`, `AppFeature`,
   `ApprovalDelegation`, `NotificationService`, `SequenceService`
   entities/services as-is (no breaking changes to them).
2. Currency is single-currency per tenant (existing system has no
   multi-currency concept); amounts stored as `decimal(18,2)`.
3. Interest: **Reducing balance** is the default/system method; **Flat**
   is supported as a per-Loan-Type configurable alternative (per
   confirmed scope).
4. Approval: **Configurable N-level** workflow driven by an Approval
   Matrix on the Loan Policy (amount-threshold based), built on top of
   the existing Maker-Checker primitive (`ProbationConfirmation`
   pattern) rather than a new engine.
5. Payroll integration is via a new `LoanDeductionComponent` hook read
   by `PayrollBusinessService` during payroll run — the payroll run
   engine itself is not being rewritten.

## 1.6 Out of Scope (this phase)

- Direct bank disbursement API/payment gateway integration (disbursement
  is recorded, not executed, by this module).
- Multi-currency loans.
- Full-and-final settlement engine itself (only the *hook* — "outstanding
  loan balance to recover at exit" — is exposed for it to consume).
- Statutory/tax treatment of interest-free advances (perquisite tax) —
  flagged as a future enhancement, not calculated in v1.

---
**Next:** Phase 2 — Business Flow & Use Cases.
