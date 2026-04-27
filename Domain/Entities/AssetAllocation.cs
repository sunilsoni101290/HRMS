using System;
using System.Collections.Generic;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    public class AssetAllocation : BaseEntity
    {
        public string AssetId { get; set; }
        public virtual Asset Asset { get; set; }

        public string EmployeeId { get; set; }
        public virtual Employee Employee { get; set; }

        public DateTime AllocatedOn { get; set; }

        public DateTime? ReturnedOn { get; set; }

        public AllocationStatus AllocationStatus { get; set; } // Allocated / Returned / Lost / Damaged

        public string ConditionOnIssue { get; set; }
        public string ConditionOnReturn { get; set; }

        public string Remarks { get; set; }
        public override string GetSequencePrefix() => "AA";
    }
}
