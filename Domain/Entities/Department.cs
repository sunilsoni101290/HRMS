using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Domain.Entities
{
    public class Department : BaseEntity
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

        // Hierarchy
        public string? ParentDepartmentId { get; set; }
        public Department ParentDepartment { get; set; }

        public ICollection<Department> ChildDepartments { get; set; }

        // Relations
        public ICollection<Designation> Designations { get; set; }
        public ICollection<Employee> Employees { get; set; }
        public override string GetSequencePrefix() => "DPT";
    }
}
