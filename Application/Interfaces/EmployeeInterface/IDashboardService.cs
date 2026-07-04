using Application.DTOs.Employee;
using Application.DTOs.KPI;
using Application.DTOs.Leaves;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces.EmployeeInterface
{
    public interface IDashboardService
    {
        //Task<DashboardDto> GetDashboardAsync(string tenantId);

        // Employee
        Task<int> GetTotalEmployeesAsync();
        Task<int> GetActiveEmployeesAsync();
        Task<int> GetNewJoinersAsync(DateTime fromDate, DateTime toDate);
        Task<int> GetResignedEmployeesAsync(DateTime fromDate, DateTime toDate);

        // Attendance
        Task<int> GetPresentTodayAsync();
        Task<int> GetAbsentTodayAsync();
        Task<int> GetLateArrivalsTodayAsync();

        // Leave
        Task<int> GetEmployeesOnLeaveTodayAsync();
        Task<int> GetPendingLeaveApprovalsAsync();

        // Recruitment
        Task<int> GetOpenPositionsAsync();

        // Optional Dashboard Cards
        Task<int> GetTotalCompaniesAsync();
        Task<int> GetTotalBranchesAsync();
        Task<int> GetTodayBirthdaysAsync();
        Task<int> GetTodayWorkAnniversariesAsync();

        Task<int> GetTotalLeaveRequestsAsync();
        Task<int> GetTotalLeavePendingRequestsAsync();
        Task<int> GetTotalLeaveApprovedRequestsAsync();
        Task<int> GetTotalLeaveRejectedRequestsAsync();

        // Leave
        Task<List<RecentLeaveRequestDto>> GetRecentLeaveRequestsAsync(int take = 10);

        // Events
        Task<UpcomingEventsDashboardDto> GetUpcomingEventsAsync(int days = 30);

        // Birthdays
        //Task<List<EmployeeBirthdayDto>> GetUpcomingBirthdaysAsync(int days = 30);
        Task<BirthdayDashboardDto> GetBirthdayDashboardAsync(int days = 30);

        // Announcements
        //Task<List<AnnouncementDto>> GetActiveAnnouncementsAsync();
        Task<AnnouncementDashboardDto> GetAnnouncementDashboardAsync(int take = 10);

        // Department
        //Task<List<DepartmentHeadcountDto>> GetDepartmentHeadcountAsync();
        Task<DepartmentHeadcountDashboardDto> GetDepartmentHeadcountAsync(string tenantId);

        //Multi-Tenant
        //Task<List<DepartmentHeadcountDto>> GetDepartmentHeadcountAsync(string userId, string tenantId);

        // Dashboard Summary
        Task<DashboardKpiDto> GetDashboardAsync(string tenantId);
    }
}
