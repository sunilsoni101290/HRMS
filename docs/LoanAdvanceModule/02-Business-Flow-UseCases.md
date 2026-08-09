# Loan & Advance Module — Phase 2: Business Flow & Use Cases

## 2.1 Loan Lifecycle — State Machine

```
Draft ──submit──▶ Submitted ──▶ [Approval Matrix: L1..Ln]
                                     │
                     ┌───────────────┼───────────────┐
                     ▼ (any level)   ▼ (all levels)   
                 Rejected         Approved
                     │                │
                     │            disburse (Finance)
                     │                ▼
                     │            Disbursed ──generate EMI Schedule──▶ Active
                     │                                                   │
                     │                       ┌───────────────────────────┼───────────────────────┐
                     │                       ▼ (EMI paid each cycle)     ▼ (pre-closure request)  ▼ (employee exits)
                     │                  Active (until 0 balance)     PreClosureRequested       SettlementPending
                     │                       │                            │                        │
                     │                       ▼                            ▼ (approve + pay lump)   ▼ (approve + settle vs F&F / write-off)
                     │                    Closed  ◀───────────────────────┴────────────────────────┘
                     ▼
                (terminal)
```

States: `Draft, Submitted, PendingApprovalL{n}, Approved, Rejected,
Disbursed, Active, PreClosureRequested, SettlementPending, Closed,
Foreclosed, Cancelled`.

## 2.2 Advance Lifecycle — State Machine

```
Draft ──submit──▶ Submitted ──approve/reject──▶ Approved / Rejected
                                                    │
                                                disburse
                                                    ▼
                                                Disbursed
                                                    │
                                     payroll deducts installment(s)
                                                    ▼
                                        Recovered ─▶ Settled
```

Advances skip EMI amortization — they use a flat installment split
(amount ÷ N installments, no interest by default) unless the Advance
Type is marked interest-bearing.

## 2.3 Multi-Level Approval Flow (sequence)

```
Employee (Maker)
   │ submit request
   ▼
System resolves Approval Matrix from Loan Policy
   (amount → level count, e.g. ≤50k = [Manager, HR], >50k = [Manager, HR, Finance])
   │
   ▼
Level 1 approver notified ──reject──▶ Status=Rejected, notify employee, END
   │ approve (remarks)
   ▼
Level 2 approver notified ──reject──▶ Status=Rejected, END
   │ approve
   ▼
   ... Level N ...
   │ approve (final level)
   ▼
Status = Approved → Finance notified → Disbursement
```

Guard rails (server-enforced, mirrors `ProbationConfirmation`):
- A given level's Checker `User.Id` must not equal the Maker's `User.Id`.
- A user cannot approve the same level twice; re-submission after a
  Reject restarts the matrix from Level 1.
- If the assigned approver has an active `ApprovalDelegation`, the
  delegate can act in their place (audit log records the delegate).

## 2.4 Payroll Recovery Flow

```
Payroll Run starts for Company/Branch, Period P
   │
   ▼
For each Employee with Active Loan(s)/Advance(s):
   fetch EMI/Installment rows where DueDate ∈ Period P and Status=Pending
   │
   ▼
Compute max deductible = f(Net Pay, Policy.MaxDeductionPercent, existing
statutory deductions) — see Phase 8 §8.4
   │
   ├─ sufficient ▶ mark installment Recovered, write LoanPaymentHistory,
   │                update Loan.OutstandingPrincipal
   │
   └─ insufficient ▶ mark installment Skipped (reason: "Net pay
                      insufficient"), audit-logged, rolls to next cycle,
                      HR/Finance notified
   │
   ▼
If OutstandingPrincipal reaches 0 → Loan/Advance.Status = Closed
```

This runs as part of the existing `PayrollBusinessService` payroll
generation step (new hook, not a rewrite) so it is naturally idempotent
per (EmployeeId, LoanId/AdvanceId, InstallmentNo) — re-running a payroll
draft never double-deducts an already-`Recovered` row.

