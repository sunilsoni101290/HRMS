using System;
using System.Collections.Generic;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Application.DTOs.Company
{
    public class CompanyDto
    {
        public string? Id { get; set; }

        // Basic Info
        public string Name { get; set; }
        public string Code { get; set; }

        // Legal Details
        public string? GSTNumber { get; set; }
        public string? PANNumber { get; set; }
        public string? CINNumber { get; set; }

        // Contact Info
        public string? Email { get; set; }
        public string Phone { get; set; }
        public string? AlternatePhone { get; set; }

        // Address
        public string? Address { get; set; }
        public string Pincode { get; set; }

        // Business
        public BusinessOwnershipType OwnershipType { get; set; }
        public BusinessCategory BusinessCategory { get; set; }

        // Multi Tenant
        public string TenantId { get; set; }

        // Location
        public string CountryId { get; set; }
        public string StateId { get; set; }
        public string CityId { get; set; }

        // Financial
        public DateTime? IncorporationDate { get; set; }

        // Branding
        public string? Logo { get; set; }
        public string? WebsiteUrl { get; set; }
    }

    public class CompanyListDto
    {
        public string Id { get; set; }

        public string Name { get; set; }
        public string Code { get; set; }

        public string? GSTNumber { get; set; }

        public string Phone { get; set; }
        public string? Email { get; set; }

        public string? CountryName { get; set; }
        public string? StateName { get; set; }
        public string? CityName { get; set; }

        public string? Logo { get; set; }
    }
}
