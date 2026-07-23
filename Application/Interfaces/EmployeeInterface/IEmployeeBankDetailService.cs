using Application.DTOs.Employee;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces.EmployeeInterface
{
    public interface IEmployeeBankDetailService
    {
        Task<List<EmployeeBankDetailDto>> GetAllAsync(string tenantId, string? search);

        Task<EmployeeBankDetailDto> GetByIdAsync(string id, string tenantId);

        Task<List<EmployeeBankDetailDto>> GetByEmployeeIdAsync(string employeeId, string tenantId);

        Task<EmployeeBankDetailDto> CreateAsync(EmployeeBankDetailDto dto, string tenantId, string actingUserId);

        Task<EmployeeBankDetailDto> UpdateAsync(string id, EmployeeBankDetailDto dto, string tenantId, string actingUserId);

        Task<bool> DeleteAsync(string id, string tenantId);

        Task<bool> SetPrimaryAsync(string id, string tenantId, string actingUserId);
    }
}
