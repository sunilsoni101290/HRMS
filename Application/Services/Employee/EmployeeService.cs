using Application.DTOs.Employee;
using Application.Interfaces;
using Application.Interfaces.Employee;
using Application.Mappings;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services.Employee
{
    public class EmployeeService : BaseService, IEmployeeService
    {
        private readonly ITenantService _tenantService;
        private readonly ISequenceService _sequenceService;
        private readonly IFinancialYearService _financialYearService;

        public EmployeeService(ApplicationDbContext db, ITenantService tenantService, ISequenceService sequenceService,
            IFinancialYearService financialYearService) : base(db)
        {
            _tenantService = tenantService;
            _sequenceService = sequenceService;
            _financialYearService = financialYearService;
        }
        public async Task<PagedResult<EmployeeListDto>> SearchAsync(EmployeeSearchRequest request)
        {
            var query = _db.Employees
                .Where(x => !x.IsDeleted)
                .AsQueryable();

            // =========================
            // 🔍 SEARCH
            // =========================
            if (!string.IsNullOrEmpty(request.SearchText))
            {
                var search = request.SearchText.ToLower();

                query = query.Where(x =>
                    x.FirstName.ToLower().Contains(search) ||
                    x.LastName.ToLower().Contains(search) ||
                    x.Phone.Contains(search) ||
                    x.Email.ToLower().Contains(search)
                );
            }

            // =========================
            // 🎯 FILTERS
            // =========================
            if (!string.IsNullOrEmpty(request.DepartmentId))
                query = query.Where(x => x.DepartmentId == request.DepartmentId);

            if (!string.IsNullOrEmpty(request.DesignationId))
                query = query.Where(x => x.DesignationId == request.DesignationId);

            if (!string.IsNullOrEmpty(request.CompanyId))
                query = query.Where(x => x.CompanyId == request.CompanyId);

            // =========================
            // 🔄 SORTING
            // =========================
            query = request.SortDirection?.ToLower() == "asc"
                ? query.OrderBy(x => EF.Property<object>(x, request.SortBy))
                : query.OrderByDescending(x => EF.Property<object>(x, request.SortBy));

            // =========================
            // 📊 TOTAL COUNT
            // =========================
            var totalRecords = await query.CountAsync();

            // =========================
            // 📄 PAGINATION
            // =========================
            var data = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(x => EmployeeMapper.ToDto(x))
                .ToListAsync();

            return new PagedResult<EmployeeListDto>
            {
                TotalRecords = totalRecords,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                Data = data
            };
        }
        public async Task<string> CreateAsync(EmployeeDto dto)
        {
            // 🔥 Duplicate Check
            if (_db.Employees.Any(x => x.Phone == dto.Phone && !x.IsDeleted))
                throw new Exception("Employee already exists with same phone");

            // 🔥 Generate Employee Code (Tenant + Financial Year)
            var employeeCode = await _sequenceService
                .GetNextERPIdAsync("EMP", dto.TenantId);

            var entity = EmployeeMapper.ToEntity(dto);

            // 🔥 Assign Generated Code
            entity.EmployeeCode = employeeCode;

            _db.Employees.Add(entity);

            await SaveAsync();

            return entity.Id;
        }

        // ==============================
        // 🔹 DELETE (SOFT DELETE)
        // ==============================
        public async Task<bool> DeleteMultipleAsync(List<string> ids)
        {
            if (ids == null || !ids.Any())
                return false;

            var employees = await _db.Employees
                .Where(x => ids.Contains(x.Id) && !x.IsDeleted)
                .ToListAsync();

            if (!employees.Any())
                return false;

            foreach (var emp in employees)
            {
                emp.IsDeleted = true;
                emp.ModifiedOn = DateTime.UtcNow;
                emp.ModifiedBy = "System";
            }

            await SaveAsync();
            return true;
        }

        // ==============================
        // 🔹 GET ALL
        // ==============================
        public async Task<List<EmployeeListDto>> GetAllAsync()
        {
            var employees = await _db.Employees
                .Include(x => x.Company)
                .Include(x => x.Branch)
                .Include(x => x.Department)
                .Include(x => x.Designation)
                .Include(x => x.ReportingManager)
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .ToListAsync();

            return employees
                .Select(EmployeeMapper.ToDto)
                .ToList();
        }

        // ==============================
        // 🔹 FILTER BY DEPARTMENT
        // ==============================
        public async Task<List<EmployeeListDto>> GetByDepartmentAsync(string departmentId)
        {
            return await _db.Employees
                .Where(x => x.DepartmentId == departmentId && !x.IsDeleted)
                .Select(x => EmployeeMapper.ToDto(x))
                .ToListAsync();
        }

        // ==============================
        // 🔹 FILTER BY DESIGNATION
        // ==============================
        public async Task<List<EmployeeListDto>> GetByDesignationAsync(string designationId)
        {
            return await _db.Employees
                .Where(x => x.DesignationId == designationId && !x.IsDeleted)
                .Select(x => EmployeeMapper.ToDto(x))
                .ToListAsync();
        }

        // ==============================
        // 🔹 GET BY ID
        // ==============================
        public async Task<EmployeeListDto> GetByIdAsync(string id)
        {
            var entity = await _db.Employees.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null)
                return null;

            return EmployeeMapper.ToDto(entity);
        }

        // ==============================
        // 🔹 UPDATE
        // ==============================
        public async Task<bool> UpdateAsync(EmployeeDto dto)
        {
            var entity = await _db.Employees.FindAsync(dto.Id);

            if (entity == null)
                throw new Exception("Employee not found");

            EmployeeMapper.UpdateEntity(entity, dto);

            await SaveAsync();

            return true;
        }




        // ✅ HIERARCHY
        public async Task<List<EmployeeHierarchyDto>> GetHierarchyAsync(string tenantId)
        {
            var employees = await _db.Employees
                .Where(x => x.TenantId == tenantId && !x.IsDeleted)
                .Select(x => new
                {
                    x.Id,
                    x.FirstName,
                    x.LastName,
                    x.ReportingManagerId,
                    Designation = x.Designation.Name
                })
                .ToListAsync();

            // 🔹 Root employees (no manager)
            var roots = employees
                .Where(x => string.IsNullOrEmpty(x.ReportingManagerId))
                .ToList();

            List<EmployeeHierarchyDto> BuildTree(string parentId)
            {
                return employees
                    .Where(x => x.ReportingManagerId == parentId)
                    .Select(x => new EmployeeHierarchyDto
                    {
                        Id = x.Id,
                        Name = x.FirstName + " " + x.LastName,
                        Designation = x.Designation,
                        Children = BuildTree(x.Id)
                    })
                    .ToList();
            }

            var hierarchy = roots.Select(x => new EmployeeHierarchyDto
            {
                Id = x.Id,
                Name = x.FirstName + " " + x.LastName,
                Designation = x.Designation,
                Children = BuildTree(x.Id)
            }).ToList();

            return hierarchy;
        }
    }
}
