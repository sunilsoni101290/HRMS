using Application.Common.Exceptions;
using Application.DTOs.LoanAdvance;
using Application.Interfaces.LoanAdvance;
using Domain.Entities;
using Domain.Helper;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using static Domain.Enums.EnumExtensions;

namespace Application.Services.LoanAdvance
{
    /// <summary>See ILoanAdvanceAttachmentService for the scope (metadata only, no file I/O here).</summary>
    public class LoanAdvanceAttachmentService : ILoanAdvanceAttachmentService
    {
        private readonly IUnitOfWork _uow;

        public LoanAdvanceAttachmentService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<List<LoanAdvanceAttachmentDto>> GetForEntityAsync(int entityType, string entityId, string tenantId, string actingUserId)
        {
            await EnsureCanAccessEntityAsync(entityType, entityId, tenantId, actingUserId);

            var entities = await _uow.Repository<LoanAdvanceAttachment>().Query()
                .Where(a => (int)a.EntityType == entityType && a.EntityId == entityId)
                .OrderByDescending(a => a.CreatedOn)
                .ToListAsync();

            var nameMap = await BuildUserNameMapAsync(entities.Select(x => x.UploadedBy));

            return entities.Select(a => Map(a, nameMap)).ToList();
        }

        public async Task<LoanAdvanceAttachmentDto> AddMetadataAsync(
            int entityType, string entityId, string fileName, string filePath, string contentType, long fileSizeBytes,
            string tenantId, string actingUserId)
        {
            if (string.IsNullOrWhiteSpace(entityId))
                throw new BadRequestException("EntityId is required.");

            if (string.IsNullOrWhiteSpace(fileName) || string.IsNullOrWhiteSpace(filePath))
                throw new BadRequestException("File name/path are required.");

            if (fileSizeBytes <= 0 || fileSizeBytes > 10 * 1024 * 1024)
                throw new BadRequestException("File size must be between 1 byte and 10 MB.");

            await EnsureCanAccessEntityAsync(entityType, entityId, tenantId, actingUserId);

            var entity = new LoanAdvanceAttachment
            {
                EntityType = (LoanAttachmentEntityType)entityType,
                EntityId = entityId,
                FileName = fileName,
                FilePath = filePath,
                ContentType = contentType,
                FileSizeBytes = fileSizeBytes,
                UploadedBy = actingUserId,
                CreatedBy = actingUserId,
                CreatedOn = DateTime.UtcNow
            };

            await _uow.Repository<LoanAdvanceAttachment>().AddAsync(entity);
            await _uow.SaveChangesAsync();

            return Map(entity, new Dictionary<string, string>());
        }

        public async Task<bool> DeleteAsync(string attachmentId, string tenantId, string actingUserId)
        {
            var entity = await _uow.Repository<LoanAdvanceAttachment>().Query(asNoTracking: false)
                .FirstOrDefaultAsync(a => a.Id == attachmentId && !a.IsDeleted);

            if (entity == null)
                throw new NotFoundException("Attachment not found.");

            await EnsureCanAccessEntityAsync((int)entity.EntityType, entity.EntityId, tenantId, actingUserId, requireEditPermission: true);

            entity.IsDeleted = true;
            entity.ModifiedBy = actingUserId;
            entity.ModifiedOn = DateTime.UtcNow;

            _uow.Repository<LoanAdvanceAttachment>().Update(entity);
            await _uow.SaveChangesAsync();

            return true;
        }

        // Authorization mirrors EmployeeLoanService/EmployeeAdvanceService's
        // "own record OR feature permission" rule - an attachment is only
        // ever as visible/editable as its parent Loan/Advance request.
        private async Task EnsureCanAccessEntityAsync(int entityType, string entityId, string tenantId, string actingUserId, bool requireEditPermission = false)
        {
            var ownEmployeeId = await _uow.Repository<User>().Query()
                .Where(u => u.Id == actingUserId)
                .Select(u => u.EmployeeId)
                .FirstOrDefaultAsync();

            string? ownerEmployeeId;
            string feature;

            if ((LoanAttachmentEntityType)entityType == LoanAttachmentEntityType.Loan)
            {
                ownerEmployeeId = await _uow.Repository<EmployeeLoan>().Query()
                    .Where(x => x.Id == entityId && x.TenantId == tenantId && !x.IsDeleted)
                    .Select(x => x.EmployeeId)
                    .FirstOrDefaultAsync();
                feature = AppFeatureConstants.EMPLOYEE_LOAN;
            }
            else
            {
                ownerEmployeeId = await _uow.Repository<EmployeeAdvance>().Query()
                    .Where(x => x.Id == entityId && x.TenantId == tenantId && !x.IsDeleted)
                    .Select(x => x.EmployeeId)
                    .FirstOrDefaultAsync();
                feature = AppFeatureConstants.EMPLOYEE_ADVANCE;
            }

            if (ownerEmployeeId == null)
                throw new NotFoundException("The parent loan/advance request was not found.");

            if (!string.IsNullOrEmpty(ownEmployeeId) && ownEmployeeId == ownerEmployeeId)
                return;

            var action = requireEditPermission ? Actions.Edit : Actions.View;

            var allowed = await (
                from ur in _uow.Repository<UserRole>().Query()
                join rp in _uow.Repository<RolePermission>().Query().Where(x => x.IsAllowed) on ur.RoleId equals rp.RoleId
                join p in _uow.Repository<Permission>().Query().Where(x => x.FeatureId == feature && x.Action == action)
                    on rp.PermissionId equals p.Id
                where ur.UserId == actingUserId
                select p.Id
            ).AnyAsync();

            if (!allowed)
                throw new UnauthorizedException("You are not authorized to access this attachment.");
        }

        private static LoanAdvanceAttachmentDto Map(LoanAdvanceAttachment a, Dictionary<string, string> nameMap) => new()
        {
            Id = a.Id,
            EntityType = (int)a.EntityType,
            EntityId = a.EntityId,
            FileName = a.FileName,
            FilePath = a.FilePath,
            ContentType = a.ContentType,
            FileSizeBytes = a.FileSizeBytes,
            UploadedBy = a.UploadedBy,
            UploadedByName = nameMap.GetValueOrDefault(a.UploadedBy),
            CreatedOn = a.CreatedOn
        };

        private async Task<Dictionary<string, string>> BuildUserNameMapAsync(IEnumerable<string> userIds)
        {
            var ids = userIds.Distinct().ToList();
            if (ids.Count == 0)
                return new Dictionary<string, string>();

            var users = await _uow.Repository<User>().Query()
                .Include(u => u.Employee)
                .Where(u => ids.Contains(u.Id))
                .ToListAsync();

            return users.ToDictionary(
                u => u.Id,
                u => u.Employee != null ? $"{u.Employee.FirstName} {u.Employee.LastName}".Trim() : u.Username);
        }
    }
}
