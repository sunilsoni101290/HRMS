using System.ComponentModel.DataAnnotations;

namespace APP.Models.DTOs
{
    public class PermissionDto
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Permission name is required.")]
        [MaxLength(150)]
        [Display(Name = "Permission Name")]
        public string Name { get; set; }

        [MaxLength(100)]
        [Display(Name = "Code")]
        public string? Code { get; set; }

        [Required(ErrorMessage = "Module is required.")]
        [MaxLength(100)]
        [Display(Name = "Module")]
        public string Module { get; set; }

        [Required(ErrorMessage = "Please select the feature this permission applies to.")]
        [Display(Name = "Feature")]
        public string FeatureId { get; set; }
        public string? FeatureName { get; set; }

        [Required(ErrorMessage = "Please select an action.")]
        [Display(Name = "Action")]
        public string Action { get; set; }

        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; }

        public string? CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }
    }

    public class PermissionListDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string? Code { get; set; }
        public string Module { get; set; }
        public string? FeatureId { get; set; }
        public string? FeatureName { get; set; }
        public string Action { get; set; }
        public int RoleCount { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}
