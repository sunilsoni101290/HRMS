using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class PayrollDetail : BaseEntity
    {
        public string PayrollId { get; set; }
        public virtual Payroll Payroll { get; set; }

        public string SalaryComponentId { get; set; }
        public virtual SalaryComponent SalaryComponent { get; set; }

        public decimal Amount { get; set; }

        public bool IsEarning { get; set; }
        public override string GetSequencePrefix() => "PRD";
    }
}
