using Application.DTOs.Assets;
using Application.Interfaces.Assets;
using Domain.Entities;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Assets
{
    public class AssetCategoryService : IAssetCategoryService
    {
        private readonly ApplicationDbContext _context;

        public AssetCategoryService(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Get All

        public async Task<List<AssetCategoryListDto>> GetAllAsync()
        {
            try
            {
            return await _context.AssetCategories
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.Name)
                .Select(x => new AssetCategoryListDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    IsActive = x.IsActive,
                    AssetCount = _context.Assets.Count(a => a.AssetCategoryId == x.Id && !a.IsDeleted)
                })
                .ToListAsync();
            }
            catch (Exception)
            {
                return new List<AssetCategoryListDto>();
            }
        }

        #endregion

        #region Get By Id

        public async Task<AssetCategoryDto> GetByIdAsync(string id)
        {
            try
            {
            var entity = await _context.AssetCategories
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null)
                return null;

            return new AssetCategoryDto
            {
                Id = entity.Id,
                Name = entity.Name,
                TenantId = entity.TenantId,
                CreatedBy = entity.CreatedBy,
                ModifiedBy = entity.ModifiedBy,
                ModifiedOn = entity.ModifiedOn
            };
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion

        #region Create

        public async Task<string> CreateAsync(AssetCategoryDto dto)
        {
            try
            {
            var isDuplicate = await _context.AssetCategories
                .AnyAsync(x => x.Name == dto.Name
                            && x.TenantId == dto.TenantId
                            && !x.IsDeleted);

            if (isDuplicate)
                return "Category Already Exists";

            var entity = new AssetCategory
            {
                Id = IDManager.GetNewId(new AssetCategory()),
                Name = dto.Name,
                TenantId = dto.TenantId,
                CreatedBy = dto.CreatedBy
            };

            await _context.AssetCategories.AddAsync(entity);
            await _context.SaveChangesAsync();

            return entity.Id;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        #endregion

        #region Update

        public async Task<string> UpdateAsync(string id, AssetCategoryDto dto)
        {
            try
            {
            var entity = await _context.AssetCategories
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null)
                return "Category Not Found";

            entity.Name = dto.Name;
            entity.TenantId = dto.TenantId;
            entity.ModifiedBy = dto.ModifiedBy;
            entity.ModifiedOn = dto.ModifiedOn ?? DateTime.UtcNow;

            _context.AssetCategories.Update(entity);
            await _context.SaveChangesAsync();

            return entity.Id;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        #endregion

        #region Delete (Soft)

        public async Task<bool> DeleteAsync(string id)
        {
            try
            {
            var entity = await _context.AssetCategories
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null)
                return false;

            entity.IsDeleted = true;
            entity.ModifiedOn = DateTime.UtcNow;

            _context.AssetCategories.Update(entity);
            await _context.SaveChangesAsync();

            return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion
    }
}
