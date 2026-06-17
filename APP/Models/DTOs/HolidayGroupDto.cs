using System.ComponentModel.DataAnnotations;

namespace APP.Models.DTOs
{
    public class HolidayGroupDto
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Holiday Group Name is required")]
        [MaxLength(150)]
        [Display(Name = "Holiday Group Name")]
        public string Name { get; set; }

        [MaxLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }
        public string? TenantId { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }
    }

    public class HolidayGroupDetailDto
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Holiday Group is required")]
        [Display(Name = "Holiday Group Name")] 
        public string HolidayGroupId { get; set; }

        public string? HolidayGroupName { get; set; }

        [Required(ErrorMessage = "Holiday Date is required")]
        [Display(Name = "Holiday Date")]
        [DataType(DataType.Date)]
        public DateTime HolidayDate { get; set; }

        [Required(ErrorMessage = "Holiday Name is required")]
        [MaxLength(200)]
        [Display(Name = "Holiday Name")]
        public string HolidayName { get; set; }

        [MaxLength(500)]
        [Display(Name = "Remarks")]
        public string? Remarks { get; set; }

        [Display(Name = "Is Optional Holiday")]
        public bool IsOptional { get; set; } = false;

        public string? TenantId { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }
    }
}
