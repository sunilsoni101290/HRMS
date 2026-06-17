using Application.DTOs.Masters;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces.Masters
{
    public interface ICountryService
    {
        Task<List<CountryDto>> GetAllAsync();

        Task<CountryDto?> GetByIdAsync(string id);

        Task<CountryDto> CreateAsync(CountryDto dto);

        Task<CountryDto?> UpdateAsync(string id, CountryDto dto);

        Task<bool> DeleteAsync(string id);
    }
}
