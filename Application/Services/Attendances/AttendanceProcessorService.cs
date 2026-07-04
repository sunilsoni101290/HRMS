using Application.DTOs.Attendances;
using Application.Interfaces.Attendances;
using Domain.Entities;
using Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;
using static Domain.Enums.EnumExtensions;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Attendances
{
    public class AttendanceProcessorService
    : IAttendanceProcessorService
    {
        private readonly ApplicationDbContext _db;

        public AttendanceProcessorService(
            ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<bool>
            ProcessAttendanceAsync()
        {
            try
            {
            var rawLogs =
                await _db.BiometricAttendanceLogs
                .Where(x => !x.IsProcessed)
                .OrderBy(x => x.PunchTime)
                .ToListAsync();

            foreach (var raw in rawLogs)
            {
                var mapping =
                    await _db
                    .EmployeeBiometricMappings
                    .FirstOrDefaultAsync(x =>
                        x.BiometricEmployeeCode ==
                        raw.EmployeeCode);

                if (mapping == null)
                    continue;

                var employee =
                    await _db.Employees
                    .FirstOrDefaultAsync(x =>
                        x.Id == mapping.EmployeeId);

                if (employee == null)
                    continue;

                var date = raw.PunchTime.Date;

                var attendance =
                    await _db.Attendances
                    .FirstOrDefaultAsync(x =>
                        x.EmployeeId ==
                        employee.Id &&
                        x.Date == date);

                if (attendance == null)
                {
                    attendance = new Attendance
                    {
                        TenantId = employee.TenantId,
                        CompanyId = employee.CompanyId,
                        BranchId = employee.BranchId,
                        EmployeeId = employee.Id,
                        Date = date,
                        ShiftId = employee.ShiftId,
                        FirstIn = raw.PunchTime,
                        Status = AttendanceStatus.Present,
                        IsBiometricAttendance = true,
                        SourceDeviceId =
                            raw.DeviceId
                    };

                    _db.Attendances.Add(attendance);

                    await _db.SaveChangesAsync();
                }
                else
                {
                    attendance.LastOut =
                        raw.PunchTime;
                }

                _db.AttendanceLogs.Add(
                    new AttendanceLog
                    {
                        AttendanceId =
                            attendance.Id,

                        EmployeeId =
                            employee.Id,

                        PunchTime =
                            raw.PunchTime,

                        PunchType =
                            raw.PunchType,

                        DeviceId =
                            raw.DeviceId,

                        BiometricCode =
                            raw.EmployeeCode
                    });

                raw.IsProcessed = true;
            }

            await _db.SaveChangesAsync();

            return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<AttendanceProcessDto>
            CalculateAttendanceAsync(
                string employeeId,
                DateTime date)
        {
            try
            {
            var attendance =
                await _db.Attendances
                .FirstOrDefaultAsync(x =>
                    x.EmployeeId == employeeId &&
                    x.Date == date);

            if (attendance == null)
                return null;

            return new AttendanceProcessDto
            {
                EmployeeId = attendance.EmployeeId,
                AttendanceDate = attendance.Date,
                FirstIn = attendance.FirstIn,
                LastOut = attendance.LastOut,
                WorkingHours =
                    attendance.TotalWorkingHours,
                BreakHours =
                    attendance.BreakHours,
                OvertimeHours =
                    attendance.OvertimeHours,
                Status = attendance.Status
            };
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
