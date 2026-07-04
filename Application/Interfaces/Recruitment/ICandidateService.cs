using Application.DTOs.Recruitment;

namespace Application.Interfaces.Recruitment
{
    public interface ICandidateService
    {
        Task<List<CandidateListDto>> GetAllAsync();
        Task<CandidateDto> GetByIdAsync(string id);
        Task<string> CreateAsync(CandidateDto dto);
        Task<string> UpdateAsync(string id, CandidateDto dto);
        Task<bool> DeleteAsync(string id);
    }
}
