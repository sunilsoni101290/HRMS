using Application.DTOs.Attendances;
using Application.DTOs.Communication;
using Application.DTOs.Dashboard;
using Application.Interfaces.Dashboard;
using Domain.Entities;
using Domain.Helper;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using static Domain.Enums.EnumExtensions;

namespace Application.Services.Dashboard
{
    public class EmployeeDashboardService : IEmployeeDashboardService
    {
        private readonly ApplicationDbContext _context;

        private static readonly AttendanceStatus[] PresentStatuses =
        {
            AttendanceStatus.Present, AttendanceStatus.Late, AttendanceStatus.EarlyExit,
            AttendanceStatus.WorkFromHome, AttendanceStatus.OnDuty,
            AttendanceStatus.Overtime, AttendanceStatus.CompOff
        };

        public EmployeeDashboardService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<EmployeeDashboardDto> GetAsync(string userId)
        {
            try
            {
            var dto = new EmployeeDashboardDto();

            var user = await _context.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            var employeeId = user?.EmployeeId;
            var tenantId = user?.TenantId;

            // Unread notifications work off the userId even without an employee record
            dto.UnreadNotificationCount = await _context.NotificationRecipients
                .AsNoTracking()
                .CountAsync(r => r.UserId == userId && !r.IsRead && !r.IsDeleted && !r.Notification.IsDeleted);

            if (string.IsNullOrEmpty(employeeId))
            {
                dto.FullName = user?.Username ?? "";
                await FillCompanyWideAsync(dto, tenantId);
                return dto;
            }

            var employee = await _context.Employees.AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == employeeId && !e.IsDeleted);

            if (employee == null)
            {
                dto.FullName = user?.Username ?? "";
                await FillCompanyWideAsync(dto, tenantId);
                return dto;
            }

            var now = DateTime.UtcNow;
            var today = now.Date;

            // ---------- Profile ----------
            dto.EmployeeId = employee.Id;
            dto.FullName = $"{employee.FirstName} {employee.LastName}".Trim();
            dto.EmployeeCode = employee.EmployeeCode;
            dto.PhotoUrl = employee.FilePath;
            dto.JoiningDate = employee.JoiningDate;
            dto.DepartmentName = await _context.Departments
                .Where(d => d.Id == employee.DepartmentId).Select(d => d.Name).FirstOrDefaultAsync();
            dto.DesignationName = await _context.Designations
                .Where(d => d.Id == employee.DesignationId).Select(d => d.Name).FirstOrDefaultAsync();
            dto.ProfileCompletionPercent = ProfileCompletion(employee);

            // ---------- Attendance Today ----------
            var todayAtt = await _context.Attendances.AsNoTracking()
                .FirstOrDefaultAsync(a => a.EmployeeId == employeeId
                                       && a.Date.Date == today && !a.IsDeleted);
            if (todayAtt != null)
            {
                dto.CheckIn = todayAtt.FirstIn;
                dto.CheckOut = todayAtt.LastOut;
                dto.WorkingHours = todayAtt.TotalWorkingHours;
                dto.AttendanceStatus = todayAtt.Status.ToString();
            }

            // ---------- Week Attendance (Mon..Sun) ----------
            int diff = (7 + (int)today.DayOfWeek - (int)DayOfWeek.Monday) % 7;
            var weekStart = today.AddDays(-diff);
            var weekEnd = weekStart.AddDays(6);

            var weekAtt = await _context.Attendances.AsNoTracking()
                .Where(a => a.EmployeeId == employeeId
                         && a.Date.Date >= weekStart && a.Date.Date <= weekEnd && !a.IsDeleted)
                .Select(a => new { a.Date, a.Status })
                .ToListAsync();

            for (int i = 0; i < 7; i++)
            {
                var d = weekStart.AddDays(i);
                var rec = weekAtt.FirstOrDefault(x => x.Date.Date == d);
                dto.WeekAttendance.Add(new DayAttendanceDto
                {
                    Date = d,
                    DayName = d.ToString("ddd"),
                    // Extracted to a shared helper (Domain/Helper/AttendanceStatusHelper.cs)
                    // so the richer Attendance Calendar/Team/Summary/Dashboard
                    // aggregation added later shares the same classification
                    // rules - MapLegacyWeekStatus() reproduces this widget's
                    // original behavior exactly (Attendance.Status.ToString()
                    // if a row exists for the day, else "None"), unchanged.
                    Status = AttendanceStatusHelper.MapLegacyWeekStatus(rec?.Status)
                });
            }

            // ---------- This-month KPIs ----------
            var monthStatuses = await _context.Attendances.AsNoTracking()
                .Where(a => a.EmployeeId == employeeId
                         && a.Date.Year == today.Year && a.Date.Month == today.Month && !a.IsDeleted)
                .Select(a => a.Status)
                .ToListAsync();

            dto.PresentDaysThisMonth = monthStatuses.Count(s => PresentStatuses.Contains(s))
                                     + monthStatuses.Count(s => s == AttendanceStatus.HalfDay); // half counted as attended
            dto.WorkingDaysThisMonth = monthStatuses.Count(s => s != AttendanceStatus.Holiday
                                                             && s != AttendanceStatus.WeekOff
                                                             && s != AttendanceStatus.None);
            dto.AttendancePercent = dto.WorkingDaysThisMonth > 0
                ? (int)Math.Round(dto.PresentDaysThisMonth * 100.0 / dto.WorkingDaysThisMonth)
                : 0;

            // ---------- Leave Balances ----------
            dto.LeaveBalances = await _context.LeaveBalances.AsNoTracking()
                .Where(b => b.EmployeeId == employeeId && b.Year == today.Year && !b.IsDeleted)
                .Select(b => new LeaveBalanceItemDto
                {
                    LeaveTypeName = b.LeaveType != null ? b.LeaveType.Name : "",
                    Balance = b.Balance
                })
                .ToListAsync();
            dto.TotalLeaveBalance = dto.LeaveBalances.Sum(x => x.Balance);

            // ---------- Pending Leave Requests ----------
            dto.PendingLeaveRequests = await _context.LeaveApplications.AsNoTracking()
                .Where(l => l.EmployeeId == employeeId
                         && l.Status == ApprovalStatus.Pending && !l.IsDeleted)
                .OrderByDescending(l => l.FromDate)
                .Take(5)
                .Select(l => new LeaveRequestItemDto
                {
                    LeaveTypeName = l.LeaveType != null ? l.LeaveType.Name : "",
                    FromDate = l.FromDate,
                    ToDate = l.ToDate,
                    TotalDays = l.TotalDays,
                    StatusText = "Pending"
                })
                .ToListAsync();
            dto.PendingLeaveCount = await _context.LeaveApplications
                .CountAsync(l => l.EmployeeId == employeeId
                              && l.Status == ApprovalStatus.Pending && !l.IsDeleted);

            // ---------- Recent Payslip ----------
            var payroll = await _context.Payrolls.AsNoTracking()
                .Where(p => p.EmployeeId == employeeId && !p.IsDeleted)
                .OrderByDescending(p => p.SalaryYear).ThenByDescending(p => p.SalaryMonth)
                .FirstOrDefaultAsync();
            if (payroll != null)
            {
                dto.RecentPayslip = new RecentPayslipDto
                {
                    MonthName = MonthName(payroll.SalaryMonth),
                    Year = payroll.SalaryYear,
                    NetSalary = payroll.NetSalary,
                    PayDate = payroll.SalaryDate,
                    Status = payroll.Status
                };
            }

            // ---------- Tasks ----------
            dto.MyTasks = await _context.EmployeeTasks.AsNoTracking()
                .Where(t => t.EmployeeId == employeeId && !t.IsDeleted && t.Status != "Completed")
                .OrderBy(t => t.DueDate)
                .Take(5)
                .Select(t => new TaskItemDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    DueDate = t.DueDate,
                    Status = t.Status,
                    Priority = t.Priority
                })
                .ToListAsync();
            dto.PendingTaskCount = await _context.EmployeeTasks
                .CountAsync(t => t.EmployeeId == employeeId && !t.IsDeleted && t.Status != "Completed");

