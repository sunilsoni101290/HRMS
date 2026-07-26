using System;

namespace Application.DTOs.Leaves
{
    // Read-only helper DTO for the "which optional holidays can I still
    // claim as a Restricted Holiday" widget - see
    // LeaveApplicationService.GetAvailableRestrictedHolidaysAsync.
    public class AvailableRestrictedHolidayDto
    {
        public DateTime HolidayDate { get; set; }

        public string HolidayName { get; set; }

        // True if this employee already has a Pending or Approved
        // "Restricted Holiday" LeaveApplication for this date - shown so
        // the UI can stop them from trying to double-claim the same date
        // while a request is still awaiting approval.
        public bool AlreadyClaimed { get; set; }
    }
}
