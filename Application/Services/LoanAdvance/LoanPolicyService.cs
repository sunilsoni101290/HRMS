using Application.Common.Exceptions;
using Application.DTOs.LoanAdvance;
using Application.Interfaces.LoanAdvance;
using Domain.Entities;
using Domain.Helper;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using static Domain.Enums.EnumExtensions;

namespace Application.Services.LoanAdvance
{
    /// <summary>
    /// CRUD + versioning for Domain.Entities.LoanPolicy and its nested
    /// Domain.Entities.LoanPolicyApprovalLevel matrix - HR/Admin only
    /// (AppFeatureConstants.LOAN_POLICY), no Maker-Checker (a policy edit
    /// takes effect immediately for NEW requests; it never touches
    /// in-flight/historical EmployeeLoan rows, which snapshot their own
    /// LoanPolicyId - see UpdateAsync).
    /// </summary>
    public class LoanPolicyService : ILoanPolicyService
    {
        private readonly IUnitOfWork _uow;

        public LoanPolicyService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<List<LoanPolicyDto>> GetAllAsync(string tenantId, string actingUserId, string? loanTypeId = null, string? companyId = null)
        {
            await EnsurePermissionAsync(actingUserId, Actions.View);

            var query = _uow.Repository<LoanPolicy>().Query()
                .Include(x => x.LoanType)
                .Include(x => x.Company)
                .Include(x => x.Branch)
                .Include(x => x.ApprovalLevels)
                .Where(x => x.TenantId == tenantId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(loanTypeId))
                query = query.Where(x => x.LoanTypeId == loanTypeId);

            if (!string.IsNullOrWhiteSpace(companyId))
                query = query.Where(x => x.CompanyId == companyId || x.CompanyId == null);

            var entities = await query
                .OrderByDescending(x => x.EffectiveFrom)
                .ToListAsync();

            return entities.Select(Map).ToList();
        }

        public async Task<LoanPolicyDto> GetByIdAsync(string id, string tenantId, string actingUserId)
        {
            await EnsurePermissionAsync(actingUserId, Actions.View);

            var entity = await GetEntityAsync(id, tenantId);
            return Map(entity);
        }

        public async Task<LoanPolicyDto> CreateAsync(LoanPolicyDto dto, string tenantId, string actingUserId)
        {
            await EnsurePermissionAsync(actingUserId, Actions.Create);

            ValidateDto(dto);

            var entity = BuildEntity(dto, tenantId, actingUserId, versionNumber: 1);

            await _uow.Repository<LoanPolicy>().AddAsync(entity);
            await _uow.SaveChangesAsync();

            return Map(entity);
        }

        public async Task<LoanPolicyDto> UpdateAsync(LoanPolicyDto dto, string tenantId, string actingUserId)
        {
            await EnsurePermissionAsync(actingUserId, Actions.Edit);

            if (string.IsNullOrWhiteSpace(dto.Id))
                throw new BadRequestException("Id is required for update.");

            ValidateDto(dto);

            var current = await _uow.Repository<LoanPolicy>().Query(asNoTracking: false)
                .Include(x => x.ApprovalLevels)
                .FirstOrDefaultAsync(x => x.Id == dto.Id && x.TenantId == tenantId && !x.IsDeleted);

            if (current == null)
                throw new NotFoundException("Loan Policy not found.");

            // Close the current version rather than mutating it in place -
            // see ILoanPolicyService.UpdateAsync's XML doc for why.
            current.EffectiveTo = DateTime.UtcNow;
            current.IsActive = false;
            current.ModifiedBy = actingUserId;
            current.ModifiedOn = DateTime.UtcNow;
            _uow.Repository<LoanPolicy>().Update(current);

            var next = BuildEntity(dto, tenantId, actingUserId, versionNumber: current.VersionNumber + 1);
            await _uow.Repository<LoanPolicy>().AddAsync(next);

            await _uow.SaveChangesAsync();

            return Map(next);
        }

        public async Task<bool> DeactivateAsync(string id, string tenantId, string actingUserId)
        {
            await EnsurePermissionAsync(actingUserId, Actions.Delete);

            var entity = await _uow.Repository<LoanPolicy>().Query(asNoTracking: false)
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted);

