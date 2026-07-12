using Application.DTOs.Support;
using Application.Interfaces.Support;
using Domain.Entities;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Support
{
    public class FaqService : IFaqService
    {
        private readonly ApplicationDbContext _context;

        public FaqService(ApplicationDbContext context)
        {
            _context = context;
        }

        private static FaqItemDto AssembleDto(FaqItem x) => new()
        {
            Id = x.Id,
            CompanyId = x.CompanyId,
            Category = x.Category,
            Question = x.Question,
            Answer = x.Answer,
            DisplayOrder = x.DisplayOrder,
            IsActive = x.IsActive,
            TenantId = x.TenantId,
            CreatedBy = x.CreatedBy,
            ModifiedOn = x.ModifiedOn,
            ModifiedBy = x.ModifiedBy
        };

        public async Task<List<FaqItemDto>> GetAllAsync()
        {
            try
            {
                var items = await _context.FaqItems
                    .OrderBy(x => x.Category)
                    .ThenBy(x => x.DisplayOrder)
                    .ToListAsync();

                return items.Select(AssembleDto).ToList();
            }
            catch (Exception)
            {
                return new List<FaqItemDto>();
            }
        }

        public async Task<List<FaqItemDto>> GetActiveAsync()
        {
            try
            {
                var items = await _context.FaqItems
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.Category)
                    .ThenBy(x => x.DisplayOrder)
                    .ToListAsync();

                return items.Select(AssembleDto).ToList();
            }
            catch (Exception)
            {
                return new List<FaqItemDto>();
            }
        }

        public async Task<FaqItemDto> GetByIdAsync(string id)
        {
            try
            {
                var entity = await _context.FaqItems.FirstOrDefaultAsync(x => x.Id == id);
                return entity == null ? null : AssembleDto(entity);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<string> CreateAsync(FaqItemDto dto)
        {
            try
            {
                var entity = new FaqItem
                {
                    Id = IDManager.GetNewId(new FaqItem()),
                    TenantId = dto.TenantId,
                    CompanyId = dto.CompanyId,
                    Category = dto.Category,
                    Question = dto.Question,
                    Answer = dto.Answer,
                    DisplayOrder = dto.DisplayOrder,
                    IsActive = dto.IsActive,
                    CreatedBy = dto.CreatedBy
                };

                await _context.FaqItems.AddAsync(entity);
                await _context.SaveChangesAsync();

                return entity.Id;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<string> UpdateAsync(string id, FaqItemDto dto)
        {
            try
            {
                var entity = await _context.FaqItems.FirstOrDefaultAsync(x => x.Id == id);
                if (entity == null)
                    return null;

                entity.Category = dto.Category;
                entity.Question = dto.Question;
                entity.Answer = dto.Answer;
                entity.DisplayOrder = dto.DisplayOrder;
                entity.IsActive = dto.IsActive;
                entity.ModifiedOn = DateTime.UtcNow;
                entity.ModifiedBy = dto.ModifiedBy;

                await _context.SaveChangesAsync();

                return entity.Id;
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
                var entity = await _context.FaqItems.FirstOrDefaultAsync(x => x.Id == id);
                if (entity == null)
                    return false;

                entity.IsDeleted = true;
                entity.ModifiedOn = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> ToggleActiveAsync(string id)
        {
            try
            {
                var entity = await _context.FaqItems.FirstOrDefaultAsync(x => x.Id == id);
                if (entity == null)
                    return false;

                entity.IsActive = !entity.IsActive;
                entity.ModifiedOn = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
