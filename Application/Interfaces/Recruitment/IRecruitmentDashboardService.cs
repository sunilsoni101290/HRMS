using Application.DTOs.Recruitment;

namespace Application.Interfaces.Recruitment
{
    public interface IRecruitmentDashboardService
    {
        Task<RecruitmentDashboardDto> GetDashboardAsync();
    }
}
