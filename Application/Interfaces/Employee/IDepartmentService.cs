using Application.DTOs.Employee;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces.Employee
{
    public interface IDepartmentService
    {
        Task<List<DepartmentListDto>> GetAllAsync();
        Task<DepartmentDto> GetByIdAsync(string id);
        Task<string> CreateAsync(DepartmentDto dto);
        Task<string> UpdateAsync(string id, DepartmentDto dto);
        Task<bool> DeleteAsync(string id);
    }
}
