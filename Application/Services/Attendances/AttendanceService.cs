using Application.DTOs.Attendance;
using Application.DTOs.Attendances;
using Application.Interfaces.Attendances;
using Domain.Entities;
using Domain.Enums;
using Domain.Helper;
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
            try
            {
            var now = dto.PunchTime;

            // =====================================================
            // STEP 1 : GET EMPLOYEE
            // =====================================================

            var employee = await _db.Employees
                .Include(x => x.DefaultShift)
                .FirstOrDefaultAsync(x =>
                    x.Id == dto.EmployeeId);

            if (employee == null)
                throw new Exception("Employee not found");

            // =====================================================
            // STEP 2 : FIND ACTIVE SHIFT
            // =====================================================

            Shift shift = null;

            // FIRST TRY :
            // EmployeeShiftMapping
            var shiftMapping = await _db.EmployeeShiftMappings
                .Include(x => x.Shift)
                .FirstOrDefaultAsync(x =>
                    x.EmployeeId == dto.EmployeeId &&
                    x.EffectiveFrom.Date <= now.Date &&
                    (
                        x.EffectiveTo == null ||
                        x.EffectiveTo.Value.Date >= now.Date
                    ));

            // =====================================================
            // STEP 3 : PRIORITY SHIFT LOGIC
            // =====================================================

            if (shiftMapping != null)
            {
                // TEMPORARY / ROSTER SHIFT
                shift = shiftMapping.Shift;
            }
            else
            {
                // DEFAULT SHIFT
                shift = employee.DefaultShift;
            }

            if (shift == null)
                throw new Exception("Shift not assigned");

            // =====================================================
            // STEP 4 : GET ATTENDANCE DATE
            // =====================================================

            var attendanceDate =
                GetAttendanceDate(now, shift);

            // =====================================================
            // STEP 5 : FIND EXISTING ATTENDANCE
            // =====================================================

            var attendance = await _db.Attendances
                .Include(x => x.Logs.OrderBy(l => l.PunchTime))
                .FirstOrDefaultAsync(x =>
                    x.EmployeeId == dto.EmployeeId &&
                    x.ShiftId == shift.Id &&
                    x.Date >= attendanceDate.Date &&
                    x.Date < attendanceDate.Date.AddDays(1));

            // =====================================================
            // STEP 6 : CREATE NEW ATTENDANCE
            // =====================================================

            if (attendance == null)
            {
                attendance = new Attendance
                {
                    Id = IDManager.GetNewId(new Attendance()),

                    TenantId = employee.TenantId,

                    CompanyId = employee.CompanyId,

                    BranchId = employee.BranchId,

                    EmployeeId = employee.Id,

                    ShiftId = shift.Id,

                    Date = attendanceDate.Date,

                    FirstIn = now,

                    Status = AttendanceStatus.Present,

                    IsManualEntry = dto.IsManual,

                    CreatedBy = dto.CreatedBy,

                    Logs = new List<AttendanceLog>()
                };

                await _db.Attendances.AddAsync(attendance);
            }

            // =====================================================
            // STEP 7 : PREVENT DUPLICATE IN
            // =====================================================

            var lastLog = attendance.Logs
                .OrderBy(x => x.PunchTime)
                .LastOrDefault();

            if (lastLog != null &&
                lastLog.PunchType == PunchType.In)
            {
                throw new Exception("Already punched in");
            }

            // =====================================================
            // STEP 8 : ADD IN LOG
            // =====================================================

            attendance.Logs.Add(new AttendanceLog
            {
                Id = IDManager.GetNewId(new AttendanceLog()),

                EmployeeId = employee.Id,

                AttendanceId = attendance.Id,

                PunchTime = now,

                PunchType = PunchType.In,

                IsManual = dto.IsManual,

                DeviceType = dto.DeviceType,
                OS = dto.OS,
                Browser = dto.Browser,
                Version = dto.Version,
                Location = dto.Location,

                CreatedBy = dto.CreatedBy,
                TenantId = employee.TenantId
            });

            // =====================================================
            // STEP 9 : LATE CHECK
            // =====================================================

            var shiftStartDateTime =
                attendanceDate.Date.Add(shift.StartTime);

            // NIGHT SHIFT
            if (shift.IsNightShift &&
                shift.EndTime < shift.StartTime)
            {
                if (now.TimeOfDay < shift.EndTime)
                {
                    shiftStartDateTime =
                        shiftStartDateTime.AddDays(-1);
                }
            }

            var allowedIn =
                shiftStartDateTime.AddMinutes(
                    shift.GraceInMinutes);

            attendance.IsLate = now > allowedIn;

            // =====================================================
            // STEP 10 : SAVE
            // =====================================================

            await _db.SaveChangesAsync();

            return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        //public async Task<bool> PunchInAsync(PunchRequestDto dto)
        //{
        //    var now = dto.PunchTime;

        //    var employee = await _db.Employees
        //        .Include(x => x.DefaultShift)
        //        .FirstOrDefaultAsync(x => x.Id == dto.EmployeeId);

        //    if (employee == null)
        //        throw new Exception("Employee not found");

        //    var shift = employee.DefaultShift;

        //    // 👉 Get shift date (important for night shift)
        //    var attendanceDate = GetAttendanceDate(now, shift);

        //    var attendance = await _db.Attendances
        //        .Include(x => x.Logs)
        //        .FirstOrDefaultAsync(x =>
        //            x.EmployeeId == dto.EmployeeId &&
        //            x.Date == attendanceDate);

        //    if (attendance == null)
        //    {
        //        attendance = new Attendance
        //        {
        //            Id=IDManager.GetNewId(new Attendance()),
        //            EmployeeId = dto.EmployeeId,
        //            Date = attendanceDate,
        //            ShiftId = shift.Id,
        //            CompanyId = employee.CompanyId,
        //            BranchId = employee.BranchId,
        //            TenantId = employee.TenantId,
        //            CreatedBy= dto.CreatedBy,
        //            Logs = new List<AttendanceLog>()
        //        };

        //        _db.Attendances.Add(attendance);
        //    }

        //    // ❌ Prevent duplicate IN
        //    if (attendance.Logs.Any(x => x.PunchType == PunchType.In && x.PunchTime.Date == now.Date))
        //        throw new Exception("Already punched in");

        //    // ✅ Add Punch IN
        //    attendance.Logs.Add(new AttendanceLog
        //    {
        //        Id = IDManager.GetNewId(new AttendanceLog()),
        //        EmployeeId = dto.EmployeeId,
        //        PunchTime = now,
        //        PunchType = PunchType.In,
        //        DeviceType = dto.DeviceType,
        //        OS = dto.OS,
        //        Browser = dto.Browser,
        //        Version = dto.Version,
        //        IsManual =dto.IsManual,
        //        Location = dto.Location,
        //        CreatedBy = dto.CreatedBy
        //    });

        //    attendance.FirstIn = attendance.Logs.Min(x => x.PunchTime);

        //    // ✅ Late Calculation
        //    var shiftStart = attendanceDate.Add(shift.StartTime);
        //    var allowedTime = shiftStart.AddMinutes(shift.GraceInMinutes);

        //    attendance.IsLate = now > allowedTime;

        //    await _db.SaveChangesAsync();
        //    return true;
        //}

        // =========================
        // 🔴 PUNCH OUT
        // =========================
        //public async Task<bool> PunchOutAsync(PunchRequestDto dto)
        //{
        //    var now = dto.PunchTime;

        //    var attendances = await _db.Attendances
        //    .Include(x => x.Logs)
        //    .Include(x => x.Shift)
        //    .Where(x => x.EmployeeId == dto.EmployeeId)
        //    .ToListAsync();

        //    var attendance = attendances
        //        .FirstOrDefault(x =>
        //            x.Date == GetAttendanceDate(now, x.Shift));

        //    if (attendance == null)
        //        throw new Exception("Punch in not found");

        //    // ❌ Prevent duplicate OUT
        //    if (attendance.Logs.LastOrDefault()?.PunchType == PunchType.Out)
        //        throw new Exception("Already punched out");

        //    // ✅ Add Punch OUT
        //    attendance.Logs.Add(new AttendanceLog
        //    {
        //        Id = IDManager.GetNewId(new AttendanceLog()),
        //        EmployeeId = dto.EmployeeId,
        //        PunchTime = now,
        //        PunchType = PunchType.Out,
        //        IsManual =dto.IsManual,
        //        DeviceType = dto.DeviceType,
        //        OS = dto.OS,
        //        Browser = dto.Browser,
        //        Version = dto.Version,
        //        Location = dto.Location,
        //        CreatedBy = dto.CreatedBy,
        //        ModifiedBy= dto.ModifiedBy,
        //        ModifiedOn= dto.ModifiedOn,
        //    });

        //    attendance.LastOut = now;

        //    // ✅ Calculate Working Hours
        //    CalculateWorkingHours(attendance);

        //    // ✅ Early Exit
        //    var shiftEnd = attendance.Date.Add(attendance.Shift.EndTime);
        //    var allowedOut = shiftEnd.AddMinutes(-attendance.Shift.GraceOutMinutes);

        //    attendance.IsEarlyExit = now < allowedOut;

        //    // ✅ Status
        //    attendance.Status = GetAttendanceStatus(attendance);

        //    await _db.SaveChangesAsync();
        //    return true;
        //}

        public async Task<bool> PunchOutAsync(PunchRequestDto dto)
        {
            try
            {
            var now = dto.PunchTime;

            // =====================================================
            // STEP 1 : GET EMPLOYEE
            // =====================================================

            var employee = await _db.Employees
                .Include(x => x.DefaultShift)
                .FirstOrDefaultAsync(x =>
                    x.Id == dto.EmployeeId);

            if (employee == null)
                throw new Exception("Employee not found");

            // =====================================================
            // STEP 2 : FIND ACTIVE SHIFT
            // =====================================================

            Shift shift = null;

            // FIRST PRIORITY :
            // Employee Shift Mapping
            var shiftMapping = await _db.EmployeeShiftMappings
                .Include(x => x.Shift)
                .FirstOrDefaultAsync(x =>
                    x.EmployeeId == dto.EmployeeId &&
                    x.EffectiveFrom.Date <= now.Date &&
                    (
                        x.EffectiveTo == null ||
                        x.EffectiveTo.Value.Date >= now.Date
                    ));

            // =====================================================
            // STEP 3 : SHIFT PRIORITY
            // =====================================================

            if (shiftMapping != null)
            {
                shift = shiftMapping.Shift;
            }
            else
            {
                shift = employee.DefaultShift;
            }

            if (shift == null)
                throw new Exception("Shift not assigned");

            // =====================================================
            // STEP 4 : GET ATTENDANCE DATE
            // =====================================================

            var attendanceDate =
                GetAttendanceDate(now, shift);

            // =====================================================
            // STEP 5 : FIND ATTENDANCE
            // =====================================================

            var attendance = await _db.Attendances
                .Include(x => x.Logs.OrderBy(l => l.PunchTime))
                .Include(x => x.Shift)
                .FirstOrDefaultAsync(x =>
                    x.EmployeeId == dto.EmployeeId &&
                    x.ShiftId == shift.Id &&
                    x.Date >= attendanceDate.Date &&
                    x.Date < attendanceDate.Date.AddDays(1));

            if (attendance == null)
                throw new Exception("Punch in not found");

            // =====================================================
            // STEP 6 : PREVENT DUPLICATE OUT
            // =====================================================

            var lastLog = attendance.Logs
                .OrderBy(x => x.PunchTime)
                .LastOrDefault();

            if (lastLog == null)
                throw new Exception("Punch in not found");

            // NOT ALLOW :
            // OUT -> OUT
            if (lastLog.PunchType == PunchType.Out)
                throw new Exception("Already punched out");

            // =====================================================
            // STEP 7 : ADD OUT LOG
            // =====================================================

            attendance.Logs.Add(new AttendanceLog
            {
                Id = IDManager.GetNewId(new AttendanceLog()),

                AttendanceId = attendance.Id,

                EmployeeId = employee.Id,

                PunchTime = now,

                PunchType = PunchType.Out,

                IsManual = dto.IsManual,

                DeviceType = dto.DeviceType,
                OS = dto.OS,
                Browser = dto.Browser,
                Version = dto.Version,
                Location = dto.Location,

                CreatedBy = dto.CreatedBy,
                ModifiedBy = dto.ModifiedBy,
                ModifiedOn = dto.ModifiedOn
            });

            // =====================================================
            // STEP 8 : UPDATE LAST OUT
            // =====================================================

            attendance.LastOut = now;

            // =====================================================
            // STEP 9 : CALCULATE WORKING HOURS
            // =====================================================

            CalculateWorkingHours(attendance);

            // =====================================================
            // STEP 10 : EARLY EXIT CHECK
            // =====================================================

            var shiftEndDateTime =
                attendanceDate.Date.Add(shift.EndTime);

            // NIGHT SHIFT HANDLE
            // Example:
            // 10 PM -> 6 AM
            if (shift.IsNightShift &&
                shift.EndTime < shift.StartTime)
            {
                shiftEndDateTime =
                    shiftEndDateTime.AddDays(1);
            }

            var allowedOut =
                shiftEndDateTime.AddMinutes(
                    -shift.GraceOutMinutes);

            attendance.IsEarlyExit =
                now < allowedOut;

            // =====================================================
            // STEP 11 : OVERTIME
            // =====================================================

            var totalMinutes =
                (decimal)(attendance.LastOut.Value -
                          attendance.FirstIn.Value)
                .TotalMinutes;

            if (totalMinutes >
                shift.MinimumWorkingMinutes)
            {
                attendance.OvertimeHours =
                    Math.Round(
                        (totalMinutes -
                         shift.MinimumWorkingMinutes) / 60,
                        2);
            }
            else
            {
                attendance.OvertimeHours = 0;
            }

            // =====================================================
            // STEP 12 : ATTENDANCE STATUS
            // =====================================================

            attendance.Status = GetAttendanceStatus(attendance, shift);

            // =====================================================
            // STEP 13 : SAVE
            // =====================================================

            await _db.SaveChangesAsync();

            return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // =========================
        // 🟢 BREAK IN
        // =========================
        public async Task<bool> BreakInAsync(PunchRequestDto dto)
        {
            try
            {
            var now = dto.PunchTime;

            // =====================================================
            // STEP 1 : GET ACTIVE ATTENDANCE
            // =====================================================

            var attendance = await _db.Attendances
                .Include(x => x.Logs.OrderBy(l => l.PunchTime))
                .FirstOrDefaultAsync(x =>
                    x.EmployeeId == dto.EmployeeId &&
                    x.LastOut == null);

            if (attendance == null)
                throw new Exception("Active attendance not found");

            // =====================================================
            // STEP 2 : GET LAST LOG
            // =====================================================

            var lastLog = attendance.Logs
                .OrderBy(x => x.PunchTime)
                .LastOrDefault();

            if (lastLog == null)
                throw new Exception(
                    "Break out required");

            // =====================================================
            // STEP 3 : VALIDATION
            // =====================================================

            // Allowed only after BREAK OUT

            if (lastLog.PunchType != PunchType.BreakOut)
            {
                throw new Exception(
                    "Break in not allowed");
            }

            // =====================================================
            // STEP 4 : ADD BREAK IN
            // =====================================================

            attendance.Logs.Add(new AttendanceLog
            {
                Id = IDManager.GetNewId(new AttendanceLog()),

                AttendanceId = attendance.Id,

                EmployeeId = dto.EmployeeId,

                PunchTime = now,

                PunchType = PunchType.BreakIn,

                IsManual = dto.IsManual,

                DeviceType = dto.DeviceType,
                OS = dto.OS,
                Browser = dto.Browser,
                Version = dto.Version,
                Location = dto.Location,

                CreatedBy = dto.CreatedBy
            });

            // =====================================================
            // STEP 5 : SAVE
            // =====================================================

            await _db.SaveChangesAsync();

            return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // =========================
        // 🟢 BREAK OUT
        // =========================
        public async Task<bool> BreakOutAsync(PunchRequestDto dto)
        {
            try
            {
            var now = dto.PunchTime;

            // =====================================================
            // STEP 1 : GET ACTIVE ATTENDANCE
            // =====================================================

            var attendance = await _db.Attendances
                .Include(x => x.Logs.OrderBy(l => l.PunchTime))
                .FirstOrDefaultAsync(x =>
                    x.EmployeeId == dto.EmployeeId &&
                    x.LastOut == null);

            if (attendance == null)
                throw new Exception("Active attendance not found");

            // =====================================================
            // STEP 2 : GET LAST LOG
            // =====================================================

            var lastLog = attendance.Logs
                .OrderBy(x => x.PunchTime)
                .LastOrDefault();

            if (lastLog == null)
                throw new Exception("Punch in required");

            // =====================================================
            // STEP 3 : VALIDATION
            // =====================================================

            // Allowed only after:
            // IN or BREAK IN

            if (lastLog.PunchType != PunchType.In &&
                lastLog.PunchType != PunchType.BreakIn)
            {
                throw new Exception(
                    "Break out not allowed");
            }

            // =====================================================
            // STEP 4 : ADD BREAK OUT
            // =====================================================

            attendance.Logs.Add(new AttendanceLog
            {
                Id = IDManager.GetNewId(new AttendanceLog()),

                AttendanceId = attendance.Id,

                EmployeeId = dto.EmployeeId,

                PunchTime = now,

                PunchType = PunchType.BreakOut,

                IsManual = dto.IsManual,

                DeviceType = dto.DeviceType,
                OS = dto.OS,
                Browser = dto.Browser,
                Version = dto.Version,
                Location = dto.Location,

                CreatedBy = dto.CreatedBy
            });

            // =====================================================
            // STEP 5 : SAVE
            // =====================================================

            await _db.SaveChangesAsync();

            return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // =========================
        // 🟢 CURRENT STATUS
        // =========================
        public async Task<AttendanceCurrentStatusDto>GetCurrentStatusAsync(string employeeId)
        {
            // =====================================================
            // STEP 1 : GET EMPLOYEE
            // =====================================================

            var employee = await _db.Employees
                .Include(x => x.DefaultShift)
                .FirstOrDefaultAsync(x =>
                    x.Id == employeeId);

            if (employee == null)
                throw new Exception("Employee not found");

            // =====================================================
            // STEP 2 : GET ACTIVE SHIFT
            // =====================================================

            var now = DateTime.Now;

            Shift shift = null;

            // FIRST PRIORITY :
            // EmployeeShiftMapping

            var shiftMapping =
                await _db.EmployeeShiftMappings
                .Include(x => x.Shift)
                .FirstOrDefaultAsync(x =>
                    x.EmployeeId == employeeId &&
                    x.EffectiveFrom.Date <= now.Date &&
                    (
                        x.EffectiveTo == null ||
                        x.EffectiveTo.Value.Date >= now.Date
                    ));

            if (shiftMapping != null)
            {
                shift = shiftMapping.Shift;
            }
            else
            {
                shift = employee.DefaultShift;
            }

            if (shift == null)
                throw new Exception("Shift not assigned");

            // =====================================================
            // STEP 3 : GET ATTENDANCE DATE
            // =====================================================

            var attendanceDate =
                GetAttendanceDate(now, shift);

            // =====================================================
            // STEP 4 : GET TODAY ATTENDANCE
            // =====================================================

            var attendance = await _db.Attendances
                .Include(x => x.Logs)
                .Include(x => x.Shift)
                .FirstOrDefaultAsync(x =>
                    x.EmployeeId == employeeId &&
                    x.ShiftId == shift.Id &&
                    x.Date >= attendanceDate.Date &&
                    x.Date < attendanceDate.Date.AddDays(1));

            // =====================================================
            // STEP 5 : CREATE DTO
            // =====================================================

            var dto = new AttendanceCurrentStatusDto
            {
                CurrentStatus = "Not Punched In",

                ShiftName = shift.Name,

                ShiftTime =
                    $"{shift.StartTime:hh\\:mm} - {shift.EndTime:hh\\:mm}",

                CurrentTime = now.ToString("hh:mm tt")
            };

            // =====================================================
            // STEP 6 : NO ATTENDANCE
            // =====================================================

            if (attendance == null)
            {
                dto.CanPunchIn = true;

                return dto;
            }

            // =====================================================
            // STEP 7 : CALCULATE HOURS
            // =====================================================

            dto.FirstIn = attendance.FirstIn;
            dto.LastOut = attendance.LastOut;

            dto.WorkingHours =
                attendance.TotalWorkingHours;

            dto.BreakHours =
                attendance.BreakHours;

            dto.IsLate =
                attendance.IsLate;

            dto.IsEarlyExit =
                attendance.IsEarlyExit;

            dto.OvertimeHours =
                attendance.OvertimeHours;

            // =====================================================
            // STEP 8 : GET LAST LOG
            // =====================================================

            var lastLog = attendance.Logs
                .OrderByDescending(x => x.PunchTime)
                .FirstOrDefault();

            if (lastLog == null)
            {
                dto.CanPunchIn = true;

                return dto;
            }

            // =====================================================
            // STEP 9 : BUTTON VISIBILITY LOGIC
            // =====================================================

            switch (lastLog.PunchType)
            {
                // =============================================
                // PUNCH IN
                // =============================================

                case PunchType.In:

                    dto.CurrentStatus = "Working";

                    dto.LastAction = "Punch In";

                    dto.CanBreakOut = true;

                    dto.CanPunchOut = true;

                    break;

                // =============================================
                // BREAK OUT
                // =============================================

                case PunchType.BreakOut:

                    dto.CurrentStatus = "On Break";

                    dto.LastAction = "Break Out";

                    dto.CanBreakIn = true;

                    break;

                // =============================================
                // BREAK IN
                // =============================================

                case PunchType.BreakIn:

                    dto.CurrentStatus = "Working";

                    dto.LastAction = "Break In";

                    dto.CanBreakOut = true;

                    dto.CanPunchOut = true;

                    break;

                // =============================================
                // PUNCH OUT
                // =============================================

                case PunchType.Out:

                    dto.CurrentStatus = "Completed";

                    dto.LastAction = "Punch Out";

                    break;
            }

            return dto;
        }

        // =========================
        // 📊 MONTHLY
        // =========================
        public async Task<List<Attendance>> GetMonthlyAsync(string employeeId, int month, int year)
        {
            try
            {
            return await _db.Attendances
                .Where(x => x.EmployeeId == employeeId &&
                            x.Date.Month == month &&
                            x.Date.Year == year)
                .OrderBy(x => x.Date)
                .ToListAsync();
            }
            catch (Exception)
            {
                return new List<Attendance>();
            }
        }

        public async Task ProcessMonthlyAttendance(int year, int month)
        {
            try
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
                    att.Status = GetAttendanceStatus(att,att.Shift);
                }
            }

            await _db.SaveChangesAsync();
            }
            catch (Exception)
            {
                return;
            }
        }

        #region Private function
        private DateTime GetAttendanceDate(DateTime punchTime,Shift shift)
        {
            // Day Shift
            if (!shift.IsNightShift)
                return punchTime.Date;

            // Night Shift Example:
            // Shift:
            // 10 PM -> 6 AM

            // Punch at:
            // 2 AM means previous date attendance

            if (punchTime.TimeOfDay < shift.EndTime)
                return punchTime.Date.AddDays(-1);

            return punchTime.Date;
        }
        private AttendanceStatus GetAttendanceStatus(Attendance attendance,Shift shift)
        {
            var workedMinutes =
                attendance.TotalWorkingHours * 60;

            if (workedMinutes >= shift.FullDayMinutes)
                return AttendanceStatus.Present;

            if (workedMinutes >= shift.HalfDayMinutes)
                return AttendanceStatus.HalfDay;

            return AttendanceStatus.Absent;
        }
        //Showing "Currently Working / Not Working"
        public async Task<object> GetLiveStatus(string employeeId)
        {
            try
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
            catch (Exception)
            {
                return null;
            }
        }

        private void CalculateWorkingHours(Attendance attendance)
        {
            if (attendance.FirstIn == null ||
                attendance.LastOut == null)
            {
                attendance.TotalWorkingHours = 0;
                return;
            }

            var grossHours =
                (decimal)(attendance.LastOut.Value -
                          attendance.FirstIn.Value)
                .TotalHours;

            var breakHours =
                CalculateBreakHours(attendance);

            attendance.TotalWorkingHours =
                Math.Round(
                    grossHours - breakHours,
                    2);
        }

        private decimal CalculateBreakHours(Attendance attendance)
        {
            decimal totalBreakHours = 0;

            var logs = attendance.Logs
                .OrderBy(x => x.PunchTime)
                .ToList();

            DateTime? breakOutTime = null;

            foreach (var log in logs)
            {
                // BREAK OUT
                if (log.PunchType == PunchType.BreakOut)
                {
                    breakOutTime = log.PunchTime;
                }

                // BREAK IN
                if (log.PunchType == PunchType.BreakIn &&
                    breakOutTime != null)
                {
                    totalBreakHours +=
                        (decimal)(log.PunchTime -
                                  breakOutTime.Value)
                        .TotalHours;

                    breakOutTime = null;
                }
            }

            attendance.BreakHours =
                Math.Round(totalBreakHours, 2);

            return attendance.BreakHours;
        }
        //private void CalculateWorkingHours(Attendance attendance)
        //{
        //    var logs = attendance.Logs.OrderBy(x => x.PunchTime).ToList();

        //    TimeSpan total = TimeSpan.Zero;

        //    for (int i = 0; i < logs.Count - 1; i++)
        //    {
        //        if (logs[i].PunchType == PunchType.In &&
        //            logs[i + 1].PunchType == PunchType.Out)
        //        {
        //            total += logs[i + 1].PunchTime - logs[i].PunchTime;
        //        }
        //    }

        //    attendance.TotalWorkingHours = (decimal)total.TotalHours;

        //    // Overtime
        //    var shiftMinutes = attendance.Shift.FullDayMinutes;

        //    if (total.TotalMinutes > shiftMinutes)
        //    {
        //        attendance.OvertimeHours =
        //            (decimal)(total.TotalMinutes - shiftMinutes) / 60;
        //    }
        //}
        #endregion

        #region Get All

        public async Task<List<AttendanceLogDto>> GetAllAsync()
        {
            try
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

                    DeviceType = x.DeviceType,
                    OS = x.OS,
                    Browser = x.Browser,
                    Version = x.Version,
                    Location = x.Location,
                    IsManual = x.IsManual,

                    DeviceId = x.DeviceId,
                    DeviceName = x.DeviceId != null
                        ? _db.BiometricDevices
                            .Where(d => d.Id == x.DeviceId)
                            .Select(d => d.DeviceName)
                            .FirstOrDefault()
                        : null,

                    CreatedDate = x.CreatedOn
                })
                .ToListAsync();
            }
            catch (Exception)
            {
                return new List<AttendanceLogDto>();
            }
        }

        #endregion

        #region Get By Id

        public async Task<AttendanceLogDto?> GetByIdAsync(string id)
        {
            try
            {
            return await _db.AttendanceLogs
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new AttendanceLogDto
                {
                    Id = x.Id,

                    AttendanceId = x.AttendanceId,

                    EmployeeId = x.EmployeeId,

                    PunchTime = x.PunchTime,

                    PunchType = EnumHelper.GetEnumName<PunchType>((int)x.PunchType),

                    DeviceType = x.DeviceType,
                    OS = x.OS,
                    Browser = x.Browser,
                    Version = x.Version,
                    Location = x.Location,

                    IsManual = x.IsManual,

                    DeviceId = x.DeviceId,
                    DeviceName = x.DeviceId != null
                        ? _db.BiometricDevices
                            .Where(d => d.Id == x.DeviceId)
                            .Select(d => d.DeviceName)
                            .FirstOrDefault()
                        : null,

                    CreatedDate = x.CreatedOn
                })
                .FirstOrDefaultAsync();
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion

        // =========================
        // Get All
        // =========================
        public async Task<List<AttendanceDto>> GetAllAttendanceListAsync()
        {
            try
            {
            return await _db.Attendances
                .Include(x => x.Employee)
                .Include(x => x.Company)
                .Include(x => x.Branch)
                .Include(x => x.Shift)
                .Include(x => x.Tenant)
                .Select(x => new AttendanceDto
                {
                    Id = x.Id,
                    // Tenant
                    TenantId = x.TenantId,
                    TenantName = x.Tenant != null
                        ? x.Tenant.Name
                        : null,

                    // Employee
                    EmployeeId = x.EmployeeId,
                    EmployeeName = x.Employee != null
                        ? x.Employee.FirstName + " " + x.Employee.LastName
                        : null,

                    // Company
                    CompanyId = x.CompanyId,
                    CompanyName = x.Company != null
                        ? x.Company.Name
                        : null,

                    // Branch
                    BranchId = x.BranchId,
                    BranchName = x.Branch != null
                        ? x.Branch.Name
                        : null,

                    Date = x.Date,

                    // Branch
                    ShiftId = x.ShiftId,
                    ShiftName = x.Shift != null
                        ? x.Shift.Name
                        : null,

                    FirstIn = x.FirstIn,
                    LastOut = x.LastOut,

                    TotalWorkingHours = x.TotalWorkingHours,
                    BreakHours = x.BreakHours,
                    OvertimeHours = x.OvertimeHours,

                    Status = x.Status,

                    IsLate = x.IsLate,
                    IsEarlyExit = x.IsEarlyExit,

                    Remarks = x.Remarks,

                    IsManualEntry = x.IsManualEntry
                })
                .ToListAsync();
            }
            catch (Exception)
            {
                return new List<AttendanceDto>();
            }
        }

        // =========================
        // Get By Id
        // =========================
        public async Task<AttendanceDto?> GetAttendanceByIdAsync(string id)
        {
            try
            {
            var x = await _db.Attendances
                .Include(x => x.Employee)
                .Include(x => x.Company)
                .Include(x => x.Branch)
                .Include(x => x.Shift)
                .Include(x => x.Tenant)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (x == null)
                return null;

            return new AttendanceDto
            {
                Id = x.Id,
                // Tenant
                TenantId = x.TenantId,
                TenantName = x.Tenant != null
                        ? x.Tenant.Name
                        : null,

                // Employee
                EmployeeId = x.EmployeeId,
                EmployeeName = x.Employee != null
                        ? x.Employee.FirstName + " " + x.Employee.LastName
                        : null,

                // Company
                CompanyId = x.CompanyId,
                CompanyName = x.Company != null
                        ? x.Company.Name
                        : null,

                // Branch
                BranchId = x.BranchId,
                BranchName = x.Branch != null
                        ? x.Branch.Name
                        : null,

                Date = x.Date,

                // Branch
                ShiftId = x.ShiftId,
                ShiftName = x.Shift != null
                        ? x.Shift.Name
                        : null,

                FirstIn = x.FirstIn,
                LastOut = x.LastOut,

                TotalWorkingHours = x.TotalWorkingHours,
                BreakHours = x.BreakHours,
                OvertimeHours = x.OvertimeHours,

                Status = x.Status,

                IsLate = x.IsLate,
                IsEarlyExit = x.IsEarlyExit,

                Remarks = x.Remarks,

                IsManualEntry = x.IsManualEntry
            };
            }
            catch (Exception)
            {
                return null;
            }
        }

        // =========================
        // Create
        // =========================
        public async Task<bool> CreateAsync(AttendanceDto dto)
        {
            try
            {
            Attendance attendance = new Attendance
            {
                TenantId = dto.TenantId,
                CompanyId = dto.CompanyId,
                BranchId = dto.BranchId,
                EmployeeId = dto.EmployeeId,
                Date = dto.Date,
                ShiftId = dto.ShiftId,

                FirstIn = dto.FirstIn,
                LastOut = dto.LastOut,

                TotalWorkingHours = dto.TotalWorkingHours,
                BreakHours = dto.BreakHours,
                OvertimeHours = dto.OvertimeHours,

                Status = dto.Status,

                IsLate = dto.IsLate,
                IsEarlyExit = dto.IsEarlyExit,

                Remarks = dto.Remarks,
                IsManualEntry = dto.IsManualEntry
            };

            await _db.Attendances.AddAsync(attendance);
            await _db.SaveChangesAsync();

            return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // =========================
        // Update
        // =========================
        public async Task<bool> UpdateAsync(AttendanceDto dto)
        {
            try
            {
            var attendance = await _db.Attendances
                .FirstOrDefaultAsync(x => x.Id == dto.Id);

            if (attendance == null)
                return false;

            attendance.CompanyId = dto.CompanyId;
            attendance.BranchId = dto.BranchId;
            attendance.EmployeeId = dto.EmployeeId;
            attendance.Date = dto.Date;
            attendance.ShiftId = dto.ShiftId;

            attendance.FirstIn = dto.FirstIn;
            attendance.LastOut = dto.LastOut;

            attendance.TotalWorkingHours = dto.TotalWorkingHours;
            attendance.BreakHours = dto.BreakHours;
            attendance.OvertimeHours = dto.OvertimeHours;

            attendance.Status = dto.Status;

            attendance.IsLate = dto.IsLate;
            attendance.IsEarlyExit = dto.IsEarlyExit;

            attendance.Remarks = dto.Remarks;
            attendance.IsManualEntry = dto.IsManualEntry;

            _db.Attendances.Update(attendance);

            await _db.SaveChangesAsync();

            return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // =========================
        // Delete
        // =========================
        public async Task<bool> DeleteAsync(string id)
        {
            try
            {
            var attendance = await _db.Attendances
                .FirstOrDefaultAsync(x => x.Id == id);

            if (attendance == null)
                return false;

            _db.Attendances.Remove(attendance);

            await _db.SaveChangesAsync();

            return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #region Attendance Insights (Calendar / Team / Summary / Dashboard)

        // "Present-ish" buckets that should count towards a day being
        // attended, mirroring EmployeeDashboardService.PresentStatuses
        // (Present/Late/EarlyExit/WorkFromHome/OnDuty/Overtime/CompOff) plus
        // HalfDay, which the ESS dashboard counts separately but every
        // Insights aggregation below treats as "present" for the
        // Present/Absent counters. "Late" is deliberately NOT included here -
        // every call site checks it explicitly first (it counts towards both
        // the Present bucket AND its own dedicated Late bucket).
        private static bool IsPresentLikeStatus(string status) =>
            status is "Present" or "WorkFromHome" or "OnDuty" or "Overtime"
                   or "CompOff" or "EarlyExit" or "HalfDay";

        // =========================
        // 📅 CALENDAR (one employee, one month)
        // =========================
        public async Task<List<AttendanceCalendarDayDto>> GetCalendarAsync(string employeeId, int month, int year, string tenantId)
        {
            // Tenant ownership check - without this, any authenticated
            // caller could pass another tenant's employeeId and read their
            // punch times/status (cross-tenant IDOR). Every other insights
            // method (Team/Summary/Dashboard) already scopes its employee
            // set by TenantId first; this mirrors that.
            bool employeeInTenant = await _db.Employees
                .AnyAsync(e => e.Id == employeeId && e.TenantId == tenantId && !e.IsDeleted);

            if (!employeeInTenant)
                return new List<AttendanceCalendarDayDto>();

            var daysInMonth = DateTime.DaysInMonth(year, month);
            var monthStart = new DateTime(year, month, 1);
            var monthEnd = monthStart.AddDays(daysInMonth - 1);
            var today = DateTime.UtcNow.Date;

            var attendances = await _db.Attendances.AsNoTracking()
                .Include(x => x.Shift)
                .Where(x => x.EmployeeId == employeeId
                         && x.Date >= monthStart && x.Date <= monthEnd
                         && !x.IsDeleted)
                .ToListAsync();

            var leaves = await _db.LeaveApplications.AsNoTracking()
                .Where(x => x.EmployeeId == employeeId
                         && x.Status == ApprovalStatus.Approved
                         && x.FromDate <= monthEnd && x.ToDate >= monthStart)
                .Select(x => new { x.FromDate, x.ToDate })
                .ToListAsync();

            var holidaySet = (await _db.HolidayGroupDetails.AsNoTracking()
                    .Where(h => h.TenantId == tenantId && h.HolidayDate >= monthStart && h.HolidayDate <= monthEnd)
                    .Select(h => h.HolidayDate.Date)
                    .ToListAsync())
                .ToHashSet();

            var weekOffSet = (await _db.WeekOffs.AsNoTracking()
                    .Where(w => w.TenantId == tenantId)
                    .Select(w => w.Day)
                    .ToListAsync())
                .ToHashSet();

            var regularizedSet = (await _db.AttendanceRegularizations.AsNoTracking()
                    .Where(x => x.EmployeeId == employeeId
                             && x.Status == ApprovalStatus.Approved
                             && x.Date >= monthStart && x.Date <= monthEnd)
                    .Select(x => x.Date.Date)
                    .ToListAsync())
                .ToHashSet();

            var result = new List<AttendanceCalendarDayDto>();

            for (int day = 1; day <= daysInMonth; day++)
            {
                var date = new DateTime(year, month, day);
                var att = attendances.FirstOrDefault(a => a.Date.Date == date);

                bool hasApprovedLeave = leaves.Any(l => l.FromDate.Date <= date && l.ToDate.Date >= date);
                bool isHoliday = holidaySet.Contains(date);
                bool isWeekOff = weekOffSet.Contains(date.DayOfWeek);

                var status = AttendanceStatusHelper.ClassifyDay(att?.Status, hasApprovedLeave, isHoliday, isWeekOff, date, today);

                result.Add(new AttendanceCalendarDayDto
                {
                    Date = date,
                    Status = status,
                    FirstIn = att?.FirstIn?.TimeOfDay,
                    LastOut = att?.LastOut?.TimeOfDay,
                    TotalWorkingHours = att?.TotalWorkingHours,
                    ShiftName = att?.Shift?.Name,
                    IsRegularized = regularizedSet.Contains(date)
                });
            }

            return result;
        }

        // =========================
        // 🔐 HR/ADMIN CHECK (for GetTeamAttendanceAsync scoping)
        // =========================
        // Same permission-based pattern as
        // AttendanceRegularizationService.IsHrApproverAsync - does the
        // acting user hold, through any Role assigned to them, an allowed
        // RolePermission for the View action on the ATTENDANCE feature.
        public async Task<bool> IsHrOrAdminForAttendanceAsync(string? actingUserId)
        {
            if (string.IsNullOrEmpty(actingUserId))
                return false;

            return await (
                from ur in _db.UserRoles
                join rp in _db.RolePermissions.Where(x => x.IsAllowed) on ur.RoleId equals rp.RoleId
                join p in _db.Permissions.Where(x =>
                        x.FeatureId == AppFeatureConstants.ATTENDANCE && x.Action == Actions.View)
                    on rp.PermissionId equals p.Id
                where ur.UserId == actingUserId
                select p.Id
            ).AnyAsync();
        }

        // =========================
        // 👥 TEAM ATTENDANCE (one date)
        // =========================
        public async Task<List<TeamAttendanceMemberDto>> GetTeamAttendanceAsync(string actingUserId, DateTime date, string tenantId, bool isHrOrAdmin)
        {
            // Resolve "who is this login" -> Employee, same pattern as
            // AttendanceRegularizationService.GetActingContextAsync.
            var actingUser = await _db.Users.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == actingUserId);

            var actingEmployeeId = actingUser?.EmployeeId;

            var employeesQuery = _db.Employees.AsNoTracking()
                .Where(e => e.TenantId == tenantId && !e.IsDeleted);

            if (!isHrOrAdmin)
            {
                if (string.IsNullOrEmpty(actingEmployeeId))
                    return new List<TeamAttendanceMemberDto>();

                employeesQuery = employeesQuery.Where(e => e.ReportingManagerId == actingEmployeeId);
            }

            var employees = await employeesQuery
                .Select(e => new
                {
                    e.Id,
                    e.FirstName,
                    e.LastName,
                    e.EmployeeCode,
                    DepartmentName = e.Department != null ? e.Department.Name : null,
                    DesignationName = e.Designation != null ? e.Designation.Name : null
                })
                .ToListAsync();

            var employeeIds = employees.Select(e => e.Id).ToList();
            var dateOnly = date.Date;
            var today = DateTime.UtcNow.Date;

            var attendances = await _db.Attendances.AsNoTracking()
                .Where(a => employeeIds.Contains(a.EmployeeId) && a.Date.Date == dateOnly && !a.IsDeleted)
                .ToListAsync();

            var leaveEmployeeIds = (await _db.LeaveApplications.AsNoTracking()
                    .Where(l => employeeIds.Contains(l.EmployeeId)
                             && l.Status == ApprovalStatus.Approved
                             && l.FromDate.Date <= dateOnly && l.ToDate.Date >= dateOnly)
                    .Select(l => l.EmployeeId)
                    .ToListAsync())
                .ToHashSet();

            bool isHoliday = await _db.HolidayGroupDetails.AsNoTracking()
                .AnyAsync(h => h.TenantId == tenantId && h.HolidayDate.Date == dateOnly);

            var weekOffDays = (await _db.WeekOffs.AsNoTracking()
                    .Where(w => w.TenantId == tenantId)
                    .Select(w => w.Day)
                    .ToListAsync())
                .ToHashSet();
            bool isWeekOff = weekOffDays.Contains(dateOnly.DayOfWeek);

            return employees.Select(e =>
            {
                var att = attendances.FirstOrDefault(a => a.EmployeeId == e.Id);
                var status = AttendanceStatusHelper.ClassifyDay(
                    att?.Status, leaveEmployeeIds.Contains(e.Id), isHoliday, isWeekOff, dateOnly, today);

                return new TeamAttendanceMemberDto
                {
                    EmployeeId = e.Id,
                    EmployeeName = $"{e.FirstName} {e.LastName}".Trim(),
                    EmployeeCode = e.EmployeeCode,
                    DepartmentName = e.DepartmentName,
                    DesignationName = e.DesignationName,
                    Status = status,
                    FirstIn = att?.FirstIn?.TimeOfDay,
                    LastOut = att?.LastOut?.TimeOfDay,
                    TotalWorkingHours = att?.TotalWorkingHours
                };
            }).ToList();
        }

        // =========================
        // 📊 SUMMARY (all/filtered employees, one month)
        // =========================
        public async Task<List<AttendanceSummaryRowDto>> GetSummaryAsync(string tenantId, int month, int year, string? departmentId, string? employeeId)
        {
            var daysInMonth = DateTime.DaysInMonth(year, month);
            var monthStart = new DateTime(year, month, 1);
            var monthEnd = monthStart.AddDays(daysInMonth - 1);
            var today = DateTime.UtcNow.Date;

            var employeesQuery = _db.Employees.AsNoTracking()
                .Where(e => e.TenantId == tenantId && !e.IsDeleted);

            if (!string.IsNullOrWhiteSpace(departmentId))
                employeesQuery = employeesQuery.Where(e => e.DepartmentId == departmentId);

            if (!string.IsNullOrWhiteSpace(employeeId))
                employeesQuery = employeesQuery.Where(e => e.Id == employeeId);

            var employees = await employeesQuery
                .Select(e => new
                {
                    e.Id,
                    e.FirstName,
                    e.LastName,
                    e.EmployeeCode,
                    DepartmentName = e.Department != null ? e.Department.Name : null
                })
                .ToListAsync();

            var employeeIds = employees.Select(e => e.Id).ToList();

            var attendances = await _db.Attendances.AsNoTracking()
                .Where(a => employeeIds.Contains(a.EmployeeId) && a.Date >= monthStart && a.Date <= monthEnd && !a.IsDeleted)
                .ToListAsync();

            var leaves = await _db.LeaveApplications.AsNoTracking()
                .Where(l => employeeIds.Contains(l.EmployeeId)
                         && l.Status == ApprovalStatus.Approved
                         && l.FromDate <= monthEnd && l.ToDate >= monthStart)
                .Select(l => new { l.EmployeeId, l.FromDate, l.ToDate })
                .ToListAsync();

            var holidaySet = (await _db.HolidayGroupDetails.AsNoTracking()
                    .Where(h => h.TenantId == tenantId && h.HolidayDate >= monthStart && h.HolidayDate <= monthEnd)
                    .Select(h => h.HolidayDate.Date)
                    .ToListAsync())
                .ToHashSet();

            var weekOffSet = (await _db.WeekOffs.AsNoTracking()
                    .Where(w => w.TenantId == tenantId)
                    .Select(w => w.Day)
                    .ToListAsync())
                .ToHashSet();

            var regCountMap = (await _db.AttendanceRegularizations.AsNoTracking()
                    .Where(x => employeeIds.Contains(x.EmployeeId) && x.Date >= monthStart && x.Date <= monthEnd)
                    .GroupBy(x => x.EmployeeId)
                    .Select(g => new { EmployeeId = g.Key, Count = g.Count() })
                    .ToListAsync())
                .ToDictionary(x => x.EmployeeId, x => x.Count);

            var result = new List<AttendanceSummaryRowDto>();

            foreach (var e in employees)
            {
                var empAttendances = attendances.Where(a => a.EmployeeId == e.Id).ToList();
                var empLeaves = leaves.Where(l => l.EmployeeId == e.Id).ToList();

                var row = new AttendanceSummaryRowDto
                {
                    EmployeeId = e.Id,
                    EmployeeName = $"{e.FirstName} {e.LastName}".Trim(),
                    EmployeeCode = e.EmployeeCode,
                    DepartmentName = e.DepartmentName,
                    TotalWorkingHours = empAttendances.Sum(a => a.TotalWorkingHours),
                    OvertimeHours = empAttendances.Sum(a => a.OvertimeHours),
                    RegularizationCount = regCountMap.TryGetValue(e.Id, out var c) ? c : 0
                };

                // Only days up to today are classified/tallied - a
                // still-in-progress month shouldn't count its remaining
                // future days as Absent.
                for (int day = 1; day <= daysInMonth; day++)
                {
                    var date = new DateTime(year, month, day);
                    if (date > today) break;

                    var att = empAttendances.FirstOrDefault(a => a.Date.Date == date);
                    bool hasApprovedLeave = empLeaves.Any(l => l.FromDate.Date <= date && l.ToDate.Date >= date);
                    bool isHoliday = holidaySet.Contains(date);
                    bool isWeekOff = weekOffSet.Contains(date.DayOfWeek);

                    var status = AttendanceStatusHelper.ClassifyDay(att?.Status, hasApprovedLeave, isHoliday, isWeekOff, date, today);

                    switch (status)
                    {
                        case "Late":
                            row.PresentDays++;
                            row.LateDays++;
                            break;
                        case "HalfDay":
                            row.HalfDays++;
                            break;
                        case "Absent":
                            row.AbsentDays++;
                            break;
                        case "Leave":
                            row.LeaveDays++;
                            break;
                        case "Holiday":
                            row.HolidayDays++;
                            break;
                        case "WeekOff":
                            row.WeekOffDays++;
                            break;
                        case "Present":
                        case "WorkFromHome":
                        case "OnDuty":
                        case "Overtime":
                        case "CompOff":
                        case "EarlyExit":
                            row.PresentDays++;
                            break;
                        // "None" (future date - unreachable here since the
                        // loop breaks once date > today) is intentionally
                        // not tallied into any bucket.
                    }
                }

                result.Add(row);
            }

            return result;
        }

        // =========================
        // 📈 DASHBOARD (org-wide, today + 30-day trend)
        // =========================
        public async Task<AttendanceDashboardDto> GetDashboardAsync(string tenantId, string? companyId)
        {
            var today = DateTime.UtcNow.Date;

            var employeesQuery = _db.Employees.AsNoTracking()
                .Where(e => e.TenantId == tenantId && !e.IsDeleted && e.IsActive);

            if (!string.IsNullOrWhiteSpace(companyId))
                employeesQuery = employeesQuery.Where(e => e.CompanyId == companyId);

            var employees = await employeesQuery
                .Select(e => new
                {
                    e.Id,
                    DepartmentName = e.Department != null ? e.Department.Name : "Unassigned"
                })
                .ToListAsync();

            var employeeIds = employees.Select(e => e.Id).ToList();
            int totalActive = employees.Count;

            var weekOffSet = (await _db.WeekOffs.AsNoTracking()
                    .Where(w => w.TenantId == tenantId)
                    .Select(w => w.Day)
                    .ToListAsync())
                .ToHashSet();

            // ---- Today snapshot ----
            var todayAttendances = await _db.Attendances.AsNoTracking()
                .Where(a => employeeIds.Contains(a.EmployeeId) && a.Date.Date == today && !a.IsDeleted)
                .ToListAsync();

            var todayLeaveEmployeeIds = (await _db.LeaveApplications.AsNoTracking()
                    .Where(l => employeeIds.Contains(l.EmployeeId)
                             && l.Status == ApprovalStatus.Approved
                             && l.FromDate.Date <= today && l.ToDate.Date >= today)
                    .Select(l => l.EmployeeId)
                    .ToListAsync())
                .ToHashSet();

            bool isHolidayToday = await _db.HolidayGroupDetails.AsNoTracking()
                .AnyAsync(h => h.TenantId == tenantId && h.HolidayDate.Date == today);

            bool isWeekOffToday = weekOffSet.Contains(today.DayOfWeek);

            int presentToday = 0, absentToday = 0, lateToday = 0, onLeaveToday = 0;

            string StatusFor(string employeeId, Attendance? att) =>
                AttendanceStatusHelper.ClassifyDay(
                    att?.Status, todayLeaveEmployeeIds.Contains(employeeId), isHolidayToday, isWeekOffToday, today, today);

            foreach (var e in employees)
            {
                var att = todayAttendances.FirstOrDefault(a => a.EmployeeId == e.Id);
                var status = StatusFor(e.Id, att);

                if (status == "Absent") absentToday++;
                else if (status == "Leave") onLeaveToday++;
                else if (status == "Late") { presentToday++; lateToday++; }
                else if (IsPresentLikeStatus(status)) presentToday++;
            }

            int pendingRegularizations = await _db.AttendanceRegularizations.AsNoTracking()
                .CountAsync(x => employeeIds.Contains(x.EmployeeId) && x.Status == ApprovalStatus.Pending);

            // ---- Department-wise present today ----
            var departmentWise = employees
                .GroupBy(e => e.DepartmentName ?? "Unassigned")
                .Select(g =>
                {
                    int totalCount = g.Count();
                    int presentCount = g.Count(e =>
                    {
                        var att = todayAttendances.FirstOrDefault(a => a.EmployeeId == e.Id);
                        var status = StatusFor(e.Id, att);
                        return status == "Late" || IsPresentLikeStatus(status);
                    });

                    return new DepartmentAttendanceDto
                    {
                        DepartmentName = g.Key,
                        PresentCount = presentCount,
                        TotalCount = totalCount,
                        PresentPercent = totalCount > 0 ? Math.Round(presentCount * 100m / totalCount, 2) : 0
                    };
                })
                .OrderByDescending(x => x.TotalCount)
                .ToList();

            // ---- Last 30 days trend ----
            var trendStart = today.AddDays(-29);

            var trendAttendances = await _db.Attendances.AsNoTracking()
                .Where(a => employeeIds.Contains(a.EmployeeId) && a.Date >= trendStart && a.Date <= today && !a.IsDeleted)
                .ToListAsync();

            var trendLeaves = await _db.LeaveApplications.AsNoTracking()
                .Where(l => employeeIds.Contains(l.EmployeeId)
                         && l.Status == ApprovalStatus.Approved
                         && l.FromDate <= today && l.ToDate >= trendStart)
                .Select(l => new { l.EmployeeId, l.FromDate, l.ToDate })
                .ToListAsync();

            var trendHolidaySet = (await _db.HolidayGroupDetails.AsNoTracking()
                    .Where(h => h.TenantId == tenantId && h.HolidayDate >= trendStart && h.HolidayDate <= today)
                    .Select(h => h.HolidayDate.Date)
                    .ToListAsync())
                .ToHashSet();

            var trend = new List<AttendanceTrendPointDto>();

            for (int i = 29; i >= 0; i--)
            {
                var d = today.AddDays(-i);
                bool isHolidayDay = trendHolidaySet.Contains(d);
                bool isWeekOffDay = weekOffSet.Contains(d.DayOfWeek);

                int p = 0, a = 0, l = 0;

                foreach (var e in employees)
                {
                    var att = trendAttendances.FirstOrDefault(x => x.EmployeeId == e.Id && x.Date.Date == d);
                    bool hasLeave = trendLeaves.Any(x => x.EmployeeId == e.Id && x.FromDate.Date <= d && x.ToDate.Date >= d);
                    var status = AttendanceStatusHelper.ClassifyDay(att?.Status, hasLeave, isHolidayDay, isWeekOffDay, d, today);

                    if (status == "Absent") a++;
                    else if (status == "Late") { p++; l++; }
                    else if (IsPresentLikeStatus(status)) p++;
                }

                trend.Add(new AttendanceTrendPointDto { Date = d, PresentCount = p, AbsentCount = a, LateCount = l });
            }

            return new AttendanceDashboardDto
            {
                PresentToday = presentToday,
                AbsentToday = absentToday,
                LateToday = lateToday,
                OnLeaveToday = onLeaveToday,
                TotalActiveEmployees = totalActive,
                PresentPercentToday = totalActive > 0 ? Math.Round(presentToday * 100m / totalActive, 2) : 0,
                PendingRegularizations = pendingRegularizations,
                Last30DaysTrend = trend,
                DepartmentWisePresentToday = departmentWise
            };
        }

        #endregion

    }
}
