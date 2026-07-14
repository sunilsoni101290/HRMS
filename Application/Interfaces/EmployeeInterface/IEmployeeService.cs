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

    }
}
