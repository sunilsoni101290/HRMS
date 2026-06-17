using Application.DTOs.Attendance;
using Application.DTOs.Attendances;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces.Attendances
{
    public interface IAttendanceService
    {
        #region Attendance logs
        Task<bool> PunchInAsync(PunchRequestDto dto);
        Task<bool> PunchOutAsync(PunchRequestDto dto);
        Task<bool> BreakInAsync(PunchRequestDto dto);
        Task<bool> BreakOutAsync(PunchRequestDto dto);
        Task<AttendanceCurrentStatusDto>GetCurrentStatusAsync(string employeeId);
        Task ProcessMonthlyAttendance(int year, int month);
        Task<List<Attendance>> GetMonthlyAsync(string employeeId, int month, int year);
        Task<object> GetLiveStatus(string employeeId);
        Task<List<AttendanceLogDto>> GetAllAsync();
        Task<AttendanceLogDto?> GetByIdAsync(string id);
        #endregion

        #region Attendance
        Task<List<AttendanceDto>> GetAllAttendanceListAsync();

        Task<AttendanceDto?> GetAttendanceByIdAsync(string id);

        Task<bool> CreateAsync(AttendanceDto dto);

        Task<bool> UpdateAsync(AttendanceDto dto);

        Task<bool> DeleteAsync(string id);
        #endregion
    }
}
