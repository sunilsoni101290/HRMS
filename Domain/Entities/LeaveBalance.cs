using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class LeaveBalance : BaseEntity
    {
        public string EmployeeId { get; set; }
        public Employee Employee { get; set; }

        public string LeaveTypeId { get; set; }
        public LeaveType LeaveType { get; set; }

        public int Year { get; set; }

        public decimal OpeningBalance { get; set; }
        public decimal Earned { get; set; }
        public decimal Used { get; set; }
        public decimal Balance { get; set; }
        public override string GetSequencePrefix() => "LB";
    }
}
