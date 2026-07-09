using Application.DTOs.Attendances;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces.Attendances
{
    public interface IEmployeeBiometricMappingService
    {
        Task<List<EmployeeBiometricMappingDto>>GetAllAsync();

        Task<EmployeeBiometricMappingDto?>GetByIdAsync(string id);

        Task<EmployeeBiometricMappingDto>CreateAsync(EmployeeBiometricMappingDto dto);

        Task<bool>UpdateAsync(EmployeeBiometricMappingDto dto);

        Task<bool>DeleteAsync(string id);
    }
}
