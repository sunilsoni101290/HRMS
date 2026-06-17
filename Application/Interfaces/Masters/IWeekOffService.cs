using Application.DTOs.Masters;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces.Masters
{
    public interface IWeekOffService
    {
        Task<List<WeekOffDto>> GetAllAsync();

        Task<WeekOffDto?> GetByIdAsync(string id);

        Task<WeekOffDto> CreateAsync(WeekOffDto dto);

        Task<WeekOffDto?> UpdateAsync(string id, WeekOffDto dto);

        Task<bool> DeleteAsync(string id);

        Task<List<WeekOffDto>> GetWeekOffs(string tenantId);
        Task<WeekOffDto> AddWeekOff(WeekOffDto dto);
        Task<bool> RemoveWeekOff(string id);

        Task<bool> IsHoliday(DateTime date, string tenantId);
        Task<bool> IsWeekOff(DateTime date, string tenantId);
    }
}
