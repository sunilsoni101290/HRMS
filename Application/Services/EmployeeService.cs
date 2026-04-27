using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure;
using Infrastructure.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Application.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantService _tenantService;
        private readonly ISequenceService _sequenceService;
        private readonly IFinancialYearService _financialYearService;

        public EmployeeService(ApplicationDbContext context, ITenantService tenantService, ISequenceService sequenceService,
            IFinancialYearService financialYearService)
        {
            _context = context;
            _tenantService = tenantService;
            _sequenceService = sequenceService;
            _financialYearService = financialYearService;
        }

        // ✅ CREATE
        public async Task<Employee> CreateAsync(EmployeeDto dto)
        {
            var tenantId = _tenantService.GetTenantId();

            // 🔒 VALIDATIONS
            if (await _context.Employees.AnyAsync(x => x.Email == dto.Email && x.TenantId == tenantId))
                throw new Exception("Email already exists");

            // Get Department & Designation
            var department = await _context.Departments
                .FirstOrDefaultAsync(x => x.Id == dto.DepartmentId && x.TenantId == tenantId);

            if (department == null)
                throw new Exception("Invalid Department");

            var designation = await _context.Designations
                .FirstOrDefaultAsync(x => x.Id == dto.DesignationId && x.TenantId == tenantId);

            if (designation == null)
                throw new Exception("Invalid Designation");

            // 🔢 Generate Employee Code
            var empCode = await _sequenceService.GetNextCodeSequenceAsync(_context,"EMP");
            var financialYear = await _financialYearService.GetActiveFinancialYearAsync();

            var employee = new Employee
            {
                Id = await _sequenceService.GetNextERPId("EMP", financialYear.Name),
                EmployeeCode = empCode,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                Phone = dto.Mobile,
                DepartmentId = dto.DepartmentId,
                DesignationId = dto.DesignationId,
                JoiningDate = dto.DateOfJoining,
                DateOfBirth = dto.DateOfBirth,
                ReportingManagerId = dto.ManagerId,
                IsActive = true,
                TenantId = tenantId
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            return employee;
        }

        // ✅ GET ALL
        public async Task<IEnumerable<Employee>> GetAllAsync()
        {
            return await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Designation)
                .Where(e => e.IsActive)
                .ToListAsync();
        }

        // ✅ GET BY ID
        public async Task<Employee> GetByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Invalid Id");

            var employee = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Designation)
                .Include(e => e.ReportingManager)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
                throw new Exception("Employee not found");

            return employee;
        }

        // ✅ UPDATE
        public async Task<Employee> UpdateAsync(EmployeeDto dto)
        {
            var tenantId = _tenantService.GetTenantId();

            var employee = await _context.Employees
                .FirstOrDefaultAsync(x => x.Id == dto.Id && x.TenantId == tenantId);

            if (employee == null)
                throw new Exception("Employee not found");

            // 🔒 VALIDATION (Email Unique)
            if (await _context.Employees.AnyAsync(x =>
                x.Email == dto.Email && x.Id != dto.Id && x.TenantId == tenantId))
            {
                throw new Exception("Email already exists");
            }

            // Update fields
            employee.FirstName = dto.FirstName;
            employee.LastName = dto.LastName;
            employee.Email = dto.Email;
            employee.Phone = dto.Mobile;
            employee.DepartmentId = dto.DepartmentId;
            employee.DesignationId = dto.DesignationId;
            employee.JoiningDate = dto.DateOfJoining;
            employee.DateOfBirth = dto.DateOfBirth;
            employee.ReportingManagerId = dto.ManagerId;

            if (dto.IsActive.HasValue)
                employee.IsActive = dto.IsActive.Value;

            await _context.SaveChangesAsync();

            return employee;
        }

        // ✅ DELETE (SOFT DELETE)
        public async Task<bool> DeleteAsync(string id)
        {
            var tenantId = _tenantService.GetTenantId();

            var employee = await _context.Employees
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);

            if (employee == null)
                throw new Exception("Employee not found");

            employee.IsActive = false;

            await _context.SaveChangesAsync();

            return true;
        }

        // ✅ HIERARCHY
        //public async Task<object> GetHierarchyAsync(string id)
        //{
        //    var employee = await _context.Employees
        //        .Include(e => e.ReportingManager)
        //        .FirstOrDefaultAsync(x => x.Id == id);

        //    if (employee == null)
        //        throw new Exception("Employee not found");

        //    return new
        //    {
        //        employee.Id,
        //        Name = employee.FirstName + " " + employee.LastName,
        //        ManagerId = employee.ReportingManagerId,
        //        Subordinates = employee.ReportingManager.Select(s => new
        //        {
        //            s.Id,
        //            Name = s.FirstName + " " + s.LastName
        //        })
        //    };
        //}
    }
}
