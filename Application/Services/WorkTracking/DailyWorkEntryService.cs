using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs.WorkTracking;
using Application.Interfaces.WorkTracking;
using Domain.Entities;
using Domain.Helper;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static Domain.Enums.EnumExtensions;

namespace Application.Services.WorkTracking
{
    /// <summary>
    /// Daily Work Entry Draft/Submit/Approve/Reject workflow - see
    /// IDailyWorkEntryService's remarks. Structurally mirrors
    /// WfhRequestService (single-level Reporting-Manager approval,
    /// GetActingEmployeeIdAsync/IsHrOrAdminAsync helpers, AssembleDto
    /// pattern) rather than Leave's multi-level chain, per the spec's
    /// "Team Leader" (singular) approval model.
    /// </summary>
    public class DailyWorkEntryService : IDailyWorkEntryService
    {
        private readonly ApplicationDbContext _context;

        public DailyWorkEntryService(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==================================================================
        // Read
        // ==================================================================

        public async Task<DailyWorkLogDto> GetMyEntryForDateAsync(string workDate, string actingUserId, string tenantId)
        {
            var employeeId = await GetActingEmployeeIdAsync(actingUserId)
                ?? throw new UnauthorizedAccessException("No employee record is linked to your account.");

            if (!DateTime.TryParse(workDate, out var date))
                throw new Exception("Invalid work date.");

            var entity = await GetEntityByEmployeeDateAsync(employeeId, date.Date, tenantId);

            if (entity == null)
            {
                // No header yet for this date - hand back an empty Draft
                // shape so the UI can render a blank form rather than
                // special-casing "not found".
                return new DailyWorkLogDto
                {
                    EmployeeId = employeeId,
                    WorkDate = date.Date,
                    Status = (int)ApprovalStatus.Draft,
                    StatusName = ApprovalStatus.Draft.ToString()
                };
            }

            return await AssembleDtoAsync(entity);
        }

        public async Task<DailyWorkLogDto> GetByIdAsync(string id, string actingUserId, string tenantId)
        {
            var entity = await GetEntityByIdAsync(id, tenantId);

            var actingEmployeeId = await GetActingEmployeeIdAsync(actingUserId);

            bool isOwn = !string.IsNullOrEmpty(actingEmployeeId) && entity.EmployeeId == actingEmployeeId;

            bool isReportingManager =
                !string.IsNullOrEmpty(entity.Employee?.ReportingManagerId) &&
                !string.IsNullOrEmpty(actingEmployeeId) &&
                entity.Employee.ReportingManagerId == actingEmployeeId;

            if (!isOwn && !isReportingManager && !await IsHrOrAdminForWorkTrackingAsync(actingUserId))
                throw new UnauthorizedAccessException("You are not authorized to view this entry.");

            return await AssembleDtoAsync(entity);
        }

        public async Task<List<DailyWorkLogSummaryDto>> GetMyEntriesAsync(string actingUserId, string tenantId, DateTime? fromDate, DateTime? toDate)
        {
            var employeeId = await GetActingEmployeeIdAsync(actingUserId);
            if (string.IsNullOrEmpty(employeeId))
                return new List<DailyWorkLogSummaryDto>();

            var query = _context.DailyWorkLogs.AsNoTracking()
                .Include(x => x.Entries)
                .Where(x => x.EmployeeId == employeeId && x.TenantId == tenantId);

            if (fromDate.HasValue) query = query.Where(x => x.WorkDate >= fromDate.Value.Date);
            if (toDate.HasValue) query = query.Where(x => x.WorkDate <= toDate.Value.Date);

            var entities = await query.OrderByDescending(x => x.WorkDate).ToListAsync();

            return entities.Select(ToSummaryDto).ToList();
        }

        public async Task<List<DailyWorkLogSummaryDto>> GetPendingApprovalAsync(string actingUserId, string tenantId)
        {
            var actingEmployeeId = await GetActingEmployeeIdAsync(actingUserId);
            var isHrOrAdmin = await IsHrOrAdminForWorkTrackingAsync(actingUserId);

            var query = _context.DailyWorkLogs.AsNoTracking()
                .Include(x => x.Employee)
                .Include(x => x.Entries)
                .Where(x => x.TenantId == tenantId && x.Status == ApprovalStatus.Pending);

            if (!isHrOrAdmin)
            {
                // Team Leader must only see entries they are authorized to
                // approve (spec section 18) - never every employee's queue.
                if (string.IsNullOrEmpty(actingEmployeeId))
                    return new List<DailyWorkLogSummaryDto>();

                query = query.Where(x => x.Employee.ReportingManagerId == actingEmployeeId);
            }

            var entities = await query.OrderBy(x => x.SubmittedAt).ToListAsync();

            return entities.Select(ToSummaryDto).ToList();
        }

        private static DailyWorkLogSummaryDto ToSummaryDto(DailyWorkLog x) => new()
        {
            Id = x.Id,
            EmployeeCode = x.Employee?.EmployeeCode ?? "",
            EmployeeName = x.Employee != null ? $"{x.Employee.FirstName} {x.Employee.LastName}".Trim() : "",
            WorkDate = x.WorkDate,
            TotalHours = x.Entries?.Sum(e => e.Hours) ?? 0,
            Status = (int)x.Status,
            StatusName = x.Status.ToString(),
            SubmittedAt = x.SubmittedAt
        };

        // ==================================================================
        // Save Draft / Submit
        // ==================================================================

        public async Task<DailyWorkLogDto> SaveDraftAsync(SaveDailyWorkLogDto dto, string actingUserId, string tenantId)
            => await UpsertAsync(dto, actingUserId, tenantId, submit: false);

        public async Task<DailyWorkLogDto> SubmitAsync(SaveDailyWorkLogDto dto, string actingUserId, string tenantId)
            => await UpsertAsync(dto, actingUserId, tenantId, submit: true);

        private async Task<DailyWorkLogDto> UpsertAsync(SaveDailyWorkLogDto dto, string actingUserId, string tenantId, bool submit)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            var employeeId = await GetActingEmployeeIdAsync(actingUserId)
                ?? throw new UnauthorizedAccessException("No employee record is linked to your account.");

            if (dto.Lines == null || dto.Lines.Count == 0)
                throw new Exception("At least one activity line is required.");

            var workDate = dto.WorkDate.Date;

            var header = await GetEntityByEmployeeDateAsync(employeeId, workDate, tenantId);

            if (header == null)
            {
                header = new DailyWorkLog
                {
                    Id = IDManager.GetNewId(new DailyWorkLog()),
                    TenantId = tenantId,
                    EmployeeId = employeeId,
                    WorkDate = workDate,
                    Status = ApprovalStatus.Draft,
                    CreatedBy = actingUserId,
                    CreatedOn = DateTime.UtcNow,
                    Entries = new List<DailyWorkEntry>()
                };
                _context.DailyWorkLogs.Add(header);
            }
            else
            {
                // Only Draft (or Rejected -> being corrected/resubmitted)
                // entries can be edited - an already-Submitted/Approved day
                // is read-only (spec section 34's "read-only states for
                // submitted/approved records").
                if (header.Status != ApprovalStatus.Draft && header.Status != ApprovalStatus.Rejected)
                    throw new Exception($"This day's entry cannot be edited - current status is {header.Status}.");

                header.ModifiedBy = actingUserId;
                header.ModifiedOn = DateTime.UtcNow;

                // Replace the line set wholesale - simplest, safest way to
                // reconcile add/edit/delete of lines within one Draft
                // without diffing client-supplied Ids (which are never
                // trusted anyway).
                var existingLines = await _context.DailyWorkEntries
                    .Where(x => x.DailyWorkLogId == header.Id)
                    .ToListAsync();
                _context.DailyWorkEntries.RemoveRange(existingLines);
            }

            decimal totalHours = 0;

            foreach (var line in dto.Lines)
            {
                var entry = await BuildAndValidateLineAsync(line, header.Id, tenantId, employeeId, workDate);
                entry.CreatedBy = actingUserId;
                entry.CreatedOn = DateTime.UtcNow;
                entry.TenantId = tenantId;

                _context.DailyWorkEntries.Add(entry);
                totalHours += entry.Hours;
            }

            // Hours validation (spec section 15) - configurable ceiling
            // kept simple (24h/day, matching DailyWorkEntry.Hours' own
            // [Range(0.01,24)]) rather than reading a separate settings
            // table that doesn't otherwise exist in this codebase.
            if (totalHours > 24)
                throw new Exception($"Total hours for the day ({totalHours:0.##}) cannot exceed 24.");

            if (submit)
            {
                // Clocked Hours snapshot from the EXISTING Attendance
                // module (spec section 16) - never duplicated, only read.
                var attendance = await _context.Attendances.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.EmployeeId == employeeId && x.TenantId == tenantId && x.Date.Date == workDate);

                header.ClockedHours = attendance?.TotalWorkingHours;
                header.Status = ApprovalStatus.Pending;
                header.SubmittedAt = DateTime.UtcNow;
                header.SubmittedBy = actingUserId;
                header.RejectedAt = null;
                header.RejectedBy = null;
                header.RejectionReason = null;

                _context.DailyWorkLogApprovalHistories.Add(new DailyWorkLogApprovalHistory
                {
                    Id = IDManager.GetNewId(new DailyWorkLogApprovalHistory()),
                    TenantId = tenantId,
                    DailyWorkLogId = header.Id,
                    ActionBy = actingUserId,
                    Action = ApprovalStatus.Pending,
                    ActionDate = DateTime.UtcNow,
                    CreatedBy = actingUserId,
                    CreatedOn = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            return await GetByIdInternalAsync(header.Id, tenantId);
        }

        // Validates every relationship server-side (spec section 14/35) -
        // never trusts that a JobItem really belongs to the posted WorkJob,
        // or that a WorkActivity really belongs to the posted JobType, or
        // that a WorkEntryReason's category matches the activity's, or
        // (spec section 24) that a posted AssignmentId really belongs to
        // THIS employee and really matches the Job/JobItem/Activity being
        // charged.
        private async Task<DailyWorkEntry> BuildAndValidateLineAsync(
            DailyWorkEntryLineInputDto line, string dailyWorkLogId, string tenantId, string employeeId, DateTime workDate)
        {
            if (line.Hours <= 0 || line.Hours > 24)
                throw new Exception("Hours must be greater than 0 and no more than 24.");

            var jobType = await _context.JobTypes
                .FirstOrDefaultAsync(x => x.Id == line.JobTypeId && x.TenantId == tenantId && x.IsActive);

            if (jobType == null)
                throw new Exception("Selected job type is invalid or inactive.");

            WorkJob? workJob = null;

            if (!string.IsNullOrEmpty(line.WorkJobId))
            {
                workJob = await _context.WorkJobs
                    .FirstOrDefaultAsync(x => x.Id == line.WorkJobId && x.TenantId == tenantId && x.IsActive && x.Status == WorkJobStatus.Active);

                if (workJob == null)
                    throw new Exception("Selected job is invalid or inactive.");
            }

            if (!string.IsNullOrEmpty(line.JobItemId))
            {
                if (string.IsNullOrEmpty(line.WorkJobId))
                    throw new Exception("A job must be selected before a structure/equipment/job item.");

                var jobItemBelongs = await _context.JobItems.AnyAsync(x =>
                    x.Id == line.JobItemId && x.WorkJobId == line.WorkJobId && x.TenantId == tenantId && x.IsActive);

                if (!jobItemBelongs)
                    throw new Exception("Selected structure/equipment/job item does not belong to the selected job.");
            }

            WorkActivity? activity = null;

            if (!string.IsNullOrEmpty(line.WorkActivityId))
            {
                activity = await _context.WorkActivities
                    .FirstOrDefaultAsync(x => x.Id == line.WorkActivityId && x.TenantId == tenantId && x.IsActive);

                if (activity == null || activity.JobTypeId != line.JobTypeId)
                    throw new Exception("Selected work activity does not belong to the selected job type.");
            }

            string? reasonId = null;

            if (activity != null && (activity.WorkCategory == WorkCategory.Idle || activity.WorkCategory == WorkCategory.Downtime))
            {
                if (string.IsNullOrEmpty(line.WorkEntryReasonId))
                    throw new Exception($"A reason is required for {activity.WorkCategory} hours.");

                var expectedCategory = activity.WorkCategory == WorkCategory.Idle
                    ? WorkEntryReasonCategory.Idle
                    : WorkEntryReasonCategory.Downtime;

                var reasonValid = await _context.WorkEntryReasons.AnyAsync(x =>
                    x.Id == line.WorkEntryReasonId && x.TenantId == tenantId && x.IsActive && x.Category == expectedCategory);

                if (!reasonValid)
                    throw new Exception($"Selected reason is invalid for {activity.WorkCategory} hours - Idle and Downtime reasons must never be mixed.");

                reasonId = line.WorkEntryReasonId;
            }

            // ==============================================================
            // Assignment validation (spec section 24) - a Direct/Indirect
            // line charged against a real Job must be backed by an
            // EmployeeWorkAssignment that (a) actually belongs to THIS
            // employee, (b) is still active, and (c) its own Job/JobItem/
            // Activity match what this line is posting. An Idle/Downtime
            // line never needs one (spec section 15 - Idle/Downtime don't
            // require a Job Assignment at all). If no assignment is posted
            // for a normal project line, an explicit AdhocReason is
            // required instead - the controlled "Unassigned/Ad-hoc Work"
            // exception (spec section 14); the system never silently lets a
            // Direct/Indirect line through against an arbitrary Job with no
            // assignment and no justification.
            // ==============================================================

            var isIdleOrDowntime = activity != null &&
                (activity.WorkCategory == WorkCategory.Idle || activity.WorkCategory == WorkCategory.Downtime);

            string? assignmentId = null;

            if (!string.IsNullOrEmpty(line.AssignmentId))
            {
                var assignment = await _context.EmployeeWorkAssignments
                    .FirstOrDefaultAsync(x => x.Id == line.AssignmentId && x.TenantId == tenantId);

                if (assignment == null || assignment.EmployeeId != employeeId)
                    throw new Exception("Selected assignment does not belong to you.");

                var liveStatuses = new[] { AssignmentStatus.Assigned, AssignmentStatus.Accepted, AssignmentStatus.InProgress };
                if (!liveStatuses.Contains(assignment.Status))
                    throw new Exception($"This assignment is {assignment.Status} and can no longer accept new work entries.");

                if (assignment.WorkJobId != line.WorkJobId)
                    throw new Exception("Selected assignment does not match the selected job.");

                // Job-level assignments may cover any of that job's
                // structures/activities; an assignment pinned to a specific
                // JobItem/Activity must match exactly.
                if (!string.IsNullOrEmpty(assignment.JobItemId) && assignment.JobItemId != line.JobItemId)
                    throw new Exception("Selected assignment does not match the selected structure/equipment/job item.");

                if (!string.IsNullOrEmpty(assignment.WorkActivityId) && assignment.WorkActivityId != line.WorkActivityId)
                    throw new Exception("Selected assignment does not match the selected work activity.");

                assignmentId = assignment.Id;
            }
            else if (!isIdleOrDowntime && workJob != null)
            {
                if (string.IsNullOrWhiteSpace(line.AdhocReason))
                    throw new Exception("This job has not been assigned to you - select one of your assigned jobs, or provide a reason for unassigned/ad-hoc work.");
            }

            return new DailyWorkEntry
            {
                Id = IDManager.GetNewId(new DailyWorkEntry()),
                DailyWorkLogId = dailyWorkLogId,
                WorkJobId = workJob?.Id,
                JobTypeId = jobType.Id,
                JobItemId = string.IsNullOrEmpty(line.JobItemId) ? null : line.JobItemId,
                WorkActivityId = activity?.Id,
                WorkEntryReasonId = reasonId,
                Hours = line.Hours,
                Remarks = line.Remarks?.Trim(),
                WorkDoneToday = isIdleOrDowntime ? null : line.WorkDoneToday?.Trim(),
                AssignmentId = assignmentId,
                AdhocReason = assignmentId == null && !isIdleOrDowntime ? line.AdhocReason?.Trim() : null
            };
        }

        // ==================================================================
        // Approval
        // ==================================================================

        public async Task<DailyWorkLogDto> ApproveAsync(string id, string actingUserId, string tenantId)
        {
            var header = await _context.DailyWorkLogs
                .Include(x => x.Employee)
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId)
                ?? throw new Exception("Entry not found.");

            if (header.Status != ApprovalStatus.Pending)
                throw new Exception($"Only submitted entries can be approved - current status is {header.Status}.");

            await EnsureApproverAuthorizedAsync(header, actingUserId);

            header.Status = ApprovalStatus.Approved;
            header.ApprovedAt = DateTime.UtcNow;
            header.ApprovedBy = actingUserId;
            header.ModifiedBy = actingUserId;
            header.ModifiedOn = DateTime.UtcNow;

            _context.DailyWorkLogApprovalHistories.Add(new DailyWorkLogApprovalHistory
            {
                Id = IDManager.GetNewId(new DailyWorkLogApprovalHistory()),
                TenantId = tenantId,
                DailyWorkLogId = header.Id,
                ActionBy = actingUserId,
                Action = ApprovalStatus.Approved,
                ActionDate = DateTime.UtcNow,
                CreatedBy = actingUserId,
                CreatedOn = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return await GetByIdInternalAsync(header.Id, tenantId);
        }

        public async Task<DailyWorkLogDto> RejectAsync(string id, RejectDailyWorkLogDto reason, string actingUserId, string tenantId)
        {
            if (reason == null || string.IsNullOrWhiteSpace(reason.Reason))
                throw new Exception("Rejection reason is required.");

            var header = await _context.DailyWorkLogs
                .Include(x => x.Employee)
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId)
                ?? throw new Exception("Entry not found.");

            if (header.Status != ApprovalStatus.Pending)
                throw new Exception($"Only submitted entries can be rejected - current status is {header.Status}.");

            await EnsureApproverAuthorizedAsync(header, actingUserId);

            header.Status = ApprovalStatus.Rejected;
            header.RejectedAt = DateTime.UtcNow;
            header.RejectedBy = actingUserId;
            header.RejectionReason = reason.Reason.Trim();
            header.ModifiedBy = actingUserId;
            header.ModifiedOn = DateTime.UtcNow;

            _context.DailyWorkLogApprovalHistories.Add(new DailyWorkLogApprovalHistory
            {
                Id = IDManager.GetNewId(new DailyWorkLogApprovalHistory()),
                TenantId = tenantId,
                DailyWorkLogId = header.Id,
                ActionBy = actingUserId,
                Action = ApprovalStatus.Rejected,
                Remarks = reason.Reason.Trim(),
                ActionDate = DateTime.UtcNow,
                CreatedBy = actingUserId,
                CreatedOn = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return await GetByIdInternalAsync(header.Id, tenantId);
        }

        // ==================================================================
        // Authorization helpers (mirrors WfhRequestService exactly)
        // ==================================================================

        private async Task<string?> GetActingEmployeeIdAsync(string? userId)
        {
            if (string.IsNullOrEmpty(userId)) return null;
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId);
            return user?.EmployeeId;
        }

        private async Task<bool> IsHrOrAdminForWorkTrackingAsync(string? actingUserId)
        {
            if (string.IsNullOrEmpty(actingUserId)) return false;

            return await (
                from ur in _context.UserRoles
                join rp in _context.RolePermissions.Where(x => x.IsAllowed) on ur.RoleId equals rp.RoleId
                join p in _context.Permissions.Where(x =>
                        x.FeatureId == AppFeatureConstants.DAILY_WORK_ENTRY && x.Action == Actions.Approve)
                    on rp.PermissionId equals p.Id
                where ur.UserId == actingUserId
                select p.Id
            ).AnyAsync();
        }

        private async Task EnsureApproverAuthorizedAsync(DailyWorkLog header, string actingUserId)
        {
            var actingEmployeeId = await GetActingEmployeeIdAsync(actingUserId);

            bool isReportingManager =
                !string.IsNullOrEmpty(header.Employee?.ReportingManagerId) &&
                !string.IsNullOrEmpty(actingEmployeeId) &&
                header.Employee.ReportingManagerId == actingEmployeeId;

            if (isReportingManager) return;

            if (await IsHrOrAdminForWorkTrackingAsync(actingUserId)) return;

            throw new UnauthorizedAccessException("You are not authorized to act on this entry.");
        }

        // ==================================================================
        // Fetch helpers
        // ==================================================================

        private async Task<DailyWorkLog?> GetEntityByEmployeeDateAsync(string employeeId, DateTime date, string tenantId)
        {
            return await _context.DailyWorkLogs
                .Include(x => x.Employee).ThenInclude(e => e!.Department)
                .Include(x => x.Employee).ThenInclude(e => e!.Designation)
                .Include(x => x.Employee).ThenInclude(e => e!.ReportingManager)
                .Include(x => x.Entries!).ThenInclude(e => e.WorkJob).ThenInclude(j => j!.Client)
                .Include(x => x.Entries!).ThenInclude(e => e.JobType)
                .Include(x => x.Entries!).ThenInclude(e => e.JobItem)
                .Include(x => x.Entries!).ThenInclude(e => e.WorkActivity)
                .Include(x => x.Entries!).ThenInclude(e => e.WorkEntryReason)
                .FirstOrDefaultAsync(x => x.EmployeeId == employeeId && x.WorkDate == date && x.TenantId == tenantId);
        }

        private async Task<DailyWorkLog> GetEntityByIdAsync(string id, string tenantId)
        {
            var entity = await _context.DailyWorkLogs
                .Include(x => x.Employee).ThenInclude(e => e!.Department)
                .Include(x => x.Employee).ThenInclude(e => e!.Designation)
                .Include(x => x.Employee).ThenInclude(e => e!.ReportingManager)
                .Include(x => x.Entries!).ThenInclude(e => e.WorkJob).ThenInclude(j => j!.Client)
                .Include(x => x.Entries!).ThenInclude(e => e.JobType)
                .Include(x => x.Entries!).ThenInclude(e => e.JobItem)
                .Include(x => x.Entries!).ThenInclude(e => e.WorkActivity)
                .Include(x => x.Entries!).ThenInclude(e => e.WorkEntryReason)
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);

            if (entity == null) throw new Exception("Entry not found.");
            return entity;
        }

        private async Task<DailyWorkLogDto> GetByIdInternalAsync(string id, string tenantId)
        {
            var entity = await GetEntityByIdAsync(id, tenantId);
            return await AssembleDtoAsync(entity);
        }

        private async Task<Dictionary<string, string>> BuildUserDisplayNameMapAsync(IEnumerable<string?> userIds)
        {
            var ids = userIds.Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
            if (ids.Count == 0) return new Dictionary<string, string>();

            var users = await _context.Users.Include(u => u.Employee)
                .Where(u => ids.Contains(u.Id))
                .ToListAsync();

            return users.ToDictionary(u => u.Id, u => u.Employee != null ? $"{u.Employee.FirstName} {u.Employee.LastName}".Trim() : u.Username);
        }

        private async Task<DailyWorkLogDto> AssembleDtoAsync(DailyWorkLog x)
        {
            var nameMap = await BuildUserDisplayNameMapAsync(new[] { x.SubmittedBy, x.ApprovedBy, x.RejectedBy });

            var lines = (x.Entries ?? Enumerable.Empty<DailyWorkEntry>()).Select(e => new DailyWorkEntryLineDto
            {
                Id = e.Id,
                WorkJobId = e.WorkJobId,
                JobNumber = e.WorkJob?.JobNumber,
                ClientName = e.WorkJob?.Client?.Name,
                JobTypeId = e.JobTypeId,
                JobTypeName = e.JobType?.Name,
                JobItemId = e.JobItemId,
                JobItemCode = e.JobItem?.Code,
                WorkActivityId = e.WorkActivityId,
                WorkActivityName = e.WorkActivity?.Name,
                WorkCategory = e.WorkActivity != null ? (int)e.WorkActivity.WorkCategory : (int?)null,
                WorkCategoryName = e.WorkActivity?.WorkCategory.ToString(),
                WorkEntryReasonId = e.WorkEntryReasonId,
                WorkEntryReasonName = e.WorkEntryReason?.Name,
                Hours = e.Hours,
                Remarks = e.Remarks,
                WorkDoneToday = e.WorkDoneToday,
                AssignmentId = e.AssignmentId,
                AdhocReason = e.AdhocReason
            }).ToList();

            decimal Sum(WorkCategory cat) => lines.Where(l => l.WorkCategory == (int)cat).Sum(l => l.Hours);

            return new DailyWorkLogDto
            {
                Id = x.Id,
                EmployeeId = x.EmployeeId,
                EmployeeCode = x.Employee?.EmployeeCode,
                EmployeeName = x.Employee != null ? $"{x.Employee.FirstName} {x.Employee.LastName}".Trim() : null,
                DepartmentName = x.Employee?.Department?.Name,
                DesignationName = x.Employee?.Designation?.Name,
                ReportingManagerName = x.Employee?.ReportingManager != null
                    ? $"{x.Employee.ReportingManager.FirstName} {x.Employee.ReportingManager.LastName}".Trim()
                    : null,
                WorkDate = x.WorkDate,
                Status = (int)x.Status,
                StatusName = x.Status.ToString(),
                ClockedHours = x.ClockedHours,
                TotalHours = lines.Sum(l => l.Hours),
                DirectHours = Sum(WorkCategory.Direct),
                IndirectHours = Sum(WorkCategory.Indirect),
                IdleHours = Sum(WorkCategory.Idle),
                DowntimeHours = Sum(WorkCategory.Downtime),
                SubmittedAt = x.SubmittedAt,
                SubmittedByName = x.SubmittedBy != null && nameMap.TryGetValue(x.SubmittedBy, out var sn) ? sn : null,
                ApprovedAt = x.ApprovedAt,
                ApprovedByName = x.ApprovedBy != null && nameMap.TryGetValue(x.ApprovedBy, out var an) ? an : null,
                RejectedAt = x.RejectedAt,
                RejectedByName = x.RejectedBy != null && nameMap.TryGetValue(x.RejectedBy, out var rn) ? rn : null,
                RejectionReason = x.RejectionReason,
                Lines = lines
            };
        }
    }
}
