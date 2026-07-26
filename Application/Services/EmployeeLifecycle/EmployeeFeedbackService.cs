using Application.DTOs.EmployeeLifecycle;
using Application.Interfaces.EmployeeLifecycle;
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

namespace Application.Services.EmployeeLifecycle
{
    // Employee Feedback - Phase 4 of the "Probation & Confirmation"
    // (Employee Lifecycle) module. Plain CRUD, NO maker-checker workflow
    // (unlike ProbationConfirmationService/PipService/
    // EmployeeTransferService) - Feedback is general ongoing performance
    // feedback usable any time for any employee, not tied to probation/
    // approval workflows.
    //
    // AUTHORIZATION SUMMARY:
    //   - Create: acting user must be the target Employee's current
    //     ReportingManagerId, OR hold Create permission on
    //     EMPLOYEE_FEEDBACK (HR/Admin).
    //   - Update/Delete: only the original author (GivenByUserId ==
    //     actingUserId) or someone holding Edit permission (HR/Admin).
    //   - View (single record): the subject Employee themself (only if
    //     IsVisibleToEmployee == true), the author, the subject's current
    //     Reporting Manager, or anyone holding View permission (HR/Admin).
    //   - View (list, GetForEmployeeAsync): same per-record visibility rule
    //     applied row-by-row, since a caller may be the author of some rows
    //     about an employee without being their current Reporting Manager
    //     (e.g. a past manager, before a transfer).
    public class EmployeeFeedbackService : IEmployeeFeedbackService
    {
        private readonly ApplicationDbContext _context;

        public EmployeeFeedbackService(ApplicationDbContext context)
        {
            _context = context;
        }

        #region CRUD

        public async Task<EmployeeFeedbackDto> CreateAsync(CreateUpdateEmployeeFeedbackDto dto, string tenantId, string actingUserId)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            var employee = await _context.Employees
                .FirstOrDefaultAsync(x => x.Id == dto.EmployeeId && x.TenantId == tenantId && !x.IsDeleted);

            if (employee == null)
                throw new Exception("Employee not found.");

            if (employee.RelievingDate != null)
                throw new Exception("This employee has already exited and cannot receive new feedback.");

            // Authorization - either the acting user's own linked Employee
            // IS the target employee's current Reporting Manager, or the
            // acting user holds Create permission on this feature (HR/
            // Admin). Not self-service - Employees don't give themselves
            // feedback.
            var actingEmployeeId = await GetActingEmployeeIdAsync(actingUserId);

            bool isReportingManager =
                !string.IsNullOrEmpty(employee.ReportingManagerId) &&
                !string.IsNullOrEmpty(actingEmployeeId) &&
                employee.ReportingManagerId == actingEmployeeId;

            if (!isReportingManager && !await HasPermissionAsync(actingUserId, Actions.Create))
                throw new UnauthorizedAccessException("You are not authorized to give feedback for this employee.");

            ValidateCategoryAndRating(dto.Category, dto.Rating);

            var entity = new EmployeeFeedback
            {
                Id = IDManager.GetNewId(new EmployeeFeedback()),

                EmployeeId = dto.EmployeeId,
                GivenByUserId = actingUserId,

                FeedbackDate = dto.FeedbackDate,
                Category = (FeedbackCategory)dto.Category,
                Rating = dto.Rating,

                Strengths = string.IsNullOrWhiteSpace(dto.Strengths) ? null : dto.Strengths.Trim(),
                AreasOfImprovement = string.IsNullOrWhiteSpace(dto.AreasOfImprovement) ? null : dto.AreasOfImprovement.Trim(),
                Comments = string.IsNullOrWhiteSpace(dto.Comments) ? null : dto.Comments.Trim(),

                IsVisibleToEmployee = dto.IsVisibleToEmployee,

                TenantId = tenantId,
                CreatedOn = DateTime.UtcNow,
                CreatedBy = actingUserId
            };

            _context.EmployeeFeedbacks.Add(entity);
            await _context.SaveChangesAsync();

