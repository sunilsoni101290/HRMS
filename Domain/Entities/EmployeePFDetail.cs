using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Domain.Entities
{
    public class EmployeePFDetail : BaseEntity
    {
        [Required]
        public string EmployeeId { get; set; }

        [ForeignKey(nameof(EmployeeId))]
        public virtual Employee Employee { get; set; }

        /// <summary>
        /// Universal Account Number
        /// </summary>
        [Required]
        [StringLength(20)]
        public string UANNumber { get; set; }

        /// <summary>
        /// PF Member ID / PF Number
        /// </summary>
        [Required]
        [StringLength(50)]
        public string PFNumber { get; set; }

        [StringLength(150)]
        public string PFOffice { get; set; }

        public DateTime? PFJoiningDate { get; set; }

        public DateTime? PFExitDate { get; set; }

        public bool EPSApplicable { get; set; } = true;

        public bool EPFApplicable { get; set; } = true;

        public bool EDLIApplicable { get; set; } = true;

        public bool IsInternationalWorker { get; set; }

        [StringLength(500)]
        public string Remarks { get; set; }
        public override string GetSequencePrefix() => "PF";
    }
}
