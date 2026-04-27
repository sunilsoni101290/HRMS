using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Domain.Entities
{
    public class EmployeePFDetail : BaseEntity
    {
        public string EmployeeId { get; set; }
        public Employee Employee { get; set; }

        public bool IsPFApplicable { get; set; }

        [MaxLength(50)]
        public string PFNumber { get; set; }

        public decimal? EmployeeContribution { get; set; } // %
        public decimal? EmployerContribution { get; set; } // %

        public DateTime? PFJoiningDate { get; set; }
        public override string GetSequencePrefix() => "EPF";
    }
}
