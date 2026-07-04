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
            try
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
            catch (Exception)
            {
                return new List<DepartmentListDto>();
            }
        }

        #endregion

        #region Get By Id

        public async Task<DepartmentDto> GetByIdAsync(string id)
        {
            try
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
            catch (Exception)
            {
                return null;
            }
        }

        #endregion

        #region Create

        public async Task<string> CreateAsync(DepartmentDto dto)
        {
            try
            {
            // Check duplicate Name
            bool isNameExists = await _context.Departments.AnyAsync(x =>
                x.TenantId == dto.TenantId &&
                x.Name.ToLower() == dto.Name.Trim().ToLower());

            if (isNameExists)
                throw new Exception("Department Name already exists.");

            // Check duplicate Code
            bool isCodeExists = await _context.Departments.AnyAsync(x =>
                x.TenantId == dto.TenantId &&
                x.Code.ToLower() == dto.Code.Trim().ToLower());

            if (isCodeExists)
                throw new Exception("Department Code already exists.");

            var entity = new Department
            {
                Id = IDManager.GetNewId(new Department()),

                Name = dto.Name.Trim(),
                Code = dto.Code.Trim(),

                // Multi Tenant
                TenantId = dto.TenantId,

                // Relations
                CompanyId = dto.CompanyId,
                BranchId = dto.BranchId,

                CreatedBy = dto.CreatedBy,
                ModifiedBy = dto.ModifiedBy,
                ModifiedOn = dto.ModifiedOn,

                // Hierarchy
                ParentDepartmentId = dto.ParentDepartmentId
            };

            await _context.Departments.AddAsync(entity);
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

        public async Task<string> UpdateAsync(string id, DepartmentDto dto)
        {
            try
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
            var entity = await _context.Departments
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return false;

            _context.Departments.Remove(entity);
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
