using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs;
using Application.DTOs.WorkTracking;
using Application.Interfaces.WorkTracking;
using Domain.Entities;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static Domain.Enums.EnumExtensions;

namespace Application.Services.WorkTracking
{
    /// <inheritdoc cref="IWorkTrackingMasterService"/>
    public class WorkTrackingMasterService : IWorkTrackingMasterService
    {
        private readonly ApplicationDbContext _context;

        public WorkTrackingMasterService(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==================================================================
        // Clients
        // ==================================================================

        public async Task<List<ClientDto>> GetClientsAsync(string tenantId, bool activeOnly = false)
        {
            var query = _context.Clients.AsNoTracking().Where(x => x.TenantId == tenantId);
            if (activeOnly) query = query.Where(x => x.IsActive);

            return await query.OrderBy(x => x.Name)
                .Select(x => new ClientDto { Id = x.Id, Name = x.Name, Code = x.Code, IsActive = x.IsActive })
                .ToListAsync();
        }

        public async Task<ClientDto> SaveClientAsync(ClientDto dto, string tenantId, string actingUserId)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new Exception("Client name is required.");

            Client entity;

            if (string.IsNullOrEmpty(dto.Id))
            {
                entity = new Client { Id = IDManager.GetNewId(new Client()), TenantId = tenantId, CreatedBy = actingUserId, CreatedOn = DateTime.UtcNow };
                _context.Clients.Add(entity);
            }
            else
            {
                entity = await _context.Clients.FirstOrDefaultAsync(x => x.Id == dto.Id && x.TenantId == tenantId)
                    ?? throw new Exception("Client not found.");
                entity.ModifiedBy = actingUserId;
                entity.ModifiedOn = DateTime.UtcNow;
            }

            entity.Name = dto.Name.Trim();
            entity.Code = dto.Code?.Trim();
            entity.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();

            dto.Id = entity.Id;
            return dto;
        }

        // ==================================================================
        // WorkJobs
        // ==================================================================

        public async Task<List<WorkJobDto>> GetWorkJobsAsync(string tenantId, bool activeOnly = false)
        {
            var query = _context.WorkJobs.AsNoTracking().Include(x => x.Client).Where(x => x.TenantId == tenantId);
            if (activeOnly) query = query.Where(x => x.IsActive && x.Status == WorkJobStatus.Active);

            var entities = await query.OrderBy(x => x.JobNumber).ToListAsync();

            return entities.Select(x => new WorkJobDto
            {
                Id = x.Id,
                JobNumber = x.JobNumber,
                JobName = x.JobName,
                ClientId = x.ClientId,
                ClientName = x.Client?.Name,
                Status = (int)x.Status,
                StatusName = x.Status.ToString(),
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                IsActive = x.IsActive
            }).ToList();
        }

        public async Task<WorkJobDto> SaveWorkJobAsync(WorkJobDto dto, string tenantId, string actingUserId)
        {
            if (string.IsNullOrWhiteSpace(dto.JobNumber))
                throw new Exception("Job number is required.");
            if (string.IsNullOrWhiteSpace(dto.JobName))
                throw new Exception("Job name is required.");
            if (string.IsNullOrWhiteSpace(dto.ClientId))
                throw new Exception("Client is required.");

            var clientExists = await _context.Clients.AnyAsync(x => x.Id == dto.ClientId && x.TenantId == tenantId);
            if (!clientExists)
                throw new Exception("Selected client is invalid.");

            var duplicateNumber = await _context.WorkJobs.AnyAsync(x =>
                x.TenantId == tenantId && x.JobNumber == dto.JobNumber.Trim() && x.Id != dto.Id);
            if (duplicateNumber)
                throw new Exception($"A job with number '{dto.JobNumber}' already exists.");

            WorkJob entity;

            if (string.IsNullOrEmpty(dto.Id))
            {
                entity = new WorkJob { Id = IDManager.GetNewId(new WorkJob()), TenantId = tenantId, CreatedBy = actingUserId, CreatedOn = DateTime.UtcNow };
                _context.WorkJobs.Add(entity);
            }
            else
            {
                entity = await _context.WorkJobs.FirstOrDefaultAsync(x => x.Id == dto.Id && x.TenantId == tenantId)
                    ?? throw new Exception("Job not found.");
                entity.ModifiedBy = actingUserId;
                entity.ModifiedOn = DateTime.UtcNow;
            }

            entity.JobNumber = dto.JobNumber.Trim();
            entity.JobName = dto.JobName.Trim();
            entity.ClientId = dto.ClientId;
            entity.Status = Enum.IsDefined(typeof(WorkJobStatus), dto.Status) ? (WorkJobStatus)dto.Status : WorkJobStatus.Active;
            entity.StartDate = dto.StartDate;
            entity.EndDate = dto.EndDate;
            entity.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();

            dto.Id = entity.Id;
            return dto;
        }

