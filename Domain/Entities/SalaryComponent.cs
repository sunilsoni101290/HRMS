using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    public class SalaryComponent : BaseEntity
    {
        [Required, MaxLength(100)]
        public string Name { get; set; }   // Basic, HRA, Bonus

        [Required, MaxLength(50)]
        public string Code { get; set; }   // BASIC, HRA

        public SalaryComponentType ComponentType { get; set; } // Earning / Deduction

        public bool IsTaxable { get; set; } = false;
        public bool IsPFApplicable { get; set; } = false;
        public bool IsESICApplicable { get; set; } = false;
        public override string GetSequencePrefix() => "SC";
    }
}
