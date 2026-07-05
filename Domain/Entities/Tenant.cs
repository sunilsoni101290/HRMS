
using Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    public class Tenant :IEntity
    {
        [Key]
        public string Id { get; set; }

        #region Basic Info

        [Required(ErrorMessage = "Tenant Name is required.")]
        [Display(Name = "Tenant Name")]
        [StringLength(200, ErrorMessage = "Tenant Name cannot exceed 200 characters.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Tenant Code is required.")]
        [Display(Name = "Tenant Code")]
        [StringLength(100, ErrorMessage = "Tenant Code cannot exceed 100 characters.")]
        public string Code { get; set; }

        #endregion

        #region Domain

        [Display(Name = "Domain")]
        [StringLength(200, ErrorMessage = "Domain cannot exceed 200 characters.")]
        public string? Domain { get; set; }

        [Display(Name = "Sub Domain")]
        [StringLength(200, ErrorMessage = "Sub Domain cannot exceed 200 characters.")]
        public string? SubDomain { get; set; }

        #endregion

        #region Contact Information

        [Display(Name = "Email Address")]
        [EmailAddress(ErrorMessage = "Invalid Email Address.")]
        [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Phone Number is required.")]
        [Display(Name = "Phone Number")]
        [Phone(ErrorMessage = "Invalid Phone Number.")]
        [StringLength(15, MinimumLength = 10,
            ErrorMessage = "Phone Number must be between 10 and 15 digits.")]
        public string Phone { get; set; }

        #endregion

        #region Address

        [Required(ErrorMessage = "Address is required.")]
        [Display(Name = "Address")]
        [StringLength(300, ErrorMessage = "Address cannot exceed 300 characters.")]
        public string Address { get; set; }

        [Required(ErrorMessage = "Pincode is required.")]
        [Display(Name = "Pincode")]
        [RegularExpression(@"^\d{6}$",
            ErrorMessage = "Pincode must be 6 digits.")]
        public string Pincode { get; set; }

        #endregion

        #region Location

        [Required(ErrorMessage = "Country is required.")]
        [Display(Name = "Country")]
        public string CountryId { get; set; }

        public Country Country { get; set; }

        [Required(ErrorMessage = "State is required.")]
        [Display(Name = "State")]
        public string StateId { get; set; }

        public State State { get; set; }

        [Required(ErrorMessage = "City is required.")]
        [Display(Name = "City")]
        public string CityId { get; set; }

        public City City { get; set; }

        #endregion

        #region Subscription

        [Display(Name = "Subscription Start Date")]
        [DataType(DataType.Date)]
        public DateTime? SubscriptionStartDate { get; set; }

        [Display(Name = "Subscription End Date")]
        [DataType(DataType.Date)]
        public DateTime? SubscriptionEndDate { get; set; }

        //[Required(ErrorMessage = "Plan Name is required.")]
        //[Display(Name = "Plan Name")]
        //[StringLength(50)]
        //public string PlanName { get; set; }

        //[Required(ErrorMessage = "Maximum Users is required.")]
        //[Display(Name = "Maximum Users")]
        //[Range(1, 100000, ErrorMessage = "Maximum Users must be greater than 0.")]
        //public int MaxUsers { get; set; }

        //[Required(ErrorMessage = "Maximum Branches is required.")]
        //[Display(Name = "Maximum Branches")]
        //[Range(1, 1000, ErrorMessage = "Maximum Branches must be greater than 0.")]
        //public int MaxBranches { get; set; }

        #endregion

        #region Branding

        [Display(Name = "Logo")]
        public string? Logo { get; set; }

        [Display(Name = "Website URL")]
        [Url(ErrorMessage = "Invalid Website URL.")]
        [StringLength(250)]
        public string? WebsiteUrl { get; set; }

        #endregion

        #region Security

        [Display(Name = "Connection String")]
        public string? ConnectionString { get; set; }

        #endregion

        #region Audit

        [Display(Name = "Created On")]
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        [Required]
        [Display(Name = "Created By")]
        public string CreatedBy { get; set; }

        [Display(Name = "Modified On")]
        public DateTime? ModifiedOn { get; set; }

        [Display(Name = "Modified By")]
        public string? ModifiedBy { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        #endregion

        #region Navigation

        public ICollection<Company> Companies { get; set; } = new List<Company>();

        public virtual string GetSequencePrefix()
        {
            return "TN"; // default
        }

        public string GetKeyPrefix()
        {
            return GetSequencePrefix();
        }

        #endregion
    }
}
