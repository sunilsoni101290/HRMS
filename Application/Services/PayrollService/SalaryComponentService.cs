using Application.DTOs.Payroll;
using Application.Interfaces.Payroll;
using Domain.Entities;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static Domain.Enums.EnumExtensions;

namespace Application.Services.PayrollService
{
    public class SalaryComponentService : ISalaryComponentService
    {
        private readonly ApplicationDbContext _context;

        public SalaryComponentService(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Get All

        public async Task<List<SalaryComponentListDto>> GetAllAsync()
        {
            try
            {
            var list = await _context.SalaryComponents
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.ComponentType).ThenBy(x => x.Name)
                .Select(x => new SalaryComponentListDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Code = x.Code,
                    ComponentType = (int)x.ComponentType,
                    IsTaxable = x.IsTaxable,
                    IsPFApplicable = x.IsPFApplicable,
                    IsESICApplicable = x.IsESICApplicable
                })
                .ToListAsync();

            foreach (var item in list)
                item.ComponentTypeText = ((SalaryComponentType)item.ComponentType).ToString();

            return list;
            }
            catch (Exception)
            {
                return new List<SalaryComponentListDto>();
            }
        }

        #endregion

        #region Get By Id

        public async Task<SalaryComponentDto> GetByIdAsync(string id)
        {
            try
            {
            var entity = await _context.SalaryComponents
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null)
                return null;

            return new SalaryComponentDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Code = entity.Code,
                ComponentType = (int)entity.ComponentType,
                ComponentTypeText = entity.ComponentType.ToString(),
                IsTaxable = entity.IsTaxable,
                IsPFApplicable = entity.IsPFApplicable,
                IsESICApplicable = entity.IsESICApplicable,
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

        public async Task<string> CreateAsync(SalaryComponentDto dto)
        {
            try
            {
            var isDuplicate = await _context.SalaryComponents
                .AnyAsync(x => x.Code == dto.Code
                            && x.TenantId == dto.TenantId
                            && !x.IsDeleted);

            if (isDuplicate)
                return "Component Code Already Exists";

            var entity = new SalaryComponent
            {
                Id = IDManager.GetNewId(new SalaryComponent()),
                Name = dto.Name,
                Code = dto.Code,
                ComponentType = (SalaryComponentType)dto.ComponentType,
                IsTaxable = dto.IsTaxable,
                IsPFApplicable = dto.IsPFApplicable,
                IsESICApplicable = dto.IsESICApplicable,
                TenantId = dto.TenantId,
                CreatedBy = dto.CreatedBy
            };

            await _context.SalaryComponents.AddAsync(entity);
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

        public async Task<string> UpdateAsync(string id, SalaryComponentDto dto)
        {
            try
            {
            var entity = await _context.SalaryComponents
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null)
                return "Component Not Found";

            entity.Name = dto.Name;
            entity.Code = dto.Code;
            entity.ComponentType = (SalaryComponentType)dto.ComponentType;
            entity.IsTaxable = dto.IsTaxable;
            entity.IsPFApplicable = dto.IsPFApplicable;
            entity.IsESICApplicable = dto.IsESICApplicable;
            entity.TenantId = dto.TenantId;
            entity.ModifiedBy = dto.ModifiedBy;
            entity.ModifiedOn = dto.ModifiedOn ?? DateTime.UtcNow;

            _context.SalaryComponents.Update(entity);
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
            var entity = await _context.SalaryComponents
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null)
                return false;

            entity.IsDeleted = true;
            entity.ModifiedOn = DateTime.UtcNow;

            _context.SalaryComponents.Update(entity);
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
