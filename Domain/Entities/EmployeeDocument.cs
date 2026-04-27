using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class EmployeeDocument : BaseEntity
    {
        public string EmployeeId { get; set; }
        public string DocumentType { get; set; }
        public string FilePath { get; set; }
        public Employee Employee { get; set; }
        public override string GetSequencePrefix() => "EDC";
    }
}
