using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    public class Company : BaseEntity
    {
        // Basic Info
        [Required, MaxLength(200)]
        public string Name { get; set; }

        [Required, MaxLength(50)]
        public string Code { get; set; }

        // Legal Details
        [MaxLength(15)]
        public string? GSTNumber { get; set; }

        [MaxLength(10)]
        public string? PANNumber { get; set; }

        [MaxLength(50)]
        public string? CINNumber { get; set; } // Company Identification Number

        // Contact Info
        [MaxLength(150)]
        public string? Email { get; set; }

        [MaxLength(15)]
        public string Phone { get; set; }

        [MaxLength(15)]
        public string? AlternatePhone { get; set; }

        // Address Info
        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(10)]
        public string Pincode { get; set; }

        public BusinessOwnershipType OwnershipType { get; set; }
        public BusinessCategory BusinessCategory { get; set; }

        // Multi-Tenant
        [Required]
        public string TenantId { get; set; }
        public virtual Tenant Tenant { get; set; }

        // Location FK
        [ForeignKey(nameof(Country))]
        public string CountryId { get; set; }
        public virtual Country Country { get; set; }

        [ForeignKey(nameof(State))]
        public string StateId { get; set; }
        public virtual State State { get; set; }

        [ForeignKey(nameof(City))]
        public string CityId { get; set; }
        public virtual City City { get; set; }

        // Financial / Business
        public DateTime? IncorporationDate { get; set; }

        // Branding
        public string? Logo { get; set; }
        public string? WebsiteUrl { get; set; } = null;

        public override string GetSequencePrefix() => "CMP";
        // ✅ ADD THIS
        public ICollection<Branch> Branches { get; set; }
    }

    public class Branch : BaseEntity
    {
        // Basic Info
        [Required, MaxLength(150)]
        public string Name { get; set; }

        [Required, MaxLength(50)]
        public string Code { get; set; }

        // Company FK
        [Required]
        public string CompanyId { get; set; }
        public virtual Company Company { get; set; }

        // Multi-Tenant
        [Required]
        public string TenantId { get; set; }
        public virtual Tenant Tenant { get; set; }

        // Contact Info
        [MaxLength(150)]
        public string? Email { get; set; }

        [MaxLength(15)]
        public string Phone { get; set; }

        [MaxLength(15)]
        public string? AlternatePhone { get; set; }

        // Address
        [MaxLength(300)]
        public string Address { get; set; }

        [MaxLength(10)]
        public string Pincode { get; set; }

        // Location FK
        [ForeignKey(nameof(Country))]
        public string CountryId { get; set; }
        public virtual Country Country { get; set; }

        [ForeignKey(nameof(State))]
        public string StateId { get; set; }
        public virtual State State { get; set; }

        [ForeignKey(nameof(City))]
        public string CityId { get; set; }
        public virtual City City { get; set; }

        // GST (Important for India)
        [MaxLength(15)]
        public string? GSTNumber { get; set; }

        public override string GetSequencePrefix() => "BR";
        // Branch Type
        public bool IsHeadOffice { get; set; } = false;
    }
}
