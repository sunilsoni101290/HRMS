using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class SalaryStructure : BaseEntity
    {
        public string EmployeeId { get; set; }
        public Employee Employee { get; set; }

        public DateTime EffectiveFrom { get; set; }

        // Set when this assignment was created by applying a SalaryTemplate
        // (single or bulk Assign) - null for one-off assignments created
        // directly on this screen (unchanged legacy path, including Excel
        // import). This is a point-in-time COPY, not a live link: editing
        // or deleting the template afterwards never changes this row or
        // any Payroll already generated from it - a new template edit only
        // affects employees the (possibly-updated) template is applied to
        // again from that point on, each as its own new effective-dated
        // assignment (existing history is never overwritten).
        public string? SourceTemplateId { get; set; }
        public SalaryTemplate? SourceTemplate { get; set; }

        public ICollection<SalaryDetail> SalaryDetails { get; set; }
        public override string GetSequencePrefix() => "SS";
    }

    public class SalaryDetail : BaseEntity
    {
        public string SalaryStructureId { get; set; }
        public SalaryStructure EmployeeSalaryStructure { get; set; }

        public string SalaryComponentId { get; set; }
        public SalaryComponent SalaryComponent { get; set; }

        public decimal Amount { get; set; }
        public override string GetSequencePrefix() => "SD";
    }
}
