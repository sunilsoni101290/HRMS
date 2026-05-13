using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Domain.Entities
{
    public class Shift : BaseEntity
    {
        [Required, MaxLength(100)]
        public string Name { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        public int GraceInMinutes { get; set; } = 0;
        public int GraceOutMinutes { get; set; } = 0;

        public int HalfDayMinutes { get; set; }
        public int FullDayMinutes { get; set; }

        public bool IsNightShift { get; set; } = false;

        // ✅ NEW (important for payroll rules)
        public int MinimumWorkingMinutes { get; set; }
        public int MaximumWorkingMinutes { get; set; }

        public string TenantId { get; set; }
        public virtual Tenant Tenant { get; set; }

        public override string GetSequencePrefix() => "SH";
    }
}
