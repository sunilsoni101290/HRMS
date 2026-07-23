using Application.DTOs.Attendances;
using Application.Interfaces.Attendances;
using Domain.Entities;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static Domain.Enums.EnumExtensions;

namespace Application.Services.Attendances
{
    // Company-wide attendance rules CRUD - simple master-data style service,
    // mirrors EmployeeBankDetailService/SalaryComponentService (tenant-scoped
    // queries, ValidateAsync throwing Exception on bad input, soft delete via
    // IsDeleted).
    public class AttendancePolicyService : IAttendancePolicyService
    {
        private readonly ApplicationDbContext _context;

        public AttendancePolicyService(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Get All

        public async Task<List<AttendancePolicyDto>> GetAllAsync(string tenantId)
        {
            var entities = await _context.AttendancePolicies
                .AsNoTracking()
                .Include(x => x.Company)
                .Where(x => x.TenantId == tenantId && !x.IsDeleted)
                .OrderByDescending(x => x.EffectiveFrom)
                .ToListAsync();

            return entities.Select(MapToDto).ToList();
        }

        #endregion

        #region Get By Id

        public async Task<AttendancePolicyDto?> GetByIdAsync(string id, string tenantId)
        {
            var entity = await _context.AttendancePolicies
                .AsNoTracking()
                .Include(x => x.Company)
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted);

            return entity == null ? null : MapToDto(entity);
        }

        #endregion

        #region Get Active

        public async Task<AttendancePolicyDto?> GetActiveForTenantAsync(string tenantId, string? companyId)
        {
            var today = DateTime.UtcNow.Date;

            var query = _context.AttendancePolicies
                .AsNoTracking()
                .Include(x => x.Company)
                .Where(x => x.TenantId == tenantId && !x.IsDeleted && x.IsActive && x.EffectiveFrom.Date <= today);

            AttendancePolicy? entity = null;

            if (!string.IsNullOrWhiteSpace(companyId))
            {
                entity = await query
                    .Where(x => x.CompanyId == companyId)
                    .OrderByDescending(x => x.EffectiveFrom)
                    .FirstOrDefaultAsync();
            }

            // Fall back to the tenant-wide default (CompanyId == null) policy
            // if no company-specific one is active.
            if (entity == null)
            {
                entity = await query
                    .Where(x => x.CompanyId == null)
                    .OrderByDescending(x => x.EffectiveFrom)
                    .FirstOrDefaultAsync();
            }

            return entity == null ? null : MapToDto(entity);
        }

        #endregion

        #region Create

        public async Task<AttendancePolicyDto> CreateAsync(AttendancePolicyDto dto, string tenantId, string? actingUserId)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            await ValidateAsync(dto, tenantId);

            var createdBy = string.IsNullOrWhiteSpace(actingUserId) ? "System" : actingUserId;

            var entity = new AttendancePolicy
            {
                Id = IDManager.GetNewId(new AttendancePolicy()),

                CompanyId = string.IsNullOrWhiteSpace(dto.CompanyId) ? null : dto.CompanyId,
                PolicyName = dto.PolicyName.Trim(),
                EffectiveFrom = dto.EffectiveFrom.Date,
                IsActive = dto.IsActive,

                MaxRegularizationRequestsPerMonth = dto.MaxRegularizationRequestsPerMonth,
                MaxWfhDaysPerMonth = dto.MaxWfhDaysPerMonth,
                LateMarkGraceCount = dto.LateMarkGraceCount,
                LateMarkPenaltyType = (LateMarkPenaltyType)dto.LateMarkPenaltyType,
                MinimumAttendancePercentForFullSalary = dto.MinimumAttendancePercentForFullSalary,
                CompOffEligibleExtraHours = dto.CompOffEligibleExtraHours,
                Remarks = dto.Remarks,

                TenantId = tenantId,
                CreatedOn = DateTime.UtcNow,
                CreatedBy = createdBy
            };

            await _context.AttendancePolicies.AddAsync(entity);
            await _context.SaveChangesAsync();

            return await GetByIdAsync(entity.Id, tenantId) ?? MapToDto(entity);
        }

        #endregion

        #region Update

