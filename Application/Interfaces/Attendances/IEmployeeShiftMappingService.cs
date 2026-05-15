using Application.DTOs.Attendances;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces.Attendances
{
    public interface IEmployeeShiftMappingService
    {
        Task<List<EmployeeShiftMappingDto>> GetAllAsync();

        Task<EmployeeShiftMappingDto?> GetByIdAsync(string id);

        Task<EmployeeShiftMappingDto> CreateAsync(EmployeeShiftMappingDto dto);

        Task<EmployeeShiftMappingDto> UpdateAsync(EmployeeShiftMappingDto dto);

        Task<bool> DeleteAsync(string id);
    }
}
