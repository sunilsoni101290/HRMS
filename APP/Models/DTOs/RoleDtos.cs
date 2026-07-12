using System.ComponentModel.DataAnnotations;

namespace APP.Models.DTOs
{
    public class RoleDto
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Role name is required.")]
        [MaxLength(100)]
        [Display(Name = "Role Name")]
        public string Name { get; set; }

        [MaxLength(50)]
        [Display(Name = "Code")]
        public string? Code { get; set; }

        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        public string? TenantId { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }
    }

    public class RoleListDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string? Code { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public int UserCount { get; set; }
        public int PermissionCount { get; set; }
        public DateTime CreatedOn { get; set; }
    }

    public class RolePermissionItemDto
    {
        public string PermissionId { get; set; }
        public string PermissionName { get; set; }
        public string Action { get; set; }
        public string? FeatureId { get; set; }
        public string? FeatureName { get; set; }
        public bool IsAssigned { get; set; }
    }

    public class RolePermissionGroupDto
    {
        public string Module { get; set; }
        public List<RolePermissionItemDto> Permissions { get; set; } = new();
    }

    public class RoleUserSummaryDto
    {
        public string UserId { get; set; }
        public string Username { get; set; }
        public string? EmployeeName { get; set; }
        public bool IsActive { get; set; }
    }

    public class RoleDetailDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string? Code { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }

        public List<RolePermissionGroupDto> PermissionGroups { get; set; } = new();
        public List<RoleUserSummaryDto> Users { get; set; } = new();
    }

    public class AssignRolePermissionsRequestDto
    {
        public string RoleId { get; set; }
        public List<string> PermissionIds { get; set; } = new();
        public string? ModifiedBy { get; set; }
    }
}