            return await GetByIdInternalAsync(entity.Id, tenantId);
        }

        public async Task<EmployeeFeedbackDto> UpdateAsync(string id, CreateUpdateEmployeeFeedbackDto dto, string tenantId, string actingUserId)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            var entity = await _context.EmployeeFeedbacks
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted);

            if (entity == null)
                throw new Exception("Feedback record not found.");

            // Authorization - only the original author, or someone holding
            // Edit permission (HR/Admin). Deliberately NOT reporting-manager
            // based (unlike Create) - a manager who was replaced shouldn't
            // be able to edit someone else's feedback just because they
            // currently manage the subject employee; only the author (or
            // HR) can.
            bool isAuthor = entity.GivenByUserId == actingUserId;

            if (!isAuthor && !await HasPermissionAsync(actingUserId, Actions.Edit))
                throw new UnauthorizedAccessException("You are not authorized to update this feedback record.");

            ValidateCategoryAndRating(dto.Category, dto.Rating);

            // EmployeeId is intentionally NOT updated - feedback never moves
            // to a different employee via Update.
            entity.FeedbackDate = dto.FeedbackDate;
            entity.Category = (FeedbackCategory)dto.Category;
            entity.Rating = dto.Rating;

            entity.Strengths = string.IsNullOrWhiteSpace(dto.Strengths) ? null : dto.Strengths.Trim();
            entity.AreasOfImprovement = string.IsNullOrWhiteSpace(dto.AreasOfImprovement) ? null : dto.AreasOfImprovement.Trim();
            entity.Comments = string.IsNullOrWhiteSpace(dto.Comments) ? null : dto.Comments.Trim();

            entity.IsVisibleToEmployee = dto.IsVisibleToEmployee;

            entity.ModifiedOn = DateTime.UtcNow;
            entity.ModifiedBy = actingUserId;

            await _context.SaveChangesAsync();

            return await GetByIdInternalAsync(entity.Id, tenantId);
        }

        public async Task DeleteAsync(string id, string tenantId, string actingUserId)
        {
            var entity = await _context.EmployeeFeedbacks
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted);

            if (entity == null)
                throw new Exception("Feedback record not found.");

            // Same authorization as Update - author or Edit-permission
            // holder (HR/Admin).
            bool isAuthor = entity.GivenByUserId == actingUserId;

            if (!isAuthor && !await HasPermissionAsync(actingUserId, Actions.Edit))
                throw new UnauthorizedAccessException("You are not authorized to delete this feedback record.");

            // Soft delete - per BaseEntity.IsDeleted convention used
            // throughout this codebase.
            entity.IsDeleted = true;

            entity.ModifiedOn = DateTime.UtcNow;
            entity.ModifiedBy = actingUserId;

            await _context.SaveChangesAsync();
        }

        private async Task<EmployeeFeedback> GetEntityByIdAsync(string id, string tenantId)
        {
            var entity = await _context.EmployeeFeedbacks
                .Include(x => x.Employee).ThenInclude(e => e.Department)
                .Include(x => x.Employee).ThenInclude(e => e.Designation)
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted);

            if (entity == null)
                throw new Exception("Feedback record not found.");

            return entity;
        }

        // Internal, trusted re-fetch used immediately after this service
        // itself already performed and authorized a mutation (Create/
        // Update) - no additional authorization check here.
        private async Task<EmployeeFeedbackDto> GetByIdInternalAsync(string id, string tenantId)
        {
            var entity = await GetEntityByIdAsync(id, tenantId);
            return await AssembleDtoAsync(entity);
        }

        // Public "view a single record" path - see class header for the
        // exact authorization rule.
        public async Task<EmployeeFeedbackDto> GetByIdAsync(string id, string tenantId, string actingUserId)
        {
            var entity = await GetEntityByIdAsync(id, tenantId);

            var actingEmployeeId = await GetActingEmployeeIdAsync(actingUserId);

            bool isSubjectEmployee = !string.IsNullOrEmpty(actingEmployeeId) && entity.EmployeeId == actingEmployeeId;
            bool isAuthor = entity.GivenByUserId == actingUserId;

            bool isReportingManager =
                !string.IsNullOrEmpty(entity.Employee?.ReportingManagerId) &&
                !string.IsNullOrEmpty(actingEmployeeId) &&
                entity.Employee.ReportingManagerId == actingEmployeeId;

            // The subject Employee may only view it themself when
            // IsVisibleToEmployee is true - when false, this branch is
            // deliberately skipped so the subject falls through to the
            // other checks (author/manager/HR), exactly like anyone else.
            bool isVisibleAsSubject = isSubjectEmployee && entity.IsVisibleToEmployee;

            if (!isVisibleAsSubject && !isAuthor && !isReportingManager && !await HasPermissionAsync(actingUserId, Actions.View))
                throw new UnauthorizedAccessException("You are not authorized to view this feedback record.");

            return await AssembleDtoAsync(entity);
        }

        // Primary listing method - see class header / interface doc for the
        // per-record visibility filter. isReportingManager/hasViewPermission/
        // isSubjectEmployee are constant across the result set (all rows
        // share the requested EmployeeId); isAuthor is evaluated per row
        // since different rows may have been given by different people
        // (e.g. a past Reporting Manager who has since been replaced still
        // sees the feedback they personally authored).
        public async Task<List<EmployeeFeedbackDto>> GetForEmployeeAsync(string employeeId, string tenantId, string actingUserId)
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(x => x.Id == employeeId && x.TenantId == tenantId && !x.IsDeleted);

            if (employee == null)
                throw new Exception("Employee not found.");

            var actingEmployeeId = await GetActingEmployeeIdAsync(actingUserId);

            bool isSubjectEmployee = !string.IsNullOrEmpty(actingEmployeeId) && employee.Id == actingEmployeeId;

            bool isReportingManager =
                !string.IsNullOrEmpty(employee.ReportingManagerId) &&
                !string.IsNullOrEmpty(actingEmployeeId) &&
                employee.ReportingManagerId == actingEmployeeId;

            bool hasViewPermission = await HasPermissionAsync(actingUserId, Actions.View);

            var entities = await _context.EmployeeFeedbacks
                .Include(x => x.Employee).ThenInclude(e => e.Department)
                .Include(x => x.Employee).ThenInclude(e => e.Designation)
                .Where(x => x.EmployeeId == employeeId && x.TenantId == tenantId && !x.IsDeleted)
                .OrderByDescending(x => x.FeedbackDate)
                .ToListAsync();

            // Full access (manager/HR) sees every row regardless of
            // authorship or IsVisibleToEmployee. Otherwise each row is
            // visible only if the caller is its author, or (when the
            // caller is the subject) the row is flagged IsVisibleToEmployee.
            var visible = (isReportingManager || hasViewPermission)
                ? entities
                : entities
                    .Where(x =>
                        x.GivenByUserId == actingUserId ||
                        (isSubjectEmployee && x.IsVisibleToEmployee))
                    .ToList();

            return await AssembleDtoListAsync(visible);
        }

        // Convenience "feedback I've given" listing - inherently scoped to
        // the caller's own submissions, no extra authorization needed.
        public async Task<List<EmployeeFeedbackDto>> GetGivenByMeAsync(string tenantId, string actingUserId)
        {
            var entities = await _context.EmployeeFeedbacks
                .Include(x => x.Employee).ThenInclude(e => e.Department)
                .Include(x => x.Employee).ThenInclude(e => e.Designation)
                .Where(x => x.GivenByUserId == actingUserId && x.TenantId == tenantId && !x.IsDeleted)
                .OrderByDescending(x => x.FeedbackDate)
                .ToListAsync();

            return await AssembleDtoListAsync(entities);
        }

        #endregion

        #region Validation

        private static void ValidateCategoryAndRating(int category, int? rating)
        {
            if (!Enum.IsDefined(typeof(FeedbackCategory), category))
                throw new Exception("Invalid feedback category.");

            if (rating.HasValue && (rating.Value < 1 || rating.Value > 5))
                throw new Exception("Rating must be between 1 and 5.");
        }

        #endregion

        #region Authorization Helpers

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

        // Does the acting user hold, through any Role assigned to them, an
        // allowed RolePermission for the given action on the
        // EMPLOYEE_FEEDBACK feature - same join shape as
        // ProbationConfirmationService.HasPermissionAsync, scoped to this
        // feature's own FeatureId.
        private async Task<bool> HasPermissionAsync(string? actingUserId, string action)
        {
            if (string.IsNullOrEmpty(actingUserId))
                return false;

            return await (
                from ur in _context.UserRoles
                join rp in _context.RolePermissions.Where(x => x.IsAllowed) on ur.RoleId equals rp.RoleId
                join p in _context.Permissions.Where(x =>
                        x.FeatureId == AppFeatureConstants.EMPLOYEE_FEEDBACK && x.Action == action)
                    on rp.PermissionId equals p.Id
                where ur.UserId == actingUserId
                select p.Id
            ).AnyAsync();
        }

        #endregion

        #region DTO Assembly

        private static EmployeeFeedbackDto AssembleDto(EmployeeFeedback x)
        {
            return new EmployeeFeedbackDto
            {
                Id = x.Id,

                EmployeeId = x.EmployeeId,
                EmployeeName = x.Employee != null ? $"{x.Employee.FirstName} {x.Employee.LastName}".Trim() : null,
                EmployeeCode = x.Employee?.EmployeeCode,

                GivenByUserId = x.GivenByUserId,

                FeedbackDate = x.FeedbackDate,

                Category = (int)x.Category,
                CategoryName = x.Category.ToString(),

                Rating = x.Rating,

                Strengths = x.Strengths,
                AreasOfImprovement = x.AreasOfImprovement,
                Comments = x.Comments,

                IsVisibleToEmployee = x.IsVisibleToEmployee,

                TenantId = x.TenantId,
                CreatedOn = x.CreatedOn,
                CreatedBy = x.CreatedBy,
                ModifiedOn = x.ModifiedOn,
                ModifiedBy = x.ModifiedBy
            };
        }

        // GivenByUserId stores the acting User.Id - resolve a display name
        // via the linked Employee if one exists, else fall back to the
        // account's Username. Mirrors
        // ProbationConfirmationService.BuildUserNameMapAsync.
        private async Task<Dictionary<string, string>> BuildUserNameMapAsync(IEnumerable<string?> userIds)
        {
            var ids = userIds
                .Where(x => !string.IsNullOrEmpty(x))
                .Select(x => x!)
                .Distinct()
                .ToList();

            if (ids.Count == 0)
                return new Dictionary<string, string>();

            var users = await _context.Users
                .Include(u => u.Employee)
                .Where(u => ids.Contains(u.Id))
                .ToListAsync();

            return users.ToDictionary(
                u => u.Id,
                u => u.Employee != null ? $"{u.Employee.FirstName} {u.Employee.LastName}".Trim() : u.Username);
        }

        private async Task<EmployeeFeedbackDto> AssembleDtoAsync(EmployeeFeedback x)
        {
            var dto = AssembleDto(x);

            var nameMap = await BuildUserNameMapAsync(new[] { x.GivenByUserId });
            dto.GivenByName = nameMap.TryGetValue(x.GivenByUserId, out var name) ? name : null;

            return dto;
        }

        private async Task<List<EmployeeFeedbackDto>> AssembleDtoListAsync(List<EmployeeFeedback> entities)
        {
            var nameMap = await BuildUserNameMapAsync(entities.Select(x => x.GivenByUserId));

            var list = new List<EmployeeFeedbackDto>();

            foreach (var x in entities)
            {
                var dto = AssembleDto(x);
                dto.GivenByName = nameMap.TryGetValue(x.GivenByUserId, out var name) ? name : null;
                list.Add(dto);
            }

            return list;
        }

        #endregion
    }
}
