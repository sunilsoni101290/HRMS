using Application.DTOs.Attendances;
using Application.DTOs.Leaves;
using Application.Interfaces.Attendances;
using Application.Interfaces.Leaves;
using Domain.Entities;
using Domain.Helper;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Domain.Enums.EnumExtensions;

namespace Application.Services.Attendances
{
    // HR review side of the "System-detected, HR-approved" Comp Off flow.
    // CompOffCandidate rows are only ever created by
    // API/BackgroundServices/CompOffDetectionService.cs (no Create/POST
    // here, by design - see ICompOffService) - this service only reads them
    // and lets HR/Admin Approve (credits the balance) or Reject (no balance
    // change) each one. Authorization pattern (GetActingEmployeeIdAsync /
    // IsHrOrAdminForAttendanceAsync) copied rather than shared, matching how
    // every module in this codebase hand-rolls its own - see
    // WfhRequestService/ShortLeaveRequestService.
    public class CompOffService : ICompOffService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILeaveBalanceService _leaveBalanceService;

        public CompOffService(
            ApplicationDbContext context,
            ILeaveBalanceService leaveBalanceService)
        {
            _context = context;
            _leaveBalanceService = leaveBalanceService;
        }

        #region Queries

        public async Task<List<CompOffCandidateDto>> GetPendingReviewAsync(string tenantId, string? departmentId, string? search)
        {
            var query = _context.CompOffCandidates
                .Include(x => x.Employee).ThenInclude(e => e.Department)
                .Where(x => x.TenantId == tenantId && x.Status == CompOffCandidateStatus.PendingReview)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(departmentId))
                query = query.Where(x => x.Employee.DepartmentId == departmentId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(x =>
                    (x.Employee.FirstName + " " + x.Employee.LastName).Contains(term) ||
                    x.Employee.EmployeeCode.Contains(term));
            }

            var entities = await query.OrderBy(x => x.WorkedDate).ToListAsync();

