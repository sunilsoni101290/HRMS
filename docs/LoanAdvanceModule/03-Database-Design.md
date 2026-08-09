# Loan & Advance Module — Phase 3: Database Design

Conventions match the existing schema exactly (see `Domain/Entities/BaseEntity.cs`,
`ProbationConfirmation.cs`, `AssetAllocation.cs`):

- **PK**: `Id nvarchar(50)` — app-generated sequence string (`GetSequencePrefix()`
  + `SequenceService`, e.g. `LNR-0000000001`), not an IDENTITY column.
- Every table carries the common `BaseEntity` columns: `TenantId`,
  `IsDeleted`, `IsActive`, `CreatedOn`, `CreatedBy`, `ModifiedOn`,
  `ModifiedBy`.
- Soft delete only (`IsDeleted = 1`), enforced via EF global query filter
  on `ApplicationDbContext` exactly like existing entities.
- Money columns: `decimal(18,2)`. Rates: `decimal(5,2)`.
- All FKs `ON DELETE NO ACTION` (existing DB convention — cascade deletes
  are avoided project-wide to protect audit/history integrity).

## 3.1 ERD (textual)

```
Tenant ─┬─< Company ─┬─< Branch
        │            └─< LoanPolicy >── LoanType
        │
        ├─< LoanType ──< LoanPolicy ──< LoanPolicyApprovalLevel
        ├─< AdvanceType
        │
Employee ─┬─< EmployeeLoan >── LoanType
          │        │
          │        ├─< LoanApprovalHistory >── User(Checker)
          │        ├─< LoanEmiSchedule
          │        ├─< LoanPaymentHistory
          │        └─< LoanAdvanceAttachment (EntityType=Loan)
          │
          └─< EmployeeAdvance >── AdvanceType
                   │
                   ├─< AdvanceApprovalHistory >── User(Checker)
                   ├─< AdvanceInstallment
                   ├─< AdvancePaymentHistory
                   └─< LoanAdvanceAttachment (EntityType=Advance)

LoanAdvanceAuditLog  (polymorphic: EntityType + EntityId, all of the above)
```

## 3.2 Tables

### 3.2.1 `LoanTypes`
| Column | Type | Notes |
|---|---|---|
| Id | nvarchar(50) | PK |
| TenantId | nvarchar(50) | FK → Tenants, NOT NULL |
| Code | nvarchar(20) | NOT NULL, unique per Tenant |
| Name | nvarchar(100) | NOT NULL |
| Description | nvarchar(500) | NULL |
| InterestMethod | tinyint | enum: 1=Reducing, 2=Flat |
| DefaultInterestRatePercent | decimal(5,2) | NOT NULL, default 0 |
| MaxTenureMonths | int | NOT NULL |
| RequiresGuarantor | bit | default 0 |
| RequiresCollateral | bit | default 0 |
| IsActive / IsDeleted / audit cols | — | BaseEntity |

Indexes: `UX_LoanTypes_Tenant_Code (TenantId, Code) WHERE IsDeleted=0`.

### 3.2.2 `AdvanceTypes`
| Column | Type | Notes |
|---|---|---|
| Id | nvarchar(50) | PK |
| TenantId | nvarchar(50) | FK → Tenants |
| Code | nvarchar(20) | NOT NULL, unique per Tenant |
| Name | nvarchar(100) | NOT NULL |
| Description | nvarchar(500) | NULL |
| MaxAmount | decimal(18,2) | NULL (if null, governed by `MaxAmountSalaryMultiplier`) |
| MaxAmountSalaryMultiplier | decimal(5,2) | NULL, e.g. 2.0 × gross |
| MaxInstallments | int | NOT NULL default 1 |
| IsInterestFree | bit | default 1 |
| IsActive / IsDeleted / audit cols | — | BaseEntity |

Indexes: `UX_AdvanceTypes_Tenant_Code (TenantId, Code) WHERE IsDeleted=0`.

