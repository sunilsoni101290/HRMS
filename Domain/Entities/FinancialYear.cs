using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    public class FinancialYear :BaseEntity
    {
        public string Name { get; set; }   // 2025-2026
        public string Code { get; set; }   // FY25-26

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public FinancialYearStatus Status { get; set; }  // ✅ fixed naming

        // Multi-Tenant
        [Required]
        public string TenantId { get; set; }
        public Tenant Tenant { get; set; }

        // Company-wise FY (IMPORTANT)
        public string CompanyId { get; set; }
        public Company Company { get; set; }

        // Flags
        public bool IsCurrent { get; set; } = false;
        public override string GetSequencePrefix() => "FYR";
    }
}
