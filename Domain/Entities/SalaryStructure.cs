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
