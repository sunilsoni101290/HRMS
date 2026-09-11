using System;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    // One row per Generate/Recalculate/StatusChange event on a Payroll -
    // who did it, when, the before/after Net Salary, and why (Remarks -
    // mandatory on a Recalculate of a Processed payroll, see
    // SalaryCalculationService/PayrollBusinessService.RecalculateAsync).
    // Append-only, same purpose/shape convention as PayslipRequestAudit
    // (this module's own simple event trail, not a multi-level approval
    // chain).
    public class PayrollAuditLog : BaseEntity
    {
        [Required]
        public string PayrollId { get; set; }
        public virtual Payroll Payroll { get; set; }

        // "Generated" / "Recalculated" / "StatusChanged".
        [Required]
        public string Action { get; set; }

        public decimal? OldNetSalary { get; set; }
        public decimal? NewNetSalary { get; set; }

        public decimal? OldPayableDays { get; set; }
        public decimal? NewPayableDays { get; set; }

        // The acting User.Id (not EmployeeId) - same convention as
        // PayslipRequestAudit.PerformedBy.
        [Required]
        public string PerformedBy { get; set; }

        [MaxLength(500)]
        public string? Remarks { get; set; }

        public DateTime PerformedOn { get; set; } = DateTime.UtcNow;

        public override string GetSequencePrefix() => "PAL";
    }
}
