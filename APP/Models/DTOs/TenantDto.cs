using System.ComponentModel.DataAnnotations;

namespace APP.Models.DTOs
{
    public class TenantDto
    {
        public string? Id { get; set; }

        #region Basic Info

        [Required(ErrorMessage = "Tenant Name is required.")]
        [Display(Name = "Tenant Name")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Tenant Code is required.")]
        [Display(Name = "Tenant Code")]
        public string Code { get; set; }

        #endregion

        #region Domain

        [Display(Name = "Domain")]
        public string? Domain { get; set; }

        [Display(Name = "Sub Domain")]
        public string? SubDomain { get; set; }

        #endregion

        #region Contact

        [EmailAddress]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Required]
        [Display(Name = "Phone")]
        public string Phone { get; set; }

        #endregion

        #region Address

        [Required]
        public string Address { get; set; }

        [Required]
        public string Pincode { get; set; }

        #endregion

        #region Location

        [Required]
        [Display(Name = "Country")]
        public string CountryId { get; set; }

        public string? CountryName { get; set; }

        [Required]
        [Display(Name = "State")]
        public string StateId { get; set; }

        public string? StateName { get; set; }

        [Required]
        [Display(Name = "City")]
        public string CityId { get; set; }

        public string? CityName { get; set; }

        #endregion

        #region Subscription

        [Display(Name = "Subscription Start Date")]
        public DateTime? SubscriptionStartDate { get; set; }

        [Display(Name = "Subscription End Date")]
        public DateTime? SubscriptionEndDate { get; set; }

        #endregion

        #region Branding

        public string? Logo { get; set; }
        public IFormFile? LogoFile { get; set; }

        public string? WebsiteUrl { get; set; }

        #endregion

        #region Security

        public string? ConnectionString { get; set; }

        #endregion

        #region Audit

        public DateTime CreatedOn { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime? ModifiedOn { get; set; }

        public string? ModifiedBy { get; set; }

        public bool IsActive { get; set; }

        #endregion
    }
}
