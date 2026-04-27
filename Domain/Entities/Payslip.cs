using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class Payslip : BaseEntity
    {
        public string PayrollId { get; set; }
        public DateTime GeneratedDate { get; set; }

        public Payroll Payroll { get; set; }
        public override string GetSequencePrefix() => "PSL";
    }
}
