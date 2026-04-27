using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Domain.Entities
{
    public class LeaveType : BaseEntity
    {
        [Required, MaxLength(100)]
        public string Name { get; set; }

        [Required]
        public int MaxDaysPerYear { get; set; }

        public bool IsPaid { get; set; } = true;

        public bool AllowCarryForward { get; set; }
        public int? MaxCarryForwardDays { get; set; }

        public bool AllowHalfDay { get; set; }
        public override string GetSequencePrefix() => "LT";
    }
}
