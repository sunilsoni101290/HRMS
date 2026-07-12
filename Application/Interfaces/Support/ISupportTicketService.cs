using Application.DTOs.Support;

namespace Application.Interfaces.Support
{
    public interface ISupportTicketService
    {
        Task<List<SupportTicketListDto>> GetAllAsync();
        Task<List<SupportTicketListDto>> GetByEmployeeAsync(string employeeId);
        Task<SupportTicketDto> GetByIdAsync(string id);
        Task<string> CreateAsync(CreateSupportTicketRequestDto request);
        Task<bool> AddReplyAsync(AddReplyRequestDto request);
        Task<bool> UpdateStatusAsync(UpdateTicketStatusRequestDto request);
        Task<int> GetOpenCountAsync();
    }
}