            if (entity == null)
                throw new NotFoundException("Loan Policy not found.");

            entity.IsActive = false;
            entity.EffectiveTo = DateTime.UtcNow;
            entity.ModifiedBy = actingUserId;
            entity.ModifiedOn = DateTime.UtcNow;

            _uow.Repository<LoanPolicy>().Update(entity);
            await _uow.SaveChangesAsync();

            return true;
        }

        public async Task<LoanPolicyDto?> GetActivePolicyAsync(string loanTypeId, string tenantId, string? companyId, string? branchId)
        {
            var candidates = await _uow.Repository<LoanPolicy>().Query()
                .Include(x => x.ApprovalLevels)
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.LoanTypeId == loanTypeId &&
                    x.IsActive &&
                    !x.IsDeleted &&
                    x.EffectiveFrom <= DateTime.UtcNow &&
                    (x.EffectiveTo == null || x.EffectiveTo > DateTime.UtcNow) &&
                    (x.BranchId == null || x.BranchId == branchId) &&
                    (x.CompanyId == null || x.CompanyId == companyId))
                .ToListAsync();

            // Most specific match wins: Branch-scoped > Company-scoped > Tenant-wide.
            var best = candidates
                .OrderByDescending(x => x.BranchId != null)
                .ThenByDescending(x => x.CompanyId != null)
                .ThenByDescending(x => x.VersionNumber)
                .FirstOrDefault();

