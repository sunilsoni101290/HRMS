using System;
using System.Collections.Generic;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    public class LeaveBalance : BaseEntity
    {
        public string EmployeeId { get; set; }

        public string LeaveTypeId { get; set; }

        public int Year { get; set; }

        public decimal OpeningBalance { get; set; }

        public decimal Allocated { get; set; }

        public decimal Credited { get; set; }

        public decimal CarryForward { get; set; }

        public decimal Used { get; set; }

        public decimal Balance { get; set; }

        #region Navigation Properties

        public virtual Employee Employee { get; set; }

        public virtual LeaveType LeaveType { get; set; }

        public override string GetSequencePrefix() => "LB";

        #endregion
    }

    public class LeaveBalanceTransaction : BaseEntity
    {
        public string EmployeeId { get; set; }

        public string LeaveTypeId { get; set; }

        public int Year { get; set; }

        public LeaveTransactionType TransactionType { get; set; }

        public decimal Quantity { get; set; }

        public decimal BalanceBefore { get; set; }

        public decimal BalanceAfter { get; set; }

        public string? Remarks { get; set; }

        public DateTime TransactionDate { get; set; }

        #region Navigation Properties

        public virtual Employee Employee { get; set; }

        public virtual LeaveType LeaveType { get; set; }

        public override string GetSequencePrefix() => "LBT";

        #endregion
    }
}
