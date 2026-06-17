using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Application.DTOs.Masters
{
    public class CountryDto
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "State is required")]
        [MaxLength(150)]
        public string Name { get; set; } = default!;

        [Required(ErrorMessage = "Code is required")]
        [MaxLength(10)]
        public string Code { get; set; } = default!;

        [Required]
        [MaxLength(10)]
        [Display(Name = "Phone Code")]
        public string PhoneCode { get; set; } = default!;

        public string? TenantId { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }
    }
}