### 3.2.3 `LoanPolicies`
| Column | Type | Notes |
|---|---|---|
| Id | nvarchar(50) | PK |
| TenantId | nvarchar(50) | FK → Tenants |
| CompanyId | nvarchar(50) | FK → Companies, NULL = tenant-wide |
| BranchId | nvarchar(50) | FK → Branches, NULL = company-wide |
| LoanTypeId | nvarchar(50) | FK → LoanTypes, NOT NULL |
| MinAmount / MaxAmount | decimal(18,2) | NOT NULL |
| MinTenureMonths / MaxTenureMonths | int | NOT NULL |
| InterestRatePercent | decimal(5,2) | overrides LoanType default when set |
| MinServiceMonthsRequired | int | eligibility: min tenure of service |
| MaxActiveLoans | int | default 1 |
| MaxDeductionPercentOfNetSalary | decimal(5,2) | e.g. 40.00 |
| EligibilitySalaryMultiplier | decimal(5,2) | max eligible = multiplier × monthly gross |
| PreClosurePenaltyPercent | decimal(5,2) | default 0 |
| VersionNumber | int | NOT NULL default 1 (see §3.4) |
| EffectiveFrom | datetime2 | NOT NULL |
| EffectiveTo | datetime2 | NULL = current |
| IsActive / IsDeleted / audit cols | — | BaseEntity |

Indexes: `IX_LoanPolicies_Lookup (TenantId, CompanyId, BranchId, LoanTypeId, IsActive)`.

### 3.2.4 `LoanPolicyApprovalLevels` (Approval Matrix, child of Policy)
| Column | Type | Notes |
|---|---|---|
| Id | nvarchar(50) | PK |
| LoanPolicyId | nvarchar(50) | FK → LoanPolicies, NOT NULL |
| LevelNumber | int | NOT NULL, 1-based sequence |
| ApproverType | tinyint | enum: 1=ReportingManager, 2=SpecificRole, 3=SpecificUser |
| ApproverRoleId | nvarchar(50) | FK → Roles, NULL unless ApproverType=SpecificRole |
| ApproverUserId | nvarchar(50) | FK → Users, NULL unless ApproverType=SpecificUser |
| MinAmountThreshold | decimal(18,2) | this level applies only when request amount ≥ this |
| IsActive / audit cols | — | BaseEntity |

Indexes: `UX_LoanPolicyApprovalLevels (LoanPolicyId, LevelNumber)`.

### 3.2.5 `EmployeeLoans` (the request **and** the resulting loan account —
same entity progresses through both, per the `ProbationConfirmation`
Maker-Checker convention already established in this codebase)
| Column | Type | Notes |
|---|---|---|
| Id | nvarchar(50) | PK, sequence prefix `LNR` |
| TenantId / CompanyId / BranchId | nvarchar(50) | scoping |
| EmployeeId | nvarchar(50) | FK → Employees, NOT NULL |
| LoanTypeId | nvarchar(50) | FK → LoanTypes, NOT NULL |
| LoanPolicyId | nvarchar(50) | FK → LoanPolicies (snapshot reference), NOT NULL |
| RequestedAmount | decimal(18,2) | NOT NULL |
| ApprovedAmount | decimal(18,2) | NULL until approved |
| TenureMonths | int | NOT NULL |
| InterestRatePercent | decimal(5,2) | snapshotted at approval time |
| InterestMethod | tinyint | snapshotted at approval time |
| Purpose | nvarchar(500) | NULL |
| Status | tinyint | enum §3.3 `LoanStatus` |
| CurrentApprovalLevel | int | NOT NULL default 0 |
| MakerId | nvarchar(50) | FK → Users — who submitted |
| MakerActionOn | datetime2 | NOT NULL |
| MakerRemarks | nvarchar(1000) | NULL |
| DisbursedAmount | decimal(18,2) | NULL |
| DisbursedOn | datetime2 | NULL |
| DisbursementMode | tinyint | enum: BankTransfer/Cheque/PayrollCredit |
| DisbursementReference | nvarchar(100) | NULL |
| OutstandingPrincipal | decimal(18,2) | NOT NULL default 0, maintained by recovery/pre-closure |
| ClosedOn | datetime2 | NULL |
| ClosureReason | tinyint | enum: FullyRecovered/PreClosed/SettledOnExit/WrittenOff |
| IsActive / IsDeleted / audit cols | — | BaseEntity |

Indexes:
- `IX_EmployeeLoans_Employee_Status (EmployeeId, Status)`
- `IX_EmployeeLoans_Tenant_Company_Branch (TenantId, CompanyId, BranchId)`
- `IX_EmployeeLoans_Status_DueTracking (Status) INCLUDE (OutstandingPrincipal)`