            // ---------- Charts ----------
            for (int i = 5; i >= 0; i--)
            {
                var m = today.AddMonths(-i);
                var count = await _context.Attendances.AsNoTracking()
                    .CountAsync(a => a.EmployeeId == employeeId
                                  && a.Date.Year == m.Year && a.Date.Month == m.Month
                                  && (a.Status == AttendanceStatus.Present
                                      || a.Status == AttendanceStatus.Late
                                      || a.Status == AttendanceStatus.WorkFromHome
                                      || a.Status == AttendanceStatus.OnDuty)
                                  && !a.IsDeleted);
                dto.AttendanceTrend.Add(new TrendPointDto { Label = m.ToString("MMM"), Value = count });
            }

            dto.LeaveDistribution = await _context.LeaveBalances.AsNoTracking()
                .Where(b => b.EmployeeId == employeeId && b.Year == today.Year && b.Used > 0 && !b.IsDeleted)
                .Select(b => new LeaveSliceDto
                {
                    Name = b.LeaveType != null ? b.LeaveType.Name : "",
                    Value = b.Used
                })
                .ToListAsync();

            // ---------- Recent Activity ----------
            var recentAtt = await _context.Attendances.AsNoTracking()
                .Where(a => a.EmployeeId == employeeId && !a.IsDeleted)
                .OrderByDescending(a => a.Date).Take(3)
                .Select(a => new { a.Date, a.Status })
                .ToListAsync();
            foreach (var a in recentAtt)
                dto.RecentActivity.Add(new ActivityItemDto
                {
                    Text = $"Attendance marked {a.Status}",
                    Date = a.Date,
                    Icon = "icon-calendar2"
                });