## 2.5 Use Case Catalogue

| # | Use Case | Actor | Precondition | Postcondition |
|---|---|---|---|---|
| UC-01 | Manage Loan Type | HR Admin | Permission `LoanType.Manage` | Loan Type created/updated/deactivated |
| UC-02 | Manage Advance Type | HR Admin | Permission `AdvanceType.Manage` | Advance Type created/updated/deactivated |
| UC-03 | Define Loan Policy | HR Admin | Loan Type exists | Policy (with approval matrix) saved & versioned |
| UC-04 | Check Eligibility | Employee | Employee is Active, confirmed (not on probation, per Policy) | Max eligible amount & tenure shown before submit |
| UC-05 | Submit Loan Request | Employee | Eligible, no blocking active loan of same type (per Policy `MaxActiveLoans`) | Request in `Submitted`, routed to L1 |
| UC-06 | Submit Advance Request | Employee | Within Advance Type's max amount/frequency | Request in `Submitted` |
| UC-07 | Approve/Reject Request | Manager/HR/Finance (per level) | User is the assigned approver (or valid delegate) at current level, ≠ Maker | Status advances to next level or terminal Approved/Rejected |
| UC-08 | Disburse Loan/Advance | Finance Admin | Status = `Approved` | Status → `Disbursed`; EMI/installment schedule generated |
| UC-09 | View EMI Schedule | Employee/HR/Finance | Loan disbursed | Read-only amortization table |
| UC-10 | Run Payroll Recovery | System (via PayrollBusinessService) | Payroll run in progress | Due installments recovered/skipped per §2.4 |
| UC-11 | Request Pre-Closure | Employee/Finance | Loan `Active`, balance > 0 | Pre-closure quote generated, pending Finance approval |
| UC-12 | Approve Pre-Closure & Settle | Finance Admin | Pre-closure requested | Lump sum recorded, remaining EMIs cancelled, Status → `Closed` |
| UC-13 | Settle on Exit | Finance Admin (HR exit workflow) | Employee marked as exiting, outstanding balance > 0 | Balance settled vs F&F hook or written off (with approval), Status → `Closed`/`Foreclosed` |
| UC-14 | Settle Advance | Finance Admin / System | All installments recovered or manual settlement entered | Status → `Settled` |
| UC-15 | View Outstanding Balance | Employee (own)/HR/Finance (any, scoped) | — | Real-time balance summary |
| UC-16 | View Payment History | Employee (own)/HR/Finance | — | Chronological recovery ledger |
| UC-17 | Upload/View Attachments | Employee/HR/Finance | Request exists | File stored, listed, downloadable (permission-checked) |
| UC-18 | View Audit Trail | Auditor/Admin | Permission `LoanAudit.View` | Full before/after change history for a record |
| UC-19 | View Dashboard & Reports | HR/Finance | — | KPI cards + drill-down report grids, exportable |
| UC-20 | Export Register (Excel/PDF) | HR/Finance | List filtered | File download |

## 2.6 Key Business Rules Summary

1. An employee cannot have more `Active` loans of the same Loan Type than
   `LoanPolicy.MaxActiveLoans`.
2. Total monthly deduction (all active EMIs/installments combined) must
   never exceed `LoanPolicy.MaxDeductionPercentOfNetSalary` — enforced at
   both request-time (projected) and payroll-time (actual, since net pay
   can vary month to month).
3. Only a `Submitted`/`PendingApproval*` request can be approved/rejected;
   only an `Approved` request can be disbursed; only a `Disbursed`/
   `Active` loan can be pre-closed or settled — every service method
   guards its own valid input states (Phase 6/8) and returns a typed
   `ApiResponse` failure rather than throwing for expected invalid
   transitions.
4. All monetary calculations happen server-side in the Service/Domain
   layer; the API never trusts a client-supplied EMI/interest figure.

---
**Next:** Phase 3 — Database Design (ERD, Tables, PK/FK, Indexes).