### 3.2.6 `LoanApprovalHistories` (one row per level acted on — mirrors
`LeaveApprovalHistory` / `AttendanceRegularizationApprovalHistory`)
| Column | Type | Notes |
|---|---|---|
| Id | nvarchar(50) | PK |
| EmployeeLoanId | nvarchar(50) | FK → EmployeeLoans, NOT NULL |
| LevelNumber | int | NOT NULL |
| CheckerId | nvarchar(50) | FK → Users, NOT NULL |
| ActedAsDelegateForUserId | nvarchar(50) | FK → Users, NULL unless acted via delegation |
| Decision | tinyint | enum: Approved/Rejected |
| Remarks | nvarchar(1000) | NULL |
| ActionOn | datetime2 | NOT NULL |
| audit cols | — | BaseEntity |

Indexes: `IX_LoanApprovalHistories_Loan (EmployeeLoanId, LevelNumber)`.

### 3.2.7 `LoanEmiSchedules`
| Column | Type | Notes |
|---|---|---|
| Id | nvarchar(50) | PK |
| EmployeeLoanId | nvarchar(50) | FK → EmployeeLoans, NOT NULL |
| InstallmentNumber | int | NOT NULL |
| DueDate | date | NOT NULL |
| OpeningBalance | decimal(18,2) | NOT NULL |
| PrincipalComponent | decimal(18,2) | NOT NULL |
| InterestComponent | decimal(18,2) | NOT NULL |
| EmiAmount | decimal(18,2) | NOT NULL |
| ClosingBalance | decimal(18,2) | NOT NULL |
| Status | tinyint | enum: Pending/Recovered/Skipped/Waived/Cancelled |
| RecoveredOn | datetime2 | NULL |
| PayrollRunId | nvarchar(50) | FK → Payrolls, NULL until recovered |
| audit cols | — | BaseEntity |

Indexes:
- `UX_LoanEmiSchedules (EmployeeLoanId, InstallmentNumber)`
- `IX_LoanEmiSchedules_DueTracking (Status, DueDate)` — used by the
  payroll recovery batch query.

### 3.2.8 `LoanPaymentHistories`
| Column | Type | Notes |
|---|---|---|
| Id | nvarchar(50) | PK |
| EmployeeLoanId | nvarchar(50) | FK → EmployeeLoans, NOT NULL |
| LoanEmiScheduleId | nvarchar(50) | FK → LoanEmiSchedules, NULL (NULL = pre-closure lump sum, not a normal EMI row) |
| PaymentSource | tinyint | enum: PayrollDeduction/ManualReceipt/PreClosure |
| AmountPaid | decimal(18,2) | NOT NULL |
| PrincipalPaid | decimal(18,2) | NOT NULL |
| InterestPaid | decimal(18,2) | NOT NULL |
| PaymentDate | datetime2 | NOT NULL |
| PayrollRunId | nvarchar(50) | FK → Payrolls, NULL |
| ReceiptReference | nvarchar(100) | NULL |
| Remarks | nvarchar(500) | NULL |
| audit cols | — | BaseEntity |

Indexes: `IX_LoanPaymentHistories_Loan_Date (EmployeeLoanId, PaymentDate)`.

### 3.2.9 `EmployeeAdvances` (near-exact mirror of `EmployeeLoans`, no
interest amortization — same convention the codebase already uses for
`OnDutyRequest` mirroring `WfhRequest`)
| Column | Type | Notes |
|---|---|---|
| Id | nvarchar(50) | PK, prefix `ADV` |
| TenantId / CompanyId / BranchId | nvarchar(50) | scoping |
| EmployeeId | nvarchar(50) | FK → Employees |
| AdvanceTypeId | nvarchar(50) | FK → AdvanceTypes |
| RequestedAmount / ApprovedAmount | decimal(18,2) | |
| Installments | int | NOT NULL |
| Purpose | nvarchar(500) | NULL |
| Status | tinyint | enum §3.3 `AdvanceStatus` (subset of LoanStatus, no PreClosure) |
| CurrentApprovalLevel | int | default 0 |
| MakerId / MakerActionOn / MakerRemarks | — | same shape as EmployeeLoans |
| DisbursedAmount / DisbursedOn / DisbursementMode / DisbursementReference | — | same shape |
| OutstandingAmount | decimal(18,2) | default 0 |
| ClosedOn | datetime2 | NULL |
| audit cols | — | BaseEntity |

Indexes: `IX_EmployeeAdvances_Employee_Status (EmployeeId, Status)`.

