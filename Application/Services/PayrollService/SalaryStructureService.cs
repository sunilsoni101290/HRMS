using Application.DTOs.Payroll;
using Application.Interfaces.Payroll;
using Domain.Entities;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static Domain.Enums.EnumExtensions;

namespace Application.Services.PayrollService
{
    public class SalaryStructureService : ISalaryStructureService
    {
        private readonly ApplicationDbContext _context;

        public SalaryStructureService(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Get All

        public async Task<List<SalaryStructureListDto>> GetAllAsync()
        {
            try
            {
            var structures = await _context.SalaryStructures
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderByDescending(x => x.EffectiveFrom)
                .Select(x => new
                {
                    x.Id,
                    x.EmployeeId,
                    EmployeeName = x.Employee != null ? x.Employee.FirstName + " " + x.Employee.LastName : "",
                    x.EffectiveFrom,
                    x.SourceTemplateId,
                    SourceTemplateName = x.SourceTemplate != null ? x.SourceTemplate.Name : null,
                    Lines = x.SalaryDetails.Select(d => new
                    {
                        d.Amount,
                        Type = (int)d.SalaryComponent.ComponentType
                    }).ToList()
                })
                .ToListAsync();

            return structures.Select(x => new SalaryStructureListDto
            {
                Id = x.Id,
                EmployeeId = x.EmployeeId,
                EmployeeName = x.EmployeeName,
                EffectiveFrom = x.EffectiveFrom,
                SourceTemplateId = x.SourceTemplateId,
                SourceTemplateName = x.SourceTemplateName,
                ComponentCount = x.Lines.Count,
                TotalEarnings = x.Lines.Where(l => l.Type == (int)SalaryComponentType.Earning).Sum(l => l.Amount),
                TotalDeductions = x.Lines.Where(l => l.Type == (int)SalaryComponentType.Deduction).Sum(l => l.Amount),
                NetSalary = x.Lines.Where(l => l.Type == (int)SalaryComponentType.Earning).Sum(l => l.Amount)
                          - x.Lines.Where(l => l.Type == (int)SalaryComponentType.Deduction).Sum(l => l.Amount)
            }).ToList();
            }
            catch (Exception)
            {
                return new List<SalaryStructureListDto>();
            }
        }

        #endregion

        #region Get By Id

        public async Task<SalaryStructureDto> GetByIdAsync(string id)
        {
            try
            {
            var entity = await _context.SalaryStructures
                .AsNoTracking()
                .Include(x => x.Employee)
                .Include(x => x.SourceTemplate)
                .Include(x => x.SalaryDetails)
                    .ThenInclude(d => d.SalaryComponent)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null)
                return null;

            var dto = new SalaryStructureDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                EmployeeName = entity.Employee != null ? entity.Employee.FirstName + " " + entity.Employee.LastName : "",
                EffectiveFrom = entity.EffectiveFrom,
                SourceTemplateId = entity.SourceTemplateId,
                SourceTemplateName = entity.SourceTemplate != null ? entity.SourceTemplate.Name : null,
                TenantId = entity.TenantId,
                CreatedBy = entity.CreatedBy,
                ModifiedBy = entity.ModifiedBy,
                ModifiedOn = entity.ModifiedOn,
                Details = entity.SalaryDetails.Select(d => new SalaryStructureLineDto
                {
                    Id = d.Id,
                    SalaryComponentId = d.SalaryComponentId,
                    SalaryComponentName = d.SalaryComponent != null ? d.SalaryComponent.Name : "",
                    ComponentType = d.SalaryComponent != null ? (int)d.SalaryComponent.ComponentType : 0,
                    Amount = d.Amount
                }).ToList()
            };

            dto.TotalEarnings = dto.Details.Where(l => l.ComponentType == (int)SalaryComponentType.Earning).Sum(l => l.Amount);
            dto.TotalDeductions = dto.Details.Where(l => l.ComponentType == (int)SalaryComponentType.Deduction).Sum(l => l.Amount);
            dto.NetSalary = dto.TotalEarnings - dto.TotalDeductions;

            return dto;
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion

        #region Create

        public async Task<string> CreateAsync(SalaryStructureDto dto)
        {
            try
            {
            var structure = new SalaryStructure
            {
                Id = IDManager.GetNewId(new SalaryStructure()),
                EmployeeId = dto.EmployeeId,
                EffectiveFrom = dto.EffectiveFrom,
                TenantId = dto.TenantId,
                CreatedBy = dto.CreatedBy
            };

            await _context.SalaryStructures.AddAsync(structure);

            foreach (var line in dto.Details.Where(l => !string.IsNullOrEmpty(l.SalaryComponentId)))
            {
                await _context.SalaryDetails.AddAsync(new SalaryDetail
                {
                    Id = IDManager.GetNewId(new SalaryDetail()),
                    SalaryStructureId = structure.Id,
                    SalaryComponentId = line.SalaryComponentId,
                    Amount = line.Amount,
                    TenantId = dto.TenantId,
                    CreatedBy = dto.CreatedBy
                });
            }

            await _context.SaveChangesAsync();
            return structure.Id;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        #endregion

        #region Update

        public async Task<string> UpdateAsync(string id, SalaryStructureDto dto)
        {
            try
            {
            var structure = await _context.SalaryStructures
                .Include(x => x.SalaryDetails)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (structure == null)
                return "Salary Structure Not Found";

            structure.EmployeeId = dto.EmployeeId;
            structure.EffectiveFrom = dto.EffectiveFrom;
            structure.ModifiedBy = dto.ModifiedBy;
            structure.ModifiedOn = dto.ModifiedOn ?? DateTime.UtcNow;

            // Replace detail lines
            _context.SalaryDetails.RemoveRange(structure.SalaryDetails);

            foreach (var line in dto.Details.Where(l => !string.IsNullOrEmpty(l.SalaryComponentId)))
            {
                await _context.SalaryDetails.AddAsync(new SalaryDetail
                {
                    Id = IDManager.GetNewId(new SalaryDetail()),
                    SalaryStructureId = structure.Id,
                    SalaryComponentId = line.SalaryComponentId,
                    Amount = line.Amount,
                    TenantId = dto.TenantId,
                    CreatedBy = dto.ModifiedBy ?? dto.CreatedBy
                });
            }

            await _context.SaveChangesAsync();
            return structure.Id;
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
            var structure = await _context.SalaryStructures
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (structure == null)
                return false;

            structure.IsDeleted = true;
            structure.ModifiedOn = DateTime.UtcNow;

            _context.SalaryStructures.Update(structure);
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
