using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    public class EmployeeDocument : BaseEntity
    {
        [Required]
        public string EmployeeId { get; set; }

        [ForeignKey(nameof(EmployeeId))]
        public virtual Employee Employee { get; set; }

        /// <summary>
        /// Aadhaar, PAN, Passport, Resume, etc.
        /// </summary>
        [Required]
        public EmployeeDocumentType DocumentType { get; set; }

        /// <summary>
        /// Document Number (Aadhaar/PAN/Passport/etc.)
        /// </summary>
        [StringLength(100)]
        public string? DocumentNumber { get; set; }

        [StringLength(200)]
        public string? DocumentName { get; set; }

        /// <summary>
        /// Original uploaded file name
        /// </summary>
        [StringLength(300)]
        public string? FileName { get; set; }

        /// <summary>
        /// Physical path or blob path
        /// </summary>
        [Required]
        [StringLength(500)]
        public string FilePath { get; set; }

        [StringLength(20)]
        public string? FileExtension { get; set; }

        public long? FileSize { get; set; }

        public DateTime? IssueDate { get; set; }

        public DateTime? ExpiryDate { get; set; }

        [StringLength(200)]
        public string? IssuedBy { get; set; }

        public bool IsVerified { get; set; }

        public DateTime? VerifiedOn { get; set; }

        [StringLength(100)]
        public string? VerifiedBy { get; set; }

        [StringLength(500)]
        public string? Remarks { get; set; }
        public override string GetSequencePrefix() => "ED";

    }
}
