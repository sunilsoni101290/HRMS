using Application.DTOs.Attendance;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces.Attendances
{
    public interface IAttendanceService
    {
        Task<bool> PunchInAsync(PunchRequestDto dto);
        Task<bool> PunchOutAsync(PunchRequestDto dto);
        Task ProcessMonthlyAttendance(int year, int month);
        Task<List<Attendance>> GetMonthlyAsync(string employeeId, int month, int year);
        Task<object> GetLiveStatus(string employeeId);
    }
}
