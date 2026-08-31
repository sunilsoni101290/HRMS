using Application.DTOs.Employee;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces.EmployeeInterface
{
    public interface IEmployeeService :IBaseService<EmployeeDto>
    {
        //Task<object> GetHierarchyAsync(string id);
        Task<List<EmployeeListDto>> GetByDepartmentAsync(string departmentId);
        Task<List<EmployeeListDto>> GetByDesignationAsync(string designationId);
        Task<List<EmployeeHierarchyDto>> GetHierarchyAsync(string tenantId);
        Task<PagedResult<EmployeeListDto>> SearchAsync(EmployeeSearchRequest request);
        Task<EmployeeListDto> GetByIdAsync(string id);
        Task<List<EmployeeListDto>> GetAllAsync();

        // Narrow, single-field update used by the "My Profile" self-service
        // photo upload - deliberately separate from the full UpdateAsync so
        // an employee posting their own new photo can never smuggle in
        // changes to any other field (salary, designation, etc.) even if
        // the request were tampered with.
        Task<bool> UpdatePhotoAsync(string id, string filePath);

        // Backs the Create/Edit form's live "Employee Code already exists"
        // check (blur-triggered AJAX) as well as the server-side re-check
        // inside CreateAsync/UpdateAsync - a single source of truth for what
        // "duplicate Employee Code" means, so the two can never disagree.
        // excludeEmployeeId must be passed (the employee's own Id) when
        // editing, so an employee's own unchanged code is never flagged.
        Task<bool> CheckEmployeeCodeExistsAsync(string employeeCode, string? excludeEmployeeId);

    }
}
