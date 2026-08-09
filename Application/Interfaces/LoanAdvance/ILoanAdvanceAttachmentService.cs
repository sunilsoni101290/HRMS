using Application.DTOs.LoanAdvance;

namespace Application.Interfaces.LoanAdvance
{
    /// <summary>
    /// Metadata-only service for Domain.Entities.LoanAdvanceAttachment -
    /// actual file I/O (saving the upload to disk/blob storage, size/type
    /// validation against the request) is the API controller's
    /// responsibility (Phase 7), same separation the rest of this codebase
    /// uses for EmployeeDocument uploads. This service just persists/
    /// reads/removes the resulting metadata row once the controller has
    /// already written the file and knows its final FilePath.
    /// </summary>
    public interface ILoanAdvanceAttachmentService
    {
        Task<List<LoanAdvanceAttachmentDto>> GetForEntityAsync(int entityType, string entityId, string tenantId, string actingUserId);

        Task<LoanAdvanceAttachmentDto> AddMetadataAsync(
            int entityType, string entityId, string fileName, string filePath, string contentType, long fileSizeBytes,
            string tenantId, string actingUserId);

        Task<bool> DeleteAsync(string attachmentId, string tenantId, string actingUserId);
    }
}
