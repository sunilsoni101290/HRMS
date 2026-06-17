using System.ComponentModel.DataAnnotations;

namespace APP.Models.DTOs
{
    public class CityDto
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "City Name is required")]
        [MaxLength(100)]
        [Display(Name = "City Name")]
        public string Name { get; set; }      // Mumbai

        [Required(ErrorMessage = "State is required")]
        [Display(Name = "State")]
        public string StateId { get; set; }

        public string? StateName { get; set; }

        public string? CountryId { get; set; }

        public string? CountryName { get; set; }

        public string? TenantId { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }

    }
}
