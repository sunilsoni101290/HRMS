using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs;
using Application.DTOs.WorkTracking;

namespace Application.Interfaces.WorkTracking
{
    /// <summary>
    /// CRUD + dropdown lookups for every Daily Work Entry master (Client,
    /// WorkJob, JobType, JobItem, WorkActivity, WorkEntryReason,
    /// DocumentStatus). Kept as one service rather than 7 near-identical
    /// ones - none of these masters has enough independent business logic
    /// to justify its own service (spec section 42: "do not create
    /// unnecessary repositories/services").
    /// </summary>
    public interface IWorkTrackingMasterService
    {
        // Clients
        Task<List<ClientDto>> GetClientsAsync(string tenantId, bool activeOnly = false);
        Task<ClientDto> SaveClientAsync(ClientDto dto, string tenantId, string actingUserId);

        // WorkJobs
        Task<List<WorkJobDto>> GetWorkJobsAsync(string tenantId, bool activeOnly = false);
        Task<WorkJobDto> SaveWorkJobAsync(WorkJobDto dto, string tenantId, string actingUserId);

        // JobItems
        Task<List<JobItemDto>> GetJobItemsAsync(string workJobId, string tenantId, bool activeOnly = false);
        Task<JobItemDto> SaveJobItemAsync(JobItemDto dto, string tenantId, string actingUserId);

        // JobTypes
        Task<List<JobTypeDto>> GetJobTypesAsync(string tenantId, bool activeOnly = false);
        Task<JobTypeDto> SaveJobTypeAsync(JobTypeDto dto, string tenantId, string actingUserId);

        // WorkActivities
        Task<List<WorkActivityDto>> GetWorkActivitiesAsync(string jobTypeId, int? skidsDiscipline, string tenantId, bool activeOnly = false);
        Task<WorkActivityDto> SaveWorkActivityAsync(WorkActivityDto dto, string tenantId, string actingUserId);

        // WorkEntryReasons
        Task<List<WorkEntryReasonDto>> GetWorkEntryReasonsAsync(int? category, string tenantId, bool activeOnly = false);
        Task<WorkEntryReasonDto> SaveWorkEntryReasonAsync(WorkEntryReasonDto dto, string tenantId, string actingUserId);

        // DocumentStatuses
        Task<List<DocumentStatusDto>> GetDocumentStatusesAsync(string tenantId, bool activeOnly = false);
        Task<DocumentStatusDto> SaveDocumentStatusAsync(DocumentStatusDto dto, string tenantId, string actingUserId);

        // Generic dropdown helpers used by the Daily Work Entry screen's
        // cascading selects.
        Task<List<DropdownDto>> GetActiveJobsForEmployeeDropdownAsync(string tenantId);
    }
}
