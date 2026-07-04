using Application.DTOs.Recruitment;

namespace Application.Interfaces.Recruitment
{
    public interface IJobOpeningService
    {
        Task<List<JobOpeningListDto>> GetAllAsync();
        Task<JobOpeningDto> GetByIdAsync(string id);
        Task<string> CreateAsync(JobOpeningDto dto);
        Task<string> UpdateAsync(string id, JobOpeningDto dto);
        Task<bool> DeleteAsync(string id);
    }
}
