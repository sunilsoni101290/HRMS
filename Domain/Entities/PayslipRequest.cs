using System;
using System.ComponentModel.DataAnnotations;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    // Employee-submitted request to receive the actual payslip DOCUMENT for
    // one specific, already-existing Payroll (salary) period of their own.
    // Unlike the old direct-access path (APP/Controllers/PayrollController.cs
    // MyPayslips/Payslip, now locked down), an employee can never view or
    // download a payslip file directly - only through a Completed request
    // they own. Two-stage approval:
    //
    //   1) The requesting employee's direct ReportingManagerId (or HR/Admin
    //      as a permission-based override) approves or rejects.
    //   2) On manager-approval, the request is auto-forwarded to Finance (a
    //      permission-based role - see AppFeatureConstants.
    //      PAYROLL_PAYSLIP_REQUEST) in the SAME call, no separate manual
    //      "forward" step. Finance then uploads/generates the payslip
    //      document (PendingFinanceAction -> PayslipGenerated) and finally
    //      marks the request Completed (PayslipGenerated -> Completed),
    //      which is the only point at which DocumentUrl becomes downloadable
    //      by the employee. Finance may also reject from either
    //      PendingFinanceAction or PayslipGenerated.
    //
    // A rejection at either stage is terminal - the employee sees the
    // rejection reason but never gets file access. See
    // PayslipRequestService for the full state machine and
    // PayslipRequestAudit for the who/when/remarks trail of every
    // transition.
    public class PayslipRequest : BaseEntity
    {
        [Required]
        public string EmployeeId { get; set; }
        public virtual Employee Employee { get; set; }

        // The specific salary period this request is for - looked up and
        // validated (must belong to EmployeeId) at creation time.
        [Required]
        public string PayrollId { get; set; }
        public virtual Payroll? Payroll { get; set; }

        // Denormalized copy of Payroll.SalaryYear/SalaryMonth at creation
        // time, purely so the duplicate/overlap check (no more than one
        // non-terminal request per employee+period) can query without an
        // extra join.
        public int PayrollYear { get; set; }
        public int PayrollMonth { get; set; }

        public PayslipRequestStatus Status { get; set; } = PayslipRequestStatus.PendingManagerApproval;

        // Manager stage (approve is optional-remarks, reject requires a
        // reason - enforced in the service layer, not here).
        [MaxLength(500)]
        public string? ManagerRemarks { get; set; }
        public string? ManagerActionBy { get; set; }
        public DateTime? ManagerActionOn { get; set; }

        // Finance stage. FinanceRemarks doubles as the finance-reject
        // reason when Status == RejectedByFinance.
        [MaxLength(500)]
        public string? FinanceRemarks { get; set; }
        public string? FinanceActionBy { get; set; }
        public DateTime? FinanceActionOn { get; set; }

        // The uploaded payslip document - set by Finance's upload step
        // (PendingFinanceAction -> PayslipGenerated). Relative URL under
        // wwwroot, same "controller saves IFormFile to disk, service only
        // ever sees a string" convention as EmployeeDocument.
        public string? DocumentUrl { get; set; }
        public string? DocumentFileName { get; set; }

        public string? GeneratedBy { get; set; }
        public DateTime? GeneratedOn { get; set; }

        public string? CompletedBy { get; set; }
        public DateTime? CompletedOn { get; set; }

        public override string GetSequencePrefix() => "PSR";
    }
}
