using Application.DTOs.Communication;

namespace Application.Interfaces.Communication
{
    public interface IAnnouncementService
    {
        Task<List<AnnouncementListDto>> GetAllAsync();
        Task<AnnouncementDto> GetByIdAsync(string id);
        Task<string> CreateAsync(AnnouncementDto dto);
        Task<string> UpdateAsync(string id, AnnouncementDto dto);
        Task<bool> DeleteAsync(string id);
    }
}
