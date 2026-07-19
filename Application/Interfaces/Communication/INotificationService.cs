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

        // Single-recipient, non-broadcast notification - used by workflow
        // engines (Leave Application approval chain, etc.) that need to
        // alert one specific user rather than fan out to everyone. Creates
        // one Notification row (IsBroadcast = false) plus exactly one
        // NotificationRecipient targeting userId. Never throws - any
        // failure is swallowed and null is returned, so a notification
        // problem can never break the caller's own (already-committed)
        // transaction.
        Task<string?> CreateDirectAsync(
            string userId,
            string title,
            string message,
            string notificationType,
            string? redirectUrl = null,
            string? featureId = null,
            string? referenceId = null,
            string? tenantId = null,
            string? createdBy = null);

        Task<bool> MarkReadAsync(string recipientId, string userId);
        Task<int> MarkAllReadAsync(string userId);
        Task<bool> DeleteAsync(string id);
    }
}
