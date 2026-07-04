using Application.DTOs.Employee;
using Domain.Entities;
using Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;
using Application.Interfaces.EmployeeInterface;

namespace Application.Services.EmployeeServices
{
    public class DesignationService : IDesignationService
    {
        private readonly ApplicationDbContext _context;

        public DesignationService(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Get All

        public async Task<List<DesignationListDto>> GetAllAsync()
        {
            try
            {
            return await _context.Designations
                .Select(x => new DesignationListDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Code = x.Code,

                    DepartmentId = x.DepartmentId,
                    DepartmentName = x.Department != null
                        ? x.Department.Name
                        : "",

                    ParentDesignationId = x.ParentDesignationId,
                    ParentDesignationName = x.ParentDesignation != null
                        ? x.ParentDesignation.Name
                        : "",

                    CompanyId = x.CompanyId,
                    CompanyName = x.Company != null
                        ? x.Company.Name
                        : "",
                    BranchId = x.BranchId,
                    BranchName = x.Branch != null
                        ? x.Branch.Name
                        : "",

                    Level = x.Level,

                    MinSalary = x.MinSalary,
                    MaxSalary = x.MaxSalary
                })
                .ToListAsync();
            }
            catch (Exception)
            {
                return new List<DesignationListDto>();
            }
        }

        #endregion

        #region Get By Id

        public async Task<DesignationDto> GetByIdAsync(string id)
        {
            try
            {
            var entity = await _context.Designations
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return null;

            return new DesignationDto
            {
                Id = entity.Id,

                Name = entity.Name,
                Code = entity.Code,

                // Multi Tenant
                TenantId = entity.TenantId,

                // Relations
                CompanyId = entity.CompanyId,
                BranchId = entity.BranchId,

                // Department
                DepartmentId = entity.DepartmentId,

                // Hierarchy
                ParentDesignationId = entity.ParentDesignationId,

                // Level
                Level = entity.Level,

                // Salary
                MinSalary = entity.MinSalary,
                MaxSalary = entity.MaxSalary
            };
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion

        #region Create

        public async Task<string> CreateAsync(DesignationDto dto)
        {
            try
            {
            var entity = new Designation
            {
                Id = IDManager.GetNewId(new Designation()),

                Name = dto.Name,
                Code = dto.Code,

                // Multi Tenant
                TenantId = dto.TenantId,

                // Relations
                CompanyId = dto.CompanyId,
                BranchId = dto.BranchId,

                // Department
                DepartmentId = dto.DepartmentId,

                // Hierarchy
                ParentDesignationId = dto.ParentDesignationId,

                // Level
                Level = dto.Level,

                // Salary
                MinSalary = dto.MinSalary,
                MaxSalary = dto.MaxSalary,

                CreatedBy= dto.CreatedBy
            };

            await _context.Designations.AddAsync(entity);
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

        public async Task<string> UpdateAsync(string id, DesignationDto dto)
        {
            try
            {
            var entity = await _context.Designations
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return "Designation Not Found";

            entity.Name = dto.Name;
            entity.Code = dto.Code;

            // Multi Tenant
            entity.TenantId = dto.TenantId;

            // Relations
            entity.CompanyId = dto.CompanyId;
            entity.BranchId = dto.BranchId;

            // Department
            entity.DepartmentId = dto.DepartmentId;

            // Hierarchy
            entity.ParentDesignationId = dto.ParentDesignationId;

            // Level
            entity.Level = dto.Level;

            // Salary
            entity.MinSalary = dto.MinSalary;
            entity.MaxSalary = dto.MaxSalary;

            entity.CreatedBy = dto.CreatedBy;
            entity.ModifiedBy = dto.ModifiedBy;
            entity.ModifiedOn = dto.ModifiedOn;

            _context.Designations.Update(entity);
            await _context.SaveChangesAsync();

            return entity.Id;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        #endregion

        #region Delete

        public async Task<bool> DeleteAsync(string id)
        {
            try
            {
            var entity = await _context.Designations
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return false;

            _context.Designations.Remove(entity);
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
