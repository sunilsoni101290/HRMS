using System;
using System.Collections.Generic;

namespace Application.DTOs.Dashboard
{
    public class EmployeeDashboardDto
    {
        // Profile
        public string? EmployeeId { get; set; }
        public string FullName { get; set; } = "";
        public string? EmployeeCode { get; set; }
        public string? DepartmentName { get; set; }
        public string? DesignationName { get; set; }
        public string? PhotoUrl { get; set; }
        public DateTime? JoiningDate { get; set; }
        public int ProfileCompletionPercent { get; set; }

        // Attendance Today
        public DateTime? CheckIn { get; set; }
        public DateTime? CheckOut { get; set; }
        public decimal WorkingHours { get; set; }
        public string AttendanceStatus { get; set; } = "—";

        // KPIs
        public int PresentDaysThisMonth { get; set; }
        public int WorkingDaysThisMonth { get; set; }
        public int AttendancePercent { get; set; }
        public decimal TotalLeaveBalance { get; set; }
        public int PendingTaskCount { get; set; }
        public int PendingLeaveCount { get; set; }
        public int UnreadNotificationCount { get; set; }

        // Widgets
        public List<LeaveBalanceItemDto> LeaveBalances { get; set; } = new();
        public List<HolidayItemDto> UpcomingHolidays { get; set; } = new();
        public List<LeaveRequestItemDto> PendingLeaveRequests { get; set; } = new();
        public List<DayAttendanceDto> WeekAttendance { get; set; } = new();
        public RecentPayslipDto? RecentPayslip { get; set; }
        public List<AnnouncementItemDto> Announcements { get; set; } = new();
        public List<EventItemDto> UpcomingEvents { get; set; } = new();
        public List<PersonItemDto> Birthdays { get; set; } = new();
        public List<PersonItemDto> WorkAnniversaries { get; set; } = new();
        public List<TaskItemDto> MyTasks { get; set; } = new();
        public List<ActivityItemDto> RecentActivity { get; set; } = new();

        // Charts
        public List<TrendPointDto> AttendanceTrend { get; set; } = new();   // last 6 months
        public List<LeaveSliceDto> LeaveDistribution { get; set; } = new(); // used per type
    }

    public class LeaveBalanceItemDto
    {
        public string? LeaveTypeName { get; set; }
        public string? Code { get; set; }
        public decimal Balance { get; set; }
    }

    public class HolidayItemDto
    {
        public string? Name { get; set; }
        public DateTime Date { get; set; }
    }

    public class LeaveRequestItemDto
    {
        public string? LeaveTypeName { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalDays { get; set; }
        public string? StatusText { get; set; }
    }

    public class DayAttendanceDto
    {
        public DateTime Date { get; set; }
        public string DayName { get; set; } = "";
        public string Status { get; set; } = "";  // Present / Absent / HalfDay / Leave / WeekOff / Holiday / None
    }

    public class RecentPayslipDto
    {
        public string? MonthName { get; set; }
        public int Year { get; set; }
        public decimal NetSalary { get; set; }
        public DateTime PayDate { get; set; }
        public string? Status { get; set; }
    }

    public class AnnouncementItemDto
    {
        public string? Title { get; set; }
        public string? Message { get; set; }
        public DateTime PublishDate { get; set; }
        public string? TypeText { get; set; }
    }

    public class EventItemDto
    {
        public string? Title { get; set; }
        public DateTime StartDate { get; set; }
        public string? TypeText { get; set; }
        public string? Location { get; set; }
    }

    public class PersonItemDto
    {
        public string? Name { get; set; }
        public string? DepartmentName { get; set; }
        public DateTime Date { get; set; }
        public string? PhotoUrl { get; set; }
        public int Years { get; set; } // for anniversaries
    }

    public class TaskItemDto
    {
        public string Id { get; set; } = "";
        public string? Title { get; set; }
        public DateTime? DueDate { get; set; }
        public string? Status { get; set; }
        public string? Priority { get; set; }
    }

    public class ActivityItemDto
    {
        public string? Text { get; set; }
        public DateTime Date { get; set; }
        public string? Icon { get; set; }
    }

    public class TrendPointDto
    {
        public string? Label { get; set; }
        public int Value { get; set; }
    }

    public class LeaveSliceDto
    {
        public string? Name { get; set; }
        public decimal Value { get; set; }
    }

    public class EmployeeProfileDto
    {
        public string EmployeeId { get; set; }

        public string EmployeeCode { get; set; }

        public string FullName { get; set; }

        public string Designation { get; set; }

        public string Department { get; set; }

        public string Branch { get; set; }

        public string Company { get; set; }

        public string ProfileImage { get; set; }

        public DateTime JoiningDate { get; set; }

        public int ExperienceYears { get; set; }

        public string Email { get; set; }

        public string Mobile { get; set; }

        public decimal ProfileCompletion { get; set; }

        public bool IsCheckedIn { get; set; }

        public string ReportingManager { get; set; }

        public string EmploymentType { get; set; }

        public string EmploymentStatus { get; set; }
    }

    public class LeaveDto
    {
        public decimal CasualLeave { get; set; }

        public decimal EarnedLeave { get; set; }

        public decimal SickLeave { get; set; }

        public decimal CompOff { get; set; }

        public decimal TotalAvailable { get; set; }

        public decimal UsedLeave { get; set; }

        public decimal PendingApproval { get; set; }

        public decimal ApprovedLeave { get; set; }

        public decimal RejectedLeave { get; set; }
    }

    public class PayslipDto
    {
        public string SalaryMonth { get; set; }

        public decimal GrossSalary { get; set; }

        public decimal TotalAllowance { get; set; }

        public decimal TotalDeduction { get; set; }

        public decimal NetSalary { get; set; }

        public DateTime SalaryDate { get; set; }

        public bool IsPaid { get; set; }

        public string PayslipUrl { get; set; }
    }

    public class TaskDto
    {
        public long Id { get; set; }

        public string TaskName { get; set; }

        public string Description { get; set; }

        public DateTime DueDate { get; set; }

        public string Status { get; set; }

        public int Progress { get; set; }

        public string Priority { get; set; }
    }

    public class QuickLinkDto
    {
        public string Title { get; set; }

        public string Icon { get; set; }

        public string Url { get; set; }

        public string Color { get; set; }

        public int DisplayOrder { get; set; }
    }

    public class ExpenseDto
    {
        public decimal TotalClaimed { get; set; }

        public decimal ApprovedAmount { get; set; }

        public decimal PendingAmount { get; set; }

        public decimal RejectedAmount { get; set; }

        public int PendingClaims { get; set; }
    }

    public class PerformanceDto
    {
        public decimal CurrentRating { get; set; }

        public decimal PreviousRating { get; set; }

        public decimal GoalCompletion { get; set; }

        public int TotalGoals { get; set; }

        public int CompletedGoals { get; set; }

        public string LastAppraisal { get; set; }

        public string PerformanceGrade { get; set; }
    }

    public class DocumentDto
    {
        public long Id { get; set; }

        public string DocumentName { get; set; }

        public string DocumentType { get; set; }

        public string FileName { get; set; }

        public string FileUrl { get; set; }

        public DateTime UploadDate { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public bool IsVerified { get; set; }

        public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value.Date < DateTime.Today;
    }
}
