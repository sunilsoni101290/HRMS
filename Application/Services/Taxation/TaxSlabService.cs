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
    // Admin-only master-data CRUD for income-tax slabs - see
    // Domain/Entities/TaxSlab.cs / ITaxSlabService. Plain permission-gated
    // CRUD, no workflow (mirrors FinancialYearService's shape).
    public class TaxSlabService : ITaxSlabService
    {
        private readonly ApplicationDbContext _context;

        public TaxSlabService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<TaxSlabDto>> GetAllAsync(string tenantId, string? financialYearId, int? regime, string actingUserId)
        {
            if (!await HasPermissionAsync(actingUserId, Actions.View))
                throw new UnauthorizedAccessException("You are not authorized to view Tax Slabs.");

            var query = _context.TaxSlabs
                .Include(x => x.FinancialYear)
                .Where(x => x.TenantId == tenantId && !x.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(financialYearId))
                query = query.Where(x => x.FinancialYearId == financialYearId);

            if (regime.HasValue)
                query = query.Where(x => (int)x.Regime == regime.Value);

            var entities = await query
                .OrderBy(x => x.FinancialYearId).ThenBy(x => x.Regime).ThenBy(x => x.SlabOrder)
                .ToListAsync();

            return entities.Select(AssembleDto).ToList();
        }

        public async Task<TaxSlabDto> GetByIdAsync(string id, string tenantId, string actingUserId)
        {
            if (!await HasPermissionAsync(actingUserId, Actions.View))
                throw new UnauthorizedAccessException("You are not authorized to view Tax Slabs.");

            var entity = await GetEntityAsync(id, tenantId);
            return AssembleDto(entity);
        }

        public async Task<TaxSlabDto> CreateAsync(TaxSlabDto dto, string tenantId, string actingUserId)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            if (!await HasPermissionAsync(actingUserId, Actions.Create))
                throw new UnauthorizedAccessException("You are not authorized to create Tax Slabs.");

            ValidateSlab(dto);

            var entity = new TaxSlab
            {
                Id = IDManager.GetNewId(new TaxSlab()),

                FinancialYearId = dto.FinancialYearId,
                Regime = (TaxRegime)dto.Regime,
                SlabOrder = dto.SlabOrder,
                MinIncome = dto.MinIncome,
                MaxIncome = dto.MaxIncome,
                RatePercent = dto.RatePercent,

                TenantId = tenantId,
                CreatedOn = DateTime.UtcNow,
                CreatedBy = actingUserId
            };

            _context.TaxSlabs.Add(entity);
            await _context.SaveChangesAsync();

            return await GetByIdAsync(entity.Id, tenantId, actingUserId);
        }

        public async Task<TaxSlabDto> UpdateAsync(string id, TaxSlabDto dto, string tenantId, string actingUserId)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            if (!await HasPermissionAsync(actingUserId, Actions.Edit))
                throw new UnauthorizedAccessException("You are not authorized to edit Tax Slabs.");

            ValidateSlab(dto);

            var entity = await GetEntityAsync(id, tenantId);

            entity.FinancialYearId = dto.FinancialYearId;
            entity.Regime = (TaxRegime)dto.Regime;
            entity.SlabOrder = dto.SlabOrder;
            entity.MinIncome = dto.MinIncome;
            entity.MaxIncome = dto.MaxIncome;
            entity.RatePercent = dto.RatePercent;

            entity.ModifiedOn = DateTime.UtcNow;
            entity.ModifiedBy = actingUserId;

            await _context.SaveChangesAsync();

            return await GetByIdAsync(entity.Id, tenantId, actingUserId);
        }

        public async Task<bool> DeleteAsync(string id, string tenantId, string actingUserId)
        {
            if (!await HasPermissionAsync(actingUserId, Actions.Delete))
                throw new UnauthorizedAccessException("You are not authorized to delete Tax Slabs.");

            var entity = await GetEntityAsync(id, tenantId);

            entity.IsDeleted = true;
            entity.ModifiedOn = DateTime.UtcNow;
            entity.ModifiedBy = actingUserId;

            await _context.SaveChangesAsync();
            return true;
        }

        private static void ValidateSlab(TaxSlabDto dto)
        {
            if (!Enum.IsDefined(typeof(TaxRegime), dto.Regime))
                throw new Exception("Invalid Regime.");

            if (dto.MaxIncome.HasValue && dto.MaxIncome.Value <= dto.MinIncome)
                throw new Exception("Maximum Income must be greater than Minimum Income.");
        }

        private async Task<TaxSlab> GetEntityAsync(string id, string tenantId)
        {
            var entity = await _context.TaxSlabs
                .Include(x => x.FinancialYear)
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted);

            if (entity == null)
                throw new Exception("Tax Slab not found.");

            return entity;
        }

        private static TaxSlabDto AssembleDto(TaxSlab x)
        {
            return new TaxSlabDto
            {
                Id = x.Id,
                FinancialYearId = x.FinancialYearId,
                FinancialYearName = x.FinancialYear?.Name,
                Regime = (int)x.Regime,
                RegimeName = x.Regime.ToString(),
                SlabOrder = x.SlabOrder,
                MinIncome = x.MinIncome,
                MaxIncome = x.MaxIncome,
                RatePercent = x.RatePercent,
                TenantId = x.TenantId,
                CreatedOn = x.CreatedOn,
                CreatedBy = x.CreatedBy
            };
        }

        // Does the acting user hold, through any Role assigned to them, an
        // allowed RolePermission for the given action on the TAX_SLAB
        // feature - same join shape as
        // ProbationConfirmationService.HasPermissionAsync.
        private async Task<bool> HasPermissionAsync(string? actingUserId, string action)
        {
            if (string.IsNullOrEmpty(actingUserId))
                return false;

            return await (
                from ur in _context.UserRoles
                join rp in _context.RolePermissions.Where(x => x.IsAllowed) on ur.RoleId equals rp.RoleId
                join p in _context.Permissions.Where(x =>
                        x.FeatureId == AppFeatureConstants.TAX_SLAB && x.Action == action)
                    on rp.PermissionId equals p.Id
                where ur.UserId == actingUserId
                select p.Id
            ).AnyAsync();
        }
    }
}
