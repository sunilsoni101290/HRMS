using Application.DTOs.Employee;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces.Employee
{
    public interface ICityService
    {
        Task<List<CityListDto>> GetAllAsync();
        Task<CityDto> GetByIdAsync(string id);
        Task<string> CreateAsync(CityDto dto);
        Task<string> UpdateAsync(string id, CityDto dto);
        Task<bool> DeleteAsync(string id);
    }
}
