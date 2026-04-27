using Application.DTOs;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface IEmployeeService
    {
        Task<Employee> CreateAsync(EmployeeDto dto);
        Task<IEnumerable<Employee>> GetAllAsync();
        Task<Employee> GetByIdAsync(string id);
        Task<Employee> UpdateAsync(EmployeeDto dto);
        Task<bool> DeleteAsync(string id);
        //Task<object> GetHierarchyAsync(string id);
    }
}
