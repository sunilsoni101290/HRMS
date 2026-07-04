using Application.DTOs.Dashboard;

namespace Application.Interfaces.Dashboard
{
    public interface IEmployeeDashboardService
    {
        Task<EmployeeDashboardDto> GetAsync(string userId);
    }
}
