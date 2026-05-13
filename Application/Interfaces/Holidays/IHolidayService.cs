using Application.DTOs.Holidays;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces.Holidays
{
    public interface IHolidayService
    {
        Task<List<HolidayDto>> GetHolidays(string tenantId, int year);
        Task<HolidayDto> CreateHoliday(CreateHolidayDto dto);
        Task<bool> DeleteHoliday(string id);

        Task<List<WeekOffDto>> GetWeekOffs(string tenantId);
        Task<WeekOffDto> AddWeekOff(CreateWeekOffDto dto);
        Task<bool> RemoveWeekOff(string id);

        Task<bool> IsHoliday(DateTime date, string tenantId);
        Task<bool> IsWeekOff(DateTime date, string tenantId);
    }
}
