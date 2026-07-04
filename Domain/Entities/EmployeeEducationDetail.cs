using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Domain.Entities
{
    public class EmployeeEducationDetail : BaseEntity
    {
        [Required]
        public string EmployeeId { get; set; }

        [ForeignKey(nameof(EmployeeId))]
        public virtual Employee Employee { get; set; }

        [Required]
        [StringLength(100)]
        public string Qualification { get; set; }

        [StringLength(150)]
        public string Degree { get; set; }

        [StringLength(150)]
        public string Specialization { get; set; }

        [Required]
        [StringLength(200)]
        public string InstituteName { get; set; }

        [StringLength(150)]
        public string University { get; set; }

        [StringLength(100)]
        public string Board { get; set; }

        public int PassingYear { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal PercentageOrCGPA { get; set; }

        [StringLength(20)]
        public string Grade { get; set; }

        public bool IsHighestQualification { get; set; }

        [StringLength(500)]
        public string Remarks { get; set; }

        [StringLength(300)]
        public string CertificateFilePath { get; set; }
        public override string GetSequencePrefix() => "EDU";
    }
}
