using Application.DTOs.Masters;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces.Masters
{
    public interface IStateService
    {
        Task<List<StateDto>> GetAllAsync();

        Task<StateDto?> GetByIdAsync(string id);

        Task<StateDto> CreateAsync(StateDto dto);

        Task<StateDto?> UpdateAsync(string id, StateDto dto);

        Task<bool> DeleteAsync(string id);
    }
}
