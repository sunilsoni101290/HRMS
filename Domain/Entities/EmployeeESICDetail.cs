using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Domain.Entities
{
    public class EmployeeESICDetail : BaseEntity
    {
        public string EmployeeId { get; set; }
        public Employee Employee { get; set; }

        public bool IsESICApplicable { get; set; }

        [MaxLength(50)]
        public string ESICNumber { get; set; }

        public decimal? EmployeeContribution { get; set; } // %
        public decimal? EmployerContribution { get; set; } // %

        public DateTime? ESICJoiningDate { get; set; }
        public override string GetSequencePrefix() => "ESIC";
    }
}
