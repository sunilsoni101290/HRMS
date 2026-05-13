using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class SequenceMaster : BaseEntity
    {
        public string Prefix { get; set; }
        public string FinancialYearId { get; set; } // FY-based reset// EMP, INV, PAY
        public int CurrentNumber { get; set; }
        public override string GetSequencePrefix() => "SEQ";
    }
}
