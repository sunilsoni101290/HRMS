using Application.DTOs.Masters;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces.Masters
{
    public interface IHolidayGroupService
    {
        // ======================================================
        // HOLIDAY GROUP
        // ======================================================

        Task<List<HolidayGroupDto>> GetAllGroupsAsync();

        Task<HolidayGroupDto?> GetGroupByIdAsync(string id);

        Task<HolidayGroupDto> CreateGroupAsync(HolidayGroupDto dto);

        Task<HolidayGroupDto?> UpdateGroupAsync(string id, HolidayGroupDto dto);

        Task<bool> DeleteGroupAsync(string id);

        // ======================================================
        // HOLIDAY GROUP DETAILS
        // ======================================================

        Task<List<HolidayGroupDetailDto>> GetAllDetailsAsync();

        Task<List<HolidayGroupDetailDto>> GetDetailsByGroupAsync(string holidayGroupId);

        Task<HolidayGroupDetailDto?> GetDetailByIdAsync(string id);

        Task<HolidayGroupDetailDto> CreateDetailAsync(HolidayGroupDetailDto dto);

        Task<HolidayGroupDetailDto?> UpdateDetailAsync(string id, HolidayGroupDetailDto dto);

        Task<bool> DeleteDetailAsync(string id);
    }

    //public interface IHolidayGroupService
    //{
    //    Task<List<HolidayGroupDetailDto>> GetHolidays(string tenantId, int year);
    //    Task<string> CreateHoliday(HolidayGroupDetailDto dto);
    //    Task<bool> DeleteHoliday(string id);

    //    Task<List<WeekOffDto>> GetWeekOffs(string tenantId);
    //    Task<WeekOffDto> AddWeekOff(WeekOffDto dto);
    //    Task<bool> RemoveWeekOff(string id);

    //    Task<bool> IsHoliday(DateTime date, string tenantId);
    //    Task<bool> IsWeekOff(DateTime date, string tenantId);
    //}
}