### 3.2.10 `AdvanceApprovalHistories`, `AdvanceInstallments`,
`AdvancePaymentHistories` — same column shapes as §3.2.6/3.2.7/3.2.8
with `EmployeeAdvanceId` replacing `EmployeeLoanId` and
`AdvanceInstallments` omitting the Principal/Interest split columns
(single `InstallmentAmount` only, since advances are interest-free by
default).

### 3.2.11 `LoanAdvanceAttachments` (shared, polymorphic — mirrors the
flat `EmployeeDocument` pattern already in the codebase)
| Column | Type | Notes |
|---|---|---|
| Id | nvarchar(50) | PK |
| EntityType | tinyint | enum: Loan / Advance |
| EntityId | nvarchar(50) | EmployeeLoans.Id or EmployeeAdvances.Id (no FK — polymorphic) |
| FileName | nvarchar(255) | NOT NULL |
| FilePath | nvarchar(500) | NOT NULL |
| ContentType | nvarchar(100) | NOT NULL |
| FileSizeBytes | bigint | NOT NULL |
| UploadedBy | nvarchar(50) | FK → Users |
| audit cols | — | BaseEntity |

Indexes: `IX_LoanAdvanceAttachments_Entity (EntityType, EntityId)`.

### 3.2.12 `LoanAdvanceAuditLogs` (shared, polymorphic, append-only)
| Column | Type | Notes |
|---|---|---|
| Id | nvarchar(50) | PK |
| EntityType | nvarchar(50) | e.g. "EmployeeLoan", "LoanType", "LoanPolicy" |
| EntityId | nvarchar(50) | NOT NULL |
| Action | nvarchar(50) | Create/Update/StatusChange/Approve/Reject/Disburse/Settle |
| OldValuesJson | nvarchar(max) | NULL |
| NewValuesJson | nvarchar(max) | NULL |
| PerformedBy | nvarchar(50) | FK → Users |
| PerformedOn | datetime2 | NOT NULL |
| IpAddress | nvarchar(50) | NULL |
| TenantId | nvarchar(50) | NOT NULL |

Indexes: `IX_LoanAdvanceAuditLogs_Entity (EntityType, EntityId, PerformedOn DESC)`.
No `IsDeleted`/soft-delete — audit rows are immutable and never removed.

## 3.3 Enums (Domain/Enums)

```
LoanStatus: Draft=1, Submitted=2, PendingApproval=3, Approved=4, Rejected=5,
            Disbursed=6, Active=7, PreClosureRequested=8,
            SettlementPending=9, Closed=10, Foreclosed=11, Cancelled=12

AdvanceStatus: Draft=1, Submitted=2, PendingApproval=3, Approved=4,
               Rejected=5, Disbursed=6, Recovered=7, Settled=8, Cancelled=9

InterestMethod: Reducing=1, Flat=2
ApprovalDecision: Approved=1, Rejected=2
DisbursementMode: BankTransfer=1, Cheque=2, PayrollCredit=3
PaymentSource: PayrollDeduction=1, ManualReceipt=2, PreClosure=3
InstallmentStatus: Pending=1, Recovered=2, Skipped=3, Waived=4, Cancelled=5
ClosureReason: FullyRecovered=1, PreClosed=2, SettledOnExit=3, WrittenOff=4
LoanAttachmentEntityType: Loan=1, Advance=2
ApproverType: ReportingManager=1, SpecificRole=2, SpecificUser=3
```

## 3.4 Design Notes

- **Policy versioning (§3.2.3)**: rather than a separate history table,
  `LoanPolicies` itself is versioned (`VersionNumber` + `EffectiveFrom`/
  `EffectiveTo`); `EmployeeLoans.LoanPolicyId` snapshots the exact policy
  version used at submission, so a later policy edit never retroactively
  changes an in-flight or historical loan's terms.
- **Why not a single generic `LoanTransaction` table for everything**:
  the codebase's own precedent (`WfhRequest`/`OnDutyRequest`/
  `ShortLeaveRequest` as separate, near-identical tables rather than one
  polymorphic table) is followed for `EmployeeLoans` vs
  `EmployeeAdvances` — keeps FKs simple, indexes tight, and EF queries
  strongly typed. Attachments and the audit log *are* made polymorphic
  because they are genuinely cross-cutting and low-cardinality-per-row,
  same as how `ErrorLog`/`ApiRequestLog` are already handled tenant-wide.
- **No cascading deletes**: every FK is `ON DELETE NO ACTION`, matching
  the rest of this database — closing/deleting a parent never silently
  removes child EMI/history/audit rows.

---
**Next:** Phase 4 — SQL Scripts.