            return best == null ? null : Map(best);
        }

        private static void ValidateDto(LoanPolicyDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.LoanTypeId))
                throw new BadRequestException("Loan Type is required.");

            if (dto.MaxAmount < dto.MinAmount)
                throw new BadRequestException("Max Amount cannot be less than Min Amount.");

            if (dto.MaxTenureMonths < dto.MinTenureMonths)
                throw new BadRequestException("Max Tenure cannot be less than Min Tenure.");

            if (dto.MaxDeductionPercentOfNetSalary is <= 0 or > 100)
                throw new BadRequestException("Max Deduction % of Net Salary must be between 1 and 100.");

            if (dto.ApprovalLevels == null || dto.ApprovalLevels.Count == 0)
                throw new BadRequestException("At least one approval level is required.");

            var levelNumbers = dto.ApprovalLevels.Select(x => x.LevelNumber).ToList();
            if (levelNumbers.Distinct().Count() != levelNumbers.Count)
                throw new BadRequestException("Approval level numbers must be unique within a policy.");

            foreach (var level in dto.ApprovalLevels)
            {
                switch ((ApproverType)level.ApproverType)
                {
                    case ApproverType.SpecificRole when string.IsNullOrWhiteSpace(level.ApproverRoleId):
                        throw new BadRequestException($"Level {level.LevelNumber}: Approver Role is required for ApproverType=SpecificRole.");
                    case ApproverType.SpecificUser when string.IsNullOrWhiteSpace(level.ApproverUserId):
                        throw new BadRequestException($"Level {level.LevelNumber}: Approver User is required for ApproverType=SpecificUser.");
                }
            }
        }

        private static LoanPolicy BuildEntity(LoanPolicyDto dto, string tenantId, string actingUserId, int versionNumber) => new()
        {
            TenantId = tenantId,
            CompanyId = string.IsNullOrWhiteSpace(dto.CompanyId) ? null : dto.CompanyId,
            BranchId = string.IsNullOrWhiteSpace(dto.BranchId) ? null : dto.BranchId,
            LoanTypeId = dto.LoanTypeId,
            MinAmount = dto.MinAmount,
            MaxAmount = dto.MaxAmount,
            MinTenureMonths = dto.MinTenureMonths,
            MaxTenureMonths = dto.MaxTenureMonths,
            InterestRatePercent = dto.InterestRatePercent,
            MinServiceMonthsRequired = dto.MinServiceMonthsRequired,
            MaxActiveLoans = dto.MaxActiveLoans,
            MaxDeductionPercentOfNetSalary = dto.MaxDeductionPercentOfNetSalary,
            EligibilitySalaryMultiplier = dto.EligibilitySalaryMultiplier,
            PreClosurePenaltyPercent = dto.PreClosurePenaltyPercent,
            VersionNumber = versionNumber,
            EffectiveFrom = DateTime.UtcNow,
            EffectiveTo = null,
            IsActive = true,
            CreatedBy = actingUserId,
            CreatedOn = DateTime.UtcNow,
            ApprovalLevels = dto.ApprovalLevels.Select(l => new LoanPolicyApprovalLevel
            {
                LevelNumber = l.LevelNumber,
                ApproverType = (ApproverType)l.ApproverType,
                ApproverRoleId = string.IsNullOrWhiteSpace(l.ApproverRoleId) ? null : l.ApproverRoleId,
                ApproverUserId = string.IsNullOrWhiteSpace(l.ApproverUserId) ? null : l.ApproverUserId,
                MinAmountThreshold = l.MinAmountThreshold,
                CreatedBy = actingUserId,
                CreatedOn = DateTime.UtcNow
            }).ToList()
        };

        private async Task<LoanPolicy> GetEntityAsync(string id, string tenantId)
        {
            var entity = await _uow.Repository<LoanPolicy>().Query()
                .Include(x => x.LoanType)
                .Include(x => x.Company)
                .Include(x => x.Branch)
                .Include(x => x.ApprovalLevels)
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted);

            if (entity == null)
                throw new NotFoundException("Loan Policy not found.");

            return entity;
        }

        private static LoanPolicyDto Map(LoanPolicy x) => new()
        {
            Id = x.Id,
            TenantId = x.TenantId,
            CompanyId = x.CompanyId,
            CompanyName = x.Company?.Name,
            BranchId = x.BranchId,
            BranchName = x.Branch?.Name,
            LoanTypeId = x.LoanTypeId,
            LoanTypeName = x.LoanType?.Name,
            MinAmount = x.MinAmount,
            MaxAmount = x.MaxAmount,
            MinTenureMonths = x.MinTenureMonths,
            MaxTenureMonths = x.MaxTenureMonths,
            InterestRatePercent = x.InterestRatePercent,
            MinServiceMonthsRequired = x.MinServiceMonthsRequired,
            MaxActiveLoans = x.MaxActiveLoans,
            MaxDeductionPercentOfNetSalary = x.MaxDeductionPercentOfNetSalary,
            EligibilitySalaryMultiplier = x.EligibilitySalaryMultiplier,
            PreClosurePenaltyPercent = x.PreClosurePenaltyPercent,
            VersionNumber = x.VersionNumber,
            EffectiveFrom = x.EffectiveFrom,
            EffectiveTo = x.EffectiveTo,
            IsActive = x.IsActive,
            CreatedBy = x.CreatedBy,
            CreatedOn = x.CreatedOn,
            ApprovalLevels = (x.ApprovalLevels ?? new List<LoanPolicyApprovalLevel>())
                .OrderBy(l => l.LevelNumber)
                .Select(l => new LoanPolicyApprovalLevelDto
                {
                    Id = l.Id,
                    LoanPolicyId = l.LoanPolicyId,
                    LevelNumber = l.LevelNumber,
                    ApproverType = (int)l.ApproverType,
                    ApproverTypeName = l.ApproverType.ToString(),
                    ApproverRoleId = l.ApproverRoleId,
                    ApproverRoleName = l.ApproverRole?.Name,
                    ApproverUserId = l.ApproverUserId,
                    ApproverUserName = l.ApproverUser?.Username,
                    MinAmountThreshold = l.MinAmountThreshold
                }).ToList()
        };

        private async Task EnsurePermissionAsync(string? actingUserId, string action)
        {
            if (string.IsNullOrEmpty(actingUserId))
                throw new UnauthorizedException("You are not authorized to perform this action.");

            var allowed = await (
                from ur in _uow.Repository<UserRole>().Query()
                join rp in _uow.Repository<RolePermission>().Query().Where(x => x.IsAllowed) on ur.RoleId equals rp.RoleId
                join p in _uow.Repository<Permission>().Query().Where(x =>
                        x.FeatureId == AppFeatureConstants.LOAN_POLICY && x.Action == action)
                    on rp.PermissionId equals p.Id
                where ur.UserId == actingUserId
                select p.Id
            ).AnyAsync();

            if (!allowed)
                throw new UnauthorizedException($"You are not authorized to {action} Loan Policies.");
        }
    }
}