        // ==================================================================
        // JobItems
        // ==================================================================

        public async Task<List<JobItemDto>> GetJobItemsAsync(string workJobId, string tenantId, bool activeOnly = false)
        {
            var query = _context.JobItems.AsNoTracking()
                .Include(x => x.DocumentStatus)
                .Where(x => x.TenantId == tenantId && x.WorkJobId == workJobId);

            if (activeOnly) query = query.Where(x => x.IsActive);

            var entities = await query.OrderBy(x => x.Code).ToListAsync();

            return entities.Select(x => new JobItemDto
            {
                Id = x.Id,
                WorkJobId = x.WorkJobId,
                Code = x.Code,
                Description = x.Description,
                ItemType = (int)x.ItemType,
                ItemTypeName = x.ItemType.ToString(),
                TotalWeightMT = x.TotalWeightMT,
                DocumentStatusId = x.DocumentStatusId,
                DocumentStatusName = x.DocumentStatus?.DisplayName,
                IsActive = x.IsActive
            }).ToList();
        }

        public async Task<JobItemDto> SaveJobItemAsync(JobItemDto dto, string tenantId, string actingUserId)
        {
            if (string.IsNullOrWhiteSpace(dto.WorkJobId))
                throw new Exception("Job is required.");
            if (string.IsNullOrWhiteSpace(dto.Code))
                throw new Exception("Item code is required.");

            var jobExists = await _context.WorkJobs.AnyAsync(x => x.Id == dto.WorkJobId && x.TenantId == tenantId);
            if (!jobExists)
                throw new Exception("Selected job is invalid.");

            if (!string.IsNullOrEmpty(dto.DocumentStatusId))
            {
                var docStatusExists = await _context.DocumentStatuses.AnyAsync(x => x.Id == dto.DocumentStatusId && x.TenantId == tenantId);
                if (!docStatusExists)
                    throw new Exception("Selected document status is invalid.");
            }

            JobItem entity;

            if (string.IsNullOrEmpty(dto.Id))
            {
                entity = new JobItem { Id = IDManager.GetNewId(new JobItem()), TenantId = tenantId, CreatedBy = actingUserId, CreatedOn = DateTime.UtcNow };
                _context.JobItems.Add(entity);
            }
            else
            {
                entity = await _context.JobItems.FirstOrDefaultAsync(x => x.Id == dto.Id && x.TenantId == tenantId)
                    ?? throw new Exception("Job item not found.");
                entity.ModifiedBy = actingUserId;
                entity.ModifiedOn = DateTime.UtcNow;
            }

            entity.WorkJobId = dto.WorkJobId;
            entity.Code = dto.Code.Trim();
            entity.Description = dto.Description?.Trim();
            entity.ItemType = Enum.IsDefined(typeof(JobItemType), dto.ItemType) ? (JobItemType)dto.ItemType : JobItemType.Other;
            entity.TotalWeightMT = dto.TotalWeightMT;
            entity.DocumentStatusId = string.IsNullOrEmpty(dto.DocumentStatusId) ? null : dto.DocumentStatusId;
            entity.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();

            dto.Id = entity.Id;
            return dto;
        }

        // ==================================================================
        // JobTypes
        // ==================================================================

