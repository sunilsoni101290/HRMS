using Application.DTOs.Employee;
using Application.Interfaces.Employee;
using Domain.Entities;
using Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;

namespace Application.Services.Employee
{
    public class DepartmentService : IDepartmentService
    {
        private readonly ApplicationDbContext _context;

        public DepartmentService(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Get All

        public async Task<List<DepartmentListDto>> GetAllAsync()
        {
            return await _context.Departments
                .Select(x => new DepartmentListDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Code = x.Code,

                    CompanyId = x.CompanyId,
                    CompanyName = x.Company != null
                        ? x.Company.Name
                        : "",
                    BranchId = x.BranchId,
                    BranchName = x.Branch != null
                        ? x.Branch.Name
                        : "",

                    ParentDepartmentId = x.ParentDepartmentId,
                    ParentDepartmentName = x.ParentDepartment != null
                        ? x.ParentDepartment.Name
                        : ""
                })
                .ToListAsync();
        }

        #endregion

        #region Get By Id

        public async Task<DepartmentDto> GetByIdAsync(string id)
        {
            var entity = await _context.Departments
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return null;

            return new DepartmentDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Code = entity.Code,

                // Multi Tenant
                TenantId = entity.TenantId,

                // Relations
                CompanyId = entity.CompanyId,
                BranchId = entity.BranchId,

                // Hierarchy
                ParentDepartmentId = entity.ParentDepartmentId
            };
        }

        #endregion

        #region Create

        public async Task<string> CreateAsync(DepartmentDto dto)
        {
            var entity = new Department
            {
                Id = IDManager.GetNewId(new Department()),

                Name = dto.Name,
                Code = dto.Code,

                // Multi Tenant
                TenantId = dto.TenantId,

                // Relations
                CompanyId = dto.CompanyId,
                BranchId = dto.BranchId,

                CreatedBy=dto.CreatedBy,
                ModifiedBy=dto.ModifiedBy,
                ModifiedOn=dto.ModifiedOn,

                // Hierarchy
                ParentDepartmentId = dto.ParentDepartmentId
            };

            await _context.Departments.AddAsync(entity);
            await _context.SaveChangesAsync();

            return entity.Id;
        }

        #endregion

        #region Update

        public async Task<string> UpdateAsync(string id, DepartmentDto dto)
        {
            var entity = await _context.Departments
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return "Department Not Found";

            entity.Name = dto.Name;
            entity.Code = dto.Code;

            // Multi Tenant
            entity.TenantId = dto.TenantId;

            // Relations
            entity.CompanyId = dto.CompanyId;
            entity.BranchId = dto.BranchId;

            // Hierarchy
            entity.ParentDepartmentId = dto.ParentDepartmentId;

            entity.CreatedBy = dto.CreatedBy;
            entity.ModifiedBy = dto.ModifiedBy;
            entity.ModifiedOn = dto.ModifiedOn;

            _context.Departments.Update(entity);
            await _context.SaveChangesAsync();

            return entity.Id;
        }

        #endregion

        #region Delete

        public async Task<bool> DeleteAsync(string id)
        {
            var entity = await _context.Departments
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return false;

            _context.Departments.Remove(entity);
            await _context.SaveChangesAsync();

            return true;
        }

        #endregion
    }
}
