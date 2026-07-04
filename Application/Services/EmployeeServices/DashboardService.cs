using Application.DTOs.Employee;
using Application.DTOs.KPI;
using Application.Interfaces.EmployeeInterface;
using Domain.Entities;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using static Domain.Enums.EnumExtensions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Application.Services.EmployeeServices
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;

        public DashboardService(ApplicationDbContext db)
        {
            _context = db;
        }
        public async Task<int> GetTotalEmployeesAsync()
        {
            try
            {
            return await _context.Employees.CountAsync(x => !x.IsDeleted);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public async Task<int> GetActiveEmployeesAsync()
        {
            try
            {
            return await _context.Employees
                .CountAsync(x => x.IsActive);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public async Task<int> GetNewJoinersAsync(
            DateTime fromDate,
            DateTime toDate)
        {
            try
            {
            return await _context.Employees
                .CountAsync(x =>
                    x.JoiningDate >= fromDate &&
                    x.JoiningDate <= toDate);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public async Task<int> GetResignedEmployeesAsync(
            DateTime fromDate,
            DateTime toDate)
        {
            try
            {
            return await _context.Employees
                .CountAsync(x =>
                    x.RelievingDate != null &&
                    x.RelievingDate >= fromDate &&
                    x.RelievingDate <= toDate);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public async Task<int> GetPresentTodayAsync()
        {
            try
            {
            var today = DateTime.Today;

            return await _context.AttendanceLogs
                .CountAsync(x =>
                    x.Attendance.Date == today &&
                    x.Attendance.Status == AttendanceStatus.Present);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public async Task<int> GetAbsentTodayAsync()
        {
            try
            {
            var today = DateTime.Today;

            return await _context.AttendanceLogs
                .CountAsync(x =>
                    x.Attendance.Date == today &&
                    x.Attendance.Status == AttendanceStatus.Absent);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public async Task<int> GetLateArrivalsTodayAsync()
        {
            try
            {
            var today = DateTime.Today;

            return await _context.AttendanceLogs
                .CountAsync(x =>
                    x.Attendance.Date == today &&
                    x.Attendance.IsLate);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public async Task<int> GetEmployeesOnLeaveTodayAsync()
        {
            try
            {
            var today = DateTime.Today;

            return await _context.LeaveApplications
                .CountAsync(x =>
                    x.Status == ApprovalStatus.Approved &&
                    x.FromDate.Date <= today &&
                    x.ToDate.Date >= today);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public async Task<int> GetPendingLeaveApprovalsAsync()
        {
            try
            {
            return await _context.LeaveApplications
                .CountAsync(x =>
                    x.Status == ApprovalStatus.Pending);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public async Task<int> GetOpenPositionsAsync()
        {
            try
            {
            return await _context.JobOpenings
                .CountAsync(x => x.IsActive);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public async Task<int> GetTotalCompaniesAsync()
        {
            try
            {
            return await _context.Companies.CountAsync();
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public async Task<int> GetTotalBranchesAsync()
        {
            try
            {
            return await _context.Branches.CountAsync();
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public async Task<int> GetTodayBirthdaysAsync()
        {
            try
            {
            var today = DateTime.Today;

            return await _context.Employees.CountAsync(x =>
            x.DateOfBirth.HasValue &&
            x.DateOfBirth.Value.Month == today.Month &&
            x.DateOfBirth.Value.Day == today.Day);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public async Task<int> GetTodayWorkAnniversariesAsync()
        {
            try
            {
            var today = DateTime.Today;

            return await _context.Employees
                .CountAsync(x =>
                    x.JoiningDate.Month == today.Month &&
                    x.JoiningDate.Day == today.Day);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public async Task<List<RecentLeaveRequestDto>> GetRecentLeaveRequestsAsync(int take = 10)
        {
            try
            {
            return await _context.LeaveApplications
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .OrderByDescending(x => x.CreatedOn)
                .Take(take)
                .Select(x => new RecentLeaveRequestDto
                {
                    Id = x.Id,

                    EmployeeName = x.Employee.FirstName + " " + x.Employee.LastName,

                    Designation = x.Employee.Designation != null
                                    ? x.Employee.Designation.Name
                                    : string.Empty,

                    LeaveType = x.LeaveType.Name,

                    FromDate = x.FromDate,

                    ToDate = x.ToDate,

                    TotalDays = x.TotalDays,

                    Status = x.Status,

                    EmployeePhoto = x.Employee.FilePath
                })
                .ToListAsync();
            }
            catch (Exception)
            {
                return new List<RecentLeaveRequestDto>();
            }
        }

        public async Task<UpcomingEventsDashboardDto> GetUpcomingEventsAsync(int days = 30)
        {
            try
            {
            var today = DateTime.Today;
            var endDate = today.AddDays(days);

            var events = new List<UpcomingEventDto>();

            #region Company Events

            var companyEvents = await _context.Events
                .AsNoTracking()
                .Include(x => x.Department)
                .Where(x => x.StartDate.Date >= today &&
                            x.StartDate.Date <= endDate)
                .Select(x => new UpcomingEventDto
                {
                    Id = x.Id,
                    Title = x.Title,
                    EventType = x.EventType.ToString(),
                    EventDate = x.StartDate,
                    Department = x.Department.Name,
                    Description = x.Description,
                    Location = x.Location,
                    StartTime = x.StartTime,
                    EndTime = x.EndTime,

                    Color =
                        x.EventType == EventType.Meeting ? "primary" :
                        x.EventType == EventType.Training ? "info" :
                        x.EventType == EventType.Celebration ? "success" :
                        x.EventType == EventType.Holiday ? "danger" :
                        "default",

                    Icon =
                        x.EventType == EventType.Meeting ? "icon-users" :
                        x.EventType == EventType.Training ? "icon-graduation2" :
                        x.EventType == EventType.Celebration ? "icon-gift" :
                        x.EventType == EventType.Holiday ? "icon-flag3" :
                        "icon-calendar3"
                })
                .ToListAsync();

            events.AddRange(companyEvents);

            #endregion

            #region Birthdays

            var employees = await _context.Employees
                .AsNoTracking()
                .Include(x => x.Department)
                .Where(x => x.DateOfBirth.HasValue)
                .ToListAsync();

            foreach (var emp in employees)
            {
                var dob = emp.DateOfBirth!.Value;

                var day = Math.Min(
                    dob.Day,
                    DateTime.DaysInMonth(today.Year, dob.Month));

                var birthday = new DateTime(today.Year, dob.Month, day);

                if (birthday < today)
                {
                    day = Math.Min(
                        dob.Day,
                        DateTime.DaysInMonth(today.Year + 1, dob.Month));

                    birthday = new DateTime(today.Year + 1, dob.Month, day);
                }

                if (birthday <= endDate)
                {
                    events.Add(new UpcomingEventDto
                    {
                        Id = emp.Id,
                        Title = $"{emp.FirstName} {emp.LastName} Birthday",
                        EventType = "Birthday",
                        EventDate = birthday,
                        Department = emp.Department?.Name,
                        Description = "Birthday",
                        Color = "success",
                        Icon = "icon-gift"
                    });
                }
            }

            #endregion

            #region Holidays

            var holidays = await _context.HolidayGroupDetails
                .AsNoTracking()
                .Where(x => x.HolidayDate >= today &&
                            x.HolidayDate <= endDate)
                .Select(x => new UpcomingEventDto
                {
                    Id = x.Id,
                    Title = x.HolidayName,
                    EventType = "Holiday",
                    EventDate = x.HolidayDate,
                    Description = x.Remarks,
                    Color = "danger",
                    Icon = "icon-flag3"
                })
                .ToListAsync();

            events.AddRange(holidays);

            #endregion

            events = events
                .OrderBy(x => x.EventDate)
                .ToList();

            return new UpcomingEventsDashboardDto
            {
                Events = events,

                TotalUpcomingEvents = events.Count,

                BirthdayCount = events.Count(x => x.EventType == "Birthday"),

                HolidayCount = events.Count(x => x.EventType == "Holiday"),

                EventCount = events.Count(x =>
                    x.EventType != "Birthday" &&
                    x.EventType != "Holiday"),

                TodayEventCount = events.Count(x =>
                    x.EventDate.Date == today)
            };
            }
            catch (Exception)
            {
                return null;
            }
        }

        //public async Task<List<EmployeeBirthdayDto>> GetUpcomingBirthdaysAsync(int days = 30)
        //{
        //    var today = DateTime.Today;
        //    var endDate = today.AddDays(days);

        //    var employees = await _context.Employees
        //        .AsNoTracking()
        //        .Include(x => x.Department)
        //        .Where(x => x.DateOfBirth.HasValue)
        //        .ToListAsync();

        //    var birthdays = employees
        //        .Select(x =>
        //        {
        //            var dob = x.DateOfBirth!.Value;

        //            int day = Math.Min(
        //                dob.Day,
        //                DateTime.DaysInMonth(today.Year, dob.Month));

        //            var nextBirthday = new DateTime(today.Year, dob.Month, day);

        //            if (nextBirthday < today)
        //            {
        //                day = Math.Min(
        //                    dob.Day,
        //                    DateTime.DaysInMonth(today.Year + 1, dob.Month));

        //                nextBirthday = new DateTime(today.Year + 1, dob.Month, day);
        //            }

        //            return new EmployeeBirthdayDto
        //            {
        //                EmployeeId = x.Id,
        //                EmployeeName = $"{x.FirstName} {x.LastName}",
        //                Department = x.Department?.Name,
        //                DateOfBirth = dob,
        //                Birthday = nextBirthday,
        //                Age = nextBirthday.Year - dob.Year
        //            };
        //        })
        //        .Where(x => x.Birthday <= endDate)
        //        .OrderBy(x => x.Birthday)
        //        .ToList();

        //    return birthdays;
        //}

        //public async Task<List<AnnouncementDto>> GetActiveAnnouncementsAsync()
        //{
        //    var today = DateTime.Today;

        //    return await _context.Announcements
        //        .AsNoTracking()
        //        .Where(x => x.IsActive && x.ExpiryDate.HasValue
        //                 && x.PublishDate.Date <= today
        //                 && x.ExpiryDate.Value.Date >= today)
        //        .OrderByDescending(x => x.Priority)
        //        .ThenByDescending(x => x.PublishDate)
        //        .Select(x => new AnnouncementDto
        //        {
        //            Id = x.Id,
        //            Title = x.Title,
        //            Description = x.Message,
        //            StartDate = x.PublishDate,
        //            EndDate = x.ExpiryDate ?? DateTime.Today,
        //            IsHighPriority = x.Priority,
        //            Type = x.AnnouncementType,
        //            IsActive = x.IsActive
        //        })
        //        .ToListAsync();
        //}

        //public async Task<List<DepartmentHeadcountDto>> GetDepartmentHeadcountAsync()
        //{
        //    // Total Employees
        //    var totalEmployees = await _context.Employees
        //        .AsNoTracking()
        //        .CountAsync();

        //    if (totalEmployees == 0)
        //        return new List<DepartmentHeadcountDto>();

        //    var result = await _context.Departments
        //        .AsNoTracking()
        //        .Select(d => new DepartmentHeadcountDto
        //        {
        //            DepartmentId = d.Id,
        //            DepartmentName = d.Name,
        //            HeadCount = d.Employees.Count()
        //        })
        //        .OrderByDescending(x => x.HeadCount)
        //        .ToListAsync();

        //    foreach (var item in result)
        //    {
        //        item.Percentage = Math.Round(
        //            (decimal)item.HeadCount * 100 / totalEmployees,
        //            2);
        //    }

        //    return result;
        //}

        //Multi-Tenant
        //public async Task<List<DepartmentHeadcountDto>> GetDepartmentHeadcountAsync(string userId, string tenantId)
        //{
        //    // Total Employees
        //    var totalEmployees = await _context.Employees
        //        .AsNoTracking()
        //        .CountAsync(x => x.TenantId == tenantId);

        //    if (totalEmployees == 0)
        //        return new List<DepartmentHeadcountDto>();

        //    var result = await _context.Departments
        //        .AsNoTracking()
        //        .Where(d => d.TenantId == tenantId)
        //        .Select(d => new DepartmentHeadcountDto
        //        {
        //            DepartmentId = d.Id,
        //            DepartmentName = d.Name,
        //            HeadCount = d.Employees.Count(e => e.TenantId == tenantId)
        //        })
        //        .OrderByDescending(x => x.HeadCount)
        //        .ToListAsync();

        //    foreach (var item in result)
        //    {
        //        item.Percentage = Math.Round(
        //            (decimal)item.HeadCount * 100 / totalEmployees,
        //            2);
        //    }

        //    return result;
        //}

        public async Task<DashboardKpiDto> GetDashboardAsync(string tenantId)
        {
            try
            {
            var today = DateTime.Today;

            return new DashboardKpiDto
            {
                TotalEmployees = await GetTotalEmployeesAsync(),
                ActiveEmployees = await GetActiveEmployeesAsync(),
                NewJoiners = await GetNewJoinersAsync(new DateTime(today.Year, today.Month, 1),today),

                ResignedEmployees = await GetResignedEmployeesAsync(new DateTime(today.Year, today.Month, 1),today),

                PresentToday = await GetPresentTodayAsync(),
                AbsentToday = await GetAbsentTodayAsync(),
                LateArrivals = await GetLateArrivalsTodayAsync(),

                OnLeaveToday = await GetEmployeesOnLeaveTodayAsync(),
                PendingApprovals = await GetPendingLeaveApprovalsAsync(),

                OpenPositions = await GetOpenPositionsAsync(),

                TotalCompanies = await GetTotalCompaniesAsync(),
                TotalBranches = await GetTotalBranchesAsync(),

                TodayBirthdays = await GetTodayBirthdaysAsync(),
                TodayWorkAnniversaries = await GetTodayWorkAnniversariesAsync(),

                AnnouncementDashboard = await GetAnnouncementDashboardAsync(),
                RecentLeaveRequests = await GetRecentLeaveRequestsAsync(),
                UpcomingDashboardEvents = await GetUpcomingEventsAsync(),
                EmployeeBirthdays = await GetBirthdayDashboardAsync(),
                DepartmentDashboard = await GetDepartmentHeadcountAsync(tenantId),

                TotalRequests = await GetTotalLeaveRequestsAsync(),
                PendingRequests = await GetTotalLeavePendingRequestsAsync(),
                ApprovedRequests = await GetTotalLeaveApprovedRequestsAsync(),
                RejectedRequests = await GetTotalLeaveRejectedRequestsAsync()
            };
            }
            catch (Exception)
            {
                return null;
            }
        }
        //public async Task<DashboardDto> GetDashboardAsync(string tenantId)
        //{
        //    var today = DateTime.UtcNow.Date;

        //    var employees = _context.Employees
        //        .Where(x => x.TenantId == tenantId && !x.IsDeleted);

        //    // =========================
        //    // 👥 Employee Stats
        //    // =========================
        //    var totalEmployees = await employees.CountAsync();
        //    var activeEmployees = await employees.CountAsync(x => x.RelievingDate == null);
        //    var inactiveEmployees = totalEmployees - activeEmployees;

        //    // =========================
        //    // 🕒 Attendance
        //    // =========================
        //    var presentToday = await _context.Attendances
        //        .CountAsync(x => x.Date == today && x.Status == AttendanceStatus.Present);

        //    var absentToday = totalEmployees - presentToday;

        //    // =========================
        //    // 🌴 Leave
        //    // =========================
        //    var onLeaveToday = await _context.LeaveApplications
        //        .CountAsync(x =>
        //            x.FromDate <= today &&
        //            x.ToDate >= today &&
        //            x.Status == ApprovalStatus.Approved);

        //    // =========================
        //    // 🎉 Upcoming Birthdays (next 7 days)
        //    // =========================
        //    var next7Days = today.AddDays(7);

        //    var birthdays = await employees
        //        .Where(x => x.DateOfBirth.HasValue &&
        //            x.DateOfBirth.Value.Month >= today.Month &&
        //            x.DateOfBirth.Value.Month <= next7Days.Month)
        //        .Take(5)
        //        .Select(x => new EmployeeMiniDto
        //        {
        //            Id = x.Id,
        //            Name = x.FirstName + " " + x.LastName,
        //            Designation = x.Designation.Name
        //        })
        //        .ToListAsync();

        //    // =========================
        //    // 🆕 Recent Joinees
        //    // =========================
        //    var recentJoinees = await employees
        //        .OrderByDescending(x => x.JoiningDate)
        //        .Take(5)
        //        .Select(x => new EmployeeMiniDto
        //        {
        //            Id = x.Id,
        //            Name = x.FirstName + " " + x.LastName,
        //            Designation = x.Designation.Name
        //        })
        //        .ToListAsync();

        //    // =========================
        //    // 🔔 Notifications
        //    // =========================
        //    var notifications = await _context.Notifications
        //        .OrderByDescending(x => x.CreatedOn)
        //        .Take(5)
        //        .Select(x => x.Message)
        //        .ToListAsync();

        //    return new DashboardDto
        //    {
        //        TotalEmployees = totalEmployees,
        //        ActiveEmployees = activeEmployees,
        //        InactiveEmployees = inactiveEmployees,

        //        PresentToday = presentToday,
        //        AbsentToday = absentToday,

        //        OnLeaveToday = onLeaveToday,

        //        RecentJoinees = recentJoinees,
        //        UpcomingBirthdays = birthdays,

        //        Notifications = notifications
        //    };
        //}

        public async Task<int> GetTotalLeaveRequestsAsync()
        {
            try
            {
            var query = _context.LeaveApplications
            .AsNoTracking();
            return await query.CountAsync();

            }
            catch (Exception)
            {
                return 0;
            }
        }

        public async Task<int> GetTotalLeavePendingRequestsAsync()
        {
            try
            {
            var query = _context.LeaveApplications
            .AsNoTracking();

            return await query.CountAsync(x => x.Status == ApprovalStatus.Pending);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public async Task<int> GetTotalLeaveApprovedRequestsAsync()
        {
            try
            {
            var query = _context.LeaveApplications
            .AsNoTracking();
            return await query.CountAsync(x => x.Status == ApprovalStatus.Approved);

            }
            catch (Exception)
            {
                return 0;
            }
        }

        public async Task<int> GetTotalLeaveRejectedRequestsAsync()
        {
            try
            {
            var query = _context.LeaveApplications
            .AsNoTracking();
            return await query.CountAsync(x => x.Status == ApprovalStatus.Rejected);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public async Task<BirthdayDashboardDto> GetBirthdayDashboardAsync(int days = 30)
        {
            try
            {
            var today = DateTime.Today;
            var endDate = today.AddDays(days);

            var employees = await _context.Employees
                .AsNoTracking()
                .Include(x => x.Designation)
                .Where(x => x.DateOfBirth.HasValue)
                .ToListAsync();

            var birthdays = new List<EmployeeBirthdayDto>();

            foreach (var emp in employees)
            {
                var dob = emp.DateOfBirth!.Value;

                int day = Math.Min(
                    dob.Day,
                    DateTime.DaysInMonth(today.Year, dob.Month));

                var nextBirthday = new DateTime(today.Year, dob.Month, day);

                if (nextBirthday < today)
                {
                    day = Math.Min(
                        dob.Day,
                        DateTime.DaysInMonth(today.Year + 1, dob.Month));

                    nextBirthday = new DateTime(today.Year + 1, dob.Month, day);
                }

                if (nextBirthday <= endDate)
                {
                    birthdays.Add(new EmployeeBirthdayDto
                    {
                        Id = emp.Id,
                        EmployeeName = emp.FirstName + " " + emp.LastName,
                        Designation = emp.Designation?.Name,
                        Department = emp.Department?.Name,
                        Photo = emp.FilePath,
                        DateOfBirth = dob,
                        Birthday = nextBirthday,
                        Age = nextBirthday.Year - dob.Year,
                        DaysLeft = (nextBirthday - today).Days
                    });
                }
            }

            birthdays = birthdays
                .OrderBy(x => x.Birthday)
                .ToList();

            return new BirthdayDashboardDto
            {
                Birthdays = birthdays,

                BirthdaysToday = birthdays.Count(x => x.DaysLeft == 0),

                BirthdaysThisWeek = birthdays.Count(x => x.DaysLeft <= 7),

                BirthdaysThisMonth = birthdays.Count(x =>
                    x.Birthday.Month == today.Month)
            };
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<AnnouncementDashboardDto> GetAnnouncementDashboardAsync(int take = 10)
        {
            try
            {
            var today = DateTime.Today;

            var announcements = await _context.Announcements
                .AsNoTracking()
                .Where(x => x.IsActive &&
                            x.PublishDate <= today &&
                            x.ExpiryDate >= today)
                .OrderByDescending(x => x.Priority)
                .ThenByDescending(x => x.PublishDate)
                .Take(take)
                .Select(x => new AnnouncementDto
                {
                    Id = x.Id,
                    Title = x.Title,
                    Description = x.Message,
                    StartDate = x.PublishDate,
                    Department = x.Department.Name,
                    Type = x.AnnouncementType,
                    Priority = x.Priority,

                    Color =
                        x.Priority == AnnouncementPriority.High ? "danger" :
                        x.Priority == AnnouncementPriority.Medium ? "warning" :
                        "primary",

                    Icon =
                        x.AnnouncementType == AnnouncementType.General ? "icon-megaphone" :
                        x.AnnouncementType == AnnouncementType.Important ? "icon-warning22" :
                        x.AnnouncementType == AnnouncementType.Achievement ? "icon-trophy3" :
                        "icon-newspaper"
                })
                .ToListAsync();

            return new AnnouncementDashboardDto
            {
                ActiveAnnouncements = announcements.Count,

                ThisWeekAnnouncements = announcements.Count(x =>
                    x.StartDate >= today.AddDays(-7)),

                HighPriorityAnnouncements = announcements.Count(x =>
                    x.Priority == AnnouncementPriority.High),

                Announcements = announcements
            };
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<DepartmentHeadcountDashboardDto> GetDepartmentHeadcountAsync(string tenantId)
        {
            try
            {
            var totalEmployees = await _context.Employees
                .CountAsync(x => x.TenantId == tenantId);

            var totalDepartments = await _context.Departments
                .CountAsync(x => x.TenantId == tenantId);

            var newJoiners = await _context.Employees
                .CountAsync(x => x.TenantId == tenantId &&
                                 x.JoiningDate >= DateTime.Today.AddDays(-30));

            var departments = await _context.Departments
                .Where(x => x.TenantId == tenantId)
                .Select(x => new DepartmentHeadcountDto
                {
                    DepartmentId = x.Id,
                    DepartmentName = x.Name,
                    EmployeeCount = x.Employees.Count(e => e.TenantId == tenantId)
                })
                .OrderByDescending(x => x.EmployeeCount)
                .ToListAsync();

            string[] colors =
            {
                "primary",
                "success",
                "warning",
                "info",
                "danger",
                "purple",
                "pink",
                "teal"
            };

            for (int i = 0; i < departments.Count; i++)
            {
                departments[i].Percentage = totalEmployees == 0
                    ? 0
                    : Math.Round((decimal)departments[i].EmployeeCount * 100 / totalEmployees, 2);

                departments[i].Color = colors[i % colors.Length];
            }

            return new DepartmentHeadcountDashboardDto
            {
                TotalEmployees = totalEmployees,
                TotalDepartments = totalDepartments,
                NewJoiners = newJoiners,
                Departments = departments
            };
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
