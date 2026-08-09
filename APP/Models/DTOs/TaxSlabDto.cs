using System;
using System.ComponentModel.DataAnnotations;

namespace APP.Models.DTOs
{
    // Mirrors Application.DTOs.Taxation.TaxSlabDto exactly - property
    // names/types must match the API's JSON 1:1 or model binding silently
    // breaks. Backed by API/Controllers/TaxSlabController.cs
    // (api/taxslab).
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

        [Display(Name = "Maximum Income")]
        public decimal? MaxIncome { get; set; }

        [Required(ErrorMessage = "Rate is required")]
        [Range(0, 100, ErrorMessage = "Rate must be between 0 and 100")]
        [Display(Name = "Rate (%)")]
        public decimal RatePercent { get; set; }

        public string TenantId { get; set; }
        public string ActingUserId { get; set; }
        public DateTime CreatedOn { get; set; }
        public string? CreatedBy { get; set; }
    }
}
