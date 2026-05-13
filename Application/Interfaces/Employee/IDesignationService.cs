using Application.DTOs.Employee;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces.Employee
{
    public interface IDesignationService
    {
        Task<List<DesignationListDto>> GetAllAsync();
        Task<DesignationDto> GetByIdAsync(string id);
        Task<string> CreateAsync(DesignationDto dto);
        Task<string> UpdateAsync(string id, DesignationDto dto);
        Task<bool> DeleteAsync(string id);
    }
}
