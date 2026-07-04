using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Domain.Entities
{
    public class EmployeeESICDetail : BaseEntity
    {
        [Required]
        public string TenantId { get; set; }

        [Required]
        public string EmployeeId { get; set; }

        [ForeignKey(nameof(EmployeeId))]
        public virtual Employee Employee { get; set; }

        /// <summary>
        /// ESIC Insurance Number
        /// </summary>
        [Required]
        [StringLength(20)]
        public string ESICNumber { get; set; }

        public DateTime? RegistrationDate { get; set; }

        public DateTime? ExitDate { get; set; }

        [StringLength(150)]
        public string ESICDispensary { get; set; }

        [StringLength(200)]
        public string BranchOffice { get; set; }

        public bool IsEligible { get; set; } = true;

        [StringLength(500)]
        public string Remarks { get; set; }
        public override string GetSequencePrefix() => "ESIC";
    }

}
