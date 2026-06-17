using Application.DTOs.Masters;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces.Masters
{
    public interface ICityService
    {
        Task<List<CityDto>> GetAllAsync();

        Task<CityDto?> GetByIdAsync(string id);

        Task<CityDto> CreateAsync(CityDto dto);

        Task<CityDto?> UpdateAsync(string id, CityDto dto);

        Task<bool> DeleteAsync(string id);
    }
}
