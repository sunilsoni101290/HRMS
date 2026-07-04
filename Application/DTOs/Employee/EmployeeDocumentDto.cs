using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Application.DTOs.Employee
{
    public class EmployeeDocumentDto
    {
        public string? Id { get; set; }

        #region Employee

        [Required(ErrorMessage = "Employee is required.")]
        [Display(Name = "Employee")]
        public string EmployeeId { get; set; }

        public string? EmployeeName { get; set; }

        #endregion

        #region Document

        [Required(ErrorMessage = "Document Type is required.")]
        [Display(Name = "Document Type")]
        public EmployeeDocumentType DocumentType { get; set; }

        public string? DocumentTypeText { get; set; }

        [Display(Name = "Document Number")]
        [StringLength(100)]
        public string? DocumentNumber { get; set; }

        [Display(Name = "Document Name")]
        [StringLength(200)]
        public string? DocumentName { get; set; }

        #endregion

        #region File

        public string? FileName { get; set; }

        public string? FilePath { get; set; }

        public string? FileExtension { get; set; }

        public long? FileSize { get; set; }

        #endregion

        #region Validity

        [Display(Name = "Issue Date")]
        [DataType(DataType.Date)]
        public DateTime? IssueDate { get; set; }

        [Display(Name = "Expiry Date")]
        [DataType(DataType.Date)]
        public DateTime? ExpiryDate { get; set; }

        [Display(Name = "Issued By")]
        [StringLength(100)]
        public string? IssuedBy { get; set; }

        #endregion

        #region Verification

        [Display(Name = "Verified")]
        public bool IsVerified { get; set; }

        [Display(Name = "Verified On")]
        public DateTime? VerifiedOn { get; set; }

        [Display(Name = "Verified By")]
        public string? VerifiedBy { get; set; }

        #endregion

        #region Remarks

        [StringLength(500)]
        public string? Remarks { get; set; }

        #endregion

        #region Audit

        public string? TenantId { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime CreatedOn { get; set; }

        public DateTime? ModifiedOn { get; set; }

        public string? ModifiedBy { get; set; }

        #endregion
    }
}
