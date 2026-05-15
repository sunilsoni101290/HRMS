using Application.DTOs;
using Application.DTOs.Attendances;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces.Attendances
{
    public interface IShiftService
    {
        // Get All
        Task<List<ShiftDto>> GetAllAsync();

        // Get By Id
        Task<ShiftDto?> GetByIdAsync(string id);

        // Create
        Task<ShiftDto> CreateAsync(ShiftDto dto);

        // Update
        Task<ShiftDto> UpdateAsync(ShiftDto dto);

        // Delete
        Task<bool> DeleteAsync(string id);
    }
}
