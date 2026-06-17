using Application.DTOs.Attendances;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces.Attendances
{
    public interface IAttendanceProcessorService
    {
        Task<bool> ProcessAttendanceAsync();
        Task<AttendanceProcessDto>CalculateAttendanceAsync(string employeeId,DateTime date);
    }
}
