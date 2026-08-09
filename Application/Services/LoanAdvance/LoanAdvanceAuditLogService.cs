using Application.Common.Exceptions;
using Application.DTOs.LoanAdvance;
using Application.Interfaces.LoanAdvance;
using Domain.Entities;
using Domain.Helper;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.LoanAdvance
{
    /// <summary>See ILoanAdvanceAuditLogService for the Phase 6 vs Phase 16 scope split.</summary>
    public class LoanAdvanceAuditLogService : ILoanAdvanceAuditLogService
    {
        private readonly IUnitOfWork _uow;

        public LoanAdvanceAuditLogService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task LogAsync(string entityType, string entityId, string action, string? oldValuesJson, string? newValuesJson, string tenantId, string performedBy, string? ipAddress = null)
        {
            // Never let audit logging itself take down the calling
            // operation - mirrors ErrorLogger's "never crash the app due
            // to logging failure" convention elsewhere in this codebase.
            try
            {
                await _uow.Repository<LoanAdvanceAuditLog>().AddAsync(new LoanAdvanceAuditLog
                {
                    EntityType = entityType,
                    EntityId = entityId,
                    Action = action,
                    OldValuesJson = oldValuesJson,
                    NewValuesJson = newValuesJson,
                    PerformedBy = performedBy,
                    PerformedOn = DateTime.UtcNow,
                    IpAddress = ipAddress,
                    TenantId = tenantId
                });

                await _uow.SaveChangesAsync();
            }
            catch
            {
                // Swallowed intentionally - see comment above.
            }
        }

        public async Task<List<LoanAdvanceAuditLogDto>> GetForEntityAsync(string entityType, string entityId, string tenantId, string actingUserId)
        {
            await EnsurePermissionAsync(actingUserId, Actions.View);

            var entities = await _uow.Repository<LoanAdvanceAuditLog>().Query()
                .Where(x => x.EntityType == entityType && x.EntityId == entityId && x.TenantId == tenantId)
                .OrderByDescending(x => x.PerformedOn)
                .ToListAsync();

            var userIds = entities.Select(x => x.PerformedBy).Distinct().ToList();

            var nameMap = userIds.Count == 0
                ? new Dictionary<string, string>()
                : (await _uow.Repository<User>().Query()
                    .Include(u => u.Employee)
                    .Where(u => userIds.Contains(u.Id))
                    .ToListAsync())
                    .ToDictionary(u => u.Id, u => u.Employee != null ? $"{u.Employee.FirstName} {u.Employee.LastName}".Trim() : u.Username);

            return entities.Select(x => new LoanAdvanceAuditLogDto
            {
                Id = x.Id,
                EntityType = x.EntityType,
                EntityId = x.EntityId,
                Action = x.Action,
                OldValuesJson = x.OldValuesJson,
                NewValuesJson = x.NewValuesJson,
                PerformedBy = x.PerformedBy,
                PerformedByName = nameMap.GetValueOrDefault(x.PerformedBy),
                PerformedOn = x.PerformedOn,
                IpAddress = x.IpAddress
            }).ToList();
        }

        public async Task<List<LoanAdvanceAuditLogDto>> GetRecentAsync(string tenantId, string actingUserId, string? entityType = null, int take = 200)
        {
            await EnsurePermissionAsync(actingUserId, Actions.View);

            var query = _uow.Repository<LoanAdvanceAuditLog>().Query()
                .Where(x => x.TenantId == tenantId);

            if (!string.IsNullOrWhiteSpace(entityType))
                query = query.Where(x => x.EntityType == entityType);

            var entities = await query
                .OrderByDescending(x => x.PerformedOn)
                .Take(take)
                .ToListAsync();

            var userIds = entities.Select(x => x.PerformedBy).Distinct().ToList();

            var nameMap = userIds.Count == 0
                ? new Dictionary<string, string>()
                : (await _uow.Repository<User>().Query()
                    .Include(u => u.Employee)
                    .Where(u => userIds.Contains(u.Id))
                    .ToListAsync())
                    .ToDictionary(u => u.Id, u => u.Employee != null ? $"{u.Employee.FirstName} {u.Employee.LastName}".Trim() : u.Username);

            return entities.Select(x => new LoanAdvanceAuditLogDto
            {
                Id = x.Id,
                EntityType = x.EntityType,
                EntityId = x.EntityId,
                Action = x.Action,
                OldValuesJson = x.OldValuesJson,
                NewValuesJson = x.NewValuesJson,
                PerformedBy = x.PerformedBy,
                PerformedByName = nameMap.GetValueOrDefault(x.PerformedBy),
                PerformedOn = x.PerformedOn,
                IpAddress = x.IpAddress
            }).ToList();
        }

        private async Task EnsurePermissionAsync(string? actingUserId, string action)
        {
            if (string.IsNullOrEmpty(actingUserId))
                throw new UnauthorizedException("You are not authorized to view the audit trail.");

            var allowed = await (
                from ur in _uow.Repository<UserRole>().Query()
                join rp in _uow.Repository<RolePermission>().Query().Where(x => x.IsAllowed) on ur.RoleId equals rp.RoleId
                join p in _uow.Repository<Permission>().Query().Where(x =>
                        x.FeatureId == AppFeatureConstants.LOAN_ADVANCE_AUDIT_LOG && x.Action == action)
                    on rp.PermissionId equals p.Id
                where ur.UserId == actingUserId
                select p.Id
            ).AnyAsync();

            if (!allowed)
                throw new UnauthorizedException("You are not authorized to view the audit trail.");
        }
    }
}
