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
    /// <summary>Master CRUD for Domain.Entities.AdvanceType - mirror of LoanTypeService.</summary>
    public class AdvanceTypeService : IAdvanceTypeService
    {
        private readonly IUnitOfWork _uow;

        public AdvanceTypeService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<List<AdvanceTypeDto>> GetAllAsync(string tenantId, string actingUserId, bool includeInactive = false)
        {
            await EnsurePermissionAsync(actingUserId, Actions.View);

            var query = _uow.Repository<AdvanceType>().Query()
                .Where(x => x.TenantId == tenantId);

            if (!includeInactive)
                query = query.Where(x => x.IsActive);

            var entities = await query.OrderBy(x => x.Name).ToListAsync();

            var activeCounts = await _uow.Repository<EmployeeAdvance>()
                .Query()
                .Where(x => x.TenantId == tenantId &&
                    (x.Status == AdvanceStatus.Disbursed))
                .GroupBy(x => x.AdvanceTypeId)
                .Select(g => new { AdvanceTypeId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.AdvanceTypeId, x => x.Count);

            return entities.Select(x => Map(x, activeCounts.GetValueOrDefault(x.Id))).ToList();
        }

        public async Task<AdvanceTypeDto> GetByIdAsync(string id, string tenantId, string actingUserId)
        {
            await EnsurePermissionAsync(actingUserId, Actions.View);

            var entity = await GetEntityAsync(id, tenantId);

            var activeCount = await _uow.Repository<EmployeeAdvance>().Query()
                .CountAsync(x => x.AdvanceTypeId == id && x.Status == AdvanceStatus.Disbursed);

            return Map(entity, activeCount);
        }

        public async Task<AdvanceTypeDto> CreateAsync(AdvanceTypeDto dto, string tenantId, string actingUserId)
        {
            await EnsurePermissionAsync(actingUserId, Actions.Create);

            if (string.IsNullOrWhiteSpace(dto.Code))
                throw new BadRequestException("Code is required.");

            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new BadRequestException("Name is required.");

            if (!dto.MaxAmount.HasValue && !dto.MaxAmountSalaryMultiplier.HasValue)
                throw new BadRequestException("Either Max Amount or Max Amount Salary Multiplier must be set.");

            var codeInUse = await _uow.Repository<AdvanceType>().ExistsAsync(x =>
                x.TenantId == tenantId && !x.IsDeleted && x.Code == dto.Code.Trim());

            if (codeInUse)
                throw new BadRequestException($"Advance Type code '{dto.Code}' is already in use.");

            var entity = new AdvanceType
            {
                TenantId = tenantId,
                Code = dto.Code.Trim(),
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim(),
                MaxAmount = dto.MaxAmount,
                MaxAmountSalaryMultiplier = dto.MaxAmountSalaryMultiplier,
                MaxInstallments = dto.MaxInstallments,
                IsInterestFree = dto.IsInterestFree,
                IsActive = dto.IsActive,
                CreatedBy = actingUserId,
                CreatedOn = DateTime.UtcNow
            };

            await _uow.Repository<AdvanceType>().AddAsync(entity);
            await _uow.SaveChangesAsync();

            return Map(entity, 0);
        }

        public async Task<AdvanceTypeDto> UpdateAsync(AdvanceTypeDto dto, string tenantId, string actingUserId)
        {
            await EnsurePermissionAsync(actingUserId, Actions.Edit);

            if (string.IsNullOrWhiteSpace(dto.Id))
                throw new BadRequestException("Id is required for update.");

            var entity = await GetEntityAsync(dto.Id, tenantId, forUpdate: true);

            var codeInUse = await _uow.Repository<AdvanceType>().ExistsAsync(x =>
                x.TenantId == tenantId && !x.IsDeleted && x.Id != entity.Id && x.Code == dto.Code.Trim());

            if (codeInUse)
                throw new BadRequestException($"Advance Type code '{dto.Code}' is already in use.");

            entity.Code = dto.Code.Trim();
            entity.Name = dto.Name.Trim();
            entity.Description = dto.Description?.Trim();
            entity.MaxAmount = dto.MaxAmount;
            entity.MaxAmountSalaryMultiplier = dto.MaxAmountSalaryMultiplier;
            entity.MaxInstallments = dto.MaxInstallments;
            entity.IsInterestFree = dto.IsInterestFree;
            entity.IsActive = dto.IsActive;
            entity.ModifiedBy = actingUserId;
            entity.ModifiedOn = DateTime.UtcNow;

            _uow.Repository<AdvanceType>().Update(entity);
            await _uow.SaveChangesAsync();

            return Map(entity, 0);
        }

        public async Task<bool> DeleteAsync(string id, string tenantId, string actingUserId)
        {
            await EnsurePermissionAsync(actingUserId, Actions.Delete);

            var entity = await GetEntityAsync(id, tenantId, forUpdate: true);

            var inUse = await _uow.Repository<EmployeeAdvance>().ExistsAsync(x =>
                x.AdvanceTypeId == id &&
                x.Status != AdvanceStatus.Settled &&
                x.Status != AdvanceStatus.Rejected &&
                x.Status != AdvanceStatus.Cancelled);

            if (inUse)
                throw new BadRequestException(
                    "This Advance Type has open requests and cannot be deleted. Deactivate it instead.");

            entity.IsDeleted = true;
            entity.ModifiedBy = actingUserId;
            entity.ModifiedOn = DateTime.UtcNow;

            _uow.Repository<AdvanceType>().Update(entity);
            await _uow.SaveChangesAsync();

            return true;
        }

        private async Task<AdvanceType> GetEntityAsync(string id, string tenantId, bool forUpdate = false)
        {
            var entity = forUpdate
                ? await _uow.Repository<AdvanceType>().Query(asNoTracking: false)
                    .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted)
                : await _uow.Repository<AdvanceType>().Query()
                    .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted);

            if (entity == null)
                throw new NotFoundException("Advance Type not found.");

            return entity;
        }

        private static AdvanceTypeDto Map(AdvanceType x, int activeCount) => new()
        {
            Id = x.Id,
            TenantId = x.TenantId,
            Code = x.Code,
            Name = x.Name,
            Description = x.Description,
            MaxAmount = x.MaxAmount,
            MaxAmountSalaryMultiplier = x.MaxAmountSalaryMultiplier,
            MaxInstallments = x.MaxInstallments,
            IsInterestFree = x.IsInterestFree,
            IsActive = x.IsActive,
            ActiveAdvanceCount = activeCount,
            CreatedBy = x.CreatedBy,
            CreatedOn = x.CreatedOn,
            ModifiedBy = x.ModifiedBy,
            ModifiedOn = x.ModifiedOn
        };

        private async Task EnsurePermissionAsync(string? actingUserId, string action)
        {
            if (string.IsNullOrEmpty(actingUserId))
                throw new UnauthorizedException("You are not authorized to perform this action.");

            var allowed = await (
                from ur in _uow.Repository<UserRole>().Query()
                join rp in _uow.Repository<RolePermission>().Query().Where(x => x.IsAllowed) on ur.RoleId equals rp.RoleId
                join p in _uow.Repository<Permission>().Query().Where(x =>
                        x.FeatureId == AppFeatureConstants.ADVANCE_TYPE && x.Action == action)
                    on rp.PermissionId equals p.Id
                where ur.UserId == actingUserId
                select p.Id
            ).AnyAsync();

            if (!allowed)
                throw new UnauthorizedException($"You are not authorized to {action} Advance Types.");
        }
    }
}
