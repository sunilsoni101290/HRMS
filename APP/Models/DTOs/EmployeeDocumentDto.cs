using System.ComponentModel.DataAnnotations;
using static APP.Helpers.EnumExtensions;

namespace APP.Models.DTOs
{
    public class EmployeeDocumentDto : IValidatableObject
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

        [Display(Name = "Upload Document")]
        public IFormFile? UploadFile { get; set; }

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

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Expiry Date Validation
            if (IssueDate.HasValue &&
                ExpiryDate.HasValue &&
                ExpiryDate <= IssueDate)
            {
                yield return new ValidationResult(
                    "Expiry Date must be greater than Issue Date.",
                    new[] { nameof(ExpiryDate) });
            }

            // Verification Validation
            if (IsVerified)
            {
                if (string.IsNullOrWhiteSpace(VerifiedBy))
                {
                    yield return new ValidationResult(
                        "Verified By is required.",
                        new[] { nameof(VerifiedBy) });
                }

                if (!VerifiedOn.HasValue)
                {
                    yield return new ValidationResult(
                        "Verified On is required.",
                        new[] { nameof(VerifiedOn) });
                }
            }

            // File Size Validation (10 MB)
            if (UploadFile != null && UploadFile.Length > 10 * 1024 * 1024)
            {
                yield return new ValidationResult(
                    "Maximum allowed file size is 10 MB.",
                    new[] { nameof(UploadFile) });
            }

            // Allowed File Types
            if (UploadFile != null)
            {
                var allowedExtensions = new[]
                {
                ".pdf",
                ".jpg",
                ".jpeg",
                ".png",
                ".doc",
                ".docx"
            };

                var extension = Path.GetExtension(UploadFile.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    yield return new ValidationResult(
                        "Only PDF, JPG, JPEG, PNG, DOC and DOCX files are allowed.",
                        new[] { nameof(UploadFile) });
                }
            }
        }
    }
}