        public async Task<AttendancePolicyDto> UpdateAsync(string id, AttendancePolicyDto dto, string tenantId, string? actingUserId)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            var entity = await _context.AttendancePolicies
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted);

            if (entity == null)
                throw new Exception("Attendance policy not found.");

            await ValidateAsync(dto, tenantId, id);

            entity.CompanyId = string.IsNullOrWhiteSpace(dto.CompanyId) ? null : dto.CompanyId;
            entity.PolicyName = dto.PolicyName.Trim();
            entity.EffectiveFrom = dto.EffectiveFrom.Date;
            entity.IsActive = dto.IsActive;

            entity.MaxRegularizationRequestsPerMonth = dto.MaxRegularizationRequestsPerMonth;
            entity.MaxWfhDaysPerMonth = dto.MaxWfhDaysPerMonth;
            entity.LateMarkGraceCount = dto.LateMarkGraceCount;
            entity.LateMarkPenaltyType = (LateMarkPenaltyType)dto.LateMarkPenaltyType;
            entity.MinimumAttendancePercentForFullSalary = dto.MinimumAttendancePercentForFullSalary;
            entity.CompOffEligibleExtraHours = dto.CompOffEligibleExtraHours;
            entity.Remarks = dto.Remarks;

            entity.ModifiedOn = DateTime.UtcNow;
            entity.ModifiedBy = string.IsNullOrWhiteSpace(actingUserId) ? "System" : actingUserId;

            _context.AttendancePolicies.Update(entity);
            await _context.SaveChangesAsync();

            return await GetByIdAsync(entity.Id, tenantId) ?? MapToDto(entity);
        }

        #endregion

        #region Delete (Soft)

        public async Task<bool> DeleteAsync(string id, string tenantId)
        {
            var entity = await _context.AttendancePolicies
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted);

            if (entity == null)
                return false;

            entity.IsDeleted = true;
            entity.ModifiedOn = DateTime.UtcNow;

            _context.AttendancePolicies.Update(entity);
            await _context.SaveChangesAsync();

            return true;
        }

        #endregion

        #region Private Helpers

        private async Task ValidateAsync(AttendancePolicyDto dto, string tenantId, string? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(dto.PolicyName))
                throw new Exception("Policy Name is required.");

            if (dto.EffectiveFrom == default)
                throw new Exception("Effective From date is required.");

            if (!string.IsNullOrWhiteSpace(dto.CompanyId))
            {
                var companyExists = await _context.Companies
                    .AnyAsync(x => x.Id == dto.CompanyId && !x.IsDeleted);

                if (!companyExists)
                    throw new Exception("Company not found.");
            }

            if (dto.MaxRegularizationRequestsPerMonth < 0)
                throw new Exception("Max Regularization Requests / Month cannot be negative.");

            if (dto.MaxWfhDaysPerMonth < 0)
                throw new Exception("Max WFH Days / Month cannot be negative.");

            if (dto.LateMarkGraceCount < 0)
                throw new Exception("Late Mark Grace Count cannot be negative.");

            if (dto.CompOffEligibleExtraHours < 0)
                throw new Exception("Comp-Off Eligible Extra Hours cannot be negative.");

            if (dto.MinimumAttendancePercentForFullSalary < 0 || dto.MinimumAttendancePercentForFullSalary > 100)
                throw new Exception("Minimum Attendance % For Full Salary must be between 0 and 100.");

            if (!Enum.IsDefined(typeof(LateMarkPenaltyType), dto.LateMarkPenaltyType))
                throw new Exception("Invalid Late Mark Penalty Type.");

            // Guard against two policies with the exact same
            // CompanyId + EffectiveFrom (would make GetActiveForTenantAsync's
            // "most recent EffectiveFrom" tie-break ambiguous).
            var duplicate = await _context.AttendancePolicies
                .AnyAsync(x => x.TenantId == tenantId
                            && !x.IsDeleted
                            && x.CompanyId == (string.IsNullOrWhiteSpace(dto.CompanyId) ? null : dto.CompanyId)
                            && x.EffectiveFrom.Date == dto.EffectiveFrom.Date
                            && x.Id != excludeId);

            if (duplicate)
                throw new Exception("A policy already exists for this Company with the same Effective From date.");
        }

        private static AttendancePolicyDto MapToDto(AttendancePolicy entity)
        {
            return new AttendancePolicyDto
            {
                Id = entity.Id,

                CompanyId = entity.CompanyId,
                CompanyName = entity.Company?.Name,

                PolicyName = entity.PolicyName,
                EffectiveFrom = entity.EffectiveFrom,
                IsActive = entity.IsActive,

                MaxRegularizationRequestsPerMonth = entity.MaxRegularizationRequestsPerMonth,
                MaxWfhDaysPerMonth = entity.MaxWfhDaysPerMonth,
                LateMarkGraceCount = entity.LateMarkGraceCount,
                LateMarkPenaltyType = (int)entity.LateMarkPenaltyType,
                LateMarkPenaltyTypeName = entity.LateMarkPenaltyType.ToString(),
                MinimumAttendancePercentForFullSalary = entity.MinimumAttendancePercentForFullSalary,
                CompOffEligibleExtraHours = entity.CompOffEligibleExtraHours,
                Remarks = entity.Remarks,

                TenantId = entity.TenantId,
                CreatedOn = entity.CreatedOn,
                CreatedBy = entity.CreatedBy,
                ModifiedOn = entity.ModifiedOn,
                ModifiedBy = entity.ModifiedBy
            };
        }

        #endregion
    }
}
