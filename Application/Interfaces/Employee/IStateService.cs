using Application.DTOs.Employee;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces.Employee
{
    public interface IStateService
    {
        Task<List<StateListDto>> GetAllAsync();
        Task<StateDto> GetByIdAsync(string id);
        Task<string> CreateAsync(StateDto dto);
        Task<string> UpdateAsync(string id, StateDto dto);
        Task<bool> DeleteAsync(string id);
    }
}
