using Application.DTOs.Employee;
using Application.Interfaces.Employee;
using Domain.Entities;
using Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Employee
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
            return await _context.Designations
                .Select(x => new DesignationListDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Code = x.Code,

                    DepartmentName = x.Department != null
                        ? x.Department.Name
                        : "",

                    CompanyName = x.Company != null
                        ? x.Company.Name
                        : "",

                    BranchName = x.Branch != null
                        ? x.Branch.Name
                        : "",

                    Level = x.Level,

                    MinSalary = x.MinSalary,
                    MaxSalary = x.MaxSalary
                })
                .ToListAsync();
        }

        #endregion

        #region Get By Id

        public async Task<DesignationDto> GetByIdAsync(string id)
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

        #endregion

        #region Create

        public async Task<string> CreateAsync(DesignationDto dto)
        {
            var entity = new Designation
            {
                Id = Guid.NewGuid().ToString(),

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
                MaxSalary = dto.MaxSalary
            };

            await _context.Designations.AddAsync(entity);
            await _context.SaveChangesAsync();

            return entity.Id;
        }

        #endregion

        #region Update

        public async Task<string> UpdateAsync(string id, DesignationDto dto)
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

            _context.Designations.Update(entity);
            await _context.SaveChangesAsync();

            return entity.Id;
        }

        #endregion

        #region Delete

        public async Task<bool> DeleteAsync(string id)
        {
            var entity = await _context.Designations
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return false;

            _context.Designations.Remove(entity);
            await _context.SaveChangesAsync();

            return true;
        }

        #endregion
    }
}
