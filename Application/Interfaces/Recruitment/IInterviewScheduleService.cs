using Application.DTOs.Recruitment;

namespace Application.Interfaces.Recruitment
{
    public interface IInterviewScheduleService
    {
        Task<List<InterviewScheduleListDto>> GetAllAsync();
        Task<List<InterviewScheduleListDto>> GetByApplicationAsync(string applicationId);
        Task<InterviewScheduleDto> GetByIdAsync(string id);
        Task<string> CreateAsync(InterviewScheduleDto dto);
        Task<string> UpdateAsync(string id, InterviewScheduleDto dto);
        Task<string> ChangeStatusAsync(string id, int status, string feedback, string userId);
        Task<bool> DeleteAsync(string id);
    }
}
