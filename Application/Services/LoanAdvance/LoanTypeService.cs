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
    /// Master CRUD for Domain.Entities.LoanType - HR/Admin only
    /// (View/Create/Edit/Delete on AppFeatureConstants.LOAN_TYPE), no
    /// Maker-Checker workflow (same "plain CRUD master" shape as
    /// LeaveTypeService/AssetCategoryService).
    /// </summary>
    public class LoanTypeService : ILoanTypeService
    {
        private readonly IUnitOfWork _uow;

        public LoanTypeService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<List<LoanTypeDto>> GetAllAsync(string tenantId, string actingUserId, bool includeInactive = false)
        {
            await EnsurePermissionAsync(actingUserId, Actions.View);

            var repo = _uow.Repository<LoanType>();

            var query = repo.Query()
                .Where(x => x.TenantId == tenantId);

            if (!includeInactive)
                query = query.Where(x => x.IsActive);

            var entities = await query.OrderBy(x => x.Name).ToListAsync();

            var activeLoanCounts = await _uow.Repository<EmployeeLoan>()
                .Query()
                .Where(x => x.TenantId == tenantId && x.Status == LoanStatus.Active)
                .GroupBy(x => x.LoanTypeId)
                .Select(g => new { LoanTypeId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.LoanTypeId, x => x.Count);

            return entities.Select(x => Map(x, activeLoanCounts.GetValueOrDefault(x.Id))).ToList();
        }

        public async Task<LoanTypeDto> GetByIdAsync(string id, string tenantId, string actingUserId)
        {
            await EnsurePermissionAsync(actingUserId, Actions.View);

            var entity = await GetEntityAsync(id, tenantId);

            var activeLoanCount = await _uow.Repository<EmployeeLoan>()
                .Query()
                .CountAsync(x => x.LoanTypeId == id && x.Status == LoanStatus.Active);

            return Map(entity, activeLoanCount);
        }

        public async Task<LoanTypeDto> CreateAsync(LoanTypeDto dto, string tenantId, string actingUserId)
        {
            await EnsurePermissionAsync(actingUserId, Actions.Create);

            if (string.IsNullOrWhiteSpace(dto.Code))
                throw new BadRequestException("Code is required.");

            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new BadRequestException("Name is required.");

            var codeInUse = await _uow.Repository<LoanType>().ExistsAsync(x =>
                x.TenantId == tenantId && !x.IsDeleted && x.Code == dto.Code.Trim());

            if (codeInUse)
                throw new BadRequestException($"Loan Type code '{dto.Code}' is already in use.");

            var entity = new LoanType
            {
                TenantId = tenantId,
                Code = dto.Code.Trim(),
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim(),
                InterestMethod = (InterestMethod)dto.InterestMethod,
                DefaultInterestRatePercent = dto.DefaultInterestRatePercent,
                MaxTenureMonths = dto.MaxTenureMonths,
                RequiresGuarantor = dto.RequiresGuarantor,
                RequiresCollateral = dto.RequiresCollateral,
                IsActive = dto.IsActive,
                CreatedBy = actingUserId,
                CreatedOn = DateTime.UtcNow
            };

            await _uow.Repository<LoanType>().AddAsync(entity);
            await _uow.SaveChangesAsync();

            return Map(entity, 0);
        }

        public async Task<LoanTypeDto> UpdateAsync(LoanTypeDto dto, string tenantId, string actingUserId)
        {
            await EnsurePermissionAsync(actingUserId, Actions.Edit);

            if (string.IsNullOrWhiteSpace(dto.Id))
                throw new BadRequestException("Id is required for update.");

            var entity = await GetEntityAsync(dto.Id, tenantId, forUpdate: true);

            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new BadRequestException("Name is required.");

            var codeInUse = await _uow.Repository<LoanType>().ExistsAsync(x =>
                x.TenantId == tenantId && !x.IsDeleted && x.Id != entity.Id && x.Code == dto.Code.Trim());

            if (codeInUse)
                throw new BadRequestException($"Loan Type code '{dto.Code}' is already in use.");

            entity.Code = dto.Code.Trim();
            entity.Name = dto.Name.Trim();
            entity.Description = dto.Description?.Trim();
            entity.InterestMethod = (InterestMethod)dto.InterestMethod;
            entity.DefaultInterestRatePercent = dto.DefaultInterestRatePercent;
            entity.MaxTenureMonths = dto.MaxTenureMonths;
            entity.RequiresGuarantor = dto.RequiresGuarantor;
            entity.RequiresCollateral = dto.RequiresCollateral;
            entity.IsActive = dto.IsActive;
            entity.ModifiedBy = actingUserId;
            entity.ModifiedOn = DateTime.UtcNow;

            _uow.Repository<LoanType>().Update(entity);
            await _uow.SaveChangesAsync();

            return Map(entity, 0);
        }

        public async Task<bool> DeleteAsync(string id, string tenantId, string actingUserId)
        {
            await EnsurePermissionAsync(actingUserId, Actions.Delete);

            var entity = await GetEntityAsync(id, tenantId, forUpdate: true);

            var inUse = await _uow.Repository<EmployeeLoan>().ExistsAsync(x =>
                x.LoanTypeId == id &&
                x.Status != LoanStatus.Closed &&
                x.Status != LoanStatus.Rejected &&
                x.Status != LoanStatus.Cancelled);

            if (inUse)
                throw new BadRequestException(
                    "This Loan Type has open (non-Closed/Rejected/Cancelled) loan requests and cannot be deleted. Deactivate it instead.");

            entity.IsDeleted = true;
            entity.ModifiedBy = actingUserId;
            entity.ModifiedOn = DateTime.UtcNow;

            _uow.Repository<LoanType>().Update(entity);
            await _uow.SaveChangesAsync();

            return true;
        }

        private async Task<LoanType> GetEntityAsync(string id, string tenantId, bool forUpdate = false)
        {
            var entity = forUpdate
                ? await _uow.Repository<LoanType>().Query(asNoTracking: false)
                    .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted)
                : await _uow.Repository<LoanType>().Query()
                    .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted);

            if (entity == null)
                throw new NotFoundException("Loan Type not found.");

            return entity;
        }

        private static LoanTypeDto Map(LoanType x, int activeLoanCount) => new()
        {
            Id = x.Id,
            TenantId = x.TenantId,
            Code = x.Code,
            Name = x.Name,
            Description = x.Description,
            InterestMethod = (int)x.InterestMethod,
            InterestMethodName = x.InterestMethod.ToString(),
            DefaultInterestRatePercent = x.DefaultInterestRatePercent,
            MaxTenureMonths = x.MaxTenureMonths,
            RequiresGuarantor = x.RequiresGuarantor,
            RequiresCollateral = x.RequiresCollateral,
            IsActive = x.IsActive,
            ActiveLoanCount = activeLoanCount,
            CreatedBy = x.CreatedBy,
            CreatedOn = x.CreatedOn,
            ModifiedBy = x.ModifiedBy,
            ModifiedOn = x.ModifiedOn
        };

        // Same HasPermissionAsync join shape as
        // ProbationConfirmationService, scoped to AppFeatureConstants.LOAN_TYPE.
        private async Task EnsurePermissionAsync(string? actingUserId, string action)
        {
            if (string.IsNullOrEmpty(actingUserId))
                throw new UnauthorizedException("You are not authorized to perform this action.");

            var allowed = await (
                from ur in _uow.Repository<UserRole>().Query()
                join rp in _uow.Repository<RolePermission>().Query().Where(x => x.IsAllowed) on ur.RoleId equals rp.RoleId
                join p in _uow.Repository<Permission>().Query().Where(x =>
                        x.FeatureId == AppFeatureConstants.LOAN_TYPE && x.Action == action)
                    on rp.PermissionId equals p.Id
                where ur.UserId == actingUserId
                select p.Id
            ).AnyAsync();

            if (!allowed)
                throw new UnauthorizedException($"You are not authorized to {action} Loan Types.");
        }
    }
}
