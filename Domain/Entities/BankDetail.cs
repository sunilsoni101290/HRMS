using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Domain.Entities
{
    public class BankDetail : BaseEntity
    {
        public string EmployeeId { get; set; }
        public Employee Employee { get; set; }

        [Required, MaxLength(100)]
        public string BankName { get; set; }

        [Required, MaxLength(50)]
        public string AccountNumber { get; set; }

        [MaxLength(20)]
        public string IFSCCode { get; set; }

        [MaxLength(100)]
        public string AccountHolderName { get; set; }

        public bool IsPrimary { get; set; } = true;
        public override string GetSequencePrefix() => "BNK";
    }
}
