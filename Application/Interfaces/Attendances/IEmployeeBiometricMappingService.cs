using Application.DTOs.Attendances;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces.Attendances
{
    public interface IEmployeeBiometricMappingService
    {
        Task<List<EmployeeBiometricMappingDto>>GetAllAsync();

        Task<EmployeeBiometricMappingDto>CreateAsync(EmployeeBiometricMappingDto dto);
    }
}
