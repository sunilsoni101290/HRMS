using Application.DTOs.LoginHistory;

namespace Application.Interfaces.LoginHistory
{
    public interface ILoginHistoryService
    {
        Task<List<LoginHistoryListDto>> GetAllAsync();
        Task<List<LoginHistoryListDto>> GetByUserAsync(string userId);
    }
}
