using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    public class EmployeeBankDetail : BaseEntity
    {
        [Required]
        public string EmployeeId { get; set; }

        [ForeignKey(nameof(EmployeeId))]
        public virtual Employee Employee { get; set; }

        [Required]
        [StringLength(150)]
        public string BankName { get; set; }

        [StringLength(150)]
        public string BranchName { get; set; }

        [Required]
        [StringLength(25)]
        public string AccountNumber { get; set; }

        [Required]
        [StringLength(150)]
        public string AccountHolderName { get; set; }

        [Required]
        [StringLength(20)]
        public string IFSCCode { get; set; }

        [StringLength(20)]
        public string? MICRCode { get; set; }

        [StringLength(20)]
        public BankAccountType AccountType { get; set; } // Savings / Current / Salary

        [StringLength(300)]
        public string? CancelledChequeFilePath { get; set; }

        public bool IsPrimary { get; set; } = true;

        [StringLength(500)]
        public string? Remarks { get; set; }

        public override string GetSequencePrefix() => "BNK";
    }
}
