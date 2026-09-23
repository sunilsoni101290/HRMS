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

        // Additive: returns the concrete calendar dates in `year`/`month`
        // that are a week-off for `tenantId`, resolving BOTH the legacy
        // "fixed weekday every week" pattern and the new "Nth weekday of
        // month" pattern (see Domain.Helper.WeekOffCalculator). This is the
        // computational core the task calls for - a caller that only needs
        // "does this pattern make Sept 2026 have week-offs on the 12th and
        // 26th" (the "2nd/4th Saturday" example) uses this.
        Task<List<DateTime>> GetWeekOffDatesForMonth(int year, int month, string tenantId);
    }
}
