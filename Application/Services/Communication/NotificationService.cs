using Application.DTOs.Communication;
using Application.Interfaces.Communication;
using Domain.Entities;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Communication
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Admin List

        public async Task<List<NotificationListDto>> GetAllAsync()
        {
            try
            {
            return await _context.Notifications
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderByDescending(x => x.CreatedOn)
                .Select(x => new NotificationListDto
                {
                    Id = x.Id,
                    Title = x.Title,
                    Message = x.Message,
                    NotificationType = x.NotificationType,
                    Priority = x.Priority,
                    IsBroadcast = x.IsBroadcast,
                    RecipientCount = _context.NotificationRecipients
                        .Count(r => r.NotificationId == x.Id && !r.IsDeleted),
                    ReadCount = _context.NotificationRecipients
                        .Count(r => r.NotificationId == x.Id && !r.IsDeleted && r.IsRead),
                    CreatedOn = x.CreatedOn
                })
                .ToListAsync();
            }
            catch (Exception)
            {
                return new List<NotificationListDto>();
            }
        }

        #endregion

        #region Per-user Inbox

        public async Task<List<MyNotificationDto>> GetMyNotificationsAsync(string userId)
        {
            try
            {
            return await _context.NotificationRecipients
                .AsNoTracking()
                .Where(r => r.UserId == userId && !r.IsDeleted && !r.Notification.IsDeleted)
                .OrderByDescending(r => r.Notification.CreatedOn)
                .Select(r => new MyNotificationDto
                {
                    RecipientId = r.Id,
                    NotificationId = r.NotificationId,
                    Title = r.Notification.Title,
                    Message = r.Notification.Message,
                    NotificationType = r.Notification.NotificationType,
                    Priority = r.Notification.Priority,
                    RedirectUrl = r.Notification.RedirectUrl,
                    IsRead = r.IsRead,
                    ReadDate = r.ReadDate,
                    CreatedOn = r.Notification.CreatedOn
                })
                .ToListAsync();
            }
            catch (Exception)
            {
                return new List<MyNotificationDto>();
            }
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            try
            {
            return await _context.NotificationRecipients
                .AsNoTracking()
                .CountAsync(r => r.UserId == userId
                              && !r.IsRead
                              && !r.IsDeleted
                              && !r.Notification.IsDeleted);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        #endregion

        #region Broadcast

        public async Task<string> CreateBroadcastAsync(NotificationCreateDto dto)
        {
            try
            {
            var notification = new Notification
            {
                Id = IDManager.GetNewId(new Notification()),
                Title = dto.Title,
                Message = dto.Message,
                NotificationType = dto.NotificationType,
                NotificationModule = dto.NotificationModule ?? "HRMS",
                FeatureId = dto.FeatureId,
                ReferenceId = dto.ReferenceId,
                RedirectUrl = dto.RedirectUrl,
                Priority = dto.Priority,
                IsBroadcast = true,
                ExpiryDate = dto.ExpiryDate,
                TenantId = dto.TenantId,
                CreatedBy = dto.CreatedBy
            };

            await _context.Notifications.AddAsync(notification);

            // Fan out to all active users in the tenant
            var userIds = await _context.Users
                .AsNoTracking()
                .Where(u => !u.IsDeleted && u.IsActive && u.TenantId == dto.TenantId)
                .Select(u => u.Id)
                .ToListAsync();

            foreach (var uid in userIds)
            {
                await _context.NotificationRecipients.AddAsync(new NotificationRecipient
                {
                    Id = IDManager.GetNewId(new NotificationRecipient()),
                    NotificationId = notification.Id,
                    UserId = uid,
                    IsDelivered = true,
                    DeliveredDate = DateTime.UtcNow,
                    IsRead = false,
                    TenantId = dto.TenantId,
                    CreatedBy = dto.CreatedBy
                });
            }

            await _context.SaveChangesAsync();
            return notification.Id;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        #endregion

        #region Read Tracking

        public async Task<bool> MarkReadAsync(string recipientId, string userId)
        {
            try
            {
            var recipient = await _context.NotificationRecipients
                .FirstOrDefaultAsync(r => r.Id == recipientId && r.UserId == userId && !r.IsDeleted);

            if (recipient == null)
                return false;

            if (!recipient.IsRead)
            {
                recipient.IsRead = true;
                recipient.ReadDate = DateTime.UtcNow;
                _context.NotificationRecipients.Update(recipient);
                await _context.SaveChangesAsync();
            }

            return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<int> MarkAllReadAsync(string userId)
        {
            try
            {
            var items = await _context.NotificationRecipients
                .Where(r => r.UserId == userId && !r.IsRead && !r.IsDeleted)
                .ToListAsync();

            foreach (var r in items)
            {
                r.IsRead = true;
                r.ReadDate = DateTime.UtcNow;
            }

            if (items.Count > 0)
                await _context.SaveChangesAsync();

            return items.Count;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        #endregion

        #region Delete (Soft)

        public async Task<bool> DeleteAsync(string id)
        {
            try
            {
            var entity = await _context.Notifications
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null)
                return false;

            entity.IsDeleted = true;
            entity.ModifiedOn = DateTime.UtcNow;

            _context.Notifications.Update(entity);
            await _context.SaveChangesAsync();

            return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion
    }
}
