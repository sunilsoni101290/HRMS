using Application.DTOs.Leaves;
using Application.Interfaces;
using Application.Interfaces.Communication;
using Application.Interfaces.Leaves;
using Application.Interfaces.Masters;
using Domain.Entities;
using Domain.Helper;
using Domain.Interfaces;
using Infrastructure;
using Infrastructure.Data;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Application.Services.Leaves
{
    public class LeaveApplicationService : ILeaveApplicationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWeekOffService _weekOffService;
        private readonly ITenantService _tenantService;
        private readonly ILeaveBalanceService _leaveBalanceService;
        private readonly INotificationService _notificationService;
        private readonly IEmailSender _emailSender;
        private readonly IApprovalDelegationService _approvalDelegationService;
        private readonly ILogger<LeaveApplicationService> _logger;
        private string tenantId = string.Empty;
        public LeaveApplicationService(ApplicationDbContext context, IWeekOffService weekOffService,
            ITenantService tenantService, ILeaveBalanceService leaveBalanceService,
            INotificationService notificationService, IEmailSender emailSender,
            IApprovalDelegationService approvalDelegationService,
            ILogger<LeaveApplicationService> logger)
        {
            _context = context;
            _tenantService = tenantService;
            _weekOffService = weekOffService;
            _leaveBalanceService = leaveBalanceService;
            _notificationService = notificationService;
            _emailSender = emailSender;
            _approvalDelegationService = approvalDelegationService;
            _logger = logger;
        }

        #region Approval Chain Helpers

        // Level 1 = Reporting Manager, Level 2 = Department Head (the
        // employee holding the most senior Designation - lowest Level
        // number - in that department, excluding the applicant themself),
        // Level 3 = HR (role-based, not tied to one employee).

        private static string GetLevelName(int level) => level switch
        {
            1 => "Reporting Manager",
            2 => "Department Head",
            3 => "HR",
            _ => "Unknown"
        };

        private async Task<string?> GetDepartmentHeadIdAsync(string? departmentId, string applicantEmployeeId)
        {
            if (string.IsNullOrEmpty(departmentId))
                return null;

            return await _context.Employees
                .Where(e =>
                    e.DepartmentId == departmentId &&
                    e.Id != applicantEmployeeId &&
                    e.DesignationId != null)
                .OrderBy(e => e.Designation.Level)
                .Select(e => e.Id)
                .FirstOrDefaultAsync();
        }

        // Bulk version for list screens - avoids one query per row by
        // pre-computing, per department, the seniority-ordered employee
        // list once and reusing it for every leave application in the list.
        private async Task<Dictionary<string, List<(string EmployeeId, int Level)>>> BuildDepartmentSeniorityMapAsync()
        {
            var employees = await _context.Employees
                .Where(x => x.DepartmentId != null && x.DesignationId != null)
                .Select(x => new { x.Id, x.DepartmentId, x.Designation.Level })
                .ToListAsync();

            return employees
                .GroupBy(x => x.DepartmentId!)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderBy(x => x.Level).Select(x => (x.Id, x.Level)).ToList());
        }

        private static string? GetDepartmentHeadIdFromMap(
            Dictionary<string, List<(string EmployeeId, int Level)>> map,
            string? departmentId,
            string applicantEmployeeId)
        {
            if (string.IsNullOrEmpty(departmentId) || !map.TryGetValue(departmentId, out var list))
                return null;

            foreach (var entry in list)
            {
                if (entry.EmployeeId != applicantEmployeeId)
                    return entry.EmployeeId;
            }

            return null;
        }

        private async Task<int> ResolveStartingLevelAsync(Employee employee)
        {
            if (!string.IsNullOrEmpty(employee.ReportingManagerId))
                return 1;

            var deptHead = await GetDepartmentHeadIdAsync(employee.DepartmentId, employee.Id);
            return !string.IsNullOrEmpty(deptHead) ? 2 : 3;
        }

        private async Task<int> ResolveNextLevelAsync(int currentLevel, Employee employee)
        {
            if (currentLevel <= 1)
            {
                var deptHead = await GetDepartmentHeadIdAsync(employee.DepartmentId, employee.Id);
                return !string.IsNullOrEmpty(deptHead) ? 2 : 3;
            }

            return 3;
        }

        // Resolves who is acting (their linked Employee + their role name)
        // from the caller's own UserId - never trusted from client input -
        // so authorization can't be spoofed by posting someone else's id.
        private async Task<(string? EmployeeId, string? RoleName)> GetActingContextAsync(string userId)
        {
            var user = await _context.Users
                .Include(x => x.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(x => x.Id == userId);

            var roleName = user?.UserRoles?
                .Select(ur => ur.Role?.Name)
                .FirstOrDefault(n => !string.IsNullOrEmpty(n));

            return (user?.EmployeeId, roleName);
        }

        // Level 1/2: the acting employee is authorized if they ARE the
        // resolved Reporting Manager/Department Head, OR if they are that
        // person's currently-active out-of-office delegate (Part 2 - see
        // Domain/Entities/ApprovalDelegation.cs). Delegation is checked
        // against "now" (DateTime.UtcNow), not any date on the leave
        // application itself - the question is "is a proxy authorized to
        // act at the moment this approval action is being taken", not
        // whether the leave's own FromDate/ToDate falls inside some window.
        //
        // Level 3 (HR): formalized from a loose actingRoleName.Contains("HR")
        // substring match (which would spuriously match any role whose
        // display name happened to contain "HR", and would silently break if
        // "HR Manager" were ever renamed) to a real permission check - does
        // the acting user hold, via any of their assigned Roles, an allowed
        // RolePermission for a Permission scoped to the Leave Approval
        // feature's Approve action (FeatureId = LEAVE_APPROVAL, Action =
        // Approve). See IsHrApproverAsync. Delegation deliberately does not
        // apply at Level 3 - it is role-based (any permission holder), not
        // tied to one employee, so there is nobody singular to delegate away
        // from.
        private async Task<bool> IsAuthorizedForLevelAsync(
            LeaveApplication leave,
            int level,
            string? actingEmployeeId,
            string? actingUserId)
        {
            switch (level)
            {
                case 1:
                    return await IsEmployeeOrActiveDelegateAsync(leave.Employee?.ReportingManagerId, actingEmployeeId);

                case 2:
                    var l2 = await GetDepartmentHeadIdAsync(leave.Employee?.DepartmentId, leave.EmployeeId);
                    return await IsEmployeeOrActiveDelegateAsync(l2, actingEmployeeId);

                case 3:
                    return await IsHrApproverAsync(actingUserId);

                default:
                    return false;
            }
        }

        private async Task<bool> IsEmployeeOrActiveDelegateAsync(string? requiredApproverEmployeeId, string? actingEmployeeId)
        {
            if (string.IsNullOrEmpty(requiredApproverEmployeeId) || string.IsNullOrEmpty(actingEmployeeId))
                return false;

            if (requiredApproverEmployeeId == actingEmployeeId)
                return true;

            var activeDelegate = await _approvalDelegationService.GetActiveDelegateForAsync(requiredApproverEmployeeId, DateTime.UtcNow);

            return !string.IsNullOrEmpty(activeDelegate) && activeDelegate == actingEmployeeId;
        }

        // Real permission check backing the Level 3 (HR) authorization -
        // does the acting user (by User.Id) hold, through any Role assigned
        // to them, an allowed RolePermission for the "Approve Leave"
        // Permission (FeatureId = LEAVE_APPROVAL, Action = Approve - seeded
        // as "LEAVE_APPROVE" and also auto-generated as
        // "LEAVE_APPROVAL_APPROVE" by DbSeeder.ReconcilePermissionsAsync,
        // both match on FeatureId+Action here so either seeding path works).
        // Soft-deleted Users/UserRoles/RolePermissions/Permissions are
        // already excluded by the global query filter (ApplySoftDeleteFilter
        // in ApplicationDbContext), so no explicit !IsDeleted checks are
        // needed here.
        public async Task<bool> IsHrApproverAsync(string? actingUserId)
        {
            if (string.IsNullOrEmpty(actingUserId))
                return false;

            return await (
                from ur in _context.UserRoles
                join rp in _context.RolePermissions.Where(x => x.IsAllowed) on ur.RoleId equals rp.RoleId
                join p in _context.Permissions.Where(x =>
                        x.FeatureId == AppFeatureConstants.LEAVE_APPROVAL && x.Action == Actions.Approve)
                    on rp.PermissionId equals p.Id
                where ur.UserId == actingUserId
                select p.Id
            ).AnyAsync();
        }

        private static LeaveApplicationDto AssembleDto(LeaveApplication x, string? currentApproverEmployeeId)
        {
            return new LeaveApplicationDto
            {
                Id = x.Id,

                CompanyId = x.CompanyId,
                BranchId = x.BranchId,

                EmployeeId = x.EmployeeId,
                EmployeeName = x.Employee != null ? $"{x.Employee.FirstName} {x.Employee.LastName}".Trim() : null,

                LeaveTypeId = x.LeaveTypeId,
                LeaveTypeName = x.LeaveType?.Name,

                FromDate = x.FromDate,
                ToDate = x.ToDate,

                TotalDays = x.TotalDays,

                IsHalfDay = x.IsHalfDay,
                HalfDayType = x.HalfDayType,

                Reason = x.Reason,

                Status = x.Status,

                ApprovedBy = x.ApprovedBy,
                ApprovedDate = x.ApprovedDate,

                RejectedReason = x.RejectedReason,

                CurrentLevel = x.CurrentLevel,
                CurrentLevelName = x.Status == ApprovalStatus.Pending ? GetLevelName(x.CurrentLevel) : null,
                CurrentApproverEmployeeId = x.Status == ApprovalStatus.Pending ? currentApproverEmployeeId : null,
                SendBackReason = x.SendBackReason,

                DocumentUrl = x.DocumentUrl,

                CreatedOn = x.CreatedOn,
                CreatedBy = x.CreatedBy,

                ModifiedOn = x.ModifiedOn,
                ModifiedBy = x.ModifiedBy
            };
        }

        private async Task<LeaveApplicationDto> AssembleDtoAsync(LeaveApplication x)
        {
            string? approverId = null;

            if (x.Status == ApprovalStatus.Pending)
            {
                approverId = x.CurrentLevel switch
                {
                    1 => x.Employee?.ReportingManagerId,
                    2 => await GetDepartmentHeadIdAsync(x.Employee?.DepartmentId, x.EmployeeId),
                    _ => null
                };
            }

            return AssembleDto(x, approverId);
        }

        private List<LeaveApplicationDto> AssembleDtoList(
            List<LeaveApplication> entities,
            Dictionary<string, List<(string EmployeeId, int Level)>> deptSeniorityMap)
        {
            var list = new List<LeaveApplicationDto>();

            foreach (var x in entities)
            {
                string? approverId = null;

                if (x.Status == ApprovalStatus.Pending)
                {
                    approverId = x.CurrentLevel switch
                    {
                        1 => x.Employee?.ReportingManagerId,
                        2 => GetDepartmentHeadIdFromMap(deptSeniorityMap, x.Employee?.DepartmentId, x.EmployeeId),
                        _ => null
                    };
                }

                list.Add(AssembleDto(x, approverId));
            }

            return list;
        }

        #endregion

        #region Notifications

        // AttendanceRegularization's ApprovedBy/RejectedBy already store
        // User.Id (not EmployeeId) - same precedent followed here.
        // Notification.UserId needs a User.Id, but the approval chain is
        // resolved in terms of EmployeeId, so every notification target has
        // to be translated Employee -> User first. If an employee has no
        // linked User account, this resolves to null and the caller skips
        // that notification (logged, never thrown).
        private async Task<string?> ResolveUserIdForEmployeeAsync(string? employeeId)
        {
            if (string.IsNullOrEmpty(employeeId))
                return null;

            return await _context.Users
                .Where(u => u.EmployeeId == employeeId && !u.IsDeleted)
                .Select(u => u.Id)
                .FirstOrDefaultAsync();
        }

        // Who should be notified for a given approval level. Levels 1/2 are
        // a single employee (Reporting Manager / Department Head), plus
        // their active out-of-office delegate if one is in effect right now
        // (Part 2 - a delegate is equally authorized to act per
        // IsAuthorizedForLevelAsync and must not be left out of the loop).
        // Level 3 is role-based (HR), so it fans out to every active user
        // who actually holds the "Approve Leave" permission (see
        // IsHrApproverAsync) rather than one person.
        private async Task<List<string>> ResolveApproverUserIdsAsync(int level, Employee? employee, string applicantEmployeeId)
        {
            var userIds = new List<string>();

            if (employee == null)
                return userIds;

            if (level >= 3)
            {
                var hrUserIds = await (
                    from ur in _context.UserRoles
                    join rp in _context.RolePermissions.Where(x => x.IsAllowed) on ur.RoleId equals rp.RoleId
                    join p in _context.Permissions.Where(x =>
                            x.FeatureId == AppFeatureConstants.LEAVE_APPROVAL && x.Action == Actions.Approve)
                        on rp.PermissionId equals p.Id
                    join u in _context.Users.Where(x => x.IsActive) on ur.UserId equals u.Id
                    select u.Id
                ).Distinct().ToListAsync();

                userIds.AddRange(hrUserIds);

                return userIds;
            }

            string? approverEmployeeId = level switch
            {
                1 => employee.ReportingManagerId,
                2 => await GetDepartmentHeadIdAsync(employee.DepartmentId, applicantEmployeeId),
                _ => null
            };

            var userId = await ResolveUserIdForEmployeeAsync(approverEmployeeId);

            if (!string.IsNullOrEmpty(userId))
                userIds.Add(userId);

            var delegateEmployeeId = await _approvalDelegationService.GetActiveDelegateForAsync(approverEmployeeId, DateTime.UtcNow);
            var delegateUserId = await ResolveUserIdForEmployeeAsync(delegateEmployeeId);

            if (!string.IsNullOrEmpty(delegateUserId))
                userIds.Add(delegateUserId);

            return userIds;
        }

        // Maps the specific leave-workflow event to the UI severity the
        // existing Notification views already switch on (Info/Success/
        // Warning/Error - see Views/Notification/Index.cshtml's TypeIcon).
        // Keeps LeaveNotificationEvent as the single source of truth for
        // "what happened", rather than scattering severity literals across
        // every call site.
        private static string SeverityFor(LeaveNotificationEvent leaveEvent) => leaveEvent switch
        {
            LeaveNotificationEvent.LeaveApproved => "Success",
            LeaveNotificationEvent.LeaveRejected => "Error",
            LeaveNotificationEvent.LeaveSentBack => "Warning",
            _ => "Info"
        };

        // Best-effort fan-out to every resolved recipient: creates the
        // in-app Notification (the channel that must actually work) and
        // then, independently, tries the email (secondary channel that must
        // never be able to break the caller). Every failure - notification
        // or email - is caught and logged here, never rethrown, so a
        // problem resolving/notifying one recipient can't stop the others
        // or bubble back into the already-committed leave transaction.
        private async Task NotifyLeaveEventAsync(
            IEnumerable<string> userIds,
            string title,
            string message,
            LeaveNotificationEvent leaveEvent,
            string leaveApplicationId,
            string? tenantId,
            string? actorUserId)
        {
            var severity = SeverityFor(leaveEvent);

            foreach (var userId in userIds.Where(x => !string.IsNullOrEmpty(x)).Distinct())
            {
                try
                {
                    await _notificationService.CreateDirectAsync(
                        userId,
                        title,
                        message,
                        severity,
                        redirectUrl: $"/LeaveApplication/Details/{leaveApplicationId}",
                        featureId: "LEAVE",
                        referenceId: leaveApplicationId,
                        tenantId: tenantId,
                        createdBy: actorUserId ?? "System");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "In-app notification failed for leave {LeaveApplicationId}, user {UserId}.",
                        leaveApplicationId, userId);
                }

                try
                {
                    var email = await _context.Users
                        .Where(u => u.Id == userId)
                        .Select(u => u.Email)
                        .FirstOrDefaultAsync();

                    if (!string.IsNullOrWhiteSpace(email))
                        await _emailSender.SendAsync(email, title, message);
                }
                catch (Exception ex)
                {
                    // Never let an email problem affect the workflow or the
                    // in-app notification that already succeeded above.
                    _logger.LogWarning(ex,
                        "Email notification failed for leave {LeaveApplicationId}, user {UserId}.",
                        leaveApplicationId, userId);
                }
            }
        }

        private async Task NotifyApplicantAsync(
            string applicantEmployeeId,
            string title,
            string message,
            LeaveNotificationEvent leaveEvent,
            string leaveApplicationId,
            string? tenantId,
            string? actorUserId)
        {
            // Everything below (recipient resolution AND dispatch) must never
            // throw out of this method - every call site sits inside the same
            // outer try/catch that wraps the already-committed SaveChangesAsync
            // for this leave transition, so an exception here would make a
            // genuinely successful Apply/Approve/Reject/etc. falsely report
            // failure to the caller even though nothing needs to be undone.
            try
            {
                var userId = await ResolveUserIdForEmployeeAsync(applicantEmployeeId);

                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogInformation(
                        "Employee {EmployeeId} has no linked User account - skipping applicant notification for leave {LeaveApplicationId}.",
                        applicantEmployeeId, leaveApplicationId);

                    return;
                }

                await NotifyLeaveEventAsync(new[] { userId }, title, message, leaveEvent, leaveApplicationId, tenantId, actorUserId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Applicant notification resolution/dispatch failed for leave {LeaveApplicationId} - the leave transition itself is unaffected.",
                    leaveApplicationId);
            }
        }

        private async Task NotifyLevelAsync(
            int level,
            Employee? employee,
            string applicantEmployeeId,
            string title,
            string message,
            LeaveNotificationEvent leaveEvent,
            string leaveApplicationId,
            string? tenantId,
            string? actorUserId)
        {
            // Same rationale as NotifyApplicantAsync above - resolution must
            // never throw out of this method.
            try
            {
                var userIds = await ResolveApproverUserIdsAsync(level, employee, applicantEmployeeId);

                if (userIds.Count == 0)
                {
                    _logger.LogInformation(
                        "No resolvable approver/User at level {Level} for leave {LeaveApplicationId} - skipping notification.",
                        level, leaveApplicationId);

                    return;
                }

                await NotifyLeaveEventAsync(userIds, title, message, leaveEvent, leaveApplicationId, tenantId, actorUserId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Approver notification resolution/dispatch failed at level {Level} for leave {LeaveApplicationId} - the leave transition itself is unaffected.",
                    level, leaveApplicationId);
            }
        }

        #endregion

        #region CRUD

        public async Task<LeaveApplicationDto> CreateAsync(ApplyLeaveRequestDto request)
        {
            // An employee must wait for their current leave request to be
            // approved or rejected before they can submit another one -
            // regardless of the dates chosen for the new request. Checked
            // (and left to propagate, not swallowed by the catch below) so
            // the caller can surface the real reason to the user instead of
            // a generic failure.
            bool hasPendingLeave = await _context.LeaveApplications
                .AnyAsync(x =>
                    x.EmployeeId == request.EmployeeId &&
                    x.Status == ApprovalStatus.Pending);

            if (hasPendingLeave)
                throw new InvalidOperationException(
                    "You already have a leave request awaiting approval. Please wait until it is approved or rejected before applying for another leave.");

            var applicant = await _context.Employees.FirstOrDefaultAsync(x => x.Id == request.EmployeeId);

            if (applicant == null)
                throw new Exception("Employee not found.");

            try
            {
            decimal totalDays = await CalculateTotalDaysAsync(
                request.FromDate,
                request.ToDate,
                request.IsHalfDay,
                request.TenantId);

            int startingLevel = await ResolveStartingLevelAsync(applicant);

            var entity = new LeaveApplication
            {
                Id=IDManager.GetNewId(new LeaveApplication()),
                TenantId = request.TenantId,
                CompanyId = request.CompanyId,
                BranchId = request.BranchId,

                EmployeeId = request.EmployeeId,
                LeaveTypeId = request.LeaveTypeId,

                FromDate = request.FromDate,
                ToDate = request.ToDate,

                TotalDays = totalDays,

                IsHalfDay = request.IsHalfDay,
                HalfDayType = request.HalfDayType,

                Reason = request.Reason,

                Status = ApprovalStatus.Pending,
                CurrentLevel = startingLevel,

                DocumentUrl = request.DocumentUrl,

                CreatedOn = DateTime.UtcNow,
                CreatedBy = request.CreatedBy
            };

            _context.LeaveApplications.Add(entity);

            await _context.SaveChangesAsync();

            await CreateApprovalHistoryAsync(
                entity.Id,
                request.CreatedBy,
                ApprovalStatus.Pending,
                $"Leave Applied - awaiting {GetLevelName(startingLevel)} approval");

            var applicantName = $"{applicant.FirstName} {applicant.LastName}".Trim();

            await NotifyLevelAsync(
                startingLevel,
                applicant,
                applicant.Id,
                "New Leave Request Awaiting Your Approval",
                $"{applicantName} applied for leave from {entity.FromDate:dd-MMM-yyyy} to {entity.ToDate:dd-MMM-yyyy} ({entity.TotalDays} day(s)). It is awaiting your approval as {GetLevelName(startingLevel)}.",
                LeaveNotificationEvent.LeaveApplied,
                entity.Id,
                entity.TenantId,
                request.CreatedBy);

            return await GetByIdAsync(entity.Id);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<LeaveApplicationDto> UpdateAsync(string id,ApplyLeaveRequestDto request)
        {
            try
            {
            var entity = await _context.LeaveApplications
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                throw new Exception("Leave application not found.");

            if (entity.Status == ApprovalStatus.Approved)
                throw new Exception("Approved leave cannot be modified.");

            decimal totalDays = await CalculateTotalDaysAsync(
                request.FromDate,
                request.ToDate,
                request.IsHalfDay,
                request.TenantId ?? entity.TenantId);

            entity.CompanyId = request.CompanyId;
            entity.BranchId = request.BranchId;

            entity.EmployeeId = request.EmployeeId;
            entity.LeaveTypeId = request.LeaveTypeId;

            entity.FromDate = request.FromDate;
            entity.ToDate = request.ToDate;

            entity.TotalDays = totalDays;

            entity.IsHalfDay = request.IsHalfDay;
            entity.HalfDayType = request.HalfDayType;

            entity.Reason = request.Reason;

            entity.DocumentUrl = request.DocumentUrl;

            entity.ModifiedOn = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return await GetByIdAsync(entity.Id);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool> DeleteAsync(string id)
        {
            try
            {
            var entity = await _context.LeaveApplications
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return false;

            _context.LeaveApplications.Remove(entity);

            await _context.SaveChangesAsync();

            return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<LeaveApplicationDto> GetByIdAsync(string id)
        {
            try
            {
            var entity = await _context.LeaveApplications
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                throw new Exception("Leave application not found.");

            return await AssembleDtoAsync(entity);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<List<LeaveApplicationDto>> GetAllAsync()
        {
            try
            {
            var entities = await _context.LeaveApplications
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .OrderByDescending(x => x.CreatedOn)
                .ToListAsync();

            var deptMap = await BuildDepartmentSeniorityMapAsync();

            return AssembleDtoList(entities, deptMap);
            }
            catch (Exception)
            {
                return new List<LeaveApplicationDto>();
            }
        }

        #endregion

        #region Workflow

        public async Task<bool> ApplyLeaveAsync(ApplyLeaveRequestDto request)
        {
            try
            {
            decimal totalDays = await CalculateTotalDaysAsync(
                request.FromDate,
                request.ToDate,
                request.IsHalfDay,
                request.TenantId);

            // Check Leave Balance
            var leaveBalance =
                await _leaveBalanceService.GetEmployeeLeaveBalanceAsync(
                    request.EmployeeId,
                    request.LeaveTypeId,
                    request.FromDate.Year);

            if (leaveBalance == null)
                throw new Exception("Leave balance not found.");

            if (leaveBalance.Balance < totalDays)
                throw new Exception("Insufficient leave balance.");

            // Check overlapping leave
            bool overlapExists =
                await _context.LeaveApplications.AnyAsync(x =>
                    x.EmployeeId == request.EmployeeId &&
                    x.Status != ApprovalStatus.Rejected &&
                    x.Status != ApprovalStatus.Cancelled &&
                    request.FromDate <= x.ToDate &&
                    request.ToDate >= x.FromDate);

            if (overlapExists)
                throw new Exception(
                    "Leave already applied for selected dates.");

            var applicant = await _context.Employees.FirstOrDefaultAsync(x => x.Id == request.EmployeeId);

            // Same starting-level resolution as CreateAsync (Reporting
            // Manager -> Department Head -> HR) - without this, CurrentLevel
            // would silently stay at the entity default of 1 even for an
            // employee with no Reporting Manager, and both the approval
            // chain and this notification would target the wrong level.
            int startingLevel = applicant != null
                ? await ResolveStartingLevelAsync(applicant)
                : 1;

            var entity = new LeaveApplication
            {
                Id = IDManager.GetNewId(new LeaveApplication()),
                TenantId = request.TenantId,
                CompanyId = request.CompanyId,
                BranchId = request.BranchId,

                EmployeeId = request.EmployeeId,
                LeaveTypeId = request.LeaveTypeId,

                FromDate = request.FromDate,
                ToDate = request.ToDate,

                TotalDays = totalDays,

                IsHalfDay = request.IsHalfDay,
                HalfDayType = request.HalfDayType,

                Reason = request.Reason,

                Status = ApprovalStatus.Pending,
                CurrentLevel = startingLevel,

                DocumentUrl = request.DocumentUrl,

                CreatedOn = DateTime.UtcNow,
                CreatedBy = request.CreatedBy
            };

            _context.LeaveApplications.Add(entity);

            await _context.SaveChangesAsync();

            await CreateApprovalHistoryAsync(entity.Id, request.CreatedBy, ApprovalStatus.Pending, "Leave Applied");

            if (applicant != null)
            {
                var applicantName = $"{applicant.FirstName} {applicant.LastName}".Trim();

                await NotifyLevelAsync(
                    startingLevel,
                    applicant,
                    applicant.Id,
                    "New Leave Request Awaiting Your Approval",
                    $"{applicantName} applied for leave from {entity.FromDate:dd-MMM-yyyy} to {entity.ToDate:dd-MMM-yyyy} ({entity.TotalDays} day(s)). It is awaiting your approval as {GetLevelName(startingLevel)}.",
                    LeaveNotificationEvent.LeaveApplied,
                    entity.Id,
                    entity.TenantId,
                    request.CreatedBy);
            }

            return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> ApproveLeaveAsync(ApproveLeaveRequestDto request)
        {
            var leave =
                await _context.LeaveApplications
                    .Include(x => x.Employee)
                    .FirstOrDefaultAsync(x =>
                        x.Id == request.LeaveApplicationId);

            if (leave == null)
                throw new Exception("Leave application not found.");

            if (leave.Status != ApprovalStatus.Pending)
                throw new Exception(
                    "Only pending leave can be approved.");

            var (actingEmployeeId, _) = await GetActingContextAsync(request.ApprovedBy);

            if (!await IsAuthorizedForLevelAsync(leave, leave.CurrentLevel, actingEmployeeId, request.ApprovedBy))
                throw new UnauthorizedAccessException(
                    $"You are not authorized to approve this leave at the {GetLevelName(leave.CurrentLevel)} level.");

            try
            {
            if (leave.CurrentLevel >= 3)
            {
                // Final level - actually deduct the balance and close it out.
                var deductLeaveReq = new LeaveAdjustmentRequestDto()
                {
                    EmployeeId = leave.EmployeeId,
                    LeaveTypeId = leave.LeaveTypeId,
                    Days = leave.TotalDays
                };

                await _leaveBalanceService.DeductLeaveAsync(deductLeaveReq);

                leave.Status = ApprovalStatus.Approved;

                leave.ApprovedBy = request.ApprovedBy;
                leave.ApprovedDate = DateTime.UtcNow;

                leave.ModifiedOn = DateTime.UtcNow;
                leave.ModifiedBy = request.ApprovedBy;

                await _context.SaveChangesAsync();

                await CreateApprovalHistoryAsync(
                    request.LeaveApplicationId,
                    request.ApprovedBy,
                    ApprovalStatus.Approved,
                    request.Remarks ?? $"Approved by {GetLevelName(leave.CurrentLevel)} - fully approved.");

                // Final level (HR) approval - the employee is the one
                // waiting on this now, not another approver.
                await NotifyApplicantAsync(
                    leave.EmployeeId,
                    "Your Leave Request Has Been Approved",
                    $"Your leave from {leave.FromDate:dd-MMM-yyyy} to {leave.ToDate:dd-MMM-yyyy} ({leave.TotalDays} day(s)) has been fully approved.",
                    LeaveNotificationEvent.LeaveApproved,
                    leave.Id,
                    leave.TenantId,
                    request.ApprovedBy);
            }
            else
            {
                int approvedAtLevel = leave.CurrentLevel;

                leave.CurrentLevel = await ResolveNextLevelAsync(leave.CurrentLevel, leave.Employee);

                // A new level means a fresh "how long has this been pending
                // at THIS level" clock for LeaveEscalationService - without
                // resetting this, a request forwarded to Level 2 could
                // inherit Level 1's already-crossed reminder state and skip
                // straight past the new level's approver's first reminder.
                leave.LastReminderSentOn = null;

                leave.ModifiedOn = DateTime.UtcNow;
                leave.ModifiedBy = request.ApprovedBy;

                await _context.SaveChangesAsync();

                await CreateApprovalHistoryAsync(
                    request.LeaveApplicationId,
                    request.ApprovedBy,
                    ApprovalStatus.Approved,
                    request.Remarks ?? $"Approved by {GetLevelName(approvedAtLevel)} - forwarded to {GetLevelName(leave.CurrentLevel)}.");

                // Forwarded to the next level in the chain - notify whoever
                // is up next (Department Head, or every HR user if level 3).
                var applicantName = $"{leave.Employee?.FirstName} {leave.Employee?.LastName}".Trim();

                await NotifyLevelAsync(
                    leave.CurrentLevel,
                    leave.Employee,
                    leave.EmployeeId,
                    "Leave Request Awaiting Your Approval",
                    $"{applicantName}'s leave from {leave.FromDate:dd-MMM-yyyy} to {leave.ToDate:dd-MMM-yyyy} ({leave.TotalDays} day(s)) was approved by {GetLevelName(approvedAtLevel)} and is now awaiting your approval as {GetLevelName(leave.CurrentLevel)}.",
                    LeaveNotificationEvent.ApprovalPending,
                    leave.Id,
                    leave.TenantId,
                    request.ApprovedBy);
            }

            return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> RejectLeaveAsync(RejectLeaveRequestDto request)
        {
            var leave =
                await _context.LeaveApplications
                    .Include(x => x.Employee)
                    .FirstOrDefaultAsync(x =>
                        x.Id == request.LeaveApplicationId);

            if (leave == null)
                throw new Exception("Leave application not found.");

            if (leave.Status != ApprovalStatus.Pending)
                throw new Exception(
                    "Only pending leave can be rejected.");

            var (actingEmployeeId, _) = await GetActingContextAsync(request.RejectedBy);

            if (!await IsAuthorizedForLevelAsync(leave, leave.CurrentLevel, actingEmployeeId, request.RejectedBy))
                throw new UnauthorizedAccessException(
                    $"You are not authorized to reject this leave at the {GetLevelName(leave.CurrentLevel)} level.");

            try
            {
            int rejectedAtLevel = leave.CurrentLevel;

            leave.Status = ApprovalStatus.Rejected;

            leave.ApprovedBy = request.RejectedBy;
            leave.ApprovedDate = DateTime.UtcNow;

            leave.RejectedReason = request.RejectedReason;

            leave.ModifiedOn = DateTime.UtcNow;
            leave.ModifiedBy = request.RejectedBy;

            await _context.SaveChangesAsync();

            await CreateApprovalHistoryAsync(
                request.LeaveApplicationId,
                request.RejectedBy,
                ApprovalStatus.Rejected,
                $"Rejected by {GetLevelName(rejectedAtLevel)}: {request.RejectedReason}");

            await NotifyApplicantAsync(
                leave.EmployeeId,
                "Your Leave Request Was Rejected",
                $"Your leave from {leave.FromDate:dd-MMM-yyyy} to {leave.ToDate:dd-MMM-yyyy} was rejected by {GetLevelName(rejectedAtLevel)}. Reason: {request.RejectedReason}",
                LeaveNotificationEvent.LeaveRejected,
                leave.Id,
                leave.TenantId,
                request.RejectedBy);

            return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> SendBackLeaveAsync(SendBackLeaveRequestDto request)
        {
            var leave =
                await _context.LeaveApplications
                    .Include(x => x.Employee)
                    .FirstOrDefaultAsync(x =>
                        x.Id == request.LeaveApplicationId);

            if (leave == null)
                throw new Exception("Leave application not found.");

            if (leave.Status != ApprovalStatus.Pending)
                throw new Exception(
                    "Only pending leave can be sent back.");

            var (actingEmployeeId, _) = await GetActingContextAsync(request.SentBackBy);

            if (!await IsAuthorizedForLevelAsync(leave, leave.CurrentLevel, actingEmployeeId, request.SentBackBy))
                throw new UnauthorizedAccessException(
                    $"You are not authorized to act on this leave at the {GetLevelName(leave.CurrentLevel)} level.");

            try
            {
            int sentBackFromLevel = leave.CurrentLevel;

            leave.Status = ApprovalStatus.ReturnedToEmployee;
            leave.SendBackReason = request.Reason;

            leave.ModifiedOn = DateTime.UtcNow;
            leave.ModifiedBy = request.SentBackBy;

            await _context.SaveChangesAsync();

            await CreateApprovalHistoryAsync(
                request.LeaveApplicationId,
                request.SentBackBy,
                ApprovalStatus.ReturnedToEmployee,
                $"Sent back by {GetLevelName(sentBackFromLevel)}: {request.Reason}");

            await NotifyApplicantAsync(
                leave.EmployeeId,
                "Your Leave Request Was Sent Back",
                $"Your leave from {leave.FromDate:dd-MMM-yyyy} to {leave.ToDate:dd-MMM-yyyy} was sent back by {GetLevelName(sentBackFromLevel)} for correction. Reason: {request.Reason}",
                LeaveNotificationEvent.LeaveSentBack,
                leave.Id,
                leave.TenantId,
                request.SentBackBy);

            return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // The employee edits and resubmits a leave that was sent back to
        // them - this restarts the approval chain from the top (Level 1),
        // it does not resume from wherever it was sent back from.
        public async Task<LeaveApplicationDto> ResubmitAsync(string id, ApplyLeaveRequestDto request, string resubmittedBy)
        {
            var leave =
                await _context.LeaveApplications
                    .Include(x => x.Employee)
                    .FirstOrDefaultAsync(x => x.Id == id);

            if (leave == null)
                throw new Exception("Leave application not found.");

            if (leave.Status != ApprovalStatus.ReturnedToEmployee)
                throw new Exception("Only a leave request that was sent back can be resubmitted.");

            if (leave.EmployeeId != request.EmployeeId)
                throw new UnauthorizedAccessException("You can only resubmit your own leave request.");

            try
            {
            decimal totalDays = await CalculateTotalDaysAsync(
                request.FromDate,
                request.ToDate,
                request.IsHalfDay,
                request.TenantId ?? leave.TenantId);

            leave.LeaveTypeId = request.LeaveTypeId;
            leave.FromDate = request.FromDate;
            leave.ToDate = request.ToDate;
            leave.TotalDays = totalDays;
            leave.IsHalfDay = request.IsHalfDay;
            leave.HalfDayType = request.HalfDayType;
            leave.Reason = request.Reason;

            if (!string.IsNullOrWhiteSpace(request.DocumentUrl))
                leave.DocumentUrl = request.DocumentUrl;

            leave.Status = ApprovalStatus.Pending;
            leave.CurrentLevel = await ResolveStartingLevelAsync(leave.Employee);
            leave.SendBackReason = null;

            // Fresh reminder clock - the chain restarts from the top, so
            // any reminder already sent for the pending state before it was
            // sent back is no longer relevant.
            leave.LastReminderSentOn = null;

            leave.ModifiedOn = DateTime.UtcNow;
            leave.ModifiedBy = resubmittedBy;

            await _context.SaveChangesAsync();

            await CreateApprovalHistoryAsync(
                leave.Id,
                resubmittedBy,
                ApprovalStatus.Pending,
                $"Resubmitted by employee - awaiting {GetLevelName(leave.CurrentLevel)} approval");

            // The chain restarts from the top - notify whoever it now
            // resolves to (Level 1), same as a brand-new Apply.
            var applicantName = $"{leave.Employee?.FirstName} {leave.Employee?.LastName}".Trim();

            await NotifyLevelAsync(
                leave.CurrentLevel,
                leave.Employee,
                leave.EmployeeId,
                "Leave Request Resubmitted - Awaiting Your Approval",
                $"{applicantName} resubmitted their leave from {leave.FromDate:dd-MMM-yyyy} to {leave.ToDate:dd-MMM-yyyy} ({leave.TotalDays} day(s)). It is awaiting your approval as {GetLevelName(leave.CurrentLevel)}.",
                LeaveNotificationEvent.LeaveResubmitted,
                leave.Id,
                leave.TenantId,
                resubmittedBy);

            return await GetByIdAsync(leave.Id);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool> CancelLeaveAsync(CancelLeaveRequestDto request)
        {
            try
            {
            var leave =
                await _context.LeaveApplications
                    .Include(x => x.Employee)
                    .FirstOrDefaultAsync(x =>
                        x.Id == request.LeaveApplicationId);

            if (leave == null)
                throw new Exception("Leave application not found.");

            if (leave.Status == ApprovalStatus.Cancelled)
                throw new Exception("Leave already cancelled.");

            if (leave.Status == ApprovalStatus.Approved)
            {
                var creditLeaveReq = new LeaveAdjustmentRequestDto()
                {
                    EmployeeId= leave.EmployeeId,
                    LeaveTypeId= leave.LeaveTypeId,
                    Days= leave.TotalDays
                };

                await _leaveBalanceService.CreditLeaveAsync(creditLeaveReq);
            }

            // Captured before the status is overwritten below - only a
            // request that was actually still Pending has a "current
            // approver" left waiting on it.
            bool wasPending = leave.Status == ApprovalStatus.Pending;
            int pendingAtLevel = leave.CurrentLevel;

            leave.Status = ApprovalStatus.Cancelled;

            leave.ModifiedOn = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await CreateApprovalHistoryAsync(request.LeaveApplicationId, request.CancelledBy, ApprovalStatus.Cancelled, "Leave Cancelled");

            if (wasPending)
            {
                // Whoever was currently sitting on this request shouldn't
                // keep waiting on a request the employee just withdrew.
                var applicantName = $"{leave.Employee?.FirstName} {leave.Employee?.LastName}".Trim();

                await NotifyLevelAsync(
                    pendingAtLevel,
                    leave.Employee,
                    leave.EmployeeId,
                    "Leave Request Cancelled",
                    $"{applicantName}'s leave from {leave.FromDate:dd-MMM-yyyy} to {leave.ToDate:dd-MMM-yyyy}, which was awaiting your approval, has been cancelled by the employee.",
                    LeaveNotificationEvent.LeaveCancelled,
                    leave.Id,
                    leave.TenantId,
                    request.CancelledBy);
            }

            return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region Queries

        public async Task<List<LeaveApplicationDto>>GetEmployeeLeavesAsync(string employeeId)
        {
            var entities = await _context.LeaveApplications
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .Where(x => x.EmployeeId == employeeId)
                .OrderByDescending(x => x.CreatedOn)
                .ToListAsync();

            var deptMap = await BuildDepartmentSeniorityMapAsync();

            return AssembleDtoList(entities, deptMap);
        }

        public async Task<List<LeaveApplicationDto>>GetPendingLeavesAsync()
        {
            var entities = await _context.LeaveApplications
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .Where(x => x.Status == ApprovalStatus.Pending)
                .OrderByDescending(x => x.CreatedOn)
                .ToListAsync();

            var deptMap = await BuildDepartmentSeniorityMapAsync();

            return AssembleDtoList(entities, deptMap);
        }

        public async Task<List<LeaveApplicationDto>> GetPendingForApproverAsync(string? employeeId, string? userId)
        {
            bool isHr = await IsHrApproverAsync(userId);

            var pending = await _context.LeaveApplications
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .Where(x => x.Status == ApprovalStatus.Pending)
                .OrderBy(x => x.CreatedOn)
                .ToListAsync();

            var deptMap = await BuildDepartmentSeniorityMapAsync();

            // For Level 1/2, "mine" also includes anything currently
            // awaiting the person I am an active delegate FOR (Part 2) - not
            // just requests directly pointed at my own EmployeeId.
            var mine = new List<LeaveApplication>();

            foreach (var x in pending)
            {
                bool isMine = x.CurrentLevel switch
                {
                    1 => await IsEmployeeOrActiveDelegateAsync(x.Employee?.ReportingManagerId, employeeId),
                    2 => await IsEmployeeOrActiveDelegateAsync(
                            GetDepartmentHeadIdFromMap(deptMap, x.Employee?.DepartmentId, x.EmployeeId), employeeId),
                    _ => isHr
                };

                if (isMine)
                    mine.Add(x);
            }

            return AssembleDtoList(mine, deptMap);
        }

        // Public helpers reused outside this class (LeaveEscalationService -
        // API/BackgroundServices/LeaveEscalationService.cs) so the
        // stale-pending reminder job resolves "who is the current approver"
        // through the exact same chain/delegation logic as everywhere else
        // in this file, rather than re-implementing it.
        #region External Resolution Helpers

        // The User.Id(s) who should currently be notified/act on this
        // pending leave application - same resolution as the live transition
        // notifications (NotifyLevelAsync/ResolveApproverUserIdsAsync),
        // delegate-aware for Level 1/2. Empty if the leave isn't Pending, or
        // resolves to nobody.
        public async Task<List<string>> ResolveCurrentApproverUserIdsAsync(string leaveApplicationId)
        {
            var leave = await _context.LeaveApplications
                .Include(x => x.Employee)
                .FirstOrDefaultAsync(x => x.Id == leaveApplicationId);

            if (leave == null || leave.Status != ApprovalStatus.Pending || leave.Employee == null)
                return new List<string>();

            return await ResolveApproverUserIdsAsync(leave.CurrentLevel, leave.Employee, leave.EmployeeId);
        }

        // The User.Id(s) of whoever is NEXT in the chain after the current
        // level - used only for the escalation job's FYI "heads up" notice
        // once a request has been pending long enough to cross the
        // (longer) EscalateAfterHours threshold. Deliberately read-only:
        // this does NOT change CurrentLevel/Status or reassign approval
        // ownership - see LeaveEscalationService for why auto-reassignment
        // was intentionally not implemented.
        public async Task<List<string>> ResolveNextLevelApproverUserIdsAsync(string leaveApplicationId)
        {
            var leave = await _context.LeaveApplications
                .Include(x => x.Employee)
                .FirstOrDefaultAsync(x => x.Id == leaveApplicationId);

            if (leave == null || leave.Status != ApprovalStatus.Pending || leave.Employee == null || leave.CurrentLevel >= 3)
                return new List<string>();

            int nextLevel = await ResolveNextLevelAsync(leave.CurrentLevel, leave.Employee);

            return await ResolveApproverUserIdsAsync(nextLevel, leave.Employee, leave.EmployeeId);
        }

        #endregion

        public async Task<List<LeaveApplicationDto>>GetApprovedLeavesAsync()
        {
            var entities = await _context.LeaveApplications
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .Where(x => x.Status == ApprovalStatus.Approved)
                .OrderByDescending(x => x.ApprovedDate)
                .ToListAsync();

            var deptMap = await BuildDepartmentSeniorityMapAsync();

            return AssembleDtoList(entities, deptMap);
        }

        public async Task<List<LeaveApplicationDto>>GetRejectedLeavesAsync()
        {
            var entities = await _context.LeaveApplications
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .Where(x => x.Status == ApprovalStatus.Rejected)
                .OrderByDescending(x => x.ApprovedDate)
                .ToListAsync();

            var deptMap = await BuildDepartmentSeniorityMapAsync();

            return AssembleDtoList(entities, deptMap);
        }

        public async Task<List<LeaveApplicationDto>>GetCancelledLeavesAsync()
        {
            var entities = await _context.LeaveApplications
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .Where(x => x.Status == ApprovalStatus.Cancelled)
                .OrderByDescending(x => x.ModifiedOn)
                .ToListAsync();

            var deptMap = await BuildDepartmentSeniorityMapAsync();

            return AssembleDtoList(entities, deptMap);
        }

        public async Task<List<LeaveApplicationDto>>GetFilteredAsync(LeaveApplicationFilterRequestDto request)
        {
            var query = _context.LeaveApplications
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.EmployeeId))
            {
                query = query.Where(x =>
                    x.EmployeeId == request.EmployeeId);
            }

            if (!string.IsNullOrWhiteSpace(request.LeaveTypeId))
            {
                query = query.Where(x =>
                    x.LeaveTypeId == request.LeaveTypeId);
            }

            if (request.Status.HasValue)
            {
                query = query.Where(x =>
                    x.Status == request.Status.Value);
            }

            if (request.FromDate.HasValue)
            {
                query = query.Where(x =>
                    x.FromDate.Date >= request.FromDate.Value.Date);
            }

            if (request.ToDate.HasValue)
            {
                query = query.Where(x =>
                    x.ToDate.Date <= request.ToDate.Value.Date);
            }

            var entities = await query
                .OrderByDescending(x => x.CreatedOn)
                .ToListAsync();

            var deptMap = await BuildDepartmentSeniorityMapAsync();

            return AssembleDtoList(entities, deptMap);
        }

        #endregion

        #region Count

        public async Task<int> GetPendingLeaveCountAsync()
        {
            try
            {
            return await _context.LeaveApplications
                .CountAsync(x =>
                    x.Status == ApprovalStatus.Pending);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public async Task<int> GetApprovedLeaveCountAsync()
        {
            try
            {
            return await _context.LeaveApplications
                .CountAsync(x =>
                    x.Status == ApprovalStatus.Approved);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public async Task<int> GetTodayLeaveCountAsync()
        {
            try
            {
            var today = DateTime.Today;

            return await _context.LeaveApplications
                .CountAsync(x =>
                    x.Status == ApprovalStatus.Approved &&
                    x.FromDate.Date <= today &&
                    x.ToDate.Date >= today);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        #endregion

        #region Approval History
        public async Task<List<LeaveApprovalHistoryDetailDto>>GetApprovalHistoryAsync(string leaveApplicationId)
        {
            return await _context.LeaveApprovalHistories
                .Include(x => x.LeaveApplication)
                    .ThenInclude(x => x.Employee)
                .Include(x => x.LeaveApplication)
                    .ThenInclude(x => x.LeaveType)
                .Where(x => x.LeaveApplicationId == leaveApplicationId)
                .OrderBy(x => x.ActionDate)
                .Select(x => new LeaveApprovalHistoryDetailDto
                {
                    Id = x.Id,

                    LeaveApplicationId = x.LeaveApplicationId,

                    LeaveApplicationNo = x.LeaveApplication.Id,

                    EmployeeId = x.LeaveApplication.EmployeeId,

                    EmployeeName =
                        x.LeaveApplication.Employee.FirstName +
                        " " +
                        x.LeaveApplication.Employee.LastName,

                    LeaveTypeId = x.LeaveApplication.LeaveTypeId,

                    LeaveTypeName =
                        x.LeaveApplication.LeaveType.Name,

                    FromDate = x.LeaveApplication.FromDate,

                    ToDate = x.LeaveApplication.ToDate,

                    TotalDays = x.LeaveApplication.TotalDays,

                    ActionBy = x.ActionBy,

                    Action = x.Action,

                    Remarks = x.Remarks,

                    ActionDate = x.ActionDate,

                    CreatedOn = x.CreatedOn,

                    CreatedBy = x.CreatedBy
                })
                .ToListAsync();
        }

        public async Task<List<LeaveApprovalHistoryDetailDto>> GetAllApprovalHistoryAsync()
        {
            try
            {
            var histories = await _context.LeaveApprovalHistories
                .Include(x => x.LeaveApplication)
                    .ThenInclude(x => x.Employee)
                .Include(x => x.LeaveApplication)
                    .ThenInclude(x => x.LeaveType)
                .OrderByDescending(x => x.ActionDate)
                .ToListAsync();

            var userIds = histories
                .Select(x => x.ActionBy)
                .Distinct()
                .ToList();

            var users = await _context.Users
                .Include(x => x.Employee)
                .Where(x => userIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id);

            return histories.Select(x => new LeaveApprovalHistoryDetailDto
            {
                Id = x.Id,

                LeaveApplicationId = x.LeaveApplicationId,

                LeaveApplicationNo = x.LeaveApplication.Id,

                EmployeeId = x.LeaveApplication.EmployeeId,

                EmployeeName =
                    x.LeaveApplication.Employee.FirstName + " " +
                    x.LeaveApplication.Employee.LastName,

                LeaveTypeId = x.LeaveApplication.LeaveTypeId,

                LeaveTypeName = x.LeaveApplication.LeaveType.Name,

                FromDate = x.LeaveApplication.FromDate,

                ToDate = x.LeaveApplication.ToDate,

                TotalDays = x.LeaveApplication.TotalDays,

                ActionBy = users.ContainsKey(x.ActionBy)
                    ? users[x.ActionBy].Employee.FirstName + " " +
                      users[x.ActionBy].Employee.LastName
                    : "",

                Action = x.Action,

                Remarks = x.Remarks,

                ActionDate = x.ActionDate,

                CreatedOn = x.CreatedOn,

                CreatedBy = x.CreatedBy

            }).ToList();
            }
            catch (Exception)
            {
                return new List<LeaveApprovalHistoryDetailDto>();
            }
        }

        public async Task<LeaveApprovalHistoryDetailDto>GetApprovalHistoryByIdAsync(string id)
        {
            var data =
                await _context.LeaveApprovalHistories
                .Include(x => x.LeaveApplication)
                    .ThenInclude(x => x.Employee)
                .Include(x => x.LeaveApplication)
                    .ThenInclude(x => x.LeaveType)
                .Where(x => x.Id == id)
                .Select(x => new LeaveApprovalHistoryDetailDto
                {
                    Id = x.Id,

                    LeaveApplicationId =
                        x.LeaveApplicationId,

                    LeaveApplicationNo =
                        x.LeaveApplication.Id,

                    EmployeeId =
                        x.LeaveApplication.EmployeeId,

                    EmployeeName =
                        x.LeaveApplication.Employee.FirstName +
                        " " +
                        x.LeaveApplication.Employee.LastName,

                    LeaveTypeId =
                        x.LeaveApplication.LeaveTypeId,

                    LeaveTypeName =
                        x.LeaveApplication.LeaveType.Name,

                    FromDate =
                        x.LeaveApplication.FromDate,

                    ToDate =
                        x.LeaveApplication.ToDate,

                    TotalDays =
                        x.LeaveApplication.TotalDays,

                    ActionBy =
                        x.ActionBy,

                    Action =
                        x.Action,

                    Remarks =
                        x.Remarks,

                    ActionDate =
                        x.ActionDate,

                    CreatedOn =
                        x.CreatedOn,

                    CreatedBy =
                        x.CreatedBy
                })
                .FirstOrDefaultAsync();

            if (data == null)
                throw new Exception(
                    "Approval history not found.");

            return data;
        }

        private async Task CreateApprovalHistoryAsync(string leaveApplicationId,string actionBy, ApprovalStatus action,string? remarks)
        {
            var history = new LeaveApprovalHistory
            {
                Id = IDManager.GetNewId(new LeaveApprovalHistory()),

                LeaveApplicationId = leaveApplicationId,

                ActionBy = actionBy,

                Action = action,

                Remarks = remarks,

                ActionDate = DateTime.UtcNow,

                CreatedOn = DateTime.UtcNow,
                CreatedBy = actionBy
            };

            _context.LeaveApprovalHistories.Add(history);

            await _context.SaveChangesAsync();
        }
        #endregion

        #region Day Calculation

        // Counts only actual working days between fromDate and toDate -
        // a day is excluded if it's a configured week-off (e.g. Sat/Sun)
        // or a holiday for the tenant, so leave taken across a weekend or
        // a public holiday doesn't burn extra leave balance for those
        // non-working days.
        public async Task<decimal> CalculateTotalDaysAsync(DateTime fromDate, DateTime toDate, bool isHalfDay, string? tenantId)
        {
            if (isHalfDay)
                return 0.5m;

            if (toDate.Date < fromDate.Date)
                return 0m;

            if (string.IsNullOrEmpty(tenantId))
            {
                // No tenant to check week-offs/holidays against - fall back
                // to plain calendar days rather than silently returning 0.
                return (decimal)((toDate.Date - fromDate.Date).Days + 1);
            }

            decimal totalDays = 0m;

            for (var date = fromDate.Date; date <= toDate.Date; date = date.AddDays(1))
            {
                bool isWeekOff = await _weekOffService.IsWeekOff(date, tenantId);
                bool isHoliday = await _weekOffService.IsHoliday(date, tenantId);

                if (!isWeekOff && !isHoliday)
                    totalDays += 1;
            }

            return totalDays;
        }

        #endregion

        #region Calendar

        // Approved leaves overlapping the given month, for the month-grid
        // Leave Calendar view. Scoping mirrors the rest of this controller's
        // admin-vs-ESS convention: an admin sees everyone (optionally
        // narrowed to one department via the dropdown filter), a
        // self-service employee is scoped to their own department so they
        // can see who on their team is out without seeing the whole
        // org's leave.
        public async Task<LeaveCalendarResponseDto> GetCalendarAsync(
            int year,
            int month,
            string? employeeId,
            bool isAdmin,
            string? tenantId,
            string? departmentId = null)
        {
            var monthStart = new DateTime(year, month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var response = new LeaveCalendarResponseDto
            {
                Year = year,
                Month = month,
                MonthName = monthStart.ToString("MMMM yyyy")
            };

            string? scopeDepartmentId = departmentId;

            if (!isAdmin)
            {
                if (string.IsNullOrEmpty(employeeId))
                    return response;

                var me = await _context.Employees
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == employeeId);

                scopeDepartmentId = me?.DepartmentId;

                // No department to scope to (e.g. no linked employee) -
                // nothing to safely show rather than accidentally falling
                // through to an org-wide view.
                if (string.IsNullOrEmpty(scopeDepartmentId))
                    return response;
            }

            var query = _context.LeaveApplications
                .AsNoTracking()
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .Where(x =>
                    x.Status == ApprovalStatus.Approved &&
                    x.FromDate.Date <= monthEnd &&
                    x.ToDate.Date >= monthStart);

            if (!string.IsNullOrEmpty(tenantId))
                query = query.Where(x => x.TenantId == tenantId);

            if (!string.IsNullOrEmpty(scopeDepartmentId))
                query = query.Where(x => x.Employee.DepartmentId == scopeDepartmentId);

            var leaves = await query
                .OrderBy(x => x.FromDate)
                .ToListAsync();

            response.Entries = leaves.Select(x => new LeaveCalendarEntryDto
            {
                LeaveApplicationId = x.Id,
                EmployeeId = x.EmployeeId,
                EmployeeName = $"{x.Employee?.FirstName} {x.Employee?.LastName}".Trim(),
                LeaveTypeName = x.LeaveType?.Name,
                FromDate = x.FromDate,
                ToDate = x.ToDate,
                IsHalfDay = x.IsHalfDay
            }).ToList();

            // Optional holiday overlay - best-effort only, reusing the same
            // per-day tenant holiday check CalculateTotalDaysAsync already
            // relies on. Skipped entirely (not an error) if there's no
            // tenant to check against.
            if (!string.IsNullOrEmpty(tenantId))
            {
                for (var date = monthStart; date <= monthEnd; date = date.AddDays(1))
                {
                    if (await _weekOffService.IsHoliday(date, tenantId))
                        response.HolidayDates.Add(date.Date);
                }
            }

            return response;
        }

        #endregion

    }

}
