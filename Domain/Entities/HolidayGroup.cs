using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Domain.Entities
{
    public class HolidayGroup : BaseEntity
    {
        [Required]
        [MaxLength(150)]
        [Display(Name = "Holiday Group Name")]
        public string Name { get; set; }

        [MaxLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        // Navigation
        public ICollection<HolidayGroupDetail> HolidayGroupDetails { get; set; }

        public override string GetSequencePrefix() => "HGR";
    }

    public class HolidayGroupDetail : BaseEntity
    {
        [Required]
        public string HolidayGroupId { get; set; }

        [ForeignKey(nameof(HolidayGroupId))]
        public HolidayGroup HolidayGroup { get; set; }

        [Required]
        [Display(Name = "Holiday Date")]
        public DateTime HolidayDate { get; set; }

        [Required]
        [MaxLength(200)]
        [Display(Name = "Holiday Name")]
        public string HolidayName { get; set; }

        [MaxLength(500)]
        [Display(Name = "Remarks")]
        public string? Remarks { get; set; }

        [Display(Name = "Is Optional Holiday")]
        public bool IsOptional { get; set; } = false;
        public override string GetSequencePrefix() => "HGD";
    }
}
