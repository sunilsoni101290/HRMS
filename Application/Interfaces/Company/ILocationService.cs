using Application.DTOs.Company;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces.Company
{
    public interface ILocationService
    {
        Task<List<LocationDto>> GetAllAsync();

        Task<List<LocationDto>> GetByBranchAsync(string branchId);

        Task<LocationDto?> GetByIdAsync(string id);

        Task<LocationDto> CreateAsync(LocationDto dto);

        Task<LocationDto?> UpdateAsync(string id, LocationDto dto);

        Task<bool> DeleteAsync(string id);
    }
}
