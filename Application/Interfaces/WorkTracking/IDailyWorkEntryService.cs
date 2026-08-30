using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.WorkTracking;

namespace Application.Interfaces.WorkTracking
{
    /// <summary>
    /// Daily Work Entry transaction service - Draft/Submit/Approve/Reject
    /// workflow (spec sections 17/18) over the DailyWorkLog header +
    /// DailyWorkEntry lines. EmployeeId is NEVER accepted as a parameter
    /// from client-controlled DTOs for self-service actions - every method
    /// resolves the acting employee server-side from actingUserId, exactly
    /// like WfhRequestService (spec section 35).
    /// </summary>
    public interface IDailyWorkEntryService
    {
        /// <summary>Fetches (or returns an empty draft shape for) the caller's own DailyWorkLog for a date - backs the Daily Work Entry screen's initial load.</summary>
        Task<DailyWorkLogDto> GetMyEntryForDateAsync(string workDate, string actingUserId, string tenantId);

        /// <summary>Save Draft - creates or updates the caller's own header+lines for the date, always leaving Status = Draft.</summary>
        Task<DailyWorkLogDto> SaveDraftAsync(SaveDailyWorkLogDto dto, string actingUserId, string tenantId);

        /// <summary>Submit - saves the lines (if provided) then transitions the header to Pending, snapshotting Clocked Hours from Attendance.</summary>
        Task<DailyWorkLogDto> SubmitAsync(SaveDailyWorkLogDto dto, string actingUserId, string tenantId);

        /// <summary>My Work Entries - the caller's own history, most recent first.</summary>
        Task<List<DailyWorkLogSummaryDto>> GetMyEntriesAsync(string actingUserId, string tenantId, System.DateTime? fromDate, System.DateTime? toDate);

        /// <summary>Full header+lines for one DailyWorkLog - only the owning employee, their reporting manager, or HR/Admin may view it.</summary>
        Task<DailyWorkLogDto> GetByIdAsync(string id, string actingUserId, string tenantId);

        /// <summary>Team Leader Approval queue - only entries whose Employee.ReportingManagerId is the acting employee (or all, for HR/Admin).</summary>
        Task<List<DailyWorkLogSummaryDto>> GetPendingApprovalAsync(string actingUserId, string tenantId);

        Task<DailyWorkLogDto> ApproveAsync(string id, string actingUserId, string tenantId);

        Task<DailyWorkLogDto> RejectAsync(string id, RejectDailyWorkLogDto reason, string actingUserId, string tenantId);
    }
}
