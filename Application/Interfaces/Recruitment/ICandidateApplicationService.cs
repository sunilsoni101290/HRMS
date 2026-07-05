using Application.DTOs.Recruitment;

namespace Application.Interfaces.Recruitment
{
    public interface ICandidateApplicationService
    {
        Task<List<CandidateApplicationListDto>> GetAllAsync();
        Task<CandidateApplicationListDto> GetByIdAsync(string id);
        Task<CandidateApplicationDto> GetForEditAsync(string id);
        Task<string> CreateAsync(CandidateApplicationDto dto);
        Task<string> UpdateAsync(string id, CandidateApplicationDto dto);
        Task<string> ChangeStageAsync(string id, int status, string userId);
        Task<bool> DeleteAsync(string id);
    }
}
