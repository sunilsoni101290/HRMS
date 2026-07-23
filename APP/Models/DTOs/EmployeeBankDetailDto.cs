using System.ComponentModel.DataAnnotations;
using static APP.Helpers.EnumExtensions;

namespace APP.Models.DTOs
{
    public class EmployeeBankDetailDto
    {
        public string? Id { get; set; }

        #region Employee

        [Required(ErrorMessage = "Employee is required.")]
        [Display(Name = "Employee")]
        public string EmployeeId { get; set; }

        public string? EmployeeName { get; set; }

        public string? EmployeeCode { get; set; }

        #endregion

        #region Bank Details

        [Required(ErrorMessage = "Bank Name is required.")]
        [Display(Name = "Bank Name")]
        [StringLength(150)]
        public string BankName { get; set; }

        [Display(Name = "Branch Name")]
        [StringLength(150)]
        public string? BranchName { get; set; }

        [Required(ErrorMessage = "Account Number is required.")]
        [Display(Name = "Account Number")]
        [StringLength(25)]
        public string AccountNumber { get; set; }

        [Required(ErrorMessage = "Account Holder Name is required.")]
        [Display(Name = "Account Holder Name")]
        [StringLength(150)]
        public string AccountHolderName { get; set; }

        [Required(ErrorMessage = "IFSC Code is required.")]
        [Display(Name = "IFSC Code")]
        [StringLength(20)]
        public string IFSCCode { get; set; }

        [Display(Name = "MICR Code")]
        [StringLength(20)]
        public string? MICRCode { get; set; }

        [Required(ErrorMessage = "Account Type is required.")]
        [Display(Name = "Account Type")]
        public BankAccountType AccountType { get; set; }

        public string? AccountTypeName { get; set; }

        [Display(Name = "Cancelled Cheque")]
        [StringLength(300)]
        public string? CancelledChequeFilePath { get; set; }

        [Display(Name = "Primary Account")]
        public bool IsPrimary { get; set; } = true;

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
