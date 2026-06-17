using Application.DTOs.Leaves;
using Application.Interfaces.Leaves;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Application.Services.Leaves
{
    public class LeaveDashboardService : ILeaveDashboardService
    {
        private readonly ApplicationDbContext _db;

        public LeaveDashboardService(ApplicationDbContext db)
        {
            _db = db;
        }

        // ================= SUMMARY =================
        public async Task<LeaveDashboardSummaryDto> GetSummary(string employeeId, int year)
        {
            var data = await _db.LeaveApplications
                .Where(x => x.EmployeeId == employeeId && x.FromDate.Year == year)
                .ToListAsync();

            return new LeaveDashboardSummaryDto
            {
                TotalLeaves = data.Count,
                Approved = data.Count(x => x.Status == ApprovalStatus.Approved),
                Pending = data.Count(x => x.Status == ApprovalStatus.Pending),
                Rejected = data.Count(x => x.Status == ApprovalStatus.Rejected),
                Cancelled = data.Count(x => x.Status == ApprovalStatus.Cancelled)
            };
        }

        // ================= BALANCE =================
        public async Task<List<LeaveBalanceDto>> GetBalances(string employeeId, int year)
        {
            var balances = await _db.LeaveBalances
                .Include(x => x.LeaveType)
                .Where(x => x.EmployeeId == employeeId && x.Year == year)
                .ToListAsync();

            return balances.Select(x => new LeaveBalanceDto
            {
                LeaveTypeName = x.LeaveType.Name,
                OpeningBalance = x.OpeningBalance,
                Earned = x.Earned,
                Used = x.Used,
                Balance = x.Balance
            }).ToList();
        }

        // ================= PENDING APPROVAL =================
        public async Task<List<LeaveResponseDto>> GetPendingApprovals(string managerId)
        {
            var leaves = await _db.LeaveApplications
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .Where(x => x.Status == ApprovalStatus.Pending &&
                            x.Employee.ReportingManagerId == managerId)
                .ToListAsync();

            return leaves.Select(x => new LeaveResponseDto
            {
                Id = x.Id,
                EmployeeName = $"{x.Employee.FirstName} {x.Employee.LastName}",
                LeaveType = x.LeaveType.Name,
                FromDate = x.FromDate,
                ToDate = x.ToDate,
                TotalDays = x.TotalDays,
                Status = x.Status.ToString()
            }).ToList();
        }

        // ================= CALENDAR =================
        public async Task<List<LeaveCalendarDto>> GetCalendar(string employeeId, int year, int month)
        {
            var leaves = await _db.LeaveApplications
                .Include(x => x.LeaveType)
                .Where(x => x.EmployeeId == employeeId &&
                            x.Status == ApprovalStatus.Approved &&
                            x.FromDate.Month <= month &&
                            x.ToDate.Month >= month &&
                            x.FromDate.Year == year)
                .ToListAsync();

            var result = new List<LeaveCalendarDto>();

            foreach (var leave in leaves)
            {
                for (var d = leave.FromDate.Date; d <= leave.ToDate.Date; d = d.AddDays(1))
                {
                    if (d.Month != month) continue;

                    result.Add(new LeaveCalendarDto
                    {
                        Date = d,
                        Status = leave.IsHalfDay ? "HalfDay" : "Leave",
                        LeaveType = leave.LeaveType.Name
                    });
                }
            }

            return result;
        }

        // ================================== STATS ==================================
        public async Task<List<LeaveStatsDto>> GetStats(string employeeId, int year)
        {
            var data = await _db.LeaveApplications
                .Include(x => x.LeaveType)
                .Where(x => x.EmployeeId == employeeId &&
                            x.Status == ApprovalStatus.Approved &&
                            x.FromDate.Year == year)
                .ToListAsync();

            return data
                .GroupBy(x => x.LeaveType.Name)
                .Select(g => new LeaveStatsDto
                {
                    LeaveType = g.Key,
                    UsedDays = g.Sum(x => x.TotalDays)
                })
                .ToList();
        }
    }
}
