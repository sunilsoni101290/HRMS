using System;
using System.ComponentModel.DataAnnotations;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    // Admin-configured master data - one row per income slab, per Regime,
    // per FinancialYear (reuses the existing FinancialYear master rather
    // than inventing a parallel "tax year" concept). Income tax slabs are
    // revised almost every Union Budget, so these are DB-driven (Admin
    // CRUD via TaxSlabService/TaxSlabController), never hardcoded - see
    // TaxComputationService.ComputeAsync for how a set of slabs for a
    // given (FinancialYearId, Regime) is walked progressively to compute
    // tax. MaxIncome == null means "and above" (the topmost slab).
    public class TaxSlab : BaseEntity
    {
        [Required]
        public string FinancialYearId { get; set; }
        public virtual FinancialYear FinancialYear { get; set; }

        public TaxRegime Regime { get; set; }

        // 1-based display/walk order within (FinancialYearId, Regime) -
        // e.g. 1 = 0-3L, 2 = 3L-6L, etc. Not strictly required for
        // correctness (MinIncome alone determines walk order) but keeps
        // the Admin UI list stable and human-sortable.
        public int SlabOrder { get; set; }

        public decimal MinIncome { get; set; }

        // Null = no upper bound (the topmost/final slab).
        public decimal? MaxIncome { get; set; }

        public decimal RatePercent { get; set; }

        public override string GetSequencePrefix() => "TSL";
    }
}