            return await AssembleDtoListAsync(entities);
        }

        public async Task<List<CompOffCandidateDto>> GetMyCreditsAsync(string actingUserId, string tenantId)
        {
            var employeeId = await GetActingEmployeeIdAsync(actingUserId);

            if (string.IsNullOrEmpty(employeeId))
                return new List<CompOffCandidateDto>();

            var entities = await _context.CompOffCandidates
                .Include(x => x.Employee).ThenInclude(e => e.Department)
                .Where(x => x.EmployeeId == employeeId && x.TenantId == tenantId)
                .OrderByDescending(x => x.WorkedDate)
                .ToListAsync();

            return await AssembleDtoListAsync(entities);
        }

        private async Task<CompOffCandidate> GetEntityByIdAsync(string id, string tenantId)
        {
            var entity = await _context.CompOffCandidates
                .Include(x => x.Employee).ThenInclude(e => e.Department)
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);

            if (entity == null)
                throw new Exception("Comp Off candidate not found.");

            return entity;
        }

        // Public "view a single candidate" path (API GET /{id}). Only the
        // candidate's own employee, or HR/Admin, may view it - no
        // reporting-manager angle (Comp Off review is HR-only).
        public async Task<CompOffCandidateDto> GetByIdAsync(string id, string tenantId, string actingUserId)
        {
            var entity = await GetEntityByIdAsync(id, tenantId);

            var actingEmployeeId = await GetActingEmployeeIdAsync(actingUserId);

            bool isOwnRecord = !string.IsNullOrEmpty(actingEmployeeId) && entity.EmployeeId == actingEmployeeId;

            if (!isOwnRecord && !await IsHrOrAdminForAttendanceAsync(actingUserId))
                throw new UnauthorizedAccessException("You are not authorized to view this Comp Off candidate.");

            return await AssembleDtoAsync(entity);
        }

        #endregion

        #region Workflow

        public async Task<CompOffCandidateDto> ApproveAsync(string id, string actingUserId, string tenantId)
        {
            if (!await IsHrOrAdminForAttendanceAsync(actingUserId))
                throw new UnauthorizedAccessException("You are not authorized to approve Comp Off candidates.");

            var entity = await _context.CompOffCandidates
                .Include(x => x.Employee).ThenInclude(e => e.Department)
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);

            if (entity == null)
                throw new Exception("Comp Off candidate not found.");

            if (entity.Status != CompOffCandidateStatus.PendingReview)
                throw new Exception("Only candidates pending review can be approved.");

            var compOffLeaveType = await ResolveCompOffLeaveTypeAsync(tenantId);

            if (compOffLeaveType == null)
                throw new Exception("The 'Comp Off' leave type is not configured for this tenant.");

            bool credited = await _leaveBalanceService.CreditLeaveAsync(new LeaveAdjustmentRequestDto
            {
                EmployeeId = entity.EmployeeId,
                LeaveTypeId = compOffLeaveType.Id,
                Days = entity.CreditedDays
            });

            if (!credited)
                throw new Exception("Failed to credit Comp Off balance for this candidate.");

            entity.Status = CompOffCandidateStatus.Approved;
            entity.ReviewedBy = actingUserId;
            entity.ReviewedOn = DateTime.UtcNow;

            entity.ModifiedOn = DateTime.UtcNow;
            entity.ModifiedBy = actingUserId;

            await _context.SaveChangesAsync();

            return await AssembleDtoAsync(entity);
        }

        public async Task<CompOffCandidateDto> RejectAsync(string id, string reason, string actingUserId, string tenantId)
        {
            if (!await IsHrOrAdminForAttendanceAsync(actingUserId))
                throw new UnauthorizedAccessException("You are not authorized to reject Comp Off candidates.");

            if (string.IsNullOrWhiteSpace(reason))
                throw new Exception("Rejection reason is required.");

            var entity = await _context.CompOffCandidates
                .Include(x => x.Employee).ThenInclude(e => e.Department)
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);

            if (entity == null)
                throw new Exception("Comp Off candidate not found.");

            if (entity.Status != CompOffCandidateStatus.PendingReview)
                throw new Exception("Only candidates pending review can be rejected.");

            // No balance was ever touched at PendingReview - a Reject never
            // needs to credit or deduct anything.
            entity.Status = CompOffCandidateStatus.Rejected;
            entity.ReviewedBy = actingUserId;
            entity.ReviewedOn = DateTime.UtcNow;
            entity.RejectionReason = reason.Trim();

            entity.ModifiedOn = DateTime.UtcNow;
            entity.ModifiedBy = actingUserId;

            await _context.SaveChangesAsync();

            return await AssembleDtoAsync(entity);
        }

        #endregion

        #region Helpers

        // Looks up the tenant's seeded "Comp Off" LeaveType row (see
        // DbSeeder.SeedAsync - one row per tenant, TenantId = tenantId).
        // Falls back to a tenant-less/global row (TenantId == null) if
        // present, purely as a safety net for an unusual seeding setup.
        private async Task<LeaveType?> ResolveCompOffLeaveTypeAsync(string tenantId)
        {
            var leaveType = await _context.LeaveTypes
                .FirstOrDefaultAsync(x => x.Name == "Comp Off" && x.TenantId == tenantId && !x.IsDeleted);

            if (leaveType != null)
                return leaveType;

            return await _context.LeaveTypes
                .FirstOrDefaultAsync(x => x.Name == "Comp Off" && x.TenantId == null && !x.IsDeleted);
        }

        // Resolves the acting user's linked EmployeeId from their User.Id -
        // never trusted from client input - same pattern as
        // WfhRequestService.GetActingEmployeeIdAsync.
        private async Task<string?> GetActingEmployeeIdAsync(string? userId)
        {
            if (string.IsNullOrEmpty(userId))
                return null;

            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId);
            return user?.EmployeeId;
        }

        // Same permission-based pattern as
        // WfhRequestService.IsHrOrAdminForAttendanceAsync (copied rather
        // than shared - every module in this codebase hand-rolls its own) -
        // does the acting user hold, through any Role assigned to them, an
        // allowed RolePermission for the View action on the ATTENDANCE
        // feature.
        private async Task<bool> IsHrOrAdminForAttendanceAsync(string? actingUserId)
        {
            if (string.IsNullOrEmpty(actingUserId))
                return false;

            return await (
                from ur in _context.UserRoles
                join rp in _context.RolePermissions.Where(x => x.IsAllowed) on ur.RoleId equals rp.RoleId
                join p in _context.Permissions.Where(x =>
                        x.FeatureId == AppFeatureConstants.ATTENDANCE && x.Action == Actions.View)
                    on rp.PermissionId equals p.Id
                where ur.UserId == actingUserId
                select p.Id
            ).AnyAsync();
        }

        #endregion

        #region DTO Assembly

        private static CompOffCandidateDto AssembleDto(CompOffCandidate x)
        {
            return new CompOffCandidateDto
            {
                Id = x.Id,

                EmployeeId = x.EmployeeId,
                EmployeeName = x.Employee != null ? $"{x.Employee.FirstName} {x.Employee.LastName}".Trim() : null,
                EmployeeCode = x.Employee?.EmployeeCode,
                DepartmentName = x.Employee?.Department?.Name,

                AttendanceId = x.AttendanceId,

                WorkedDate = x.WorkedDate,
                HoursWorked = x.HoursWorked,

                TriggerReason = x.TriggerReason,

                Status = (int)x.Status,
                StatusName = x.Status.ToString(),

                ReviewedBy = x.ReviewedBy,
                ReviewedOn = x.ReviewedOn,

                RejectionReason = x.RejectionReason,

                CreditedDays = x.CreditedDays,

                TenantId = x.TenantId,
                CreatedOn = x.CreatedOn
            };
        }

        // ReviewedBy stores the acting User.Id (not EmployeeId) - resolve a
        // display name via the linked Employee if one exists, else fall
        // back to the account's Username. Mirrors
        // WfhRequestService.BuildApprovedByNameMapAsync.
        private async Task<Dictionary<string, string>> BuildReviewedByNameMapAsync(IEnumerable<CompOffCandidate> entities)
        {
            var userIds = entities
                .Where(x => !string.IsNullOrEmpty(x.ReviewedBy))
                .Select(x => x.ReviewedBy!)
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

        private async Task<CompOffCandidateDto> AssembleDtoAsync(CompOffCandidate x)
        {
            var dto = AssembleDto(x);

            if (!string.IsNullOrEmpty(x.ReviewedBy))
            {
                var nameMap = await BuildReviewedByNameMapAsync(new[] { x });
                dto.ReviewedByName = nameMap.TryGetValue(x.ReviewedBy, out var name) ? name : null;
            }

            return dto;
        }

        private async Task<List<CompOffCandidateDto>> AssembleDtoListAsync(List<CompOffCandidate> entities)
        {
            var nameMap = await BuildReviewedByNameMapAsync(entities);

            var list = new List<CompOffCandidateDto>();

            foreach (var x in entities)
            {
                var dto = AssembleDto(x);

                if (!string.IsNullOrEmpty(x.ReviewedBy) && nameMap.TryGetValue(x.ReviewedBy, out var name))
                    dto.ReviewedByName = name;

                list.Add(dto);
            }

            return list;
        }

        #endregion
    }
}
