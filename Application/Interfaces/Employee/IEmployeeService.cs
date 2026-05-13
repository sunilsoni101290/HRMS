using Application.DTOs.Employee;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces.Employee
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

    }
}
