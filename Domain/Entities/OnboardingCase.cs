using System;
using System.Collections.Generic;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    // A single Employee's onboarding journey. The Employee record ALWAYS
    // already exists before a case is started (manually by HR, or
    // auto-started right after a Recruitment Candidate is converted into an
    // Employee - see Employee.CandidateId / EmployeeDto.CandidateId /
    // EmployeeService.CreateAsync). This is NOT an employee-creation wizard -
    // "Employee Creation" is just one of the 5 stages (OnboardingStageType),
    // tracked purely as checklist confirmations under this case.
    public class OnboardingCase : BaseEntity
    {
        public string EmployeeId { get; set; }
        public virtual Employee Employee { get; set; }

        // Set only when this case was auto-created because the Employee was
        // converted from a Recruitment Candidate. Null for HR-started cases.
        public string? CandidateId { get; set; }
        public virtual Candidate Candidate { get; set; }

        // Snapshot of the Employee's org placement at case-start time - kept
        // as lean scalars only (no navigation), mirroring Asset's
        // CompanyId/BranchId pattern - purely for filtering/reporting.
        public string? CompanyId { get; set; }
        public string? BranchId { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime? TargetCompletionDate { get; set; }

        public OnboardingCaseStatus Status { get; set; }
        public DateTime? CompletedOn { get; set; }

        public string? Remarks { get; set; }

        public virtual ICollection<OnboardingChecklistItem> ChecklistItems { get; set; }

        public override string GetSequencePrefix() => "ONB";
    }
}
