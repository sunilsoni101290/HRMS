using Application.DTOs.LoginHistory;
using Application.Interfaces.LoginHistory;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.LoginHistory
{
    public class LoginHistoryService : ILoginHistoryService
    {
        private readonly ApplicationDbContext _context;

        public LoginHistoryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<LoginHistoryListDto>> GetAllAsync()
        {
            try
            {
                return await _context.LoginHistories
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted)
                    .Include(x => x.User)
                        .ThenInclude(u => u.Employee)
                    .OrderByDescending(x => x.LoginTime)
                    .Select(x => new LoginHistoryListDto
                    {
                        Id = x.Id,
                        UserId = x.UserId,
                        TenantId = x.TenantId,
                        Username = x.User != null ? x.User.Username : null,
                        EmployeeName = x.User != null && x.User.Employee != null
                            ? (x.User.Employee.FirstName + " " + x.User.Employee.LastName)
                            : null,
                        LoginTime = x.LoginTime,
                        LogoutTime = x.LogoutTime,
                        LoginStatus = x.LoginStatus,
                        FailureReason = x.FailureReason,
                        IPAddress = x.IPAddress,
                        DeviceInfo = x.DeviceInfo,
                        Browser = x.Browser,
                        OS = x.OS,
                        IsSuspicious = x.IsSuspicious
                    })
                    .Take(500)
                    .ToListAsync();
            }
            catch (Exception)
            {
                return new List<LoginHistoryListDto>();
            }
        }

        public async Task<List<LoginHistoryListDto>> GetByUserAsync(string userId)
        {
            try
            {
                return await _context.LoginHistories
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted && x.UserId == userId)
                    .Include(x => x.User)
                        .ThenInclude(u => u.Employee)
                    .OrderByDescending(x => x.LoginTime)
                    .Select(x => new LoginHistoryListDto
                    {
                        Id = x.Id,
                        UserId = x.UserId,
                        TenantId = x.TenantId,
                        Username = x.User != null ? x.User.Username : null,
                        EmployeeName = x.User != null && x.User.Employee != null
                            ? (x.User.Employee.FirstName + " " + x.User.Employee.LastName)
                            : null,
                        LoginTime = x.LoginTime,
                        LogoutTime = x.LogoutTime,
                        LoginStatus = x.LoginStatus,
                        FailureReason = x.FailureReason,
                        IPAddress = x.IPAddress,
                        DeviceInfo = x.DeviceInfo,
                        Browser = x.Browser,
                        OS = x.OS,
                        IsSuspicious = x.IsSuspicious
                    })
                    .Take(200)
                    .ToListAsync();
            }
            catch (Exception)
            {
                return new List<LoginHistoryListDto>();
            }
        }
    }
}
