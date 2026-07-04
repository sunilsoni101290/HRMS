using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class AssetHistory : BaseEntity
    {
        public string AssetId { get; set; }
        public Asset Asset { get; set; }

        public string Action { get; set; }
        // Created / Allocated / Returned / Repair / Scrap

        public string? ReferenceId { get; set; } // AllocationId etc.

        public DateTime ActionDate { get; set; }

        public string PerformedBy { get; set; }
        public override string GetSequencePrefix() => "AH";
    }
}
