using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Domain.Entities
{
    public class Payroll : BaseEntity
    {
        
        public string CompanyId { get; set; }
        public string? BranchId { get; set; }

        // Employee
        [Required]
        public string EmployeeId { get; set; }
        public virtual Employee Employee { get; set; }

        // Month (IMPORTANT 🔥)
        public int SalaryYear { get; set; }
        public int SalaryMonth { get; set; }

        public DateTime SalaryDate { get; set; }

        // Summary
        public decimal GrossSalary { get; set; }
        public decimal TotalEarnings { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal NetSalary { get; set; }

        // Attendance Integration
        public decimal? TotalWorkingDays { get; set; }
        public decimal? PresentDays { get; set; }
        public decimal? LeaveDays { get; set; }

        // Status
        public string Status { get; set; } // Draft / Processed / Paid

        // Navigation
        public ICollection<PayrollDetail> PayrollDetails { get; set; }
        public override string GetSequencePrefix() => "PRL";
    }
}
