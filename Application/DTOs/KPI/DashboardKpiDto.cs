using Application.DTOs.Employee;
using System;
using System.Collections.Generic;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Application.DTOs.KPI
{
    public class DashboardKpiDto
    {
        // Employee
        public int TotalEmployees { get; set; }
        public int ActiveEmployees { get; set; }
        public int NewJoiners { get; set; }
        public int ResignedEmployees { get; set; }

        // Attendance
        public int PresentToday { get; set; }
        public int AbsentToday { get; set; }
        public int LateArrivals { get; set; }

        // Leave
        public int OnLeaveToday { get; set; }
        public int PendingApprovals { get; set; }

        // Summary
        public int TotalRequests { get; set; }
        public int PendingRequests { get; set; }
        public int ApprovedRequests { get; set; }
        public int RejectedRequests { get; set; }

        //Events
        public int TotalUpcomingEvents { get; set; }
        public int BirthdayCount { get; set; }
        public int HolidayCount { get; set; }
        public int TodayEventCount { get; set; }

        // Recruitment
        public int OpenPositions { get; set; }

        // Optional
        public int TotalCompanies { get; set; }
        public int TotalBranches { get; set; }
        public int TodayBirthdays { get; set; }
        public int TodayWorkAnniversaries { get; set; }
        public List<RecentLeaveRequestDto>? RecentLeaveRequests { get; set; } = new();
        public UpcomingEventsDashboardDto? UpcomingDashboardEvents { get; set; } = new();
        public BirthdayDashboardDto? EmployeeBirthdays { get; set; } = new();
        public AnnouncementDashboardDto? AnnouncementDashboard { get; set; } = new();
        public DepartmentHeadcountDashboardDto? DepartmentDashboard { get; set; } = new();
    }


    public class RecentLeaveRequestDto
    {
        public string Id { get; set; }

        public string EmployeeName { get; set; }

        public string Designation { get; set; }

        public string LeaveType { get; set; }

        public DateTime FromDate { get; set; }

        public DateTime ToDate { get; set; }

        public decimal TotalDays { get; set; }

        public ApprovalStatus Status { get; set; }

        public string? EmployeePhoto { get; set; }
    }

    public class UpcomingEventsDashboardDto
    {
        public int TotalUpcomingEvents { get; set; }

        public int BirthdayCount { get; set; }

        public int HolidayCount { get; set; }

        public int EventCount { get; set; }

        public int TodayEventCount { get; set; }

        public List<UpcomingEventDto> Events { get; set; } = new();
    }

    public class UpcomingEventDto
    {
        public string Id { get; set; }

        public string Title { get; set; }

        public string EventType { get; set; }

        public DateTime EventDate { get; set; }

        public string? Department { get; set; }

        public string? Description { get; set; }

        public string Color { get; set; }

        public string Icon { get; set; }

        public string? Location { get; set; }

        public TimeSpan? StartTime { get; set; }

        public TimeSpan? EndTime { get; set; }
    }

    public class EmployeeBirthdayDto
    {
        public string Id { get; set; }

        public string EmployeeName { get; set; }

        public string? Department { get; set; }
        public string? Designation { get; set; }
        public DateTime DateOfBirth { get; set; }

        public DateTime Birthday { get; set; }

        public int Age { get; set; }
        public string? Photo { get; set; }
        public bool IsToday { get; set; }
        public int DaysLeft { get; set; }
    }

    public class BirthdayDashboardDto
    {
        public int BirthdaysThisMonth { get; set; }

        public int BirthdaysToday { get; set; }

        public int BirthdaysThisWeek { get; set; }

        public List<EmployeeBirthdayDto> Birthdays { get; set; } = new();
    }

    public class AnnouncementDashboardDto
    {
        public int ActiveAnnouncements { get; set; }

        public int ThisWeekAnnouncements { get; set; }

        public int HighPriorityAnnouncements { get; set; }

        public List<AnnouncementDto> Announcements { get; set; } = new();
    }

    public class AnnouncementDto
    {
        public string Id { get; set; }

        public string Title { get; set; }

        public string? Description { get; set; }
        public string? Department { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; }

        public string TenantId { get; set; }

        public AnnouncementPriority Priority { get; set; }

        public string Color { get; set; }

        public string Icon { get; set; }

        public AnnouncementType Type { get; set; }

        public string? PostedBy { get; set; }

        public DateTime PublishDate { get; set; }
    }

    public class DepartmentHeadcountDashboardDto
    {
        public int TotalEmployees { get; set; }

        public int TotalDepartments { get; set; }

        public int NewJoiners { get; set; }

        public List<DepartmentHeadcountDto> Departments { get; set; } = new();
    }
    public class DepartmentHeadcountDto
    {
        public string DepartmentId { get; set; }

        public string DepartmentName { get; set; }

        public int EmployeeCount { get; set; }

        public decimal Percentage { get; set; }

        public string Color { get; set; } = "primary";
    }
}
