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

        // =========================
        // 🟢 BREAK IN
        // =========================
        public async Task<bool> BreakInAsync(PunchRequestDto dto)
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

        // =========================
        // 🟢 BREAK OUT
        // =========================
        public async Task<bool> BreakOutAsync(PunchRequestDto dto)
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
                    att.Status = GetAttendanceStatus(att,att.Shift);
                }
            }

            await _db.SaveChangesAsync();
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

                    CreatedDate = x.CreatedOn
                })
                .ToListAsync();
        }

        #endregion

        #region Get By Id

        public async Task<AttendanceLogDto?> GetByIdAsync(string id)
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

                    CreatedDate = x.CreatedOn
                })
                .FirstOrDefaultAsync();
        }

        #endregion

        // =========================
        // Get All
        // =========================
        public async Task<List<AttendanceDto>> GetAllAttendanceListAsync()
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

        // =========================
        // Get By Id
        // =========================
        public async Task<AttendanceDto?> GetAttendanceByIdAsync(string id)
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

        // =========================
        // Create
        // =========================
        public async Task<bool> CreateAsync(AttendanceDto dto)
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

        // =========================
        // Update
        // =========================
        public async Task<bool> UpdateAsync(AttendanceDto dto)
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

        // =========================
        // Delete
        // =========================
        public async Task<bool> DeleteAsync(string id)
        {
            var attendance = await _db.Attendances
                .FirstOrDefaultAsync(x => x.Id == id);

            if (attendance == null)
                return false;

            _db.Attendances.Remove(attendance);

            await _db.SaveChangesAsync();

            return true;
        }

    }
}
