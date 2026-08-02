using Application.DTOs.Taxation;
using Application.Interfaces.Taxation;
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

namespace Application.Services.Taxation
{
    // Employee self-service annual investment declaration - see
    // Domain/Entities/TaxDeclaration.cs / ITaxDeclarationService. NOT a
    // Maker-Checker (segregation-of-duties) feature like
    // ProbationConfirmationService - there is no "the checker must differ
    // from the maker" invariant here, because the "maker" is the employee
    // declaring facts about themselves, and the "checker" (HR/Payroll) is
    // a distinct role by construction, not merely a distinct person. The
    // authorization shape instead mirrors WfhRequestService: an employee
    // acts on their OWN record (resolved server-side from actingUserId,
    // never trusted from client input), or HR/Admin (Create/Approve
    // permission on TAX_DECLARATION) acts on anyone's.
    public class TaxDeclarationService : ITaxDeclarationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITaxComputationService _taxComputationService;

        public TaxDeclarationService(ApplicationDbContext context, ITaxComputationService taxComputationService)
        {
            _context = context;
            _taxComputationService = taxComputationService;
        }

        #region Employee Self-Service

        public async Task<TaxDeclarationDto?> GetMyDeclarationAsync(string financialYearId, string tenantId, string actingUserId)
        {
            var employeeId = await GetActingEmployeeIdAsync(actingUserId);

            if (string.IsNullOrEmpty(employeeId))
                return null;

            var entity = await _context.TaxDeclarations
                .Include(x => x.Employee).ThenInclude(e => e.Department)
                .Include(x => x.FinancialYear)
                .FirstOrDefaultAsync(x =>
                    x.EmployeeId == employeeId &&
                    x.FinancialYearId == financialYearId &&
                    x.TenantId == tenantId &&
                    !x.IsDeleted);

            return entity == null ? null : await AssembleDtoAsync(entity);
        }

        public async Task<TaxDeclarationDto> CreateOrUpdateAsync(CreateTaxDeclarationDto dto, string tenantId, string actingUserId)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            // Only for yourself, unless HR/Admin acting on someone's
            // behalf - same reasoning as WfhRequestService.CreateAsync.
            if (!await IsHrOrAdminAsync(actingUserId))
            {
                var actingEmployeeId = await GetActingEmployeeIdAsync(actingUserId);

                if (string.IsNullOrEmpty(actingEmployeeId) || actingEmployeeId != dto.EmployeeId)
                    throw new UnauthorizedAccessException("You can only submit a Tax Declaration for yourself.");
            }
            else if (!await HasPermissionAsync(actingUserId, Actions.Create))
            {
                throw new UnauthorizedAccessException("You are not authorized to create Tax Declarations.");
            }

            var employee = await _context.Employees
                .FirstOrDefaultAsync(x => x.Id == dto.EmployeeId && x.TenantId == tenantId && !x.IsDeleted);

            if (employee == null)
                throw new Exception("Employee not found.");

            var financialYear = await _context.FinancialYears
                .FirstOrDefaultAsync(x => x.Id == dto.FinancialYearId && x.TenantId == tenantId && !x.IsDeleted);

            if (financialYear == null)
                throw new Exception("Financial Year not found.");

            if (!Enum.IsDefined(typeof(TaxRegime), dto.Regime))
                throw new Exception("Invalid Regime.");

            var entity = await _context.TaxDeclarations
                .FirstOrDefaultAsync(x =>
                    x.EmployeeId == dto.EmployeeId &&
                    x.FinancialYearId == dto.FinancialYearId &&
                    x.TenantId == tenantId &&
                    !x.IsDeleted);

            // A Verified declaration is locked - the employee must not be
            // able to silently change numbers HR already signed off on.
            // Rejected declarations reopen for editing (reset to Draft) -
            // Submitted declarations are also locked until HR acts.
            if (entity != null && entity.Status != TaxDeclarationStatus.Draft && entity.Status != TaxDeclarationStatus.Rejected)
                throw new Exception($"This declaration is {entity.Status} and can no longer be edited.");

