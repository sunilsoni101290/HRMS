
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    public class Tenant : BaseEntity
    {
        // Basic Info
        [Required, MaxLength(200)]
        public string Name { get; set; }

        [Required, MaxLength(100)]
        public string Code { get; set; } // Unique (e.g. T001)

        // Domain / पहचान
        [MaxLength(200)]
        public string? Domain { get; set; } // e.g. client.myerp.com

        [MaxLength(200)]
        public string? SubDomain { get; set; } // e.g. client1

        // Contact Info
        [MaxLength(150)]
        public string? Email { get; set; }

        [MaxLength(15)]
        public string Phone { get; set; }

        // Address
        [MaxLength(300)]
        public string Address { get; set; }

        [MaxLength(10)]
        public string Pincode { get; set; }

        // Location
        public string CountryId { get; set; }
        public Country Country { get; set; }

        public string StateId { get; set; }
        public State State { get; set; }

        public string CityId { get; set; }
        public City City { get; set; }

        // Subscription / Plan
        public DateTime? SubscriptionStartDate { get; set; }
        public DateTime? SubscriptionEndDate { get; set; }

        public string PlanName { get; set; } // Basic / Premium / Enterprise
        public int MaxUsers { get; set; }
        public int MaxBranches { get; set; }

        // Branding
        public string? Logo { get; set; }
        public string? WebsiteUrl { get; set; }

        // Security
        public string? ConnectionString { get; set; } // Optional (for DB per tenant)

        // Navigation
        public ICollection<Company> Companies { get; set; }
        public override string GetSequencePrefix() => "TN";
    }
}
