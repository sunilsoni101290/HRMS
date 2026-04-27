using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Text;

namespace Domain.Entities
{
    public class State :BaseEntity
    {
        public string Name { get; set; }          // Maharashtra
        public string Code { get; set; }          // MH

        public string CountryId { get; set; }
        public Country Country { get; set; }

        public string GSTStateCode { get; set; }  // 27 (Important for GST)

         // Navigation
        public ICollection<City> Cities { get; set; }
        public override string GetSequencePrefix() => "ST";
    }
}
