using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Application.DTOs.Company
{
    public class CompanyDto
    {
        public string? Id { get; set; }

        // Basic Info
        [Required(ErrorMessage = "Company Name is required")]
        [MaxLength(200)]
        [Display(Name = "Company Name")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Company Code is required")]
        [MaxLength(50)]
        [Display(Name = "Company Code")]
        public string Code { get; set; }

        // Legal Details
        [MaxLength(15)]
        [Display(Name = "GST Number")]
        public string? GSTNumber { get; set; }

        [MaxLength(10)]
        [Display(Name = "PAN Number")]
        public string? PANNumber { get; set; }

        [MaxLength(50)]
        [Display(Name = "CIN Number")]
        public string? CINNumber { get; set; }

        // Contact Info
        [EmailAddress]
        [MaxLength(150)]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Phone Number is required")]
        [MaxLength(15)]
        [Display(Name = "Phone Number")]
        public string Phone { get; set; }

        [MaxLength(15)]
        [Display(Name = "Alternate Phone")]
        public string? AlternatePhone { get; set; }

        // Address Info
        [MaxLength(500)]
        [Display(Name = "Address")]
        public string? Address { get; set; }

        [Required(ErrorMessage = "Pincode is required")]
        [MaxLength(10)]
        [Display(Name = "Pincode")]
        public string Pincode { get; set; }

        [Required]
        [Display(Name = "Ownership Type")]
        public BusinessOwnershipType OwnershipType { get; set; }

        [Required]
        [Display(Name = "Business Category")]
        public BusinessCategory BusinessCategory { get; set; }

        // Multi-Tenant
        [Required]
        public string TenantId { get; set; }

        public string? TenantName { get; set; }

        // Location
        [Required(ErrorMessage = "Country is required")]
        public string CountryId { get; set; }

        public string? CountryName { get; set; }

        [Required(ErrorMessage = "State is required")]
        public string StateId { get; set; }

        public string? StateName { get; set; }

        [Required(ErrorMessage = "City is required")]
        public string CityId { get; set; }

        public string? CityName { get; set; }

        // Financial / Business
        [DataType(DataType.Date)]
        [Display(Name = "Incorporation Date")]
        public DateTime? IncorporationDate { get; set; }

        // Branding
        [Display(Name = "Company Logo")]
        public string? Logo { get; set; }

        [Display(Name = "Website URL")]
        public string? WebsiteUrl { get; set; }

        public string CreatedBy { get; set; }

        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }

        // Navigation
        public List<BranchDto> Branches { get; set; } = new();
    }

    public class BranchDto
    {
        public string? Id { get; set; }

        // Basic Info
        [Required(ErrorMessage = "Branch Name is required")]
        [MaxLength(150)]
        [Display(Name = "Branch Name")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Branch Code is required")]
        [MaxLength(50)]
        [Display(Name = "Branch Code")]
        public string Code { get; set; }

        // Company
        [Required]
        public string CompanyId { get; set; }

        public string? CompanyName { get; set; }

        // Tenant
        [Required]
        public string TenantId { get; set; }

        public string? TenantName { get; set; }

        // Contact Info
        [EmailAddress]
        [MaxLength(150)]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Phone Number is required")]
        [MaxLength(15)]
        [Display(Name = "Phone Number")]
        public string Phone { get; set; }

        [MaxLength(15)]
        [Display(Name = "Alternate Phone")]
        public string? AlternatePhone { get; set; }

        // Address
        [Required(ErrorMessage = "Address is required")]
        [MaxLength(300)]
        [Display(Name = "Address")]
        public string Address { get; set; }

        [Required(ErrorMessage = "Pincode is required")]
        [MaxLength(10)]
        [Display(Name = "Pincode")]
        public string Pincode { get; set; }

        // Location
        [Required(ErrorMessage = "Country is required")]
        public string CountryId { get; set; }

        public string? CountryName { get; set; }

        [Required(ErrorMessage = "State is required")]
        public string StateId { get; set; }

        public string? StateName { get; set; }

        [Required(ErrorMessage = "City is required")]
        public string CityId { get; set; }

        public string? CityName { get; set; }

        // GST
        [MaxLength(15)]
        [Display(Name = "GST Number")]
        public string? GSTNumber { get; set; }

        [Display(Name = "CIN No")]
        public string? CINNo { get; set; }

        [Display(Name = "Is Head Office")]
        public bool IsHeadOffice { get; set; } = false;

        public string CreatedBy { get; set; }

        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }

        // Navigation
        public List<LocationDto> Locations { get; set; } = new();
    }

    public class LocationDto
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Location Name is required")]
        [MaxLength(200)]
        [Display(Name = "Location Name")]
        public string LocationName { get; set; }

        public string? TenantId { get; set; }
        public string? TenantName { get; set; }

        public string? CompanyId { get; set; }
        public string? CompanyName { get; set; }

        [MaxLength(50)]
        [Display(Name = "Location Code")]
        public string? LocationCode { get; set; }

        [Required(ErrorMessage = "Branch is required")]
        public string BranchId { get; set; }

        public string? BranchName { get; set; }

        [Display(Name = "Address")]
        public string? Address { get; set; }

        [Display(Name = "Default Location")]
        public bool IsDefault { get; set; }

        public string CreatedBy { get; set; }

        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }
    }
}
