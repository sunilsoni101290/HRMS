using Application.DTOs.Leaves;
using Application.Interfaces.Leaves;
using Application.Interfaces.Masters;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure;
using Infrastructure.Data;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
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
        private string tenantId = string.Empty;
        public LeaveApplicationService(ApplicationDbContext context, IWeekOffService weekOffService, 
            ITenantService tenantService, ILeaveBalanceService leaveBalanceService)
        {
            _context = context;
            _tenantService = tenantService;
            _weekOffService = weekOffService;
            _leaveBalanceService = leaveBalanceService;
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

        private async Task<bool> IsAuthorizedForLevelAsync(
            LeaveApplication leave,
            int level,
            string? actingEmployeeId,
            string? actingRoleName)
        {
            switch (level)
            {
                case 1:
                    var l1 = leave.Employee?.ReportingManagerId;
                    return !string.IsNullOrEmpty(l1) &&
                           !string.IsNullOrEmpty(actingEmployeeId) &&
                           l1 == actingEmployeeId;

                case 2:
                    var l2 = await GetDepartmentHeadIdAsync(leave.Employee?.DepartmentId, leave.EmployeeId);
                    return !string.IsNullOrEmpty(l2) &&
                           !string.IsNullOrEmpty(actingEmployeeId) &&
                           l2 == actingEmployeeId;

                case 3:
                    return !string.IsNullOrEmpty(actingRoleName) &&
                           actingRoleName.Contains("HR", StringComparison.OrdinalIgnoreCase);

                default:
                    return false;
            }
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

                DocumentUrl = request.DocumentUrl,

                CreatedOn = DateTime.UtcNow,
                CreatedBy = request.CreatedBy
            };

            _context.LeaveApplications.Add(entity);

            await _context.SaveChangesAsync();

            await CreateApprovalHistoryAsync(entity.Id, request.CreatedBy, ApprovalStatus.Pending, "Leave Applied");

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

            var (actingEmployeeId, actingRoleName) = await GetActingContextAsync(request.ApprovedBy);

            if (!await IsAuthorizedForLevelAsync(leave, leave.CurrentLevel, actingEmployeeId, actingRoleName))
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
            }
            else
            {
                int approvedAtLevel = leave.CurrentLevel;

                leave.CurrentLevel = await ResolveNextLevelAsync(leave.CurrentLevel, leave.Employee);

                leave.ModifiedOn = DateTime.UtcNow;
                leave.ModifiedBy = request.ApprovedBy;

                await _context.SaveChangesAsync();

                await CreateApprovalHistoryAsync(
                    request.LeaveApplicationId,
                    request.ApprovedBy,
                    ApprovalStatus.Approved,
                    request.Remarks ?? $"Approved by {GetLevelName(approvedAtLevel)} - forwarded to {GetLevelName(leave.CurrentLevel)}.");
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

            var (actingEmployeeId, actingRoleName) = await GetActingContextAsync(request.RejectedBy);

            if (!await IsAuthorizedForLevelAsync(leave, leave.CurrentLevel, actingEmployeeId, actingRoleName))
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

            var (actingEmployeeId, actingRoleName) = await GetActingContextAsync(request.SentBackBy);

            if (!await IsAuthorizedForLevelAsync(leave, leave.CurrentLevel, actingEmployeeId, actingRoleName))
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

            leave.ModifiedOn = DateTime.UtcNow;
            leave.ModifiedBy = resubmittedBy;

            await _context.SaveChangesAsync();

            await CreateApprovalHistoryAsync(
                leave.Id,
                resubmittedBy,
                ApprovalStatus.Pending,
                $"Resubmitted by employee - awaiting {GetLevelName(leave.CurrentLevel)} approval");

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

            leave.Status = ApprovalStatus.Cancelled;

            leave.ModifiedOn = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await CreateApprovalHistoryAsync(request.LeaveApplicationId, request.CancelledBy, ApprovalStatus.Cancelled, "Leave Cancelled");

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

        public async Task<List<LeaveApplicationDto>> GetPendingForApproverAsync(string? employeeId, string? roleName)
        {
            bool isHr = !string.IsNullOrEmpty(roleName) && roleName.Contains("HR", StringComparison.OrdinalIgnoreCase);

            var pending = await _context.LeaveApplications
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
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

            return AssembleDtoList(mine, deptMap);
        }

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

    }

    /*
    public class LeaveApplicationService : ILeaveApplicationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWeekOffService _weekOffService;
        private readonly ITenantService _tenantService;
        private readonly ILeaveBalanceService _leaveBalanceService;
        private string tenantId = string.Empty;
        public LeaveApplicationService(ApplicationDbContext context, IWeekOffService weekOffService, ITenantService tenantService, ILeaveBalanceService leaveBalanceService)
        {
            _context = context;
            _tenantService = tenantService;
            _weekOffService = weekOffService;
            _leaveBalanceService = leaveBalanceService;
        }

        // =====================================================
        // GET ALL
        // =====================================================

        public async Task<List<LeaveApplicationDto>> GetAllAsync()
        {
            try
            {
            return await _context.LeaveApplications
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .OrderByDescending(x => x.CreatedOn)
                .Select(x => new LeaveApplicationDto
                {
                    Id = x.Id,

                    CompanyId = x.CompanyId,
                    BranchId = x.BranchId,

                    EmployeeId = x.EmployeeId,
                    EmployeeName = x.Employee.FirstName + " " + x.Employee.LastName,

                    LeaveTypeId = x.LeaveTypeId,
                    LeaveTypeName = x.LeaveType.Name,

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

                    DocumentUrl = x.DocumentUrl,

                    CreatedBy = x.CreatedBy,
                    ModifiedOn = x.ModifiedOn,
                    ModifiedBy = x.ModifiedBy
                })
                .ToListAsync();
            }
            catch (Exception)
            {
                return new List<LeaveApplicationDto>();
            }
        }

        // =====================================================
        // GET BY ID
        // =====================================================

        public async Task<LeaveApplicationDto?> GetByIdAsync(string id)
        {
            try
            {
            return await _context.LeaveApplications
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .Where(x => x.Id == id)
                .Select(x => new LeaveApplicationDto
                {
                    Id = x.Id,

                    CompanyId = x.CompanyId,
                    BranchId = x.BranchId,

                    EmployeeId = x.EmployeeId,
                    EmployeeName = x.Employee.FirstName + " " + x.Employee.LastName,

                    LeaveTypeId = x.LeaveTypeId,
                    LeaveTypeName = x.LeaveType.Name,

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

                    DocumentUrl = x.DocumentUrl
                })
                .FirstOrDefaultAsync();
            }
            catch (Exception)
            {
                return null;
            }
        }

        // =====================================================
        // GET BY EMPLOYEE
        // =====================================================

        public async Task<List<LeaveApplicationDto>> GetByEmployeeAsync(string employeeId)
        {
            try
            {
            return await _context.LeaveApplications
                .Include(x => x.LeaveType)
                .Where(x => x.EmployeeId == employeeId)
                .OrderByDescending(x => x.FromDate)
                .Select(x => new LeaveApplicationDto
                {
                    Id = x.Id,

                    EmployeeId = x.EmployeeId,

                    LeaveTypeId = x.LeaveTypeId,
                    LeaveTypeName = x.LeaveType.Name,

                    FromDate = x.FromDate,
                    ToDate = x.ToDate,
                    TotalDays = x.TotalDays,

                    Status = x.Status,

                    Reason = x.Reason,

                    ApprovedBy = x.ApprovedBy,
                    ApprovedDate = x.ApprovedDate,

                    RejectedReason = x.RejectedReason
                })
                .ToListAsync();
            }
            catch (Exception)
            {
                return new List<LeaveApplicationDto>();
            }
        }


        public async Task<LeaveApplicationDto> ApplyLeaveAsync(
        LeaveApplicationDto dto)
        {
            try
            {
            var leaveBalance =
                await _leaveBalanceService
                    .GetEmployeeLeaveBalanceAsync(
                        dto.EmployeeId,
                        dto.LeaveTypeId,
                        dto.FromDate.Year);

            if (leaveBalance == null)
                throw new Exception("Leave balance not found.");

            if (leaveBalance.Balance < dto.TotalDays)
                throw new Exception("Insufficient leave balance.");

            var entity = new LeaveApplication
            {
                CompanyId = dto.CompanyId,
                BranchId = dto.BranchId,

                EmployeeId = dto.EmployeeId,
                LeaveTypeId = dto.LeaveTypeId,

                FromDate = dto.FromDate,
                ToDate = dto.ToDate,

                TotalDays = dto.TotalDays,

                IsHalfDay = dto.IsHalfDay,
                HalfDayType = dto.HalfDayType,

                Reason = dto.Reason,

                Status = ApprovalStatus.Pending,

                DocumentUrl = dto.DocumentUrl,

                CreatedBy = dto.CreatedBy,
                CreatedOn = DateTime.UtcNow
            };

            _context.LeaveApplications.Add(entity);

            await _context.SaveChangesAsync();

            dto.Id = entity.Id;

            return dto;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool> ApproveLeaveAsync(
        string leaveApplicationId,
        string approvedBy)
        {
            try
            {
            var leave =
                await _context.LeaveApplications
                    .FirstOrDefaultAsync(x => x.Id == leaveApplicationId);

            if (leave == null)
                return false;

            if (leave.Status != ApprovalStatus.Pending)
                throw new Exception("Leave already processed.");

            await _leaveBalanceService.DeductLeaveAsync(
                leave.EmployeeId,
                leave.LeaveTypeId,
                leave.TotalDays);

            leave.Status = ApprovalStatus.Approved;
            leave.ApprovedBy = approvedBy;
            leave.ApprovedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> RejectLeaveAsync(
        string leaveApplicationId,
        string approvedBy,
        string rejectionReason)
        {
            try
            {
            var leave =
                await _context.LeaveApplications
                    .FirstOrDefaultAsync(x => x.Id == leaveApplicationId);

            if (leave == null)
                return false;

            leave.Status = ApprovalStatus.Rejected;
            leave.ApprovedBy = approvedBy;
            leave.ApprovedDate = DateTime.UtcNow;
            leave.RejectedReason = rejectionReason;

            await _context.SaveChangesAsync();

            return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> CancelLeaveAsync(
        string leaveApplicationId)
        {
            try
            {
            var leave =
                await _context.LeaveApplications
                    .FirstOrDefaultAsync(x => x.Id == leaveApplicationId);

            if (leave == null)
                return false;

            if (leave.Status == ApprovalStatus.Approved)
            {
                await _leaveBalanceService.CreditLeaveAsync(
                    leave.EmployeeId,
                    leave.LeaveTypeId,
                    leave.TotalDays);
            }

            leave.Status = ApprovalStatus.Cancelled;

            await _context.SaveChangesAsync();

            return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // =====================================================
        // DELETE
        // =====================================================
        public async Task<bool> DeleteAsync(string id)
        {
            try
            {
            var entity = await _context.LeaveApplications
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return false;

            if (entity.Status == ApprovalStatus.Approved)
            {
                await _leaveBalanceService.CreditLeaveAsync(
                    entity.EmployeeId,
                    entity.LeaveTypeId,
                    entity.TotalDays);
            }

            _context.LeaveApplications.Remove(entity);

            await _context.SaveChangesAsync();

            return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // =====================================================
        // PENDING APPROVALS
        // =====================================================

        public async Task<List<LeaveApplicationDto>> GetPendingApprovalsAsync()
        {
            try
            {
            return await _context.LeaveApplications
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .Where(x => x.Status == ApprovalStatus.Pending)
                .OrderByDescending(x => x.CreatedOn)
                .Select(x => new LeaveApplicationDto
                {
                    Id = x.Id,

                    EmployeeId = x.EmployeeId,
                    EmployeeName = x.Employee.FirstName + " " + x.Employee.LastName,

                    LeaveTypeId = x.LeaveTypeId,
                    LeaveTypeName = x.LeaveType.Name,

                    FromDate = x.FromDate,
                    ToDate = x.ToDate,

                    TotalDays = x.TotalDays,

                    Reason = x.Reason,

                    Status = x.Status,

                    CreatedBy = x.CreatedBy
                })
                .ToListAsync();
            }
            catch (Exception)
            {
                return new List<LeaveApplicationDto>();
            }
        }

        // =====================================================
        // APPROVED LEAVES
        // =====================================================

        public async Task<List<LeaveApplicationDto>> GetApprovedLeavesAsync()
        {
            try
            {
            return await _context.LeaveApplications
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .Where(x => x.Status == ApprovalStatus.Approved)
                .OrderByDescending(x => x.ApprovedDate)
                .Select(x => new LeaveApplicationDto
                {
                    Id = x.Id,

                    EmployeeId = x.EmployeeId,
                    EmployeeName = x.Employee.FirstName + " " + x.Employee.LastName,

                    LeaveTypeId = x.LeaveTypeId,
                    LeaveTypeName = x.LeaveType.Name,

                    FromDate = x.FromDate,
                    ToDate = x.ToDate,

                    TotalDays = x.TotalDays,

                    Status = x.Status,

                    ApprovedBy = x.ApprovedBy,
                    ApprovedDate = x.ApprovedDate
                })
                .ToListAsync();
            }
            catch (Exception)
            {
                return new List<LeaveApplicationDto>();
            }
        }

        // =====================================================
        // REJECTED LEAVES
        // =====================================================

        public async Task<List<LeaveApplicationDto>> GetRejectedLeavesAsync()
        {
            try
            {
            return await _context.LeaveApplications
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .Where(x => x.Status == ApprovalStatus.Rejected)
                .OrderByDescending(x => x.ApprovedDate)
                .Select(x => new LeaveApplicationDto
                {
                    Id = x.Id,

                    EmployeeId = x.EmployeeId,
                    EmployeeName = x.Employee.FirstName + " " + x.Employee.LastName,

                    LeaveTypeId = x.LeaveTypeId,
                    LeaveTypeName = x.LeaveType.Name,

                    FromDate = x.FromDate,
                    ToDate = x.ToDate,

                    TotalDays = x.TotalDays,

                    Status = x.Status,

                    RejectedReason = x.RejectedReason,

                    ApprovedBy = x.ApprovedBy,
                    ApprovedDate = x.ApprovedDate
                })
                .ToListAsync();
            }
            catch (Exception)
            {
                return new List<LeaveApplicationDto>();
            }
        }

        // =====================================================
        // DATE RANGE
        // =====================================================

        public async Task<List<LeaveApplicationDto>> GetByDateRangeAsync(
            DateTime fromDate,
            DateTime toDate)
        {
            try
            {
            return await _context.LeaveApplications
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .Where(x =>
                    x.FromDate.Date >= fromDate.Date &&
                    x.ToDate.Date <= toDate.Date)
                .OrderBy(x => x.FromDate)
                .Select(x => new LeaveApplicationDto
                {
                    Id = x.Id,

                    EmployeeId = x.EmployeeId,
                    EmployeeName = x.Employee.FirstName + " " + x.Employee.LastName,

                    LeaveTypeId = x.LeaveTypeId,
                    LeaveTypeName = x.LeaveType.Name,

                    FromDate = x.FromDate,
                    ToDate = x.ToDate,

                    TotalDays = x.TotalDays,

                    Status = x.Status,

                    Reason = x.Reason
                })
                .ToListAsync();
            }
            catch (Exception)
            {
                return new List<LeaveApplicationDto>();
            }
        }

        // =====================================================
        // PENDING COUNT
        // =====================================================

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

        // =====================================================
        // APPROVED COUNT
        // =====================================================

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

        // =====================================================
        // TODAY LEAVE COUNT
        // =====================================================

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
    }
    */
}