            if (entity == null)
            {
                entity = new TaxDeclaration
                {
                    Id = IDManager.GetNewId(new TaxDeclaration()),
                    EmployeeId = dto.EmployeeId,
                    FinancialYearId = dto.FinancialYearId,
                    TenantId = tenantId,
                    CreatedOn = DateTime.UtcNow,
                    CreatedBy = actingUserId
                };

                _context.TaxDeclarations.Add(entity);
            }
            else
            {
                entity.ModifiedOn = DateTime.UtcNow;
                entity.ModifiedBy = actingUserId;
            }

            entity.Regime = (TaxRegime)dto.Regime;
            entity.Section80C = dto.Section80C;
            entity.Section80CCD1B = dto.Section80CCD1B;
            entity.Section80D = dto.Section80D;
            entity.Section24B = dto.Section24B;
            entity.OtherDeductions = dto.OtherDeductions;
            entity.AnnualRentPaid = dto.AnnualRentPaid;
            entity.IsMetroCity = dto.IsMetroCity;
            entity.LandlordPAN = string.IsNullOrWhiteSpace(dto.LandlordPAN) ? null : dto.LandlordPAN.Trim();

            // Editing after a Reject resets to Draft, ready for
            // resubmission - clear the prior verifier decision.
            entity.Status = TaxDeclarationStatus.Draft;
            entity.SubmittedOn = null;
            entity.VerifiedBy = null;
            entity.VerifiedOn = null;
            entity.VerifierRemarks = null;

            await _context.SaveChangesAsync();

