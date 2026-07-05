using Application.DTOs.Communication;

namespace Application.Interfaces.Communication
{
    public interface INotificationService
    {
        // Admin view
        Task<List<NotificationListDto>> GetAllAsync();

        // Per-user inbox
        Task<List<MyNotificationDto>> GetMyNotificationsAsync(string userId);
        Task<int> GetUnreadCountAsync(string userId);

        // Broadcast to all active users in tenant
        Task<string> CreateBroadcastAsync(NotificationCreateDto dto);

        Task<bool> MarkReadAsync(string recipientId, string userId);
        Task<int> MarkAllReadAsync(string userId);
        Task<bool> DeleteAsync(string id);
    }
}
