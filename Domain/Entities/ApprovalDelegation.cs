using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    // Lets a Reporting Manager or Department Head (Level 1/2 of the Leave
    // Application approval chain - see LeaveApplicationService) designate a
    // proxy approver for a date range, e.g. while they are on vacation.
    // While a delegation is active and "today" falls inside
    // [StartDate, EndDate], the delegate is treated as equally authorized as
    // the delegator for any leave application currently awaiting the
    // delegator's approval - see
    // LeaveApplicationService.IsAuthorizedForLevelAsync and
    // IApprovalDelegationService.GetActiveDelegateForAsync.
    public class ApprovalDelegation : BaseEntity
    {
        // The manager who is going to be away and is handing off their
        // approval authority.
        public string DelegatorEmployeeId { get; set; }
        public virtual Employee DelegatorEmployee { get; set; }

        // The proxy who may act on the delegator's behalf for the date range.
        public string DelegateEmployeeId { get; set; }
        public virtual Employee DelegateEmployee { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public string? Reason { get; set; }

        // Lets a delegation be revoked early (the manager comes back sooner
        // than planned) without deleting the row - IsDeleted (BaseEntity) is
        // reserved for actually removing a record; this is a normal business
        // toggle that keeps the audit trail (who delegated to whom, and
        // when) intact even after revocation.
        public bool IsActive { get; set; } = true;

        public override string GetSequencePrefix() => "DEL";
    }
}
