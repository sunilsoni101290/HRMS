using Application.DTOs.Attendance;
using Application.DTOs.Attendances;
using Application.Interfaces.Attendances;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Application.Services.Attendances
{
    public class AttendanceService : IAttendanceService
    {
        private readonly ApplicationDbContext _db;

        public AttendanceService(ApplicationDbContext db)
        {
            _db = db;
        }

        // =========================
        // 🟢 PUNCH IN
        // =========================
        public async Task<bool> PunchInAsync(PunchRequestDto dto)
        {
            var now = dto.PunchTime;

            var employee = await _db.Employees
                .Include(x => x.DefaultShift)
                .FirstOrDefaultAsync(x => x.Id == dto.EmployeeId);

            if (employee == null)
                throw new Exception("Employee not found");

            var shift = employee.DefaultShift;

            // 👉 Get shift date (important for night shift)
            var attendanceDate = GetAttendanceDate(now, shift);

            var attendance = await _db.Attendances
                .Include(x => x.Logs)
                .FirstOrDefaultAsync(x =>
                    x.EmployeeId == dto.EmployeeId &&
                    x.Date == attendanceDate);

            if (attendance == null)
            {
                attendance = new Attendance
                {
                    EmployeeId = dto.EmployeeId,
                    Date = attendanceDate,
                    ShiftId = shift.Id,
                    CompanyId = employee.CompanyId,
                    BranchId = employee.BranchId,
                    TenantId = employee.TenantId,
                    Logs = new List<AttendanceLog>()
                };

                _db.Attendances.Add(attendance);
            }

            // ❌ Prevent duplicate IN
            if (attendance.Logs.Any(x => x.PunchType == PunchType.In && x.PunchTime.Date == now.Date))
                throw new Exception("Already punched in");

            // ✅ Add Punch IN
            attendance.Logs.Add(new AttendanceLog
            {
                EmployeeId = dto.EmployeeId,
                PunchTime = now,
                PunchType = PunchType.In,
                DeviceId = dto.DeviceId,
                Location = dto.Location
            });

            attendance.FirstIn = attendance.Logs.Min(x => x.PunchTime);

            // ✅ Late Calculation
            var shiftStart = attendanceDate.Add(shift.StartTime);
            var allowedTime = shiftStart.AddMinutes(shift.GraceInMinutes);

            attendance.IsLate = now > allowedTime;

            await _db.SaveChangesAsync();
            return true;
        }

        // =========================
        // 🔴 PUNCH OUT
        // =========================
        public async Task<bool> PunchOutAsync(PunchRequestDto dto)
        {
            var now = dto.PunchTime;

            var attendance = await _db.Attendances
                .Include(x => x.Logs)
                .Include(x => x.Shift)
                .FirstOrDefaultAsync(x =>
                    x.EmployeeId == dto.EmployeeId &&
                    x.Date == GetAttendanceDate(now, x.Shift));

            if (attendance == null)
                throw new Exception("Punch in not found");

            // ❌ Prevent duplicate OUT
            if (attendance.Logs.LastOrDefault()?.PunchType == PunchType.Out)
                throw new Exception("Already punched out");

            // ✅ Add Punch OUT
            attendance.Logs.Add(new AttendanceLog
            {
                EmployeeId = dto.EmployeeId,
                PunchTime = now,
                PunchType = PunchType.Out,
                DeviceId = dto.DeviceId,
                Location = dto.Location
            });

            attendance.LastOut = now;

            // ✅ Calculate Working Hours
            CalculateWorkingHours(attendance);

            // ✅ Early Exit
            var shiftEnd = attendance.Date.Add(attendance.Shift.EndTime);
            var allowedOut = shiftEnd.AddMinutes(-attendance.Shift.GraceOutMinutes);

            attendance.IsEarlyExit = now < allowedOut;

            // ✅ Status
            attendance.Status = GetAttendanceStatus(attendance);

            await _db.SaveChangesAsync();
            return true;
        }

        // =========================
        // 📊 MONTHLY
        // =========================
        public async Task<List<Attendance>> GetMonthlyAsync(string employeeId, int month, int year)
        {
            return await _db.Attendances
                .Where(x => x.EmployeeId == employeeId &&
                            x.Date.Month == month &&
                            x.Date.Year == year)
                .OrderBy(x => x.Date)
                .ToListAsync();
        }

        public async Task ProcessMonthlyAttendance(int year, int month)
        {
            var attendances = await _db.Attendances
                .Include(x => x.Shift)
                .Where(x => x.Date.Year == year && x.Date.Month == month)
                .ToListAsync();

            foreach (var att in attendances)
            {
                if (att.Status == AttendanceStatus.None) // Not calculated
                {
                    CalculateWorkingHours(att);
                    att.Status = GetAttendanceStatus(att);
                }
            }

            await _db.SaveChangesAsync();
        }

        #region Private function
        private DateTime GetAttendanceDate(DateTime punchTime, Shift? shift)
        {
            if (shift is null)
                return punchTime.Date;

            if (!shift.IsNightShift)
                return punchTime.Date;

            DateTime shiftStartToday = punchTime.Date.Add(shift.StartTime);

            return punchTime < shiftStartToday
                ? punchTime.Date.AddDays(-1)
                : punchTime.Date;
        }
        private AttendanceStatus GetAttendanceStatus(Attendance attendance)
        {
            var totalMinutes = attendance.TotalWorkingHours * 60;

            if (totalMinutes >= attendance.Shift.FullDayMinutes)
                return AttendanceStatus.Present;

            if (totalMinutes >= attendance.Shift.HalfDayMinutes)
                return AttendanceStatus.HalfDay;

            return AttendanceStatus.Absent;
        }

        //Showing "Currently Working / Not Working"
        public async Task<object> GetLiveStatus(string employeeId)
        {
            var today = DateTime.UtcNow.Date;

            var attendance = await _db.Attendances
                .Include(x => x.Logs)
                .FirstOrDefaultAsync(x =>
                    x.EmployeeId == employeeId &&
                    x.Date == today);

            if (attendance == null || !attendance.Logs.Any())
            {
                return new
                {
                    IsPunchedIn = false,
                    LastPunch = (DateTime?)null
                };
            }

            var lastLog = attendance.Logs
                .OrderByDescending(x => x.PunchTime)
                .First();

            return new
            {
                IsPunchedIn = lastLog.PunchType == PunchType.In,
                LastPunch = lastLog.PunchTime
            };
        }

        private void CalculateWorkingHours(Attendance attendance)
        {
            var logs = attendance.Logs.OrderBy(x => x.PunchTime).ToList();

            TimeSpan total = TimeSpan.Zero;

            for (int i = 0; i < logs.Count - 1; i++)
            {
                if (logs[i].PunchType == PunchType.In &&
                    logs[i + 1].PunchType == PunchType.Out)
                {
                    total += logs[i + 1].PunchTime - logs[i].PunchTime;
                }
            }

            attendance.TotalWorkingHours = (decimal)total.TotalHours;

            // Overtime
            var shiftMinutes = attendance.Shift.FullDayMinutes;

            if (total.TotalMinutes > shiftMinutes)
            {
                attendance.OvertimeHours =
                    (decimal)(total.TotalMinutes - shiftMinutes) / 60;
            }
        }
        #endregion

        #region Get All

        public async Task<List<AttendanceLogDto>> GetAllAsync()
        {
            return await _db.AttendanceLogs
                .Include(x => x.Attendance)
                .OrderByDescending(x => x.PunchTime)
                .Select(x => new AttendanceLogDto
                {
                    Id = x.Id,

                    AttendanceId = x.AttendanceId,

                    EmployeeId = x.EmployeeId,

                    PunchTime = x.PunchTime,

                    PunchType = EnumHelper.GetEnumName<PunchType>((int)x.PunchType),

                    DeviceId = x.DeviceId,

                    Location = x.Location,

                    IsManual = x.IsManual,

                    CreatedDate = x.CreatedOn
                })
                .ToListAsync();
        }

        #endregion

        #region Get By Id

        public async Task<AttendanceLogDto?> GetByIdAsync(string id)
        {
            return await _db.AttendanceLogs
                .Where(x => x.Id == id)
                .Select(x => new AttendanceLogDto
                {
                    Id = x.Id,

                    AttendanceId = x.AttendanceId,

                    EmployeeId = x.EmployeeId,

                    PunchTime = x.PunchTime,

                    PunchType = EnumHelper.GetEnumName<PunchType>((int)x.PunchType),

                    DeviceId = x.DeviceId,

                    Location = x.Location,

                    IsManual = x.IsManual,

                    CreatedDate = x.CreatedOn
                })
                .FirstOrDefaultAsync();
        }

        #endregion
    }
}
