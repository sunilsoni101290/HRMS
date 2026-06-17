using Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces.Masters
{
    public interface IAppFeatureService
    {
        Task<List<AppFeatureDto>> GetAllAsync();

        Task<AppFeatureDto?> GetByIdAsync(string id);

        Task<AppFeatureDto> CreateAsync(AppFeatureDto dto);

        Task<AppFeatureDto?> UpdateAsync(AppFeatureDto dto);

        Task<bool> DeleteAsync(string id);
        Task<List<AppFeatureDto>> GetMenuAsync();

    }
}
