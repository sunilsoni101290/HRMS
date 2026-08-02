using System;
using System.ComponentModel.DataAnnotations;
using static Domain.Enums.EnumExtensions;

namespace Application.DTOs.Taxation
{
    // Admin master-data CRUD shape for a single income-tax slab row - see
    // Domain/Entities/TaxSlab.cs. Doubles as both the read/response DTO
    // and the create/update input (same shape, matching FinancialYearDto's
    // convention in this codebase) since this is plain CRUD with no
    // workflow.
    public class TaxSlabDto
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Financial Year is required")]
        [Display(Name = "Financial Year")]
        public string FinancialYearId { get; set; }
        public string? FinancialYearName { get; set; }

        [Required(ErrorMessage = "Regime is required")]
        [Display(Name = "Regime")]
        public int Regime { get; set; }
        public string? RegimeName { get; set; }

        [Display(Name = "Slab Order")]
        public int SlabOrder { get; set; }

        [Required(ErrorMessage = "Minimum Income is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Minimum Income cannot be negative")]
        [Display(Name = "Minimum Income")]
        public decimal MinIncome { get; set; }

        // Null = no upper bound (the topmost slab).
        [Display(Name = "Maximum Income")]
        public decimal? MaxIncome { get; set; }

        [Required(ErrorMessage = "Rate is required")]
        [Range(0, 100, ErrorMessage = "Rate must be between 0 and 100")]
        [Display(Name = "Rate (%)")]
        public decimal RatePercent { get; set; }

        public string? TenantId { get; set; }
        public DateTime CreatedOn { get; set; }
        public string? CreatedBy { get; set; }
    }
}
