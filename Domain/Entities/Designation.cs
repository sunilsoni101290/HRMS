using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Domain.Entities
{
    public class Designation : BaseEntity
    {
        [Required, MaxLength(150)]
        public string Name { get; set; }

        [Required, MaxLength(50)]
        public string Code { get; set; }

        // Multi-Tenant
        [Required]
        public string TenantId { get; set; }
        public Tenant Tenant { get; set; }

        public string? CompanyId { get; set; }
        public Company Company { get; set; }

        public string? BranchId { get; set; }
        public Branch Branch { get; set; }

        // Department Mapping
        public string DepartmentId { get; set; }
        public Department Department { get; set; }

        // Designation Hierarchy (BETTER THAN LEVEL)
        public string? ParentDesignationId { get; set; }
        public Designation ParentDesignation { get; set; }

        public ICollection<Designation> ChildDesignations { get; set; }

        // Level (optional helper)
        public int Level { get; set; }

        // Salary Range
        public decimal MinSalary { get; set; } = 0.00m;
        public decimal MaxSalary { get; set; } = 0.00m;

        // Default probation length (in months) for employees hired into
        // this Designation - used by ProbationConfirmationService as the
        // fallback when computing an Employee's OriginalProbationEndDate
        // if Employee.ProbationEndDate was never explicitly set
        // (JoiningDate.AddMonths(ProbationPeriodMonths)). Populating
        // Employee.ProbationEndDate itself at Employee-create time is a
        // separate, out-of-scope enhancement for EmployeeService.CreateAsync.
        public int ProbationPeriodMonths { get; set; } = 3;

        public override string GetSequencePrefix() => "DSG";
    }

}