            // Company-wide widgets
            await FillCompanyWideAsync(dto, tenantId ?? employee.TenantId, employee.DepartmentId);

            return dto;
            }
            catch (Exception)
            {
                return null;
            }
        }

        #region Profile

        public async Task<EmployeeProfileDto> GetProfileAsync(string employeeId)
        {
            try
            {
                var e = await _context.Employees.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == employeeId && !x.IsDeleted);
                if (e == null) return null;

                var today = DateTime.UtcNow.Date;

                var department = await _context.Departments.AsNoTracking()
                    .Where(d => d.Id == e.DepartmentId).Select(d => d.Name).FirstOrDefaultAsync();
                var designation = await _context.Designations.AsNoTracking()
                    .Where(d => d.Id == e.DesignationId).Select(d => d.Name).FirstOrDefaultAsync();
                var company = await _context.Set<Company>().AsNoTracking()
                    .Where(c => c.Id == e.CompanyId).Select(c => c.Name).FirstOrDefaultAsync();
                var manager = await _context.Employees.AsNoTracking()
                    .Where(m => m.Id == e.ReportingManagerId)
                    .Select(m => m.FirstName + " " + m.LastName).FirstOrDefaultAsync();

                var isCheckedIn = await _context.Attendances.AsNoTracking()
                    .AnyAsync(a => a.EmployeeId == employeeId && a.Date.Date == today
                              && a.FirstIn != null && a.LastOut == null && !a.IsDeleted);

                return new EmployeeProfileDto
                {
                    EmployeeId = e.Id,
                    EmployeeCode = e.EmployeeCode,
                    FullName = $"{e.FirstName} {e.LastName}".Trim(),
                    Designation = designation ?? "",
                    Department = department ?? "",
                    Branch = "",
                    Company = company ?? "",
                    ProfileImage = e.FilePath,
                    JoiningDate = e.JoiningDate,
                    ExperienceYears = Math.Max(0, today.Year - e.JoiningDate.Year),
                    Email = e.Email,
                    Mobile = e.Phone,
                    ProfileCompletion = ProfileCompletion(e),
                    IsCheckedIn = isCheckedIn,
                    ReportingManager = manager ?? "",
                    EmploymentType = e.EmploymentType.ToString(),
                    EmploymentStatus = e.IsActive ? "Active" : "Inactive"
                };
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion

        #region Attendance (this month)

        public async Task<List<AttendanceDto>> GetAttendanceAsync(string employeeId)
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                var rows = await _context.Attendances.AsNoTracking()
                    .Where(a => a.EmployeeId == employeeId && !a.IsDeleted
                             && a.Date.Year == today.Year && a.Date.Month == today.Month)
                    .OrderByDescending(a => a.Date)
                    .Select(a => new
                    {
                        a.Id, a.TenantId, a.CompanyId, a.BranchId, a.EmployeeId, a.Date,
                        a.FirstIn, a.LastOut, a.TotalWorkingHours, a.BreakHours,
                        a.OvertimeHours, a.Status, a.IsLate, a.IsEarlyExit, a.Remarks
                    })
                    .ToListAsync();

                return rows.Select(a => new AttendanceDto
                {
                    Id = a.Id,
                    TenantId = a.TenantId,
                    CompanyId = a.CompanyId,
                    BranchId = a.BranchId,
                    EmployeeId = a.EmployeeId,
                    Date = a.Date,
                    FirstIn = a.FirstIn,
                    LastOut = a.LastOut,
                    TotalWorkingHours = a.TotalWorkingHours,
                    BreakHours = a.BreakHours,
                    OvertimeHours = a.OvertimeHours,
                    Status = a.Status,
                    IsLate = a.IsLate,
                    IsEarlyExit = a.IsEarlyExit,
                    Remarks = a.Remarks
                }).ToList();
            }
            catch (Exception)
            {
                return new List<AttendanceDto>();
            }
        }

        #endregion

        #region Leaves (summary)

        public async Task<List<LeaveDto>> GetLeavesAsync(string employeeId)
        {
            try
            {
                var year = DateTime.UtcNow.Year;

                var balances = await _context.LeaveBalances.AsNoTracking()
                    .Where(b => b.EmployeeId == employeeId && b.Year == year && !b.IsDeleted)
                    .Select(b => new { Name = b.LeaveType != null ? b.LeaveType.Name : "", b.Balance, b.Used })
                    .ToListAsync();

                var applications = await _context.LeaveApplications.AsNoTracking()
                    .Where(l => l.EmployeeId == employeeId && !l.IsDeleted && l.FromDate.Year == year)
                    .Select(l => new { l.Status, l.TotalDays })
                    .ToListAsync();

                decimal MatchBalance(params string[] keys) =>
                    balances.Where(b => keys.Any(k => (b.Name ?? "").ToLower().Contains(k)))
                            .Sum(b => b.Balance);

                var dto = new LeaveDto
                {
                    CasualLeave = MatchBalance("casual", "cl"),
                    EarnedLeave = MatchBalance("earned", "el", "privilege", "pl"),
                    SickLeave = MatchBalance("sick", "sl"),
                    CompOff = MatchBalance("comp"),
                    TotalAvailable = balances.Sum(b => b.Balance),
                    UsedLeave = balances.Sum(b => b.Used),
                    PendingApproval = applications.Where(a => a.Status == ApprovalStatus.Pending).Sum(a => a.TotalDays),
                    ApprovedLeave = applications.Where(a => a.Status == ApprovalStatus.Approved).Sum(a => a.TotalDays),
                    RejectedLeave = applications.Where(a => a.Status == ApprovalStatus.Rejected).Sum(a => a.TotalDays)
                };

                return new List<LeaveDto> { dto };
            }
            catch (Exception)
            {
                return new List<LeaveDto> { new LeaveDto() };
            }
        }

        #endregion

        #region Payslips

        public async Task<List<PayslipDto>> GetPayslipsAsync(string employeeId)
        {
            try
            {
                var rows = await _context.Payrolls.AsNoTracking()
                    .Where(p => p.EmployeeId == employeeId && !p.IsDeleted)
                    .OrderByDescending(p => p.SalaryYear).ThenByDescending(p => p.SalaryMonth)
                    .Take(12)
                    .Select(p => new
                    {
                        p.SalaryMonth, p.SalaryYear, p.GrossSalary, p.TotalEarnings,
                        p.TotalDeductions, p.NetSalary, p.SalaryDate, p.Status
                    })
                    .ToListAsync();

                return rows.Select(p => new PayslipDto
                {
                    SalaryMonth = $"{MonthName(p.SalaryMonth)} {p.SalaryYear}",
                    GrossSalary = p.GrossSalary,
                    TotalAllowance = p.TotalEarnings,
                    TotalDeduction = p.TotalDeductions,
                    NetSalary = p.NetSalary,
                    SalaryDate = p.SalaryDate,
                    IsPaid = string.Equals(p.Status, "Paid", StringComparison.OrdinalIgnoreCase),
                    PayslipUrl = ""
                }).ToList();
            }
            catch (Exception)
            {
                return new List<PayslipDto>();
            }
        }

        #endregion

        #region Announcements

        public async Task<List<AnnouncementDto>> GetAnnouncementsAsync()
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                var rows = await _context.Announcements.AsNoTracking()
                    .Where(a => !a.IsDeleted && a.IsActive
                             && a.PublishDate <= today
                             && (a.ExpiryDate == null || a.ExpiryDate >= today))
                    .OrderByDescending(a => a.PublishDate)
                    .Take(10)
                    .Select(a => new
                    {
                        a.Id, a.Title, a.Message, a.AnnouncementType, a.Priority,
                        a.PublishDate, a.ExpiryDate, a.CompanyId, a.BranchId, a.TenantId, a.CreatedBy
                    })
                    .ToListAsync();

                return rows.Select(a => new AnnouncementDto
                {
                    Id = a.Id,
                    Title = a.Title,
                    Message = a.Message,
                    AnnouncementType = (int)a.AnnouncementType,
                    TypeText = a.AnnouncementType.ToString(),
                    Priority = (int)a.Priority,
                    PriorityText = a.Priority.ToString(),
                    PublishDate = a.PublishDate,
                    ExpiryDate = a.ExpiryDate,
                    CompanyId = a.CompanyId,
                    BranchId = a.BranchId,
                    TenantId = a.TenantId,
                    CreatedBy = a.CreatedBy
                }).ToList();
            }
            catch (Exception)
            {
                return new List<AnnouncementDto>();
            }
        }

        #endregion

        #region Upcoming Events

        public async Task<List<EventDto>> GetUpcomingEventsAsync()
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                var rows = await _context.Events.AsNoTracking()
                    .Where(e => !e.IsDeleted && e.StartDate >= today)
                    .OrderBy(e => e.StartDate)
                    .Take(10)
                    .Select(e => new
                    {
                        e.Id, e.Title, e.Description, e.StartDate, e.EndDate,
                        e.StartTime, e.EndTime, e.Location, e.EventType,
                        e.CompanyId, e.BranchId, e.TenantId, e.CreatedBy
                    })
                    .ToListAsync();

                return rows.Select(e => new EventDto
                {
                    Id = e.Id,
                    Title = e.Title,
                    Description = e.Description,
                    StartDate = e.StartDate,
                    EndDate = e.EndDate,
                    StartTime = e.StartTime,
                    EndTime = e.EndTime,
                    Location = e.Location,
                    EventType = (int)e.EventType,
                    TypeText = e.EventType.ToString(),
                    CompanyId = e.CompanyId,
                    BranchId = e.BranchId,
                    TenantId = e.TenantId,
                    CreatedBy = e.CreatedBy
                }).ToList();
            }
            catch (Exception)
            {
                return new List<EventDto>();
            }
        }

        #endregion

        #region Tasks

        public async Task<List<TaskDto>> GetTasksAsync(string employeeId)
        {
            try
            {
                var rows = await _context.EmployeeTasks.AsNoTracking()
                    .Where(t => t.EmployeeId == employeeId && !t.IsDeleted)
                    .OrderBy(t => t.Status == "Completed")
                    .ThenBy(t => t.DueDate)
                    .Take(20)
                    .Select(t => new
                    {
                        t.Title, t.Description, t.DueDate, t.Status, t.Priority
                    })
                    .ToListAsync();

                return rows.Select(t => new TaskDto
                {
                    Id = 0,
                    TaskName = t.Title,
                    Description = t.Description,
                    DueDate = t.DueDate ?? DateTime.MinValue,
                    Status = t.Status,
                    Priority = t.Priority,
                    Progress = string.Equals(t.Status, "Completed", StringComparison.OrdinalIgnoreCase) ? 100
                             : string.Equals(t.Status, "InProgress", StringComparison.OrdinalIgnoreCase) ? 50 : 0
                }).ToList();
            }
            catch (Exception)
            {
                return new List<TaskDto>();
            }
        }

        #endregion

        #region Quick Links

        public Task<List<QuickLinkDto>> GetQuickLinksAsync()
        {
            var links = new List<QuickLinkDto>
            {
                new() { Title = "Apply Leave",    Icon = "bi bi-calendar-plus", Url = "/LeaveApplication/Create", Color = "primary", DisplayOrder = 1 },
                new() { Title = "My Profile",     Icon = "bi bi-person",        Url = "/Employee/Profile",        Color = "info",    DisplayOrder = 2 },
                new() { Title = "Payslip",        Icon = "bi bi-cash-stack",    Url = "/Payroll",                 Color = "success", DisplayOrder = 3 },
                new() { Title = "Documents",      Icon = "bi bi-folder",        Url = "/EmployeeDocument",        Color = "warning", DisplayOrder = 4 },
                new() { Title = "My Tasks",       Icon = "bi bi-list-check",    Url = "/EmployeeTask",            Color = "secondary", DisplayOrder = 5 },
                new() { Title = "Announcements",  Icon = "bi bi-megaphone",     Url = "/Announcement",            Color = "danger",  DisplayOrder = 6 }
            };
            return Task.FromResult(links);
        }

        #endregion

        #region Company-wide (announcements, events, holidays, birthdays, anniversaries)

        private async Task FillCompanyWideAsync(EmployeeDashboardDto dto, string? tenantId, string? departmentId = null)
        {
            var today = DateTime.UtcNow.Date;

            var annRaw = await _context.Announcements.AsNoTracking()
                .Where(a => !a.IsDeleted && a.IsActive
                         && a.PublishDate <= today
                         && (a.ExpiryDate == null || a.ExpiryDate >= today))
                .OrderByDescending(a => a.PublishDate)
                .Take(5)
                .Select(a => new { a.Title, a.Message, a.PublishDate, a.AnnouncementType })
                .ToListAsync();
            dto.Announcements = annRaw.Select(a => new AnnouncementItemDto
            {
                Title = a.Title,
                Message = a.Message != null && a.Message.Length > 90 ? a.Message.Substring(0, 90) + "..." : a.Message,
                PublishDate = a.PublishDate,
                TypeText = a.AnnouncementType.ToString()
            }).ToList();

            var evtRaw = await _context.Events.AsNoTracking()
                .Where(e => !e.IsDeleted && e.StartDate >= today)
                .OrderBy(e => e.StartDate)
                .Take(5)
                .Select(e => new { e.Title, e.StartDate, e.EventType, e.Location })
                .ToListAsync();
            dto.UpcomingEvents = evtRaw.Select(e => new EventItemDto
            {
                Title = e.Title,
                StartDate = e.StartDate,
                TypeText = e.EventType.ToString(),
                Location = e.Location
            }).ToList();

            dto.UpcomingHolidays = await _context.Set<HolidayGroupDetail>().AsNoTracking()
                .Where(h => !h.IsDeleted && h.HolidayDate >= today)
                .OrderBy(h => h.HolidayDate)
                .Take(5)
                .Select(h => new HolidayItemDto
                {
                    Name = h.HolidayName,
                    Date = h.HolidayDate
                })
                .ToListAsync();

            int month = today.Month;

            var people = await _context.Employees.AsNoTracking()
                .Where(e => !e.IsDeleted
                         && (tenantId == null || e.TenantId == tenantId))
                .Select(e => new
                {
                    Name = e.FirstName + " " + e.LastName,
                    e.DateOfBirth,
                    e.JoiningDate,
                    e.DepartmentId,
                    e.FilePath
                })
                .ToListAsync();

            var deptNames = await _context.Departments.AsNoTracking()
                .ToDictionaryAsync(d => d.Id, d => d.Name);

            dto.Birthdays = people
                .Where(p => p.DateOfBirth.HasValue && p.DateOfBirth.Value.Month == month)
                .OrderBy(p => p.DateOfBirth!.Value.Day)
                .Take(6)
                .Select(p => new PersonItemDto
                {
                    Name = p.Name,
                    Date = p.DateOfBirth!.Value,
                    PhotoUrl = p.FilePath,
                    DepartmentName = p.DepartmentId != null && deptNames.ContainsKey(p.DepartmentId) ? deptNames[p.DepartmentId] : ""
                })
                .ToList();

            dto.WorkAnniversaries = people
                .Where(p => p.JoiningDate.Month == month && p.JoiningDate.Year < today.Year)
                .OrderBy(p => p.JoiningDate.Day)
                .Take(6)
                .Select(p => new PersonItemDto
                {
                    Name = p.Name,
                    Date = p.JoiningDate,
                    Years = today.Year - p.JoiningDate.Year,
                    PhotoUrl = p.FilePath,
                    DepartmentName = p.DepartmentId != null && deptNames.ContainsKey(p.DepartmentId) ? deptNames[p.DepartmentId] : ""
                })
                .ToList();
        }

        #endregion

        #region Helpers

        private static int ProfileCompletion(Employee e)
        {
            var fields = new object?[]
            {
                e.FirstName, e.LastName, e.Email, e.Phone, e.DateOfBirth,
                e.Address, e.DepartmentId, e.DesignationId, e.FilePath,
                e.PANNumber, e.AadharNumber, e.EmergencyContact
            };
            int total = fields.Length;
            int filled = fields.Count(f => f != null && !string.IsNullOrWhiteSpace(f.ToString()));
            return (int)Math.Round(filled * 100.0 / total);
        }

        private static string MonthName(int month)
        {
            if (month < 1 || month > 12) return "";
            return CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month);
        }

        #endregion
    }
}
