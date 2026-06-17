using Application.DTOs.Leaves;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces.Leaves
{
    public interface ILeaveTypeService
    {
        Task<List<LeaveTypeDto>> GetAllAsync();

        Task<LeaveTypeDto?> GetByIdAsync(string id);

        Task<LeaveTypeDto> CreateAsync(LeaveTypeDto dto);

        Task<LeaveTypeDto?> UpdateAsync(string id, LeaveTypeDto dto);

        Task<bool> DeleteAsync(string id);
    }
}
