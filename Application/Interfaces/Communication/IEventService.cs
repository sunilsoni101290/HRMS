using Application.DTOs.Communication;

namespace Application.Interfaces.Communication
{
    public interface IEventService
    {
        Task<List<EventListDto>> GetAllAsync();
        Task<EventDto> GetByIdAsync(string id);
        Task<string> CreateAsync(EventDto dto);
        Task<string> UpdateAsync(string id, EventDto dto);
        Task<bool> DeleteAsync(string id);
    }
}
