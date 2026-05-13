using Application.DTOs.Leaves;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces.Leaves
{
    public interface ILeaveDashboardService
    {
        Task<LeaveDashboardSummaryDto> GetSummary(string employeeId, int year);
        Task<List<LeaveBalanceDto>> GetBalances(string employeeId, int year);
        Task<List<LeaveResponseDto>> GetPendingApprovals(string managerId);
        Task<List<LeaveCalendarDto>> GetCalendar(string employeeId, int year, int month);
        Task<List<LeaveStatsDto>> GetStats(string employeeId, int year);
    }
}
