using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Domain.Entities
{
    public class Asset : BaseEntity
    {
        [Required, MaxLength(150)]
        public string Name { get; set; }

        [MaxLength(100)]
        public string AssetCode { get; set; }

        [MaxLength(100)]
        public string SerialNumber { get; set; }

        // Category
        public string AssetCategoryId { get; set; }
        public virtual AssetCategory AssetCategory { get; set; }

        // Purchase Info
        public DateTime? PurchaseDate { get; set; }
        public decimal? PurchaseCost { get; set; }

        public string VendorName { get; set; }

        // Warranty
        public DateTime? WarrantyExpiryDate { get; set; }

        // Status
        public string Status { get; set; }
        // Available / Allocated / UnderMaintenance / Scrap

        // Location
        public string CompanyId { get; set; }
        public string BranchId { get; set; }
       
        // Extra
        public string Description { get; set; }

        public override string GetSequencePrefix() => "A";
    }
}
