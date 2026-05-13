using Application.DTOs.Employee;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces.Employee
{
    public interface ICountryService
    {
        Task<List<CountryListDto>> GetAllAsync();
        Task<CountryDto> GetByIdAsync(string id);
        Task<string> CreateAsync(CountryDto dto);
        Task<string> UpdateAsync(string id, CountryDto dto);
        Task<bool> DeleteAsync(string id);
    }
}