            return await GetByIdInternalAsync(entity.Id, tenantId);
        }

        public async Task<TaxDeclarationDto> SubmitAsync(string id, string tenantId, string actingUserId)
        {
            var entity = await _context.TaxDeclarations
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted);

            if (entity == null)
                throw new Exception("Tax Declaration not found.");

            if (!await IsHrOrAdminAsync(actingUserId))
            {
                var actingEmployeeId = await GetActingEmployeeIdAsync(actingUserId);

                if (string.IsNullOrEmpty(actingEmployeeId) || actingEmployeeId != entity.EmployeeId)
                    throw new UnauthorizedAccessException("You can only submit your own Tax Declaration.");
            }

            if (entity.Status != TaxDeclarationStatus.Draft)
                throw new Exception("Only a Draft declaration can be submitted.");

            entity.Status = TaxDeclarationStatus.Submitted;
            entity.SubmittedOn = DateTime.UtcNow;

            entity.ModifiedOn = DateTime.UtcNow;
            entity.ModifiedBy = actingUserId;

            await _context.SaveChangesAsync();

            return await GetByIdInternalAsync(entity.Id, tenantId);
        }

        #endregion

        #region HR View / Workflow

        public async Task<TaxDeclarationDto> GetByIdAsync(string id, string tenantId, string actingUserId)
        {
            var entity = await GetEntityByIdAsync(id, tenantId);

            var actingEmployeeId = await GetActingEmployeeIdAsync(actingUserId);
            bool isOwn = !string.IsNullOrEmpty(actingEmployeeId) && entity.EmployeeId == actingEmployeeId;

            if (!isOwn && !await HasPermissionAsync(actingUserId, Actions.View))
                throw new UnauthorizedAccessException("You are not authorized to view this Tax Declaration.");

            return await AssembleDtoAsync(entity);
        }

        public async Task<List<TaxDeclarationDto>> GetAllAsync(string tenantId, string? financialYearId, string? status, string? departmentId, string? search, string actingUserId)
        {
            if (!await HasPermissionAsync(actingUserId, Actions.View))
                throw new UnauthorizedAccessException("You are not authorized to view Tax Declarations.");

            var query = _context.TaxDeclarations
                .Include(x => x.Employee).ThenInclude(e => e.Department)
                .Include(x => x.FinancialYear)
                .Where(x => x.TenantId == tenantId && !x.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(financialYearId))
                query = query.Where(x => x.FinancialYearId == financialYearId);

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<TaxDeclarationStatus>(status, true, out var statusEnum))
                query = query.Where(x => x.Status == statusEnum);

            if (!string.IsNullOrWhiteSpace(departmentId))
                query = query.Where(x => x.Employee.DepartmentId == departmentId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(x =>
                    (x.Employee.FirstName + " " + x.Employee.LastName).Contains(term) ||
                    x.Employee.EmployeeCode.Contains(term));
            }

            var entities = await query.OrderByDescending(x => x.CreatedOn).ToListAsync();

            return await AssembleDtoListAsync(entities);
        }

        public async Task<TaxDeclarationDto> VerifyAsync(string id, TaxDeclarationVerifyActionDto dto, string tenantId, string actingUserId)
        {
            if (!await HasPermissionAsync(actingUserId, Actions.Approve))
                throw new UnauthorizedAccessException("You are not authorized to verify Tax Declarations.");

            var entity = await _context.TaxDeclarations
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted);

            if (entity == null)
                throw new Exception("Tax Declaration not found.");

            if (entity.Status != TaxDeclarationStatus.Submitted)
                throw new Exception("Only a Submitted declaration can be verified.");

            entity.Status = TaxDeclarationStatus.Verified;
            entity.VerifiedBy = actingUserId;
            entity.VerifiedOn = DateTime.UtcNow;
            entity.VerifierRemarks = string.IsNullOrWhiteSpace(dto?.VerifierRemarks) ? null : dto.VerifierRemarks.Trim();

            entity.ModifiedOn = DateTime.UtcNow;
            entity.ModifiedBy = actingUserId;

            await _context.SaveChangesAsync();

            // Refresh the computed tax figures immediately so HR/the
            // employee see up-to-date numbers right after verification -
            // failure to compute must not roll back the verification
            // itself (e.g. no salary structure set up yet is a Payroll
            // setup gap, not a reason to block HR's verification action).
            try
            {
                await _taxComputationService.ComputeAsync(entity.EmployeeId, entity.FinancialYearId, tenantId, actingUserId);
            }
            catch
            {
                // Swallowed deliberately - see comment above. The
                // computation can be retried later from the Tax
                // Computation screen.
            }

            return await GetByIdInternalAsync(entity.Id, tenantId);
        }

        public async Task<TaxDeclarationDto> RejectAsync(string id, TaxDeclarationVerifyActionDto dto, string tenantId, string actingUserId)
        {
            if (!await HasPermissionAsync(actingUserId, Actions.Approve))
                throw new UnauthorizedAccessException("You are not authorized to act on Tax Declarations.");

            if (string.IsNullOrWhiteSpace(dto?.VerifierRemarks))
                throw new Exception("Remarks are required to reject a Tax Declaration.");

            var entity = await _context.TaxDeclarations
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted);

            if (entity == null)
                throw new Exception("Tax Declaration not found.");

            if (entity.Status != TaxDeclarationStatus.Submitted)
                throw new Exception("Only a Submitted declaration can be rejected.");

            entity.Status = TaxDeclarationStatus.Rejected;
            entity.VerifiedBy = actingUserId;
            entity.VerifiedOn = DateTime.UtcNow;
            entity.VerifierRemarks = dto.VerifierRemarks.Trim();

            entity.ModifiedOn = DateTime.UtcNow;
            entity.ModifiedBy = actingUserId;

            await _context.SaveChangesAsync();

            return await GetByIdInternalAsync(entity.Id, tenantId);
        }

        #endregion

        #region Helpers

        private async Task<TaxDeclaration> GetEntityByIdAsync(string id, string tenantId)
        {
            var entity = await _context.TaxDeclarations
                .Include(x => x.Employee).ThenInclude(e => e.Department)
                .Include(x => x.FinancialYear)
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted);

            if (entity == null)
                throw new Exception("Tax Declaration not found.");

            return entity;
        }

        private async Task<TaxDeclarationDto> GetByIdInternalAsync(string id, string tenantId)
        {
            var entity = await GetEntityByIdAsync(id, tenantId);
            return await AssembleDtoAsync(entity);
        }

        private async Task<string?> GetActingEmployeeIdAsync(string? userId)
        {
            if (string.IsNullOrEmpty(userId))
                return null;

            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId);
            return user?.EmployeeId;
        }

        // Same permission-based override pattern as
        // WfhRequestService.IsHrOrAdminForAttendanceAsync, scoped to
        // TAX_DECLARATION's own View permission.
        private async Task<bool> IsHrOrAdminAsync(string? actingUserId)
        {
            return await HasPermissionAsync(actingUserId, Actions.View);
        }

        private async Task<bool> HasPermissionAsync(string? actingUserId, string action)
        {
            if (string.IsNullOrEmpty(actingUserId))
                return false;

            return await (
                from ur in _context.UserRoles
                join rp in _context.RolePermissions.Where(x => x.IsAllowed) on ur.RoleId equals rp.RoleId
                join p in _context.Permissions.Where(x =>
                        x.FeatureId == AppFeatureConstants.TAX_DECLARATION && x.Action == action)
                    on rp.PermissionId equals p.Id
                where ur.UserId == actingUserId
                select p.Id
            ).AnyAsync();
        }

        private static TaxDeclarationDto AssembleDto(TaxDeclaration x)
        {
            return new TaxDeclarationDto
            {
                Id = x.Id,

                EmployeeId = x.EmployeeId,
                EmployeeName = x.Employee != null ? $"{x.Employee.FirstName} {x.Employee.LastName}".Trim() : null,
                EmployeeCode = x.Employee?.EmployeeCode,
                DepartmentName = x.Employee?.Department?.Name,

                FinancialYearId = x.FinancialYearId,
                FinancialYearName = x.FinancialYear?.Name,

                Regime = (int)x.Regime,
                RegimeName = x.Regime.ToString(),

                Section80C = x.Section80C,
                Section80CCD1B = x.Section80CCD1B,
                Section80D = x.Section80D,
                Section24B = x.Section24B,
                OtherDeductions = x.OtherDeductions,

                AnnualRentPaid = x.AnnualRentPaid,
                IsMetroCity = x.IsMetroCity,
                LandlordPAN = x.LandlordPAN,

                Status = (int)x.Status,
                StatusName = x.Status.ToString(),

                SubmittedOn = x.SubmittedOn,

                VerifiedBy = x.VerifiedBy,
                VerifiedOn = x.VerifiedOn,
                VerifierRemarks = x.VerifierRemarks,

                TenantId = x.TenantId,
                CreatedOn = x.CreatedOn,
                CreatedBy = x.CreatedBy
            };
        }

        private async Task<Dictionary<string, string>> BuildUserNameMapAsync(IEnumerable<string?> userIds)
        {
            var ids = userIds.Where(x => !string.IsNullOrEmpty(x)).Select(x => x!).Distinct().ToList();

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

        private async Task<TaxDeclarationDto> AssembleDtoAsync(TaxDeclaration x)
        {
            var dto = AssembleDto(x);

            if (!string.IsNullOrEmpty(x.VerifiedBy))
            {
                var nameMap = await BuildUserNameMapAsync(new[] { x.VerifiedBy });
                dto.VerifiedByName = nameMap.TryGetValue(x.VerifiedBy, out var name) ? name : null;
            }

            return dto;
        }

        private async Task<List<TaxDeclarationDto>> AssembleDtoListAsync(List<TaxDeclaration> entities)
        {
            var nameMap = await BuildUserNameMapAsync(entities.Select(x => x.VerifiedBy));

            var list = new List<TaxDeclarationDto>();

            foreach (var x in entities)
            {
                var dto = AssembleDto(x);

                if (!string.IsNullOrEmpty(x.VerifiedBy) && nameMap.TryGetValue(x.VerifiedBy, out var name))
                    dto.VerifiedByName = name;

                list.Add(dto);
            }

            return list;
        }

        #endregion
    }
}
