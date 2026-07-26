using System;
using System.ComponentModel.DataAnnotations;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    // Employee Transfer - the THIRD phase of the "Probation & Confirmation"
    // (Employee Lifecycle) module (see Domain/Entities/ProbationConfirmation.cs
    // Phase 1, Domain/Entities/PipRecord.cs Phase 2). A MAKER (HR staff
    // holding Create permission on AppFeatureConstants.EMPLOYEE_TRANSFER)
    // proposes new Company/Branch/Department/Designation/ReportingManager
    // values for an Employee; a DIFFERENT person acting as CHECKER (holding
    // Approve permission) must Approve or Reject it before it is applied to
    // the live Employee record - see EmployeeTransferService.ApproveAsync/
    // RejectAsync for the actingUserId != MakerId invariant (identical to
    // ProbationConfirmationService's/PipService's, no override, checked
    // BEFORE any permission check).
    //
    // This row IS the audit trail for organizational changes made to an
    // Employee via this feature - EmployeeService.UpdateAsync itself keeps
    // no history for these fields. Purely additive/parallel to
    // EmployeeService; does not modify it.
    //
    // NOTE: only EmployeeId carries a navigation property (needed to load/
    // mutate the live Employee in ApproveAsync). The From*/To* Company/
    // Branch/Department/Designation/ReportingManager fields are
    // intentionally scalar-only (no EF navigation properties) - each
    // dimension would otherwise need its own uniquely-named relationship
    // (2x per org entity for From/To, 2 more self-referencing Employee
    // FKs for the reporting manager pair) purely to resolve display names.
    // Display names are instead resolved via batch ID->name lookups in
    // EmployeeTransferService, mirroring BuildUserNameMapAsync's pattern -
    // simpler and keeps this entity's EF model surface minimal.
    public class EmployeeTransfer : BaseEntity
    {
        [Required]
        public string EmployeeId { get; set; }
        public virtual Employee Employee { get; set; }

        [Required]
        public DateTime EffectiveDate { get; set; }

        [Required]
        public string Reason { get; set; }

        // ---------------- FROM (snapshot) ----------------
        // Captured automatically at proposal time (CreateAsync) from the
        // Employee's CURRENT values - never caller-supplied - defense in
        // depth against stale/manipulated "from" data.
        [Required]
        public string FromCompanyId { get; set; }

        public string? FromBranchId { get; set; }

        [Required]
        public string FromDepartmentId { get; set; }

        [Required]
        public string FromDesignationId { get; set; }

        public string? FromReportingManagerId { get; set; }

        // ---------------- TO (caller-proposed) ----------------
        // All nullable at the entity level even though the corresponding
        // Employee fields (Company/Department/Designation) are non-
        // nullable - a given transfer might only change SOME dimensions
        // (e.g. just a department transfer, reporting manager unchanged).
        // Null in a "To" field means "no change requested for this
        // dimension, keep the From value" - see
        // EmployeeTransferService.ApproveAsync for the "apply only non-
        // null To fields" logic.
        public string? ToCompanyId { get; set; }
        public string? ToBranchId { get; set; }
        public string? ToDepartmentId { get; set; }
        public string? ToDesignationId { get; set; }
        public string? ToReportingManagerId { get; set; }

        // ---------------- Maker (proposer) ----------------
        // The acting User.Id (NOT EmployeeId) of whoever proposed this
        // transfer - maker-checker is about WHO is logged in, not which
        // Employee record they represent.
        [Required]
        public string MakerId { get; set; }

        [Required]
        public DateTime MakerActionOn { get; set; }

        [MaxLength(1000)]
        public string? MakerRemarks { get; set; }

        public TransferStatus Status { get; set; } = TransferStatus.PendingChecker;

        // ---------------- Checker (approver/rejecter) ----------------
        // The acting User.Id of whoever approved/rejected - MUST differ
        // from MakerId. Enforced in
        // EmployeeTransferService.ApproveAsync/RejectAsync.
        public string? CheckerId { get; set; }
        public DateTime? CheckerActionOn { get; set; }

        [MaxLength(1000)]
        public string? CheckerRemarks { get; set; }

        public override string GetSequencePrefix() => "TRF";
    }
}
