using System;

namespace Application.DTOs.EmployeeLifecycle
{
    // Lightweight row for GetDueForReviewAsync - these are Employee
    // records (not ProbationConfirmation records - no maker-checker
    // workflow fields exist yet for them), listing who HR should review
    // next. Deliberately a separate, smaller DTO rather than reusing
    // ProbationConfirmationDto with nulled-out workflow fields, to keep the
    // "who's due" list and the "existing proposal" list unambiguous for
    // API consumers.
    public class ProbationDueForReviewDto
    {
        public string EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public string? EmployeeCode { get; set; }
        public string? DepartmentName { get; set; }
        public string? DesignationName { get; set; }

        public DateTime JoiningDate { get; set; }

        // Null if the employee's ProbationEndDate was never explicitly set
        // (i.e. still relying on the Designation's default
        // ProbationPeriodMonths, which has never been snapshotted onto the
        // Employee record).
        public DateTime? ProbationEndDate { get; set; }

        // (ProbationEndDate - today).Days - null when ProbationEndDate is
        // null. Negative means already overdue.
        public int? DaysRemaining { get; set; }
        public bool IsOverdue { get; set; }
    }
}