        public async Task<List<JobTypeDto>> GetJobTypesAsync(string tenantId, bool activeOnly = false)
        {
            var query = _context.JobTypes.AsNoTracking().Where(x => x.TenantId == tenantId);
            if (activeOnly) query = query.Where(x => x.IsActive);

            return await query.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name)
                .Select(x => new JobTypeDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Code = x.Code,
                    DisplayOrder = x.DisplayOrder,
                    HasDisciplines = x.HasDisciplines,
                    IsActive = x.IsActive
                }).ToListAsync();
        }

        public async Task<JobTypeDto> SaveJobTypeAsync(JobTypeDto dto, string tenantId, string actingUserId)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new Exception("Job type name is required.");

            JobType entity;

            if (string.IsNullOrEmpty(dto.Id))
            {
                entity = new JobType { Id = IDManager.GetNewId(new JobType()), TenantId = tenantId, CreatedBy = actingUserId, CreatedOn = DateTime.UtcNow };
                _context.JobTypes.Add(entity);
            }
            else
            {
                entity = await _context.JobTypes.FirstOrDefaultAsync(x => x.Id == dto.Id && x.TenantId == tenantId)
                    ?? throw new Exception("Job type not found.");
                entity.ModifiedBy = actingUserId;
                entity.ModifiedOn = DateTime.UtcNow;
            }

            entity.Name = dto.Name.Trim();
            entity.Code = dto.Code?.Trim();
            entity.DisplayOrder = dto.DisplayOrder;
            entity.HasDisciplines = dto.HasDisciplines;
            entity.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();

            dto.Id = entity.Id;
            return dto;
        }

        // ==================================================================
        // WorkActivities
        // ==================================================================

        public async Task<List<WorkActivityDto>> GetWorkActivitiesAsync(string jobTypeId, int? skidsDiscipline, string tenantId, bool activeOnly = false)
        {
            var query = _context.WorkActivities.AsNoTracking()
                .Include(x => x.JobType)
                .Where(x => x.TenantId == tenantId && x.JobTypeId == jobTypeId);

            if (activeOnly) query = query.Where(x => x.IsActive);

            // Cascading rule (spec section 9/14): when the Job Type has
            // disciplines (Skids Packages), only that discipline's
            // activities are offered; rows with SkidsDiscipline == null
            // under a discipline-having Job Type would be a data-entry
            // mistake, so they're excluded rather than silently shown.
            if (skidsDiscipline.HasValue && Enum.IsDefined(typeof(SkidsDiscipline), skidsDiscipline.Value))
                query = query.Where(x => x.SkidsDiscipline == (SkidsDiscipline)skidsDiscipline.Value);

            var entities = await query.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name).ToListAsync();

            return entities.Select(x => new WorkActivityDto
            {
                Id = x.Id,
                JobTypeId = x.JobTypeId,
                JobTypeName = x.JobType?.Name,
                SkidsDiscipline = x.SkidsDiscipline.HasValue ? (int)x.SkidsDiscipline.Value : (int?)null,
                SkidsDisciplineName = x.SkidsDiscipline?.ToString(),
                Name = x.Name,
                WorkCategory = (int)x.WorkCategory,
                WorkCategoryName = x.WorkCategory.ToString(),
                DisplayOrder = x.DisplayOrder,
                RequiresReason = x.RequiresReason,
                AllowFreeTextOther = x.AllowFreeTextOther,
                IsActive = x.IsActive
            }).ToList();
        }

        public async Task<WorkActivityDto> SaveWorkActivityAsync(WorkActivityDto dto, string tenantId, string actingUserId)
        {
            if (string.IsNullOrWhiteSpace(dto.JobTypeId))
                throw new Exception("Job type is required.");
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new Exception("Activity name is required.");

            var jobTypeExists = await _context.JobTypes.AnyAsync(x => x.Id == dto.JobTypeId && x.TenantId == tenantId);
            if (!jobTypeExists)
                throw new Exception("Selected job type is invalid.");

            WorkActivity entity;

            if (string.IsNullOrEmpty(dto.Id))
            {
                entity = new WorkActivity { Id = IDManager.GetNewId(new WorkActivity()), TenantId = tenantId, CreatedBy = actingUserId, CreatedOn = DateTime.UtcNow };
                _context.WorkActivities.Add(entity);
            }
            else
            {
                entity = await _context.WorkActivities.FirstOrDefaultAsync(x => x.Id == dto.Id && x.TenantId == tenantId)
                    ?? throw new Exception("Work activity not found.");
                entity.ModifiedBy = actingUserId;
                entity.ModifiedOn = DateTime.UtcNow;
            }

            entity.JobTypeId = dto.JobTypeId;
            entity.SkidsDiscipline = dto.SkidsDiscipline.HasValue && Enum.IsDefined(typeof(SkidsDiscipline), dto.SkidsDiscipline.Value)
                ? (SkidsDiscipline)dto.SkidsDiscipline.Value
                : null;
            entity.Name = dto.Name.Trim();
            entity.WorkCategory = Enum.IsDefined(typeof(WorkCategory), dto.WorkCategory) ? (WorkCategory)dto.WorkCategory : WorkCategory.Direct;
            entity.DisplayOrder = dto.DisplayOrder;
            entity.RequiresReason = dto.RequiresReason;
            entity.AllowFreeTextOther = dto.AllowFreeTextOther;
            entity.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();

            dto.Id = entity.Id;
            return dto;
        }

        // ==================================================================
        // WorkEntryReasons
        // ==================================================================

        public async Task<List<WorkEntryReasonDto>> GetWorkEntryReasonsAsync(int? category, string tenantId, bool activeOnly = false)
        {
            var query = _context.WorkEntryReasons.AsNoTracking().Where(x => x.TenantId == tenantId);

            if (category.HasValue && Enum.IsDefined(typeof(WorkEntryReasonCategory), category.Value))
                query = query.Where(x => x.Category == (WorkEntryReasonCategory)category.Value);

            if (activeOnly) query = query.Where(x => x.IsActive);

            return await query.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name)
                .Select(x => new WorkEntryReasonDto
                {
                    Id = x.Id,
                    Category = (int)x.Category,
                    CategoryName = x.Category.ToString(),
                    Name = x.Name,
                    DisplayOrder = x.DisplayOrder,
                    IsActive = x.IsActive
                }).ToListAsync();
        }

        public async Task<WorkEntryReasonDto> SaveWorkEntryReasonAsync(WorkEntryReasonDto dto, string tenantId, string actingUserId)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new Exception("Reason name is required.");
            if (!Enum.IsDefined(typeof(WorkEntryReasonCategory), dto.Category))
                throw new Exception("A valid category (Idle or Downtime) is required.");

            WorkEntryReason entity;

            if (string.IsNullOrEmpty(dto.Id))
            {
                entity = new WorkEntryReason { Id = IDManager.GetNewId(new WorkEntryReason()), TenantId = tenantId, CreatedBy = actingUserId, CreatedOn = DateTime.UtcNow };
                _context.WorkEntryReasons.Add(entity);
            }
            else
            {
                entity = await _context.WorkEntryReasons.FirstOrDefaultAsync(x => x.Id == dto.Id && x.TenantId == tenantId)
                    ?? throw new Exception("Reason not found.");
                entity.ModifiedBy = actingUserId;
                entity.ModifiedOn = DateTime.UtcNow;
            }

            entity.Category = (WorkEntryReasonCategory)dto.Category;
            entity.Name = dto.Name.Trim();
            entity.DisplayOrder = dto.DisplayOrder;
            entity.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();

            dto.Id = entity.Id;
            return dto;
        }

        // ==================================================================
        // DocumentStatuses
        // ==================================================================

        public async Task<List<DocumentStatusDto>> GetDocumentStatusesAsync(string tenantId, bool activeOnly = false)
        {
            var query = _context.DocumentStatuses.AsNoTracking().Where(x => x.TenantId == tenantId);
            if (activeOnly) query = query.Where(x => x.IsActive);

            return await query.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Code)
                .Select(x => new DocumentStatusDto
                {
                    Id = x.Id,
                    Code = x.Code,
                    DisplayName = x.DisplayName,
                    DisplayOrder = x.DisplayOrder,
                    IsActive = x.IsActive
                }).ToListAsync();
        }

        public async Task<DocumentStatusDto> SaveDocumentStatusAsync(DocumentStatusDto dto, string tenantId, string actingUserId)
        {
            if (string.IsNullOrWhiteSpace(dto.Code))
                throw new Exception("Code is required.");
            if (string.IsNullOrWhiteSpace(dto.DisplayName))
                throw new Exception("Display name is required.");

            DocumentStatus entity;

            if (string.IsNullOrEmpty(dto.Id))
            {
                entity = new DocumentStatus { Id = IDManager.GetNewId(new DocumentStatus()), TenantId = tenantId, CreatedBy = actingUserId, CreatedOn = DateTime.UtcNow };
                _context.DocumentStatuses.Add(entity);
            }
            else
            {
                entity = await _context.DocumentStatuses.FirstOrDefaultAsync(x => x.Id == dto.Id && x.TenantId == tenantId)
                    ?? throw new Exception("Document status not found.");
                entity.ModifiedBy = actingUserId;
                entity.ModifiedOn = DateTime.UtcNow;
            }

            entity.Code = dto.Code.Trim();
            entity.DisplayName = dto.DisplayName.Trim();
            entity.DisplayOrder = dto.DisplayOrder;
            entity.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();

            dto.Id = entity.Id;
            return dto;
        }

        // ==================================================================
        // Dropdown helpers
        // ==================================================================

        public async Task<List<DropdownDto>> GetActiveJobsForEmployeeDropdownAsync(string tenantId)
        {
            // "Only active/allowed jobs should appear in the Daily Work
            // Entry dropdown" (spec section 4.3) - no per-employee job
            // assignment master exists in this codebase, so "allowed"
            // currently means "Active status" for the whole tenant. If a
            // future requirement needs per-employee job assignment, extend
            // here rather than in the UI.
            return await _context.WorkJobs.AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.IsActive && x.Status == WorkJobStatus.Active)
                .OrderBy(x => x.JobNumber)
                .Select(x => new DropdownDto { Value = x.Id, Text = x.JobNumber + " - " + x.JobName })
                .ToListAsync();
        }
    }
}
