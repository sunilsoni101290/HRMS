using Application.DTOs.Support;

namespace Application.Interfaces.Support
{
    public interface IFaqService
    {
        Task<List<FaqItemDto>> GetAllAsync();
        Task<List<FaqItemDto>> GetActiveAsync();
        Task<FaqItemDto> GetByIdAsync(string id);
        Task<string> CreateAsync(FaqItemDto dto);
        Task<string> UpdateAsync(string id, FaqItemDto dto);
        Task<bool> DeleteAsync(string id);
        Task<bool> ToggleActiveAsync(string id);
    }
}
