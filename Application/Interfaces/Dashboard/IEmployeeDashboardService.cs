using Application.DTOs.Attendances;
using Application.DTOs.Communication;
using Application.DTOs.Dashboard;

namespace Application.Interfaces.Dashboard
{
    public interface IEmployeeDashboardService
    {
        Task<EmployeeDashboardDto> GetAsync(string userId);
        Task<EmployeeProfileDto> GetProfileAsync(string employeeId);

        Task<List<AttendanceDto>> GetAttendanceAsync(string employeeId);

        Task<List<LeaveDto>> GetLeavesAsync(string employeeId);

        Task<List<PayslipDto>> GetPayslipsAsync(string employeeId);

        Task<List<AnnouncementDto>> GetAnnouncementsAsync();

        Task<List<EventDto>> GetUpcomingEventsAsync();

        Task<List<TaskDto>> GetTasksAsync(string employeeId);

        Task<List<QuickLinkDto>> GetQuickLinksAsync();
    }
}
