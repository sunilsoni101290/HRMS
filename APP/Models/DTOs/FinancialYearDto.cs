using System.ComponentModel.DataAnnotations;
using static APP.Helpers.EnumExtensions;

namespace APP.Models.DTOs
{
    public class FinancialYearDto
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Financial Year Name is required")]
        [MaxLength(100)]
        [Display(Name = "Financial Year Name")]
        public string Name { get; set; }   // 2025-2026

        [Required(ErrorMessage = "Financial Year Code is required")]
        [MaxLength(20)]
        [Display(Name = "Financial Year Code")]
        public string Code { get; set; }   // FY25-26

        [Required(ErrorMessage = "Start Date is required")]
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "End Date is required")]
        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Required(ErrorMessage = "Status is required")]
        [Display(Name = "Status")]
        public FinancialYearStatus Status { get; set; }

        // Company-wise FY
        [Required(ErrorMessage = "Company is required")]
        [Display(Name = "Company")]
        public string CompanyId { get; set; }

        public string? CompanyName { get; set; }

        public string? TenantId { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }

        // Flags
        [Display(Name = "Is Current Financial Year")]
        public bool IsCurrent { get; set; } = false;
    }
}
