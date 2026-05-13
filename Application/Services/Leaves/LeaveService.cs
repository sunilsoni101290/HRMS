using Application.DTOs.Leaves;
using Application.Interfaces.Holidays;
using Application.Interfaces.Leaves;
using Domain.Entities;
using Infrastructure;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Application.Services.Leaves
{
    public class LeaveService : ILeaveService
    {
        private readonly ApplicationDbContext _db;
        private readonly IHolidayService _holidayService;
        private readonly ITenantService _tenantService;
        private string tenantId=string.Empty;
        public LeaveService(ApplicationDbContext db, IHolidayService holidayService,ITenantService tenantService)
        {
            _db = db;
            _tenantService = tenantService;
            _holidayService = holidayService;
        }

        #region 📝 APPLY LEAVE
        public async Task<LeaveResponseDto> ApplyLeaveAsync(LeaveApplyDto dto)
        {
            var totalDays = await CalculateLeaveDays(dto, _tenantService.GetTenantId());

            var exists = await _db.LeaveApplications.AnyAsync(x =>
                x.EmployeeId == dto.EmployeeId &&
                x.Status != ApprovalStatus.Rejected &&
                dto.FromDate <= x.ToDate &&
                dto.ToDate >= x.FromDate);

            if (exists)
                throw new Exception("Leave already exists in selected range");

            var balance = await _db.LeaveBalances.FirstOrDefaultAsync(x =>
                x.EmployeeId == dto.EmployeeId &&
                x.LeaveTypeId == dto.LeaveTypeId &&
                x.Year == dto.FromDate.Year);

            if (balance == null || balance.Balance < totalDays)
                throw new Exception("Insufficient leave balance");

            var entity = new LeaveApplication
            {
                EmployeeId = dto.EmployeeId,
                LeaveTypeId = dto.LeaveTypeId,
                FromDate = dto.FromDate,
                ToDate = dto.ToDate,
                TotalDays = totalDays,
                IsHalfDay = dto.IsHalfDay,
                HalfDayType = dto.HalfDayType ?? HalfDayType.FirstHalf,
                Reason = dto.Reason,
                Status = ApprovalStatus.Pending
            };

            _db.LeaveApplications.Add(entity);
            await _db.SaveChangesAsync();

            // 🔹 Load navigation for DTO
            await _db.Entry(entity).Reference(x => x.Employee).LoadAsync();
            await _db.Entry(entity).Reference(x => x.LeaveType).LoadAsync();

            return MapToDto(entity);
        }
        #endregion

        #region ✅ APPROVE LEAVE
        public async Task<LeaveResponseDto> ApproveLeaveAsync(LeaveApproveDto dto)
        {
            var leave = await _db.LeaveApplications
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .FirstOrDefaultAsync(x => x.Id == dto.LeaveId);

            if (leave == null)
                throw new Exception("Leave not found");

            if (leave.Status != ApprovalStatus.Pending)
                throw new Exception("Already processed");

            var balance = await _db.LeaveBalances.FirstOrDefaultAsync(x =>
                x.EmployeeId == leave.EmployeeId &&
                x.LeaveTypeId == leave.LeaveTypeId &&
                x.Year == leave.FromDate.Year);

            balance.Used += leave.TotalDays;
            balance.Balance = balance.OpeningBalance + balance.Earned - balance.Used;

            leave.Status = ApprovalStatus.Approved;
            leave.ApprovedBy = dto.ApproverId;
            leave.ApprovedDate = DateTime.UtcNow;

            await ApplyLeaveToAttendance(leave);

            _db.LeaveApprovalHistories.Add(new LeaveApprovalHistory
            {
                LeaveApplicationId = leave.Id,
                ActionBy = dto.ApproverId,
                Action = LeaveStatus.Approved,
                ActionDate = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            return MapToDto(leave);
        }

        #endregion

        #region ❌ REJECT
        public async Task<LeaveResponseDto> RejectLeaveAsync(LeaveRejectDto dto)
        {
            var leave = await _db.LeaveApplications
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .FirstOrDefaultAsync(x => x.Id == dto.LeaveId);

            if (leave == null)
                throw new Exception("Leave not found");

            leave.Status = ApprovalStatus.Rejected;
            leave.RejectedReason = dto.Reason;
            leave.ApprovedBy = dto.ApproverId;
            leave.ApprovedDate = DateTime.UtcNow;

            _db.LeaveApprovalHistories.Add(new LeaveApprovalHistory
            {
                LeaveApplicationId = leave.Id,
                ActionBy = dto.ApproverId,
                Action = LeaveStatus.Rejected,
                Remarks = dto.Reason,
                ActionDate = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            return MapToDto(leave);
        }
        #endregion

        #region ❌ CANCEL
        // =================  =================
        public async Task<LeaveResponseDto> CancelLeaveAsync(LeaveCancelDto dto)
        {
            var leave = await GetLeave(dto.LeaveId);

            if (leave.Status != ApprovalStatus.Approved)
                throw new Exception("Only approved leave can be cancelled");

            var balance = await _db.LeaveBalances.FirstOrDefaultAsync(x =>
                x.EmployeeId == leave.EmployeeId &&
                x.LeaveTypeId == leave.LeaveTypeId &&
                x.Year == leave.FromDate.Year);

            balance.Used -= leave.TotalDays;
            balance.Balance = balance.OpeningBalance + balance.Earned - balance.Used;

            leave.Status = ApprovalStatus.Cancelled;

            await RevertAttendance(leave);

            await _db.SaveChangesAsync();

            return MapToDto(leave);
        }
        #endregion

        #region Get Employee Leaves
        public async Task<List<LeaveResponseDto>> GetEmployeeLeaves(string employeeId)
        {
            var leaves = await _db.LeaveApplications
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .Where(x => x.EmployeeId == employeeId)
                .OrderByDescending(x => x.FromDate)
                .ToListAsync();

            return leaves.Select(MapToDto).ToList();
        }

        #endregion


        #region Sandwich Holiday
        
        #endregion

        #region Private Methods
        private async Task<LeaveApplication> GetLeave(string id)
        {
            var leave = await _db.LeaveApplications
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (leave == null)
                throw new Exception("Leave not found");

            return leave;
        }
        private async Task RevertAttendance(LeaveApplication leave)
        {
            var dates = Enumerable.Range(0, (leave.ToDate - leave.FromDate).Days + 1)
                .Select(d => leave.FromDate.AddDays(d));

            foreach (var date in dates)
            {
                var att = await _db.Attendances
                    .FirstOrDefaultAsync(x =>
                        x.EmployeeId == leave.EmployeeId &&
                        x.Date == date);

                if (att != null && att.Status == AttendanceStatus.Leave)
                {
                    att.Status = AttendanceStatus.Absent;
                }
            }
        }
        private async Task<decimal> CalculateLeaveDays(LeaveApplyDto dto, string tenantId)
        {
            if (dto.IsHalfDay)
                return 0.5m;

            // 🔥 Sandwich Check
            if (await IsSandwichLeave(dto, tenantId))
            {
                return (dto.ToDate.Date - dto.FromDate.Date).Days + 1;
            }

            int count = 0;

            for (var d = dto.FromDate.Date; d <= dto.ToDate.Date; d = d.AddDays(1))
            {
                var isHoliday = await _holidayService.IsHoliday(d, tenantId);
                var isWeekOff = await _holidayService.IsWeekOff(d, tenantId);

                if (isHoliday || isWeekOff)
                    continue;

                count++;
            }

            return count;
        }
        
        private async Task ApplyLeaveToAttendance(LeaveApplication leave)
        {
            var dates = Enumerable.Range(0, (leave.ToDate - leave.FromDate).Days + 1)
                .Select(d => leave.FromDate.AddDays(d));

            foreach (var date in dates)
            {
                var isHoliday = await _holidayService.IsHoliday(date, tenantId);
                var isWeekOff = await _holidayService.IsWeekOff(date, tenantId);

                if (isHoliday || isWeekOff)
                {
                    // If sandwich → mark leave
                    if (leave.TotalDays == (leave.ToDate - leave.FromDate).Days + 1)
                    {
                        await MarkLeave(date, leave);
                    }
                    continue;
                }

                await MarkLeave(date, leave);
            }
        }

        private async Task MarkLeave(DateTime date, LeaveApplication leave)
        {
            var att = await _db.Attendances
                .FirstOrDefaultAsync(x =>
                    x.EmployeeId == leave.EmployeeId &&
                    x.Date == date);

            if (att == null)
            {
                _db.Attendances.Add(new Attendance
                {
                    EmployeeId = leave.EmployeeId,
                    Date = date,
                    Status = leave.IsHalfDay ? AttendanceStatus.HalfDay : AttendanceStatus.Leave
                });
            }
            else
            {
                att.Status = leave.IsHalfDay ? AttendanceStatus.HalfDay : AttendanceStatus.Leave;
            }
        }
        private async Task<bool> IsSandwichLeave(LeaveApplyDto dto, string companyId)
        {
            var before = dto.FromDate.AddDays(-1);
            var after = dto.ToDate.AddDays(1);

            var beforeIsOff = await _holidayService.IsHoliday(before, companyId)
                             || await _holidayService.IsWeekOff(before, companyId);

            var afterIsOff = await _holidayService.IsHoliday(after, companyId)
                            || await _holidayService.IsWeekOff(after, companyId);

            return beforeIsOff && afterIsOff;
        }
        private LeaveResponseDto MapToDto(LeaveApplication leave)
        {
            return new LeaveResponseDto
            {
                Id = leave.Id,
                EmployeeName = leave.Employee?.FirstName + " " + leave.Employee?.LastName,
                LeaveType = leave.LeaveType?.Name,
                FromDate = leave.FromDate,
                ToDate = leave.ToDate,
                TotalDays = leave.TotalDays,
                Status = leave.Status.ToString()
            };
        }
        #endregion
    }
}
