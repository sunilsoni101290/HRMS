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
    /// Manager -> Employee Job/Work Assignment workflow - see
    /// IWorkAssignmentService's remarks. Structurally mirrors
    /// DailyWorkEntryService/WfhRequestService: GetActingEmployeeIdAsync/
    /// IsHrOrAdminForXAsync helpers, single-level Reporting-Manager
    /// authorization, never trusts EmployeeId/JobId/JobItemId/Status from
    /// the client without re-checking it server-side.
    /// </summary>
    public class WorkAssignmentService : IWorkAssignmentService
    {
        private readonly ApplicationDbContext _context;

        public WorkAssignmentService(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==================================================================
        // Assign
        // ==================================================================

        public async Task<EmployeeWorkAssignmentDto> AssignAsync(SaveEmployeeWorkAssignmentDto dto, string actingUserId, string tenantId)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            var targetEmployee = await _context.Employees
                .FirstOrDefaultAsync(x => x.Id == dto.EmployeeId && x.TenantId == tenantId && !x.IsDeleted)
                ?? throw new Exception("Selected employee was not found.");

            // Spec section 23, step 2/3: is the acting user authorized to
            // assign work, and is the target employee under their permitted
            // hierarchy? Same reporting-hierarchy-or-HR/Admin-override
            // pattern as every other approval in this module - never a
            // hard-coded role/title check (spec section 3).
            await EnsureCanAssignToAsync(targetEmployee, actingUserId);

            var (workJob, jobType, jobItem, activity) = await ValidateJobChainAsync(
                dto.WorkJobId, dto.JobTypeId, dto.JobItemId, dto.WorkActivityId, tenantId);

            var assignmentType = Enum.IsDefined(typeof(AssignmentType), dto.AssignmentType)
                ? (AssignmentType)dto.AssignmentType : AssignmentType.Job;

            var priority = Enum.IsDefined(typeof(AssignmentPriority), dto.Priority)
                ? (AssignmentPriority)dto.Priority : AssignmentPriority.Normal;

            // Consistency: an Activity-level assignment must actually carry
            // an activity; a Job-level one may optionally omit
            // JobItem/Activity entirely (spec section 4's Assignment Type
            // field). Never silently reinterpret what the manager selected.
            if (assignmentType == AssignmentType.Activity && activity == null)
                throw new Exception("An activity must be selected for an Activity-level assignment.");

            // Defect fix: no duplicate-assignment guard existed before this
            // change - the same Job/Structure/Activity could be assigned to
            // the same employee any number of times while an earlier
            // assignment was still open. Block only against assignments that
            // are still "live" (Assigned/Accepted/InProgress); a Completed,
            // Rejected or Returned assignment must never block a fresh one.
            var dupJobItemId = jobItem?.Id;
            var dupActivityId = activity?.Id;

            var duplicateExists = await _context.EmployeeWorkAssignments.AsNoTracking()
                .AnyAsync(x =>
                    x.TenantId == tenantId &&
                    x.EmployeeId == targetEmployee.Id &&
                    x.WorkJobId == workJob.Id &&
                    x.JobItemId == dupJobItemId &&
                    x.WorkActivityId == dupActivityId &&
                    (x.Status == AssignmentStatus.Assigned || x.Status == AssignmentStatus.Accepted || x.Status == AssignmentStatus.InProgress));

            if (duplicateExists)
                throw new Exception("This job/work is already assigned to this employee.");

            var assignment = new EmployeeWorkAssignment
            {
                Id = IDManager.GetNewId(new EmployeeWorkAssignment()),
                TenantId = tenantId,
                EmployeeId = targetEmployee.Id,
                AssignedBy = actingUserId,
                WorkJobId = workJob.Id,
                JobTypeId = jobType.Id,
                JobItemId = jobItem?.Id,
                WorkActivityId = activity?.Id,
                AssignmentType = assignmentType,
                Priority = priority,
                Status = AssignmentStatus.Assigned,
                StartDate = dto.StartDate,
                ExpectedEndDate = dto.ExpectedEndDate,
                EstimatedHours = dto.EstimatedHours,
                Instructions = dto.Instructions?.Trim(),
                CreatedBy = actingUserId,
                CreatedOn = DateTime.UtcNow
            };

            _context.EmployeeWorkAssignments.Add(assignment);

            _context.EmployeeWorkAssignmentHistories.Add(new EmployeeWorkAssignmentHistory
            {
                Id = IDManager.GetNewId(new EmployeeWorkAssignmentHistory()),
                TenantId = tenantId,
                EmployeeWorkAssignmentId = assignment.Id,
                ActionBy = actingUserId,
                Action = AssignmentStatus.Assigned,
                ActionDate = DateTime.UtcNow,
                CreatedBy = actingUserId,
                CreatedOn = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return await GetByIdInternalAsync(assignment.Id, tenantId);
        }

        // ==================================================================
        // Read
        // ==================================================================

        public async Task<List<EmployeeWorkAssignmentSummaryDto>> GetMyAssignmentsAsync(string actingUserId, string tenantId)
        {
            var employeeId = await GetActingEmployeeIdAsync(actingUserId);
            if (string.IsNullOrEmpty(employeeId))
                return new List<EmployeeWorkAssignmentSummaryDto>();

            var entities = await _context.EmployeeWorkAssignments.AsNoTracking()
                .Include(x => x.Employee)
                .Include(x => x.WorkJob).ThenInclude(j => j!.Client)
                .Include(x => x.JobItem)
                .Include(x => x.WorkActivity)
                .Where(x => x.EmployeeId == employeeId && x.TenantId == tenantId)
                .OrderByDescending(x => x.CreatedOn)
                .ToListAsync();

            var actualHoursByAssignment = await GetActualHoursMapAsync(entities.Select(x => x.Id), tenantId);

            return entities.Select(x => ToSummaryDto(x, actualHoursByAssignment)).ToList();
        }

        public async Task<EmployeeWorkAssignmentDto> GetByIdAsync(string id, string actingUserId, string tenantId)
        {
            var entity = await GetEntityByIdAsync(id, tenantId);

            var actingEmployeeId = await GetActingEmployeeIdAsync(actingUserId);

            bool isOwn = !string.IsNullOrEmpty(actingEmployeeId) && entity.EmployeeId == actingEmployeeId;
            bool isAssigner = entity.AssignedBy == actingUserId;
            bool isReportingManager =
                !string.IsNullOrEmpty(entity.Employee?.ReportingManagerId) &&
                !string.IsNullOrEmpty(actingEmployeeId) &&
                entity.Employee.ReportingManagerId == actingEmployeeId;

            if (!isOwn && !isAssigner && !isReportingManager && !await IsHrOrAdminForAssignmentAsync(actingUserId))
                throw new UnauthorizedAccessException("You are not authorized to view this assignment.");

            return await AssembleDtoAsync(entity);
        }

        public async Task<List<EmployeeWorkAssignmentSummaryDto>> GetAssignedByMeOrTeamAsync(string actingUserId, string tenantId)
        {
            var actingEmployeeId = await GetActingEmployeeIdAsync(actingUserId);
            var isHrOrAdmin = await IsHrOrAdminForAssignmentAsync(actingUserId);

            var query = _context.EmployeeWorkAssignments.AsNoTracking()
                .Include(x => x.Employee)
                .Include(x => x.WorkJob).ThenInclude(j => j!.Client)
                .Include(x => x.JobItem)
                .Include(x => x.WorkActivity)
                .Where(x => x.TenantId == tenantId);

            if (!isHrOrAdmin)
            {
                // Spec section 20 - Team Leader/Manager sees assignments
                // THEY made, plus their team's assignments (made by anyone)
                // - never the whole tenant just because the page loaded.
                query = query.Where(x =>
                    x.AssignedBy == actingUserId ||
                    (x.Employee != null && x.Employee.ReportingManagerId == actingEmployeeId));
            }

            var entities = await query.OrderByDescending(x => x.CreatedOn).ToListAsync();

            var actualHoursByAssignment = await GetActualHoursMapAsync(entities.Select(x => x.Id), tenantId);

            return entities.Select(x => ToSummaryDto(x, actualHoursByAssignment)).ToList();
        }

        public async Task<List<AssignedWorkComboDto>> GetMyAssignedWorkComboAsync(string actingUserId, string tenantId)
        {
            var employeeId = await GetActingEmployeeIdAsync(actingUserId);
            if (string.IsNullOrEmpty(employeeId))
                return new List<AssignedWorkComboDto>();

            var liveStatuses = new[] { AssignmentStatus.Assigned, AssignmentStatus.Accepted, AssignmentStatus.InProgress };

            return await _context.EmployeeWorkAssignments.AsNoTracking()
                .Include(x => x.WorkJob)
                .Include(x => x.JobItem)
                .Include(x => x.WorkActivity)
                .Where(x => x.EmployeeId == employeeId && x.TenantId == tenantId && liveStatuses.Contains(x.Status))
                .Select(x => new AssignedWorkComboDto
                {
                    AssignmentId = x.Id,
                    WorkJobId = x.WorkJobId,
                    JobNumber = x.WorkJob!.JobNumber,
                    JobTypeId = x.JobTypeId,
                    JobItemId = x.JobItemId,
                    JobItemCode = x.JobItem != null ? x.JobItem.Code : null,
                    WorkActivityId = x.WorkActivityId,
                    WorkActivityName = x.WorkActivity != null ? x.WorkActivity.Name : null,
                    AssignmentType = (int)x.AssignmentType,
                    Status = (int)x.Status
                })
                .ToListAsync();
        }

        // ==================================================================
        // Status transitions (employee-side)
        // ==================================================================

        public async Task<EmployeeWorkAssignmentDto> UpdateStatusAsync(string id, UpdateAssignmentStatusDto dto, string actingUserId, string tenantId)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            var entity = await _context.EmployeeWorkAssignments
                .Include(x => x.Employee)
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId)
                ?? throw new Exception("Assignment not found.");

            var actingEmployeeId = await GetActingEmployeeIdAsync(actingUserId);

            // Spec section 6 - "Employee should not be able to silently
            // remove an assignment created by their manager." Only the
            // assignment's OWN employee can move its status; there is no
            // delete endpoint at all.
            if (string.IsNullOrEmpty(actingEmployeeId) || entity.EmployeeId != actingEmployeeId)
                throw new UnauthorizedAccessException("Only the assigned employee can update this assignment's status.");

            if (!Enum.IsDefined(typeof(AssignmentStatus), dto.Status))
                throw new Exception("Invalid status.");

            var newStatus = (AssignmentStatus)dto.Status;

            EnsureLegalTransition(entity.Status, newStatus);

            var now = DateTime.UtcNow;

            switch (newStatus)
            {
                case AssignmentStatus.Accepted:
                    entity.AcceptedAt = now;
                    break;
                case AssignmentStatus.InProgress:
                    entity.StartedAt ??= now;
                    break;
                case AssignmentStatus.Completed:
                    entity.CompletedAt = now;
                    break;
                case AssignmentStatus.Rejected:
                    if (string.IsNullOrWhiteSpace(dto.Remarks))
                        throw new Exception("A reason is required to reject an assignment.");
                    entity.RejectedAt = now;
                    entity.RejectionReason = dto.Remarks.Trim();
                    break;
            }

            entity.Status = newStatus;
            entity.ModifiedBy = actingUserId;
            entity.ModifiedOn = now;

            _context.EmployeeWorkAssignmentHistories.Add(new EmployeeWorkAssignmentHistory
            {
                Id = IDManager.GetNewId(new EmployeeWorkAssignmentHistory()),
                TenantId = tenantId,
                EmployeeWorkAssignmentId = entity.Id,
                ActionBy = actingUserId,
                Action = newStatus,
                Remarks = dto.Remarks?.Trim(),
                ActionDate = now,
                CreatedBy = actingUserId,
                CreatedOn = now
            });

            await _context.SaveChangesAsync();

            return await GetByIdInternalAsync(entity.Id, tenantId);
        }

        // Assigned -> Accepted -> InProgress -> Completed, or
        // Assigned -> Rejected/Returned (spec section 6). Nothing moves
        // backward, and nothing skips straight to Completed from Assigned.
        private static void EnsureLegalTransition(AssignmentStatus current, AssignmentStatus next)
        {
            var legal = new Dictionary<AssignmentStatus, AssignmentStatus[]>
            {
                [AssignmentStatus.Assigned] = new[] { AssignmentStatus.Accepted, AssignmentStatus.Rejected, AssignmentStatus.Returned },
                [AssignmentStatus.Accepted] = new[] { AssignmentStatus.InProgress, AssignmentStatus.Returned },
                [AssignmentStatus.InProgress] = new[] { AssignmentStatus.Completed, AssignmentStatus.Returned },
            };

            if (!legal.TryGetValue(current, out var allowed) || !allowed.Contains(next))
                throw new Exception($"Cannot move assignment status from {current} to {next}.");
        }

        // ==================================================================
        // Reassignment (spec section 25 - retain history, never overwrite)
        // ==================================================================

        public async Task<EmployeeWorkAssignmentDto> ReassignAsync(string id, ReassignEmployeeWorkAssignmentDto dto, string actingUserId, string tenantId)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            var oldAssignment = await _context.EmployeeWorkAssignments
                .Include(x => x.Employee)
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId)
                ?? throw new Exception("Assignment not found.");

            await EnsureCanAssignToAsync(oldAssignment.Employee!, actingUserId);

            var newEmployee = await _context.Employees
                .FirstOrDefaultAsync(x => x.Id == dto.NewEmployeeId && x.TenantId == tenantId && !x.IsDeleted)
                ?? throw new Exception("New employee was not found.");

            await EnsureCanAssignToAsync(newEmployee, actingUserId);

            var now = DateTime.UtcNow;

            // Old row is marked Returned - NEVER deleted or overwritten, so
            // its Estimated/Actual/history stay intact for audit (spec
            // section 25).
            oldAssignment.Status = AssignmentStatus.Returned;
            oldAssignment.ModifiedBy = actingUserId;
            oldAssignment.ModifiedOn = now;

            _context.EmployeeWorkAssignmentHistories.Add(new EmployeeWorkAssignmentHistory
            {
                Id = IDManager.GetNewId(new EmployeeWorkAssignmentHistory()),
                TenantId = tenantId,
                EmployeeWorkAssignmentId = oldAssignment.Id,
                ActionBy = actingUserId,
                Action = AssignmentStatus.Returned,
                Remarks = $"Reassigned to another employee. {dto.Remarks}".Trim(),
                ActionDate = now,
                CreatedBy = actingUserId,
                CreatedOn = now
            });

            var newAssignment = new EmployeeWorkAssignment
            {
                Id = IDManager.GetNewId(new EmployeeWorkAssignment()),
                TenantId = tenantId,
                EmployeeId = newEmployee.Id,
                AssignedBy = actingUserId,
                WorkJobId = oldAssignment.WorkJobId,
                JobTypeId = oldAssignment.JobTypeId,
                JobItemId = oldAssignment.JobItemId,
                WorkActivityId = oldAssignment.WorkActivityId,
                AssignmentType = oldAssignment.AssignmentType,
                Priority = oldAssignment.Priority,
                Status = AssignmentStatus.Assigned,
                StartDate = oldAssignment.StartDate,
                ExpectedEndDate = oldAssignment.ExpectedEndDate,
                EstimatedHours = oldAssignment.EstimatedHours,
                Instructions = oldAssignment.Instructions,
                ReassignedFromId = oldAssignment.Id,
                CreatedBy = actingUserId,
                CreatedOn = now
            };

            _context.EmployeeWorkAssignments.Add(newAssignment);

            _context.EmployeeWorkAssignmentHistories.Add(new EmployeeWorkAssignmentHistory
            {
                Id = IDManager.GetNewId(new EmployeeWorkAssignmentHistory()),
                TenantId = tenantId,
                EmployeeWorkAssignmentId = newAssignment.Id,
                ActionBy = actingUserId,
                Action = AssignmentStatus.Assigned,
                Remarks = $"Reassigned from {oldAssignment.Id}.",
                ActionDate = now,
                CreatedBy = actingUserId,
                CreatedOn = now
            });

            await _context.SaveChangesAsync();

            return await GetByIdInternalAsync(newAssignment.Id, tenantId);
        }

        // ==================================================================
        // Dashboards / reports
        // ==================================================================

        public async Task<MyWorkDashboardDto> GetMyWorkDashboardAsync(string actingUserId, string tenantId)
        {
            var employeeId = await GetActingEmployeeIdAsync(actingUserId);
            if (string.IsNullOrEmpty(employeeId))
                return new MyWorkDashboardDto();

            var assignments = await _context.EmployeeWorkAssignments.AsNoTracking()
                .Include(x => x.WorkJob)
                .Include(x => x.JobItem)
                .Include(x => x.WorkActivity)
                .Where(x => x.EmployeeId == employeeId && x.TenantId == tenantId)
                .ToListAsync();

            var actualHoursByAssignment = await GetActualHoursMapAsync(assignments.Select(x => x.Id), tenantId);

            var today = DateTime.UtcNow.Date;
            var weekStart = today.AddDays(-(int)today.DayOfWeek);
            var monthStart = new DateTime(today.Year, today.Month, 1);

            var entries = await _context.DailyWorkEntries.AsNoTracking()
                .Include(x => x.DailyWorkLog)
                .Where(x => x.DailyWorkLog!.EmployeeId == employeeId && x.TenantId == tenantId && x.DailyWorkLog.WorkDate >= monthStart)
                .ToListAsync();

            var headers = await _context.DailyWorkLogs.AsNoTracking()
                .Where(x => x.EmployeeId == employeeId && x.TenantId == tenantId && x.WorkDate >= monthStart)
                .ToListAsync();

            decimal HoursOn(DateTime date) => entries.Where(e => e.DailyWorkLog!.WorkDate.Date == date).Sum(e => e.Hours);
            decimal HoursFrom(DateTime from) => entries.Where(e => e.DailyWorkLog!.WorkDate.Date >= from).Sum(e => e.Hours);

            var todaysWork = assignments
                .Where(x => x.Status == AssignmentStatus.Assigned || x.Status == AssignmentStatus.Accepted || x.Status == AssignmentStatus.InProgress)
                .Select(x => ToSummaryDto(x, actualHoursByAssignment))
                .ToList();

            return new MyWorkDashboardDto
            {
                AssignedJobs = assignments.Count(x => x.Status != AssignmentStatus.Rejected && x.Status != AssignmentStatus.Returned),
                InProgress = assignments.Count(x => x.Status == AssignmentStatus.InProgress),
                Completed = assignments.Count(x => x.Status == AssignmentStatus.Completed),
                PendingDailyEntries = headers.Count(x => x.Status == ApprovalStatus.Draft),
                TodayHours = HoursOn(today),
                ThisWeekHours = HoursFrom(weekStart),
                ThisMonthHours = HoursFrom(monthStart),
                PendingSubmission = headers.Count(x => x.Status == ApprovalStatus.Draft),
                RejectedEntries = headers.Count(x => x.Status == ApprovalStatus.Rejected),
                TodaysAssignedWork = todaysWork
            };
        }

        public async Task<TeamWorkOverviewDto> GetTeamWorkOverviewAsync(string actingUserId, string tenantId)
        {
            var actingEmployeeId = await GetActingEmployeeIdAsync(actingUserId);
            var isHrOrAdmin = await IsHrOrAdminForAssignmentAsync(actingUserId);

            var employeeQuery = _context.Employees.AsNoTracking().Where(e => e.TenantId == tenantId && !e.IsDeleted);

            if (!isHrOrAdmin)
            {
                if (string.IsNullOrEmpty(actingEmployeeId)) return new TeamWorkOverviewDto();
                employeeQuery = employeeQuery.Where(e => e.ReportingManagerId == actingEmployeeId);
            }

            var teamEmployeeIds = await employeeQuery.Select(e => e.Id).ToListAsync();

            var assignments = await _context.EmployeeWorkAssignments.AsNoTracking()
                .Where(x => x.TenantId == tenantId && teamEmployeeIds.Contains(x.EmployeeId))
                .ToListAsync();

            var actualHoursByAssignment = await GetActualHoursMapAsync(assignments.Select(x => x.Id), tenantId);

            var today = DateTime.UtcNow.Date;
            var monthStart = new DateTime(today.Year, today.Month, 1);

            var entries = await _context.DailyWorkEntries.AsNoTracking()
                .Include(x => x.DailyWorkLog)
                .Include(x => x.WorkActivity)
                .Where(x => x.TenantId == tenantId && teamEmployeeIds.Contains(x.DailyWorkLog!.EmployeeId) && x.DailyWorkLog.WorkDate >= monthStart)
                .ToListAsync();

            var pendingApprovals = await _context.DailyWorkLogs.AsNoTracking()
                .CountAsync(x => x.TenantId == tenantId && teamEmployeeIds.Contains(x.EmployeeId) && x.Status == ApprovalStatus.Pending);

            decimal SumCategory(WorkCategory cat) =>
                entries.Where(e => e.WorkActivity != null && e.WorkActivity.WorkCategory == cat).Sum(e => e.Hours);

            var liveAssignments = assignments.Where(x => x.Status != AssignmentStatus.Rejected && x.Status != AssignmentStatus.Returned).ToList();

            return new TeamWorkOverviewDto
            {
                TeamMembers = teamEmployeeIds.Count,
                ActiveAssignments = liveAssignments.Count(x => x.Status != AssignmentStatus.Completed),
                PendingDailyApprovals = pendingApprovals,
                OverdueAssignments = liveAssignments.Count(x =>
                    x.ExpectedEndDate.HasValue && x.ExpectedEndDate.Value.Date < today &&
                    x.Status != AssignmentStatus.Completed),
                EstimatedHours = liveAssignments.Sum(x => x.EstimatedHours ?? 0),
                ActualHours = liveAssignments.Sum(x => actualHoursByAssignment.TryGetValue(x.Id, out var h) ? h : 0),
                DirectHours = SumCategory(WorkCategory.Direct),
                IndirectHours = SumCategory(WorkCategory.Indirect),
                IdleHours = SumCategory(WorkCategory.Idle),
                DowntimeHours = SumCategory(WorkCategory.Downtime)
            };
        }

        public async Task<List<EmployeeAssignmentReportRowDto>> GetEmployeeAssignmentReportAsync(string actingUserId, string tenantId)
        {
            var summaries = await GetAssignedByMeOrTeamAsync(actingUserId, tenantId);

            var entities = await _context.EmployeeWorkAssignments.AsNoTracking()
                .Include(x => x.Employee)
                .Include(x => x.WorkJob)
                .Include(x => x.JobItem)
                .Include(x => x.WorkActivity)
                .Where(x => summaries.Select(s => s.Id).Contains(x.Id))
                .ToListAsync();

            var nameMap = await BuildUserDisplayNameMapAsync(entities.Select(x => x.AssignedBy));

            return entities.Select(x => new EmployeeAssignmentReportRowDto
            {
                EmployeeCode = x.Employee?.EmployeeCode ?? "",
                EmployeeName = x.Employee != null ? $"{x.Employee.FirstName} {x.Employee.LastName}".Trim() : "",
                JobNumber = x.WorkJob?.JobNumber ?? "",
                JobItemCode = x.JobItem?.Code,
                WorkActivityName = x.WorkActivity?.Name,
                AssignedByName = nameMap.TryGetValue(x.AssignedBy, out var n) ? n : null,
                AssignedDate = x.CreatedOn,
                EstimatedHours = x.EstimatedHours,
                ActualHours = summaries.First(s => s.Id == x.Id).ActualHours,
                RemainingHours = summaries.First(s => s.Id == x.Id).RemainingHours,
                StatusName = x.Status.ToString()
            }).ToList();
        }

        // ==================================================================
        // Authorization helpers (mirrors DailyWorkEntryService/WfhRequestService)
        // ==================================================================

        private async Task<string?> GetActingEmployeeIdAsync(string? userId)
        {
            if (string.IsNullOrEmpty(userId)) return null;
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId);
            return user?.EmployeeId;
        }

        private async Task<bool> IsHrOrAdminForAssignmentAsync(string? actingUserId)
        {
            if (string.IsNullOrEmpty(actingUserId)) return false;

            return await (
                from ur in _context.UserRoles
                join rp in _context.RolePermissions.Where(x => x.IsAllowed) on ur.RoleId equals rp.RoleId
                join p in _context.Permissions.Where(x =>
                        x.FeatureId == AppFeatureConstants.WORK_ASSIGNMENT && x.Action == Actions.Create)
                    on rp.PermissionId equals p.Id
                where ur.UserId == actingUserId
                select p.Id
            ).AnyAsync();
        }

        // Spec section 23 step 2/3 - "Is authorized to assign? Is target
        // Employee under their permitted hierarchy?" A manager may assign
        // work to their direct reportee, or to themselves is disallowed
        // (assigning work to yourself has no meaning in this workflow -
        // Daily Work Entry is where you record your OWN work).
        private async Task EnsureCanAssignToAsync(Employee targetEmployee, string actingUserId)
        {
            var actingEmployeeId = await GetActingEmployeeIdAsync(actingUserId);

            if (!string.IsNullOrEmpty(actingEmployeeId) && targetEmployee.Id == actingEmployeeId)
                throw new Exception("You cannot assign work to yourself.");

            bool isReportingManager =
                !string.IsNullOrEmpty(targetEmployee.ReportingManagerId) &&
                !string.IsNullOrEmpty(actingEmployeeId) &&
                targetEmployee.ReportingManagerId == actingEmployeeId;

            if (isReportingManager) return;

            if (await IsHrOrAdminForAssignmentAsync(actingUserId)) return;

            throw new UnauthorizedAccessException("You are not authorized to assign work to this employee.");
        }

        // Spec section 23 steps 4-6 - Job valid? JobItem belongs to Job?
        // Activity belongs to Job Type? Exactly the same chain
        // DailyWorkEntryService.BuildAndValidateLineAsync enforces, reused
        // here rather than re-invented, since an assignment's Job/JobItem/
        // Activity combination must be just as valid as a daily entry's.
        private async Task<(WorkJob workJob, JobType jobType, JobItem? jobItem, WorkActivity? activity)> ValidateJobChainAsync(
            string workJobId, string jobTypeId, string? jobItemId, string? workActivityId, string tenantId)
        {
            var jobType = await _context.JobTypes
                .FirstOrDefaultAsync(x => x.Id == jobTypeId && x.TenantId == tenantId && x.IsActive)
                ?? throw new Exception("Selected job type is invalid or inactive.");

            var workJob = await _context.WorkJobs
                .FirstOrDefaultAsync(x => x.Id == workJobId && x.TenantId == tenantId && x.IsActive && x.Status == WorkJobStatus.Active)
                ?? throw new Exception("Selected job is invalid or inactive.");

            JobItem? jobItem = null;

            if (!string.IsNullOrEmpty(jobItemId))
            {
                jobItem = await _context.JobItems
                    .FirstOrDefaultAsync(x => x.Id == jobItemId && x.WorkJobId == workJobId && x.TenantId == tenantId && x.IsActive)
                    ?? throw new Exception("Selected structure/equipment/job item does not belong to the selected job.");
            }

            WorkActivity? activity = null;

            if (!string.IsNullOrEmpty(workActivityId))
            {
                activity = await _context.WorkActivities
                    .FirstOrDefaultAsync(x => x.Id == workActivityId && x.TenantId == tenantId && x.IsActive);

                if (activity == null || activity.JobTypeId != jobTypeId)
                    throw new Exception("Selected work activity does not belong to the selected job type.");
            }

            return (workJob, jobType, jobItem, activity);
        }

        // ==================================================================
        // Actual/remaining hours + fetch helpers
        // ==================================================================

        // Only Pending/Approved entries count towards Actual Hours - a
        // still-Draft line hasn't really been "reported" yet, and a
        // Rejected day's hours shouldn't inflate utilization until it is
        // corrected and resubmitted.
        private async Task<Dictionary<string, decimal>> GetActualHoursMapAsync(IEnumerable<string> assignmentIds, string tenantId)
        {
            var ids = assignmentIds.Distinct().ToList();
            if (ids.Count == 0) return new Dictionary<string, decimal>();

            return await _context.DailyWorkEntries.AsNoTracking()
                .Include(x => x.DailyWorkLog)
                .Where(x => x.AssignmentId != null && ids.Contains(x.AssignmentId) && x.TenantId == tenantId &&
                            x.DailyWorkLog != null &&
                            (x.DailyWorkLog.Status == ApprovalStatus.Pending || x.DailyWorkLog.Status == ApprovalStatus.Approved))
                .GroupBy(x => x.AssignmentId!)
                .Select(g => new { AssignmentId = g.Key, Hours = g.Sum(x => x.Hours) })
                .ToDictionaryAsync(x => x.AssignmentId, x => x.Hours);
        }

        private static EmployeeWorkAssignmentSummaryDto ToSummaryDto(EmployeeWorkAssignment x, Dictionary<string, decimal> actualHoursByAssignment)
        {
            var actual = actualHoursByAssignment.TryGetValue(x.Id, out var h) ? h : 0;
            var remaining = x.EstimatedHours.HasValue ? x.EstimatedHours.Value - actual : (decimal?)null;

            return new EmployeeWorkAssignmentSummaryDto
            {
                Id = x.Id,
                EmployeeCode = x.Employee?.EmployeeCode ?? "",
                EmployeeName = x.Employee != null ? $"{x.Employee.FirstName} {x.Employee.LastName}".Trim() : "",
                JobNumber = x.WorkJob?.JobNumber ?? "",
                ClientName = x.WorkJob?.Client?.Name,
                JobItemCode = x.JobItem?.Code,
                WorkActivityName = x.WorkActivity?.Name,
                EstimatedHours = x.EstimatedHours,
                ActualHours = actual,
                RemainingHours = remaining,
                IsOverUtilized = x.EstimatedHours.HasValue && actual > x.EstimatedHours.Value,
                Status = (int)x.Status,
                StatusName = x.Status.ToString(),
                ExpectedEndDate = x.ExpectedEndDate,
                IsOverdue = x.ExpectedEndDate.HasValue && x.ExpectedEndDate.Value.Date < DateTime.UtcNow.Date && x.Status != AssignmentStatus.Completed
            };
        }

        private async Task<EmployeeWorkAssignment> GetEntityByIdAsync(string id, string tenantId)
        {
            var entity = await _context.EmployeeWorkAssignments
                .Include(x => x.Employee).ThenInclude(e => e!.Department)
                .Include(x => x.WorkJob).ThenInclude(j => j!.Client)
                .Include(x => x.JobType)
                .Include(x => x.JobItem)
                .Include(x => x.WorkActivity)
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);

            if (entity == null) throw new Exception("Assignment not found.");
            return entity;
        }

        private async Task<EmployeeWorkAssignmentDto> GetByIdInternalAsync(string id, string tenantId)
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

        private async Task<EmployeeWorkAssignmentDto> AssembleDtoAsync(EmployeeWorkAssignment x)
        {
            var actualHoursMap = await GetActualHoursMapAsync(new[] { x.Id }, x.TenantId!);
            var nameMap = await BuildUserDisplayNameMapAsync(new[] { x.AssignedBy });

            var actual = actualHoursMap.TryGetValue(x.Id, out var h) ? h : 0;
            var remaining = x.EstimatedHours.HasValue ? x.EstimatedHours.Value - actual : (decimal?)null;

            return new EmployeeWorkAssignmentDto
            {
                Id = x.Id,
                EmployeeId = x.EmployeeId,
                EmployeeCode = x.Employee?.EmployeeCode,
                EmployeeName = x.Employee != null ? $"{x.Employee.FirstName} {x.Employee.LastName}".Trim() : null,
                DepartmentName = x.Employee?.Department?.Name,
                AssignedByName = nameMap.TryGetValue(x.AssignedBy, out var an) ? an : null,
                WorkJobId = x.WorkJobId,
                JobNumber = x.WorkJob?.JobNumber,
                JobName = x.WorkJob?.JobName,
                ClientName = x.WorkJob?.Client?.Name,
                JobTypeId = x.JobTypeId,
                JobTypeName = x.JobType?.Name,
                JobItemId = x.JobItemId,
                JobItemCode = x.JobItem?.Code,
                WorkActivityId = x.WorkActivityId,
                WorkActivityName = x.WorkActivity?.Name,
                AssignmentType = (int)x.AssignmentType,
                AssignmentTypeName = x.AssignmentType.ToString(),
                Priority = (int)x.Priority,
                PriorityName = x.Priority.ToString(),
                Status = (int)x.Status,
                StatusName = x.Status.ToString(),
                StartDate = x.StartDate,
                ExpectedEndDate = x.ExpectedEndDate,
                EstimatedHours = x.EstimatedHours,
                ActualHours = actual,
                RemainingHours = remaining,
                IsOverUtilized = x.EstimatedHours.HasValue && actual > x.EstimatedHours.Value,
                Instructions = x.Instructions,
                AcceptedAt = x.AcceptedAt,
                StartedAt = x.StartedAt,
                CompletedAt = x.CompletedAt,
                RejectedAt = x.RejectedAt,
                RejectionReason = x.RejectionReason,
                ReassignedFromId = x.ReassignedFromId,
                CreatedOn = x.CreatedOn
            };
        }
    }
}
