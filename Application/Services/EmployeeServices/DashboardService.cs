using Application.DTOs.Employee;
using Application.Interfaces.EmployeeInterface;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Application.Services.EmployeeServices
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _db;

        public DashboardService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<DashboardDto> GetDashboardAsync(string tenantId)
        {
            var today = DateTime.UtcNow.Date;

            var employees = _db.Employees
                .Where(x => x.TenantId == tenantId && !x.IsDeleted);

            // =========================
            // 👥 Employee Stats
            // =========================
            var totalEmployees = await employees.CountAsync();
            var activeEmployees = await employees.CountAsync(x => x.RelievingDate == null);
            var inactiveEmployees = totalEmployees - activeEmployees;

            // =========================
            // 🕒 Attendance
            // =========================
            var presentToday = await _db.Attendances
                .CountAsync(x => x.Date == today && x.Status == AttendanceStatus.Present);

            var absentToday = totalEmployees - presentToday;

            // =========================
            // 🌴 Leave
            // =========================
            var onLeaveToday = await _db.LeaveApplications
                .CountAsync(x =>
                    x.FromDate <= today &&
                    x.ToDate >= today &&
                    x.Status == ApprovalStatus.Approved);

            // =========================
            // 🎉 Upcoming Birthdays (next 7 days)
            // =========================
            var next7Days = today.AddDays(7);

            var birthdays = await employees
                .Where(x => x.DateOfBirth.HasValue &&
                    x.DateOfBirth.Value.Month >= today.Month &&
                    x.DateOfBirth.Value.Month <= next7Days.Month)
                .Take(5)
                .Select(x => new EmployeeMiniDto
                {
                    Id = x.Id,
                    Name = x.FirstName + " " + x.LastName,
                    Designation = x.Designation.Name
                })
                .ToListAsync();

            // =========================
            // 🆕 Recent Joinees
            // =========================
            var recentJoinees = await employees
                .OrderByDescending(x => x.JoiningDate)
                .Take(5)
                .Select(x => new EmployeeMiniDto
                {
                    Id = x.Id,
                    Name = x.FirstName + " " + x.LastName,
                    Designation = x.Designation.Name
                })
                .ToListAsync();

            // =========================
            // 🔔 Notifications
            // =========================
            var notifications = await _db.Notifications
                .OrderByDescending(x => x.CreatedOn)
                .Take(5)
                .Select(x => x.Message)
                .ToListAsync();

            return new DashboardDto
            {
                TotalEmployees = totalEmployees,
                ActiveEmployees = activeEmployees,
                InactiveEmployees = inactiveEmployees,

                PresentToday = presentToday,
                AbsentToday = absentToday,

                OnLeaveToday = onLeaveToday,

                RecentJoinees = recentJoinees,
                UpcomingBirthdays = birthdays,

                Notifications = notifications
            };
        }
    }
}
