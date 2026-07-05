using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Assets
{
    // ==============================
    // Asset Category DTOs
    // ==============================

    public class AssetCategoryDto
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Please enter Category Name.")]
        [MaxLength(150)]
        [Display(Name = "Category Name")]
        public string Name { get; set; }

        // Multi Tenant
        public string? TenantId { get; set; }

        public string CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }
    }

    public class AssetCategoryListDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int AssetCount { get; set; }
        public bool IsActive { get; set; }
    }

    // ==============================
    // Asset DTOs
    // ==============================

    public class AssetDto
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Please enter Asset Name.")]
        [MaxLength(150)]
        [Display(Name = "Asset Name")]
        public string Name { get; set; }

        [MaxLength(100)]
        [Display(Name = "Asset Code")]
        public string? AssetCode { get; set; }

        [MaxLength(100)]
        [Display(Name = "Serial Number")]
        public string? SerialNumber { get; set; }

        [Required(ErrorMessage = "Please select a Category.")]
        [Display(Name = "Category")]
        public string AssetCategoryId { get; set; }
        public string? AssetCategoryName { get; set; }

        [Display(Name = "Purchase Date")]
        [DataType(DataType.Date)]
        public DateTime? PurchaseDate { get; set; }

        [Display(Name = "Purchase Cost")]
        public decimal? PurchaseCost { get; set; }

        [MaxLength(150)]
        [Display(Name = "Vendor Name")]
        public string? VendorName { get; set; }

        [Display(Name = "Warranty Expiry Date")]
        [DataType(DataType.Date)]
        public DateTime? WarrantyExpiryDate { get; set; }

        // Available / Allocated / UnderMaintenance / Scrap
        [Display(Name = "Status")]
        public string? Status { get; set; }

        // Multi Tenant
        public string? TenantId { get; set; }

        [Display(Name = "Company")]
        public string? CompanyId { get; set; }
        public string? CompanyName { get; set; }

        [Display(Name = "Branch")]
        public string? BranchId { get; set; }
        public string? BranchName { get; set; }

        [Display(Name = "Description")]
        public string? Description { get; set; }

        public string CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }

        // Details view helpers
        public List<AssetHistoryDto> Histories { get; set; } = new();
        public List<AssetAllocationListDto> Allocations { get; set; } = new();
    }

    public class AssetListDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string? AssetCode { get; set; }
        public string? SerialNumber { get; set; }

        public string? AssetCategoryId { get; set; }
        public string? AssetCategoryName { get; set; }

        public DateTime? PurchaseDate { get; set; }
        public decimal? PurchaseCost { get; set; }
        public DateTime? WarrantyExpiryDate { get; set; }

        public string? Status { get; set; }

        public string? CompanyId { get; set; }
        public string? CompanyName { get; set; }
        public string? BranchId { get; set; }
        public string? BranchName { get; set; }

        public string? CurrentEmployeeName { get; set; }
    }

    // ==============================
    // Asset Allocation DTOs
    // ==============================

    public class AssetAllocationDto
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Please select an Asset.")]
        [Display(Name = "Asset")]
        public string AssetId { get; set; }
        public string? AssetName { get; set; }

        [Required(ErrorMessage = "Please select an Employee.")]
        [Display(Name = "Employee")]
        public string EmployeeId { get; set; }
        public string? EmployeeName { get; set; }

        [Required(ErrorMessage = "Please select the Allocation Date.")]
        [Display(Name = "Allocated On")]
        [DataType(DataType.Date)]
        public DateTime AllocatedOn { get; set; } = DateTime.UtcNow;

        [Display(Name = "Returned On")]
        [DataType(DataType.Date)]
        public DateTime? ReturnedOn { get; set; }

        // Domain.Enums.EnumExtensions.AllocationStatus
        [Display(Name = "Status")]
        public int AllocationStatus { get; set; } = 1; // Allocated
        public string? AllocationStatusText { get; set; }

        [Display(Name = "Condition On Issue")]
        public string? ConditionOnIssue { get; set; }

        [Display(Name = "Condition On Return")]
        public string? ConditionOnReturn { get; set; }

        [Display(Name = "Remarks")]
        public string? Remarks { get; set; }

        // Multi Tenant
        public string? TenantId { get; set; }

        public string CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }
    }

    public class AssetAllocationListDto
    {
        public string Id { get; set; }

        public string? AssetId { get; set; }
        public string? AssetName { get; set; }
        public string? AssetCode { get; set; }

        public string? EmployeeId { get; set; }
        public string? EmployeeName { get; set; }

        public DateTime AllocatedOn { get; set; }
        public DateTime? ReturnedOn { get; set; }

        public int AllocationStatus { get; set; }
        public string? AllocationStatusText { get; set; }

        public string? ConditionOnIssue { get; set; }
        public string? ConditionOnReturn { get; set; }
        public string? Remarks { get; set; }
    }

    // ==============================
    // Asset History DTO
    // ==============================

    public class AssetHistoryDto
    {
        public string Id { get; set; }
        public string? AssetId { get; set; }
        public string? Action { get; set; }
        public string? ReferenceId { get; set; }
        public DateTime ActionDate { get; set; }
        public string? PerformedBy { get; set; }
    }
}
