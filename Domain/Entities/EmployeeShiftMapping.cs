using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class EmployeeShiftMapping :BaseEntity
    {
        public string EmployeeId { get; set; }
        public virtual Employee Employee { get; set; }

        public string ShiftId { get; set; }
        public virtual Shift Shift { get; set; }

        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public override string GetSequencePrefix() => "ESM";
    }
}
