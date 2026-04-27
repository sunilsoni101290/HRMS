using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class City:BaseEntity
    {
        public string Name { get; set; }      // Mumbai

        public string StateId { get; set; }
        public State State { get; set; }
        public override string GetSequencePrefix() => "CT";
    }
}
