using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Application.DTOs.Masters
{
    public class StateDto
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "State Name is required")]
        [MaxLength(100)]
        [Display(Name = "State Name")]
        public string Name { get; set; }          // Maharashtra

        [Required(ErrorMessage = "State Code is required")]
        [MaxLength(10)]
        [Display(Name = "State Code")]
        public string Code { get; set; }          // MH

        [Required(ErrorMessage = "Country Name is required")]
        [Display(Name = "Country")]
        public string CountryId { get; set; }

        public string? CountryName { get; set; }

        [Required(ErrorMessage = "GST State Code is required")]
        [MaxLength(10)]
        [Display(Name = "GST State Code")]
        public string GSTStateCode { get; set; }  // 27

        public string? TenantId { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }
    }
}
