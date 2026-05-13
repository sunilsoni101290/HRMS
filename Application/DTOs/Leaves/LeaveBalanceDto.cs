using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Leaves
{
    public class LeaveBalanceDto
    {
        public string LeaveType { get; set; }
        public decimal Opening { get; set; }
        public decimal Earned { get; set; }
        public decimal Used { get; set; }
        public decimal Balance { get; set; }
    }

    public class LeaveDashboardSummaryDto
    {
        public int TotalLeaves { get; set; }
        public int Approved { get; set; }
        public int Pending { get; set; }
        public int Rejected { get; set; }
        public int Cancelled { get; set; }
    }

    public class LeaveCalendarDto
    {
        public DateTime Date { get; set; }
        public string Status { get; set; } // Leave / HalfDay / Present
        public string LeaveType { get; set; }
    }

    public class LeaveStatsDto
    {
        public string LeaveType { get; set; }
        public decimal UsedDays { get; set; }
    }
}
