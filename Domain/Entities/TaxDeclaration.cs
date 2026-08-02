using System;
using System.ComponentModel.DataAnnotations;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    // Employee self-service annual investment declaration - one row per
    // (EmployeeId, FinancialYearId). NOT a Maker-Checker (segregation-of-
    // duties) feature like ProbationConfirmation - the employee declares
    // facts about themselves (closest existing shape is WfhRequest's
    // employee-submits/HR-or-manager-approves pattern), so there is no
    // "checker must differ from maker" invariant here. Workflow:
    // Draft (employee editing) -> Submitted (employee locks it in) ->
    // Verified/Rejected (HR, holding Approve permission on
    // TAX_DECLARATION) - see TaxDeclarationService. Only a Verified
    // declaration is read by TaxComputationService.ComputeAsync.
    public class TaxDeclaration : BaseEntity
    {
        [Required]
        public string EmployeeId { get; set; }
        public virtual Employee Employee { get; set; }

        [Required]
        public string FinancialYearId { get; set; }
        public virtual FinancialYear FinancialYear { get; set; }

        // The regime the employee wants applied for this Financial Year -
        // a fresh choice every year (India's Finance Act allows salaried
        // employees to switch regimes each FY, unlike business income
        // assessees who are far more restricted).
        public TaxRegime Regime { get; set; }

        // ---------------- Chapter VI-A deductions (Old Regime only -
        // TaxComputationService ignores all of these under New Regime) ----------------

        // Section 80C - LIC premium, PPF, ELSS, employee's own EPF
        // contribution, life insurance, principal repayment on home loan,
        // children's tuition fees, etc. Statutory cap enforced in
        // TaxComputationService (Rs. 1,50,000), not here - this field
        // stores the employee's claimed/declared amount, which may exceed
        // the cap (over-declaration is deliberately allowed at the
        // declaration stage; the computation engine is what caps it).
        public decimal Section80C { get; set; }

        // Section 80CCD(1B) - additional NPS (National Pension System)
        // self-contribution, over and above 80C. Cap Rs. 50,000.
        public decimal Section80CCD1B { get; set; }

        // Section 80D - medical insurance premium (self/family/parents).
        // Cap enforced in TaxComputationService.
        public decimal Section80D { get; set; }

        // Section 24(b) - home loan interest (self-occupied property).
        // Cap Rs. 2,00,000.
        public decimal Section24B { get; set; }

        // Other Chapter VI-A deductions not individually modeled here
        // (80E education loan interest, 80G donations, 80TTA/80TTB
        // savings-account interest, etc.) - lumped into a single
        // uncapped bucket rather than building a line-item table for
        // every section, which is out of scope for this phase.
        public decimal OtherDeductions { get; set; }

        // ---------------- HRA exemption inputs (Old Regime only) ----------------

        public decimal AnnualRentPaid { get; set; }

        // Drives the 50% (metro) vs 40% (non-metro) of Basic salary limb
        // of the HRA exemption formula - see
        // TaxComputationService.ComputeHraExemption.
        public bool IsMetroCity { get; set; }

        [MaxLength(1000)]
        public string? LandlordPAN { get; set; }

        public TaxDeclarationStatus Status { get; set; } = TaxDeclarationStatus.Draft;

        public DateTime? SubmittedOn { get; set; }

        // HR/Payroll acting User.Id who verified or rejected - NOT an
        // EmployeeId, same MakerId/CheckerId convention as
        // ProbationConfirmation.
        public string? VerifiedBy { get; set; }
        public DateTime? VerifiedOn { get; set; }

        [MaxLength(1000)]
        public string? VerifierRemarks { get; set; }

        public override string GetSequencePrefix() => "TXD";
    }
}
