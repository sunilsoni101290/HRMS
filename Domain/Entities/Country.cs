using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class Country :BaseEntity
    {
        public string Name { get; set; }      // India
        public string Code { get; set; }      // IN

        public string PhoneCode { get; set; } // +91
        public ICollection<State> States { get; set; }
        public override string GetSequencePrefix() => "C";
    }
}
