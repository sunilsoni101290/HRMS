using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class AssetCategory : BaseEntity
    {
        public string Name { get; set; } // Laptop, Vehicle, Furniture
        public override string GetSequencePrefix() => "AC";
    }
}
