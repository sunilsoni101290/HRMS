using Application.DTOs.Attendances;
using Application.Interfaces.Attendances;
using Domain.Entities;
using Domain.Helper;
using Domain.Interfaces;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Application.Services.Attendances
{
    // Attendance Regularization - lets an employee submit the correct
    // First-In/Last-Out for a date (missing punch OR wrong punch time,
    // deliberately one unified form rather than two request types) and
    // routes it through the exact same multi-level approval chain as
    // Leave (LeaveApplicationService). The chain-resolution helpers below
    // are a deliberate copy of LeaveApplicationService's - each module in
    // this codebase hand-rolls its own chain rather than sharing one.
    public class AttendanceRegularizationService : IAttendanceRegularizationService
    {
        private readonly ApplicationDbContext _context;

        public AttendanceRegularizationService(ApplicationDbContext context)
        {
            _context = context;
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
        // list once and reusing it for every request in the list.
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

        private async Task<bool> IsAuthorizedForLevelAsync(
            AttendanceRegularization request,
            int level,
            string? actingEmployeeId,
            string? actingRoleName,
            string? actingUserId = null)
        {
            switch (level)
            {
                case 1:
                    var l1 = request.Employee?.ReportingManagerId;
                    return !string.IsNullOrEmpty(l1) &&
                           !string.IsNullOrEmpty(actingEmployeeId) &&
                           l1 == actingEmployeeId;

                case 2:
                    var l2 = await GetDepartmentHeadIdAsync(request.Employee?.DepartmentId, request.EmployeeId);
                    return !string.IsNullOrEmpty(l2) &&
                           !string.IsNullOrEmpty(actingEmployeeId) &&
                           l2 == actingEmployeeId;

                case 3:
                    // Formalized to a real permission check (mirrors
                    // LeaveApplicationService.IsHrApproverAsync) rather than
                    // a loose substring match on the acting user's role name.
                    return await IsHrApproverAsync(actingUserId);

                default:
                    return false;
            }
        }

        // Does the acting user (by User.Id) hold, through any Role assigned
        // to them, an allowed RolePermission for the "Approve" action on the
        // Attendance Regularization feature (seeded with canApprove: true in
        // DbSeeder.ReconcileModulesAsync, so ReconcilePermissionsAsync
        // auto-generates the matching Permission row). Soft-deleted
        // Users/UserRoles/RolePermissions/Permissions are already excluded by
        // the global query filter, so no explicit !IsDeleted checks needed.
        public async Task<bool> IsHrApproverAsync(string? actingUserId)
        {
            if (string.IsNullOrEmpty(actingUserId))
                return false;

            return await (
                from ur in _context.UserRoles
                join rp in _context.RolePermissions.Where(x => x.IsAllowed) on ur.RoleId equals rp.RoleId
                join p in _context.Permissions.Where(x =>
                        x.FeatureId == AppFeatureConstants.ATTENDANCE_REGULARIZATION && x.Action == Actions.Approve)
                    on rp.PermissionId equals p.Id
                where ur.UserId == actingUserId
                select p.Id
            ).AnyAsync();
        }

        private static AttendanceRegularizationDto AssembleDto(AttendanceRegularization x, string? currentApproverEmployeeId)
        {
            return new AttendanceRegularizationDto
            {
                Id = x.Id,

                EmployeeId = x.EmployeeId,
                EmployeeName = x.Employee != null ? $"{x.Employee.FirstName} {x.Employee.LastName}".Trim() : null,

                Date = x.Date,

                AttendanceId = x.AttendanceId,

                OriginalFirstIn = x.OriginalFirstIn,
                OriginalLastOut = x.OriginalLastOut,

                RequestedFirstIn = x.RequestedFirstIn,
                RequestedLastOut = x.RequestedLastOut,

                Reason = x.Reason,

                Status = x.Status,

                ApprovedBy = x.ApprovedBy,
                ApprovedDate = x.ApprovedDate,

                RejectedReason = x.RejectedReason,

                CurrentLevel = x.CurrentLevel,
                CurrentLevelName = x.Status == ApprovalStatus.Pending ? GetLevelName(x.CurrentLevel) : null,
                CurrentApproverEmployeeId = x.Status == ApprovalStatus.Pending ? currentApproverEmployeeId : null,
                SendBackReason = x.SendBackReason,

                CreatedOn = x.CreatedOn,
                CreatedBy = x.CreatedBy,

                ModifiedOn = x.ModifiedOn,
                ModifiedBy = x.ModifiedBy
            };
        }

        // ApprovedBy/RejectedBy store the acting User.Id (not EmployeeId) -
        // resolve a display name via the linked Employee if one exists,
        // else fall back to the account's Username.
        private async Task<Dictionary<string, string>> BuildApprovedByNameMapAsync(IEnumerable<AttendanceRegularization> entities)
        {
            var userIds = entities
                .Where(x => !string.IsNullOrEmpty(x.ApprovedBy))
                .Select(x => x.ApprovedBy)
                .Distinct()
                .ToList();

            if (userIds.Count == 0)
                return new Dictionary<string, string>();

            var users = await _context.Users
                .Include(u => u.Employee)
                .Where(u => userIds.Contains(u.Id))
                .ToListAsync();

            return users.ToDictionary(
                u => u.Id,
                u => u.Employee != null ? $"{u.Employee.FirstName} {u.Employee.LastName}".Trim() : u.Username);
        }

        private async Task<AttendanceRegularizationDto> AssembleDtoAsync(AttendanceRegularization x)
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

            var dto = AssembleDto(x, approverId);

            if (!string.IsNullOrEmpty(x.ApprovedBy))
            {
                var nameMap = await BuildApprovedByNameMapAsync(new[] { x });
                dto.ApprovedByName = nameMap.TryGetValue(x.ApprovedBy, out var name) ? name : null;
            }

            return dto;
        }

        private async Task<List<AttendanceRegularizationDto>> AssembleDtoList(
            List<AttendanceRegularization> entities,
            Dictionary<string, List<(string EmployeeId, int Level)>> deptSeniorityMap)
        {
            var approvedByNameMap = await BuildApprovedByNameMapAsync(entities);

            var list = new List<AttendanceRegularizationDto>();

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

                var dto = AssembleDto(x, approverId);

                if (!string.IsNullOrEmpty(x.ApprovedBy) && approvedByNameMap.TryGetValue(x.ApprovedBy, out var name))
                    dto.ApprovedByName = name;

                list.Add(dto);
            }

            return list;
        }

        #endregion

        #region CRUD

        public async Task<AttendanceRegularizationDto> CreateAsync(RequestRegularizationDto request)
        {
            // An employee must wait for their current regularization request
            // for this date to be approved/rejected/cancelled before they
            // can submit another one for the same date - mirrors Leave's
            // "already has a pending leave" guard.
            bool hasPendingRequest = await _context.AttendanceRegularizations
                .AnyAsync(x =>
                    x.EmployeeId == request.EmployeeId &&
                    x.Date.Date == request.Date.Date &&
                    x.Status == ApprovalStatus.Pending);

            if (hasPendingRequest)
                throw new InvalidOperationException(
                    "You already have a regularization request awaiting approval for this date.");

            if (request.RequestedFirstIn == null && request.RequestedLastOut == null)
                throw new InvalidOperationException(
                    "Please provide a requested First In and/or Last Out time.");

            var applicant = await _context.Employees.FirstOrDefaultAsync(x => x.Id == request.EmployeeId);

            if (applicant == null)
                throw new Exception("Employee not found.");

            try
            {
                // Snapshot whatever Attendance already has for this date (for
                // the Details screen / write-back reference) - null if the
                // punch was fully missed and no Attendance row exists yet.
                var existingAttendance = await _context.Attendances
                    .FirstOrDefaultAsync(x =>
                        x.EmployeeId == request.EmployeeId &&
                        x.Date.Date == request.Date.Date);

                int startingLevel = await ResolveStartingLevelAsync(applicant);

                var entity = new AttendanceRegularization
                {
                    Id = IDManager.GetNewId(new AttendanceRegularization()),

                    EmployeeId = request.EmployeeId,
                    Date = request.Date.Date,

                    AttendanceId = existingAttendance?.Id,
                    OriginalFirstIn = existingAttendance?.FirstIn,
                    OriginalLastOut = existingAttendance?.LastOut,

                    RequestedFirstIn = request.RequestedFirstIn,
                    RequestedLastOut = request.RequestedLastOut,

                    Reason = request.Reason,

                    Status = ApprovalStatus.Pending,
                    CurrentLevel = startingLevel,

                    CreatedOn = DateTime.UtcNow,
                    CreatedBy = request.CreatedBy
                };

                _context.AttendanceRegularizations.Add(entity);

                await _context.SaveChangesAsync();

                await CreateApprovalHistoryAsync(
                    entity.Id,
                    request.CreatedBy,
                    ApprovalStatus.Pending,
                    $"Regularization requested - awaiting {GetLevelName(startingLevel)} approval");

                return await GetByIdAsync(entity.Id);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<AttendanceRegularizationDto> GetByIdAsync(string id)
        {
            try
            {
                var entity = await _context.AttendanceRegularizations
                    .Include(x => x.Employee)
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (entity == null)
                    throw new Exception("Attendance regularization request not found.");

                return await AssembleDtoAsync(entity);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<List<AttendanceRegularizationDto>> GetAllAsync()
        {
            try
            {
                var entities = await _context.AttendanceRegularizations
                    .Include(x => x.Employee)
                    .OrderByDescending(x => x.CreatedOn)
                    .ToListAsync();

                var deptMap = await BuildDepartmentSeniorityMapAsync();

                return await AssembleDtoList(entities, deptMap);
            }
            catch (Exception)
            {
                return new List<AttendanceRegularizationDto>();
            }
        }

        #endregion

        #region Workflow

        public async Task<bool> RequestRegularizationAsync(RequestRegularizationDto request)
        {
            var result = await CreateAsync(request);
            return result != null;
        }

        public async Task<bool> ApproveAsync(ApproveRegularizationRequestDto request)
        {
            var regularization =
                await _context.AttendanceRegularizations
                    .Include(x => x.Employee)
                        .ThenInclude(e => e.DefaultShift)
                    .FirstOrDefaultAsync(x => x.Id == request.AttendanceRegularizationId);

            if (regularization == null)
                throw new Exception("Attendance regularization request not found.");

            if (regularization.Status != ApprovalStatus.Pending)
                throw new Exception("Only pending requests can be approved.");

            var (actingEmployeeId, actingRoleName) = await GetActingContextAsync(request.ApprovedBy);

            if (!await IsAuthorizedForLevelAsync(regularization, regularization.CurrentLevel, actingEmployeeId, actingRoleName, request.ApprovedBy))
                throw new UnauthorizedAccessException(
                    $"You are not authorized to approve this request at the {GetLevelName(regularization.CurrentLevel)} level.");

            try
            {
                if (regularization.CurrentLevel >= 3)
                {
                    // Final level - actually write the correction back into
                    // Attendance/AttendanceLog.
                    await ApplyRegularizationToAttendanceAsync(regularization, request.ApprovedBy);

                    regularization.Status = ApprovalStatus.Approved;

                    regularization.ApprovedBy = request.ApprovedBy;
                    regularization.ApprovedDate = DateTime.UtcNow;

                    regularization.ModifiedOn = DateTime.UtcNow;
                    regularization.ModifiedBy = request.ApprovedBy;

                    await _context.SaveChangesAsync();

                    await CreateApprovalHistoryAsync(
                        request.AttendanceRegularizationId,
                        request.ApprovedBy,
                        ApprovalStatus.Approved,
                        request.Remarks ?? $"Approved by {GetLevelName(regularization.CurrentLevel)} - fully approved.");
                }
                else
                {
                    int approvedAtLevel = regularization.CurrentLevel;

                    regularization.CurrentLevel = await ResolveNextLevelAsync(regularization.CurrentLevel, regularization.Employee);

                    regularization.ModifiedOn = DateTime.UtcNow;
                    regularization.ModifiedBy = request.ApprovedBy;

                    await _context.SaveChangesAsync();

                    await CreateApprovalHistoryAsync(
                        request.AttendanceRegularizationId,
                        request.ApprovedBy,
                        ApprovalStatus.Approved,
                        request.Remarks ?? $"Approved by {GetLevelName(approvedAtLevel)} - forwarded to {GetLevelName(regularization.CurrentLevel)}.");
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> RejectAsync(RejectRegularizationRequestDto request)
        {
            var regularization =
                await _context.AttendanceRegularizations
                    .Include(x => x.Employee)
                    .FirstOrDefaultAsync(x => x.Id == request.AttendanceRegularizationId);

            if (regularization == null)
                throw new Exception("Attendance regularization request not found.");

            if (regularization.Status != ApprovalStatus.Pending)
                throw new Exception("Only pending requests can be rejected.");

            var (actingEmployeeId, actingRoleName) = await GetActingContextAsync(request.RejectedBy);

            if (!await IsAuthorizedForLevelAsync(regularization, regularization.CurrentLevel, actingEmployeeId, actingRoleName, request.RejectedBy))
                throw new UnauthorizedAccessException(
                    $"You are not authorized to reject this request at the {GetLevelName(regularization.CurrentLevel)} level.");

            try
            {
                int rejectedAtLevel = regularization.CurrentLevel;

                regularization.Status = ApprovalStatus.Rejected;

                regularization.ApprovedBy = request.RejectedBy;
                regularization.ApprovedDate = DateTime.UtcNow;

                regularization.RejectedReason = request.RejectedReason;

                regularization.ModifiedOn = DateTime.UtcNow;
                regularization.ModifiedBy = request.RejectedBy;

                await _context.SaveChangesAsync();

                await CreateApprovalHistoryAsync(
                    request.AttendanceRegularizationId,
                    request.RejectedBy,
                    ApprovalStatus.Rejected,
                    $"Rejected by {GetLevelName(rejectedAtLevel)}: {request.RejectedReason}");

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> SendBackAsync(SendBackRegularizationRequestDto request)
        {
            var regularization =
                await _context.AttendanceRegularizations
                    .Include(x => x.Employee)
                    .FirstOrDefaultAsync(x => x.Id == request.AttendanceRegularizationId);

            if (regularization == null)
                throw new Exception("Attendance regularization request not found.");

            if (regularization.Status != ApprovalStatus.Pending)
                throw new Exception("Only pending requests can be sent back.");

            var (actingEmployeeId, actingRoleName) = await GetActingContextAsync(request.ActionBy);

            if (!await IsAuthorizedForLevelAsync(regularization, regularization.CurrentLevel, actingEmployeeId, actingRoleName, request.ActionBy))
                throw new UnauthorizedAccessException(
                    $"You are not authorized to act on this request at the {GetLevelName(regularization.CurrentLevel)} level.");

            try
            {
                int sentBackFromLevel = regularization.CurrentLevel;

                regularization.Status = ApprovalStatus.ReturnedToEmployee;
                regularization.SendBackReason = request.SendBackReason;

                regularization.ModifiedOn = DateTime.UtcNow;
                regularization.ModifiedBy = request.ActionBy;

                await _context.SaveChangesAsync();

                await CreateApprovalHistoryAsync(
                    request.AttendanceRegularizationId,
                    request.ActionBy,
                    ApprovalStatus.ReturnedToEmployee,
                    $"Sent back by {GetLevelName(sentBackFromLevel)}: {request.SendBackReason}");

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // The employee edits and resubmits a request that was sent back to
        // them - this restarts the approval chain from the top (Level 1),
        // it does not resume from wherever it was sent back from.
        public async Task<AttendanceRegularizationDto> ResubmitAsync(string id, RequestRegularizationDto request, string resubmittedBy)
        {
            var regularization =
                await _context.AttendanceRegularizations
                    .Include(x => x.Employee)
                    .FirstOrDefaultAsync(x => x.Id == id);

            if (regularization == null)
                throw new Exception("Attendance regularization request not found.");

            if (regularization.Status != ApprovalStatus.ReturnedToEmployee)
                throw new Exception("Only a request that was sent back can be resubmitted.");

            if (regularization.EmployeeId != request.EmployeeId)
                throw new UnauthorizedAccessException("You can only resubmit your own regularization request.");

            if (request.RequestedFirstIn == null && request.RequestedLastOut == null)
                throw new InvalidOperationException(
                    "Please provide a requested First In and/or Last Out time.");

            try
            {
                regularization.Date = request.Date.Date;
                regularization.RequestedFirstIn = request.RequestedFirstIn;
                regularization.RequestedLastOut = request.RequestedLastOut;
                regularization.Reason = request.Reason;

                var existingAttendance = await _context.Attendances
                    .FirstOrDefaultAsync(x =>
                        x.EmployeeId == regularization.EmployeeId &&
                        x.Date.Date == regularization.Date.Date);

                regularization.AttendanceId = existingAttendance?.Id;
                regularization.OriginalFirstIn = existingAttendance?.FirstIn;
                regularization.OriginalLastOut = existingAttendance?.LastOut;

                regularization.Status = ApprovalStatus.Pending;
                regularization.CurrentLevel = await ResolveStartingLevelAsync(regularization.Employee);
                regularization.SendBackReason = null;

                regularization.ModifiedOn = DateTime.UtcNow;
                regularization.ModifiedBy = resubmittedBy;

                await _context.SaveChangesAsync();

                await CreateApprovalHistoryAsync(
                    regularization.Id,
                    resubmittedBy,
                    ApprovalStatus.Pending,
                    $"Resubmitted by employee - awaiting {GetLevelName(regularization.CurrentLevel)} approval");

                return await GetByIdAsync(regularization.Id);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool> CancelAsync(CancelRegularizationRequestDto request)
        {
            try
            {
                var regularization =
                    await _context.AttendanceRegularizations
                        .FirstOrDefaultAsync(x => x.Id == request.AttendanceRegularizationId);

                if (regularization == null)
                    throw new Exception("Attendance regularization request not found.");

                if (regularization.Status == ApprovalStatus.Cancelled)
                    throw new Exception("Request already cancelled.");

                regularization.Status = ApprovalStatus.Cancelled;

                regularization.ModifiedOn = DateTime.UtcNow;
                regularization.ModifiedBy = request.CancelledBy;

                await _context.SaveChangesAsync();

                await CreateApprovalHistoryAsync(
                    request.AttendanceRegularizationId,
                    request.CancelledBy,
                    ApprovalStatus.Cancelled,
                    "Regularization request cancelled");

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region Queries

        public async Task<List<AttendanceRegularizationDto>> GetEmployeeRequestsAsync(string employeeId)
        {
            var entities = await _context.AttendanceRegularizations
                .Include(x => x.Employee)
                .Where(x => x.EmployeeId == employeeId)
                .OrderByDescending(x => x.CreatedOn)
                .ToListAsync();

            var deptMap = await BuildDepartmentSeniorityMapAsync();

            return await AssembleDtoList(entities, deptMap);
        }

        public async Task<List<AttendanceRegularizationDto>> GetPendingAsync()
        {
            var entities = await _context.AttendanceRegularizations
                .Include(x => x.Employee)
                .Where(x => x.Status == ApprovalStatus.Pending)
                .OrderByDescending(x => x.CreatedOn)
                .ToListAsync();

            var deptMap = await BuildDepartmentSeniorityMapAsync();

            return await AssembleDtoList(entities, deptMap);
        }

        public async Task<List<AttendanceRegularizationDto>> GetPendingForApproverAsync(string? employeeId, string? userId)
        {
            bool isHr = await IsHrApproverAsync(userId);

            var pending = await _context.AttendanceRegularizations
                .Include(x => x.Employee)
                .Where(x => x.Status == ApprovalStatus.Pending)
                .OrderBy(x => x.CreatedOn)
                .ToListAsync();

            var deptMap = await BuildDepartmentSeniorityMapAsync();

            var mine = pending.Where(x =>
                x.CurrentLevel == 1
                    ? !string.IsNullOrEmpty(employeeId) && x.Employee?.ReportingManagerId == employeeId
                    : x.CurrentLevel == 2
                        ? !string.IsNullOrEmpty(employeeId) &&
                          GetDepartmentHeadIdFromMap(deptMap, x.Employee?.DepartmentId, x.EmployeeId) == employeeId
                        : isHr)
                .ToList();

            return await AssembleDtoList(mine, deptMap);
        }

        public async Task<List<AttendanceRegularizationDto>> GetApprovedAsync()
        {
            var entities = await _context.AttendanceRegularizations
                .Include(x => x.Employee)
                .Where(x => x.Status == ApprovalStatus.Approved)
                .OrderByDescending(x => x.ApprovedDate)
                .ToListAsync();

            var deptMap = await BuildDepartmentSeniorityMapAsync();

            return await AssembleDtoList(entities, deptMap);
        }

        public async Task<List<AttendanceRegularizationDto>> GetRejectedAsync()
        {
            var entities = await _context.AttendanceRegularizations
                .Include(x => x.Employee)
                .Where(x => x.Status == ApprovalStatus.Rejected)
                .OrderByDescending(x => x.ApprovedDate)
                .ToListAsync();

            var deptMap = await BuildDepartmentSeniorityMapAsync();

            return await AssembleDtoList(entities, deptMap);
        }

        public async Task<List<AttendanceRegularizationDto>> GetCancelledAsync()
        {
            var entities = await _context.AttendanceRegularizations
                .Include(x => x.Employee)
                .Where(x => x.Status == ApprovalStatus.Cancelled)
                .OrderByDescending(x => x.ModifiedOn)
                .ToListAsync();

            var deptMap = await BuildDepartmentSeniorityMapAsync();

            return await AssembleDtoList(entities, deptMap);
        }

        public async Task<List<AttendanceRegularizationDto>> GetFilteredAsync(AttendanceRegularizationFilterRequestDto request)
        {
            var query = _context.AttendanceRegularizations
                .Include(x => x.Employee)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.EmployeeId))
            {
                query = query.Where(x =>
                    x.EmployeeId == request.EmployeeId);
            }

            if (request.Status.HasValue)
            {
                query = query.Where(x =>
                    x.Status == request.Status.Value);
            }

            if (request.FromDate.HasValue)
            {
                query = query.Where(x =>
                    x.Date.Date >= request.FromDate.Value.Date);
            }

            if (request.ToDate.HasValue)
            {
                query = query.Where(x =>
                    x.Date.Date <= request.ToDate.Value.Date);
            }

            var entities = await query
                .OrderByDescending(x => x.CreatedOn)
                .ToListAsync();

            var deptMap = await BuildDepartmentSeniorityMapAsync();

            return await AssembleDtoList(entities, deptMap);
        }

        #endregion

        #region Approval History

        public async Task<List<AttendanceRegularizationApprovalHistoryDetailDto>> GetApprovalHistoryAsync(string attendanceRegularizationId)
        {
            return await _context.AttendanceRegularizationApprovalHistories
                .Include(x => x.AttendanceRegularization)
                    .ThenInclude(x => x.Employee)
                .Where(x => x.AttendanceRegularizationId == attendanceRegularizationId)
                .OrderBy(x => x.ActionDate)
                .Select(x => new AttendanceRegularizationApprovalHistoryDetailDto
                {
                    Id = x.Id,

                    AttendanceRegularizationId = x.AttendanceRegularizationId,

                    AttendanceRegularizationNo = x.AttendanceRegularization.Id,

                    EmployeeId = x.AttendanceRegularization.EmployeeId,

                    EmployeeName =
                        x.AttendanceRegularization.Employee.FirstName +
                        " " +
                        x.AttendanceRegularization.Employee.LastName,

                    Date = x.AttendanceRegularization.Date,

                    RequestedFirstIn = x.AttendanceRegularization.RequestedFirstIn,

                    RequestedLastOut = x.AttendanceRegularization.RequestedLastOut,

                    ActionBy = x.ActionBy,

                    Action = x.Action,

                    Remarks = x.Remarks,

                    ActionDate = x.ActionDate,

                    CreatedOn = x.CreatedOn,

                    CreatedBy = x.CreatedBy
                })
                .ToListAsync();
        }

        public async Task<List<AttendanceRegularizationApprovalHistoryDetailDto>> GetAllApprovalHistoryAsync()
        {
            try
            {
                var histories = await _context.AttendanceRegularizationApprovalHistories
                    .Include(x => x.AttendanceRegularization)
                        .ThenInclude(x => x.Employee)
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

                return histories.Select(x => new AttendanceRegularizationApprovalHistoryDetailDto
                {
                    Id = x.Id,

                    AttendanceRegularizationId = x.AttendanceRegularizationId,

                    AttendanceRegularizationNo = x.AttendanceRegularization.Id,

                    EmployeeId = x.AttendanceRegularization.EmployeeId,

                    EmployeeName =
                        x.AttendanceRegularization.Employee.FirstName + " " +
                        x.AttendanceRegularization.Employee.LastName,

                    Date = x.AttendanceRegularization.Date,

                    RequestedFirstIn = x.AttendanceRegularization.RequestedFirstIn,

                    RequestedLastOut = x.AttendanceRegularization.RequestedLastOut,

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
                return new List<AttendanceRegularizationApprovalHistoryDetailDto>();
            }
        }

        public async Task<AttendanceRegularizationApprovalHistoryDetailDto> GetApprovalHistoryByIdAsync(string id)
        {
            var data =
                await _context.AttendanceRegularizationApprovalHistories
                .Include(x => x.AttendanceRegularization)
                    .ThenInclude(x => x.Employee)
                .Where(x => x.Id == id)
                .Select(x => new AttendanceRegularizationApprovalHistoryDetailDto
                {
                    Id = x.Id,

                    AttendanceRegularizationId = x.AttendanceRegularizationId,

                    AttendanceRegularizationNo = x.AttendanceRegularization.Id,

                    EmployeeId = x.AttendanceRegularization.EmployeeId,

                    EmployeeName =
                        x.AttendanceRegularization.Employee.FirstName +
                        " " +
                        x.AttendanceRegularization.Employee.LastName,

                    Date = x.AttendanceRegularization.Date,

                    RequestedFirstIn = x.AttendanceRegularization.RequestedFirstIn,

                    RequestedLastOut = x.AttendanceRegularization.RequestedLastOut,

                    ActionBy = x.ActionBy,

                    Action = x.Action,

                    Remarks = x.Remarks,

                    ActionDate = x.ActionDate,

                    CreatedOn = x.CreatedOn,

                    CreatedBy = x.CreatedBy
                })
                .FirstOrDefaultAsync();

            if (data == null)
                throw new Exception("Approval history not found.");

            return data;
        }

        private async Task CreateApprovalHistoryAsync(string attendanceRegularizationId, string actionBy, ApprovalStatus action, string? remarks)
        {
            var history = new AttendanceRegularizationApprovalHistory
            {
                Id = IDManager.GetNewId(new AttendanceRegularizationApprovalHistory()),

                AttendanceRegularizationId = attendanceRegularizationId,

                ActionBy = actionBy,

                Action = action,

                Remarks = remarks,

                ActionDate = DateTime.UtcNow,

                CreatedOn = DateTime.UtcNow,
                CreatedBy = actionBy
            };

            _context.AttendanceRegularizationApprovalHistories.Add(history);

            await _context.SaveChangesAsync();
        }

        #endregion

        #region Attendance Write-Back

        // Final-level approval write-back: finds or creates the Attendance
        // row for (EmployeeId, Date), overwrites only whichever of
        // FirstIn/LastOut was actually requested (an existing correct value
        // that wasn't part of this request is left untouched), marks it as
        // a manual entry, inserts matching AttendanceLog row(s), and
        // recomputes TotalWorkingHours/OvertimeHours/IsLate/IsEarlyExit/
        // Status using exactly the same formulas AttendanceService uses for
        // a completed punch pair (see AttendanceService.PunchInAsync /
        // PunchOutAsync / CalculateWorkingHours / CalculateBreakHours) -
        // not an approximation of them.
        private async Task ApplyRegularizationToAttendanceAsync(AttendanceRegularization regularization, string approvedBy)
        {
            var employee = regularization.Employee ??
                await _context.Employees
                    .Include(x => x.DefaultShift)
                    .FirstOrDefaultAsync(x => x.Id == regularization.EmployeeId);

            if (employee == null)
                throw new Exception("Employee not found.");

            var date = regularization.Date.Date;

            var attendance = await _context.Attendances
                .Include(x => x.Logs)
                .Include(x => x.Shift)
                .FirstOrDefaultAsync(x =>
                    x.EmployeeId == regularization.EmployeeId &&
                    x.Date.Date == date);

            Shift? shift;

            if (attendance != null)
            {
                // An Attendance row already exists (e.g. only the Out punch
                // was missed) - reuse the shift it was originally created
                // against rather than re-resolving it, so night-shift date
                // boundaries stay consistent with how it was first recorded.
                shift = attendance.Shift ??
                    (!string.IsNullOrEmpty(attendance.ShiftId)
                        ? await _context.Shifts.FirstOrDefaultAsync(x => x.Id == attendance.ShiftId)
                        : null);
            }
            else
            {
                shift = await ResolveShiftForDateAsync(employee, date);

                if (shift == null)
                    throw new Exception(
                        "No shift is assigned to this employee - cannot create an attendance record for the regularized date.");

                attendance = new Attendance
                {
                    Id = IDManager.GetNewId(new Attendance()),

                    TenantId = employee.TenantId,
                    CompanyId = employee.CompanyId,
                    BranchId = employee.BranchId,

                    EmployeeId = employee.Id,

                    ShiftId = shift.Id,

                    Date = date,

                    Status = AttendanceStatus.Present,

                    CreatedBy = approvedBy,
                    CreatedOn = DateTime.UtcNow,

                    Logs = new List<AttendanceLog>()
                };

                _context.Attendances.Add(attendance);
            }

            // Only overwrite the punch(es) that were actually requested -
            // never null out an existing correct value that wasn't part of
            // this request.
            if (regularization.RequestedFirstIn.HasValue)
            {
                attendance.FirstIn = regularization.RequestedFirstIn;

                attendance.Logs.Add(new AttendanceLog
                {
                    Id = IDManager.GetNewId(new AttendanceLog()),

                    AttendanceId = attendance.Id,
                    EmployeeId = employee.Id,

                    PunchTime = regularization.RequestedFirstIn.Value,
                    PunchType = PunchType.In,

                    IsManual = true,

                    CreatedBy = approvedBy,
                    TenantId = employee.TenantId
                });
            }

            if (regularization.RequestedLastOut.HasValue)
            {
                attendance.LastOut = regularization.RequestedLastOut;

                attendance.Logs.Add(new AttendanceLog
                {
                    Id = IDManager.GetNewId(new AttendanceLog()),

                    AttendanceId = attendance.Id,
                    EmployeeId = employee.Id,

                    PunchTime = regularization.RequestedLastOut.Value,
                    PunchType = PunchType.Out,

                    IsManual = true,

                    CreatedBy = approvedBy,
                    TenantId = employee.TenantId
                });
            }

            attendance.IsManualEntry = true;
            attendance.ProcessedOn = DateTime.UtcNow;
            attendance.ProcessedBy = approvedBy;

            attendance.ModifiedOn = DateTime.UtcNow;
            attendance.ModifiedBy = approvedBy;

            RecomputeComputedFields(attendance, shift, date);

            regularization.AttendanceId = attendance.Id;

            await _context.SaveChangesAsync();
        }

        // Resolves the Shift that applies to the employee on the given date
        // - EmployeeShiftMapping (temporary/roster shift) takes priority
        // over the employee's DefaultShift, exactly like
        // AttendanceService.PunchInAsync/PunchOutAsync.
        private async Task<Shift?> ResolveShiftForDateAsync(Employee employee, DateTime date)
        {
            var shiftMapping = await _context.EmployeeShiftMappings
                .Include(x => x.Shift)
                .FirstOrDefaultAsync(x =>
                    x.EmployeeId == employee.Id &&
                    x.EffectiveFrom.Date <= date.Date &&
                    (x.EffectiveTo == null || x.EffectiveTo.Value.Date >= date.Date));

            if (shiftMapping != null)
                return shiftMapping.Shift;

            if (employee.DefaultShift != null)
                return employee.DefaultShift;

            if (!string.IsNullOrEmpty(employee.ShiftId))
                return await _context.Shifts.FirstOrDefaultAsync(x => x.Id == employee.ShiftId);

            return null;
        }

        // Recomputes TotalWorkingHours/BreakHours/OvertimeHours/IsLate/
        // IsEarlyExit/Status - mirrors AttendanceService's PunchInAsync/
        // PunchOutAsync + CalculateWorkingHours/CalculateBreakHours formulas
        // exactly, not an approximation of them.
        private void RecomputeComputedFields(Attendance attendance, Shift? shift, DateTime date)
        {
            // Working hours (gross - break) - same as
            // AttendanceService.CalculateWorkingHours.
            if (attendance.FirstIn == null || attendance.LastOut == null)
            {
                attendance.TotalWorkingHours = 0;
            }
            else
            {
                var grossHours = (decimal)(attendance.LastOut.Value - attendance.FirstIn.Value).TotalHours;
                var breakHours = CalculateBreakHours(attendance);

                attendance.TotalWorkingHours = Math.Round(grossHours - breakHours, 2);
            }

            if (shift == null)
                return;

            // Late check - same as AttendanceService.PunchInAsync.
            if (attendance.FirstIn.HasValue)
            {
                var shiftStartDateTime = date.Add(shift.StartTime);

                if (shift.IsNightShift && shift.EndTime < shift.StartTime &&
                    attendance.FirstIn.Value.TimeOfDay < shift.EndTime)
                {
                    shiftStartDateTime = shiftStartDateTime.AddDays(-1);
                }

                var allowedIn = shiftStartDateTime.AddMinutes(shift.GraceInMinutes);

                attendance.IsLate = attendance.FirstIn.Value > allowedIn;
            }

            // Early exit + overtime - same as AttendanceService.PunchOutAsync.
            if (attendance.LastOut.HasValue)
            {
                var shiftEndDateTime = date.Add(shift.EndTime);

                if (shift.IsNightShift && shift.EndTime < shift.StartTime)
                    shiftEndDateTime = shiftEndDateTime.AddDays(1);

                var allowedOut = shiftEndDateTime.AddMinutes(-shift.GraceOutMinutes);

                attendance.IsEarlyExit = attendance.LastOut.Value < allowedOut;

                if (attendance.FirstIn.HasValue)
                {
                    var totalMinutes = (decimal)(attendance.LastOut.Value - attendance.FirstIn.Value).TotalMinutes;

                    attendance.OvertimeHours = totalMinutes > shift.MinimumWorkingMinutes
                        ? Math.Round((totalMinutes - shift.MinimumWorkingMinutes) / 60, 2)
                        : 0;
                }
            }

            // Status - same as AttendanceService.GetAttendanceStatus.
            if (attendance.FirstIn.HasValue && attendance.LastOut.HasValue)
            {
                var workedMinutes = attendance.TotalWorkingHours * 60;

                attendance.Status = workedMinutes >= shift.FullDayMinutes
                    ? AttendanceStatus.Present
                    : workedMinutes >= shift.HalfDayMinutes
                        ? AttendanceStatus.HalfDay
                        : AttendanceStatus.Absent;
            }
            else if (attendance.FirstIn.HasValue)
            {
                // Only the In side was regularized - matches PunchInAsync,
                // which always marks Present as soon as there's a First In;
                // Late/Absent is only decided once there's an Out to
                // compute hours from.
                attendance.Status = AttendanceStatus.Present;
            }
        }

        // Same as AttendanceService.CalculateBreakHours - sums BreakOut ->
        // BreakIn pairs from the attendance's own logs.
        private decimal CalculateBreakHours(Attendance attendance)
        {
            decimal totalBreakHours = 0;

            var logs = attendance.Logs
                .OrderBy(x => x.PunchTime)
                .ToList();

            DateTime? breakOutTime = null;

            foreach (var log in logs)
            {
                if (log.PunchType == PunchType.BreakOut)
                {
                    breakOutTime = log.PunchTime;
                }

                if (log.PunchType == PunchType.BreakIn && breakOutTime != null)
                {
                    totalBreakHours += (decimal)(log.PunchTime - breakOutTime.Value).TotalHours;
                    breakOutTime = null;
                }
            }

            attendance.BreakHours = Math.Round(totalBreakHours, 2);

            return attendance.BreakHours;
        }

        #endregion
    }
}
