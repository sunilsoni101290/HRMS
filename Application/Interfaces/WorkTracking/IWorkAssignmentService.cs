using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.WorkTracking;

namespace Application.Interfaces.WorkTracking
{
    /// <summary>
    /// Manager -> Employee Job/Work Assignment workflow (spec sections
    /// 2-9, 20-25) - the layer that sits between the Job master and
    /// DailyWorkEntry. Every write here re-validates the assigner's
    /// authority over the target employee, and every read scopes results
    /// to what the acting user is actually allowed to see (spec section 20)
    /// - never trusts frontend filtering alone.
    /// </summary>
    public interface IWorkAssignmentService
    {
        /// <summary>Manager assigns Job/Structure/Activity to an Employee. Validates the assigner is authorized over the target employee (reporting hierarchy or HR/Admin override) and that Job/JobItem/Activity form a valid chain (spec section 23).</summary>
        Task<EmployeeWorkAssignmentDto> AssignAsync(SaveEmployeeWorkAssignmentDto dto, string actingUserId, string tenantId);

        /// <summary>"My Assigned Jobs" - the caller's own assignments only (spec section 7).</summary>
        Task<List<EmployeeWorkAssignmentSummaryDto>> GetMyAssignmentsAsync(string actingUserId, string tenantId);

        /// <summary>Full detail - owner, the assigner, the owner's reporting manager, or HR/Admin only.</summary>
        Task<EmployeeWorkAssignmentDto> GetByIdAsync(string id, string actingUserId, string tenantId);

        /// <summary>Assignments made by the acting user, plus (for a Team Leader/Manager) their team's assignments (spec section 20). HR/Admin see the full tenant scope.</summary>
        Task<List<EmployeeWorkAssignmentSummaryDto>> GetAssignedByMeOrTeamAsync(string actingUserId, string tenantId);

        /// <summary>Employee-side lifecycle move: Assigned -> Accepted -> InProgress -> Completed, or Assigned -> Rejected/Returned. Only the assignment's own employee may call this; only forward-legal transitions are accepted.</summary>
        Task<EmployeeWorkAssignmentDto> UpdateStatusAsync(string id, UpdateAssignmentStatusDto dto, string actingUserId, string tenantId);

        /// <summary>Reassign to a different employee - the OLD row is marked Returned (never deleted/overwritten) and a new assignment is created for the new employee, linked back via ReassignedFromId (spec section 25).</summary>
        Task<EmployeeWorkAssignmentDto> ReassignAsync(string id, ReassignEmployeeWorkAssignmentDto dto, string actingUserId, string tenantId);

        /// <summary>The flat, assignment-scoped Job/Structure/Activity combo list Daily Work Entry's cascading dropdowns filter client-side (spec section 9) - only the caller's own active (Assigned/Accepted/InProgress) assignments.</summary>
        Task<List<AssignedWorkComboDto>> GetMyAssignedWorkComboAsync(string actingUserId, string tenantId);

        /// <summary>"My Work" dashboard summary cards (spec section 18).</summary>
        Task<MyWorkDashboardDto> GetMyWorkDashboardAsync(string actingUserId, string tenantId);

        /// <summary>Manager/Team Leader "Team Work Overview" (spec section 19) - scoped to their reportees only, or the full tenant for HR/Admin.</summary>
        Task<TeamWorkOverviewDto> GetTeamWorkOverviewAsync(string actingUserId, string tenantId);

        /// <summary>Employee Assignment Report (spec section 26) - same visibility scoping as GetAssignedByMeOrTeamAsync.</summary>
        Task<List<EmployeeAssignmentReportRowDto>> GetEmployeeAssignmentReportAsync(string actingUserId, string tenantId);
    }
}
