using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    // A reusable salary "master" (e.g. "Software Developer", "Accountant") -
    // NOT tied to any one employee. Applying it to employees (bulk or
    // single) creates/updates their per-employee SalaryStructure records
    // (see SalaryStructure.cs) by copying this template's component/amount
    // lines - it never assigns salary by itself. Updating a template later
    // does NOT retroactively change any employee who was already applied
    // from it (SalaryStructure rows are a point-in-time copy, not a live
    // reference) - see SalaryStructure.SourceTemplateId's remarks for why.
    public class SalaryTemplate : BaseEntity
    {
        [Required, MaxLength(150)]
        public string Name { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        // Informational default only - the actual effective date used when
        // applying this template to employees is chosen (and can be
        // overridden) on the Assign screen, since the same template is
        // typically applied to different employees on different dates.
        public DateTime EffectiveFrom { get; set; }

        public ICollection<SalaryTemplateDetail> Details { get; set; }
        public override string GetSequencePrefix() => "ST";
    }

    public class SalaryTemplateDetail : BaseEntity
    {
        public string SalaryTemplateId { get; set; }
        public SalaryTemplate SalaryTemplate { get; set; }

        public string SalaryComponentId { get; set; }
        public SalaryComponent SalaryComponent { get; set; }

        public decimal Amount { get; set; }

        // 1 = Fixed (only supported value today - every existing
        // SalaryComponent-based amount in this codebase, template or
        // employee-assignment, is a flat fixed amount). Kept as its own
        // column rather than hardcoded so a future Percentage-of-Basic
        // style calculation can be added without another migration.
        public int CalculationType { get; set; } = 1;

        public override string GetSequencePrefix() => "STD";
    }
}
