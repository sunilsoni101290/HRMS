using Application.DTOs.Onboarding;
using Application.Interfaces.Onboarding;
using Domain.Entities;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Domain.Enums.EnumExtensions;

namespace Application.Services.Onboarding
{
    public class OnboardingService : IOnboardingService
    {
        private readonly ApplicationDbContext _context;

        public OnboardingService(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Default Checklist (used the first time a tenant hasn't configured any template items for a stage yet)

        private static readonly Dictionary<OnboardingStageType, (string Title, bool IsMandatory)[]> DefaultChecklistItems = new()
        {
            [OnboardingStageType.DocumentVerification] = new (string, bool)[]
            {
                ("Aadhaar Card Verified", true),
                ("PAN Card Verified", true),
                ("Educational Certificates Verified", true),
                ("Previous Employer Experience/Relieving Letter Verified", true),
                ("Address Proof Verified", true)
            },
            [OnboardingStageType.WelcomeKit] = new (string, bool)[]
            {
                ("ID Card Issued", true),
                ("Welcome Letter Shared", true),
                ("Employee Handbook Shared", true),
                ("Official Email ID Created", true),
                ("Business Cards Ordered (if applicable)", false)
            },
            [OnboardingStageType.EmployeeCreation] = new (string, bool)[]
            {
                ("Employee Record Created in System", true),
                ("Employee Code Generated", true),
                ("Statutory Details (PF/ESI) Captured", true),
                ("Bank Account Details Captured", true),
                ("Reporting Manager Assigned", true)
            },
            [OnboardingStageType.AssetAllocation] = new (string, bool)[]
            {
                ("Laptop/Desktop Allocated", true),
                ("Access Card Issued", true),
                ("Mobile/SIM Allocated (if applicable)", false),
                ("Software Licenses Provisioned", true),
                ("Email & System Access Configured", true)
            },
            [OnboardingStageType.JoiningChecklist] = new (string, bool)[]
            {
                ("Induction Session Scheduled", true),
                ("Reporting Manager Introduction Done", true),
                ("Department Orientation Completed", true),
                ("Company Policies Acknowledged", true),
                ("Probation Terms Communicated", true),
                ("Buddy/Mentor Assigned", false)
            }
        };

        #endregion

        #region Case CRUD

        public async Task<OnboardingCaseDto> CreateCaseAsync(CreateOnboardingCaseDto dto, string tenantId, string actingUserId)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            if (string.IsNullOrWhiteSpace(dto.EmployeeId))
                throw new Exception("Employee is required.");

            var employee = await _context.Employees
                .FirstOrDefaultAsync(x => x.Id == dto.EmployeeId && x.TenantId == tenantId && !x.IsDeleted);

            if (employee == null)
                throw new Exception("Employee not found.");

            // Only one active (NotStarted/InProgress/OnHold) case per
            // employee at a time - a Completed or Cancelled case does not
            // block starting a new one.
            bool hasActiveCase = await _context.OnboardingCases
                .AnyAsync(x => x.EmployeeId == dto.EmployeeId
                            && x.TenantId == tenantId
                            && !x.IsDeleted
                            && (x.Status == OnboardingCaseStatus.NotStarted
                             || x.Status == OnboardingCaseStatus.InProgress
                             || x.Status == OnboardingCaseStatus.OnHold));

            if (hasActiveCase)
                throw new InvalidOperationException("An active onboarding case already exists for this employee.");

            var createdBy = string.IsNullOrWhiteSpace(actingUserId) ? "System" : actingUserId;

            var entity = new OnboardingCase
            {
                Id = IDManager.GetNewId(new OnboardingCase()),
                EmployeeId = employee.Id,
                CandidateId = string.IsNullOrWhiteSpace(dto.CandidateId) ? null : dto.CandidateId,
                TenantId = tenantId,
                CompanyId = employee.CompanyId,
                BranchId = employee.BranchId,
                StartDate = DateTime.UtcNow,
                TargetCompletionDate = dto.TargetCompletionDate,
                Status = OnboardingCaseStatus.InProgress,
                Remarks = dto.Remarks,
                CreatedBy = createdBy
            };

            _context.OnboardingCases.Add(entity);

            var checklistItems = await BuildChecklistItemsForNewCaseAsync(entity.Id, tenantId, createdBy);
            _context.OnboardingChecklistItems.AddRange(checklistItems);

            await _context.SaveChangesAsync();

            return await GetByIdAsync(entity.Id, tenantId)
                   ?? throw new Exception("Onboarding case was created but could not be reloaded.");
        }

        public async Task<OnboardingCaseDto?> GetByIdAsync(string id, string tenantId)
        {
            var entity = await _context.OnboardingCases
                .Include(x => x.Employee).ThenInclude(e => e.Department)
                .Include(x => x.Employee).ThenInclude(e => e.Designation)
                .Include(x => x.ChecklistItems)
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted);

            if (entity == null)
                return null;

            var nameMap = await BuildCompletedByNameMapAsync(entity.ChecklistItems ?? new List<OnboardingChecklistItem>());
            return AssembleDto(entity, nameMap);
        }

        public async Task<List<OnboardingCaseDto>> GetAllAsync(string tenantId, string? status, string? departmentId, string? search)
        {
            var query = _context.OnboardingCases
                .AsNoTracking()
                .Include(x => x.Employee).ThenInclude(e => e.Department)
                .Include(x => x.Employee).ThenInclude(e => e.Designation)
                .Include(x => x.ChecklistItems)
                .Where(x => x.TenantId == tenantId && !x.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse<OnboardingCaseStatus>(status, true, out var parsedStatus))
            {
                query = query.Where(x => x.Status == parsedStatus);
            }

            if (!string.IsNullOrWhiteSpace(departmentId))
                query = query.Where(x => x.Employee.DepartmentId == departmentId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(x =>
                    (x.Employee.FirstName + " " + x.Employee.LastName).ToLower().Contains(term) ||
                    x.Employee.EmployeeCode.ToLower().Contains(term));
            }

            var entities = await query
                .OrderByDescending(x => x.CreatedOn)
                .ToListAsync();

            var nameMap = await BuildCompletedByNameMapAsync(entities.SelectMany(x => x.ChecklistItems ?? new List<OnboardingChecklistItem>()));

            return entities.Select(x => AssembleDto(x, nameMap)).ToList();
        }

        public async Task<OnboardingCaseDto?> GetByEmployeeIdAsync(string employeeId, string tenantId)
        {
            var entity = await _context.OnboardingCases
                .Include(x => x.Employee).ThenInclude(e => e.Department)
                .Include(x => x.Employee).ThenInclude(e => e.Designation)
                .Include(x => x.ChecklistItems)
                .Where(x => x.EmployeeId == employeeId && x.TenantId == tenantId && !x.IsDeleted)
                .OrderByDescending(x => x.CreatedOn)
                .FirstOrDefaultAsync();

            if (entity == null)
                return null;

            var nameMap = await BuildCompletedByNameMapAsync(entity.ChecklistItems ?? new List<OnboardingChecklistItem>());
            return AssembleDto(entity, nameMap);
        }

        #endregion

        #region Checklist Items

        public async Task<OnboardingChecklistItemDto> UpdateChecklistItemStatusAsync(string itemId, UpdateChecklistItemStatusDto dto, string actingUserId, string tenantId)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            var item = await _context.OnboardingChecklistItems
                .FirstOrDefaultAsync(x => x.Id == itemId && x.TenantId == tenantId && !x.IsDeleted);

            if (item == null)
                throw new Exception("Checklist item not found.");

            item.Status = dto.Status;

            if (dto.Status == OnboardingChecklistItemStatus.Completed)
            {
                item.CompletedOn = DateTime.UtcNow;
                item.CompletedBy = actingUserId;
            }
            else
            {
                // Moved away from Completed - clear the completion stamp.
                item.CompletedOn = null;
                item.CompletedBy = null;
            }

            if (!string.IsNullOrWhiteSpace(dto.Remarks))
                item.Remarks = dto.Remarks;

            item.ModifiedOn = DateTime.UtcNow;
            item.ModifiedBy = actingUserId;

            await _context.SaveChangesAsync();

            await RecomputeCaseCompletionAsync(item.OnboardingCaseId, tenantId, actingUserId);

            var nameMap = await BuildCompletedByNameMapAsync(new[] { item });
            return AssembleItemDto(item, nameMap);
        }

        // NOTE: the task brief listed this as AddChecklistItemAsync(caseId, dto, tenantId)
        // with no acting-user parameter - an actingUserId parameter was
        // added here (like every other Create method in this codebase) so
        // CreatedBy/audit trail is populated correctly. See final report for
        // this deviation.
        public async Task<OnboardingChecklistItemDto> AddChecklistItemAsync(string caseId, AddChecklistItemDto dto, string tenantId, string actingUserId)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            if (string.IsNullOrWhiteSpace(dto.Title))
                throw new Exception("Title is required.");

            var onboardingCase = await _context.OnboardingCases
                .FirstOrDefaultAsync(x => x.Id == caseId && x.TenantId == tenantId && !x.IsDeleted);

            if (onboardingCase == null)
                throw new Exception("Onboarding case not found.");

            int sortOrder = dto.SortOrder ?? await NextSortOrderAsync(caseId, dto.StageType, tenantId);

            var createdBy = string.IsNullOrWhiteSpace(actingUserId) ? "System" : actingUserId;

            var item = new OnboardingChecklistItem
            {
                Id = IDManager.GetNewId(new OnboardingChecklistItem()),
                OnboardingCaseId = caseId,
                StageType = dto.StageType,
                Title = dto.Title,
                Description = dto.Description,
                IsMandatory = dto.IsMandatory,
                SortOrder = sortOrder,
                Status = OnboardingChecklistItemStatus.Pending,
                TenantId = tenantId,
                CreatedBy = createdBy
            };

            _context.OnboardingChecklistItems.Add(item);
            await _context.SaveChangesAsync();

            return AssembleItemDto(item, new Dictionary<string, string>());
        }

        // Deliberately allows deleting ANY checklist item (mandatory or not,
        // seeded from a template or manually added) - kept simple rather
        // than tracking item provenance. Soft delete, consistent with the
        // rest of the codebase.
        public async Task<bool> DeleteChecklistItemAsync(string itemId, string tenantId, string actingUserId)
        {
            var item = await _context.OnboardingChecklistItems
                .FirstOrDefaultAsync(x => x.Id == itemId && x.TenantId == tenantId && !x.IsDeleted);

            if (item == null)
                return false;

            item.IsDeleted = true;
            item.ModifiedOn = DateTime.UtcNow;
            item.ModifiedBy = actingUserId;

            await _context.SaveChangesAsync();

            await RecomputeCaseCompletionAsync(item.OnboardingCaseId, tenantId, actingUserId);

            return true;
        }

        #endregion

        #region Case Status Transitions

        public async Task<OnboardingCaseDto> UpdateCaseStatusAsync(string caseId, OnboardingCaseStatus newStatus, string? remarks, string tenantId)
        {
            var entity = await _context.OnboardingCases
                .Include(x => x.Employee).ThenInclude(e => e.Department)
                .Include(x => x.Employee).ThenInclude(e => e.Designation)
                .Include(x => x.ChecklistItems)
                .FirstOrDefaultAsync(x => x.Id == caseId && x.TenantId == tenantId && !x.IsDeleted);

            if (entity == null)
                throw new Exception("Onboarding case not found.");

            entity.Status = newStatus;

            if (newStatus == OnboardingCaseStatus.Completed)
            {
                entity.CompletedOn = DateTime.UtcNow;
            }
            else
            {
                // Reactivated (back to NotStarted/InProgress/OnHold) or
                // Cancelled - no longer meaningfully "completed".
                entity.CompletedOn = null;
            }

            if (!string.IsNullOrWhiteSpace(remarks))
                entity.Remarks = remarks;

            entity.ModifiedOn = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var nameMap = await BuildCompletedByNameMapAsync(entity.ChecklistItems ?? new List<OnboardingChecklistItem>());
            return AssembleDto(entity, nameMap);
        }

        #endregion

        #region Template Management

        public async Task<List<OnboardingChecklistTemplateItemDto>> GetTemplateItemsAsync(string tenantId, OnboardingStageType? stage)
        {
            var query = _context.OnboardingChecklistTemplateItems
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId && !x.IsDeleted);

            if (stage.HasValue)
                query = query.Where(x => x.StageType == stage.Value);

            var items = await query
                .OrderBy(x => x.StageType)
                .ThenBy(x => x.SortOrder)
                .ToListAsync();

            return items.Select(AssembleTemplateDto).ToList();
        }

        public async Task<OnboardingChecklistTemplateItemDto> UpsertTemplateItemAsync(UpsertOnboardingTemplateItemDto dto, string? existingId, string tenantId, string actingUserId)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            if (string.IsNullOrWhiteSpace(dto.Title))
                throw new Exception("Title is required.");

            OnboardingChecklistTemplateItem entity;

            if (!string.IsNullOrWhiteSpace(existingId))
            {
                entity = await _context.OnboardingChecklistTemplateItems
                    .FirstOrDefaultAsync(x => x.Id == existingId && x.TenantId == tenantId && !x.IsDeleted);

                if (entity == null)
                    throw new Exception("Template item not found.");

                entity.ModifiedOn = DateTime.UtcNow;
                entity.ModifiedBy = string.IsNullOrWhiteSpace(actingUserId) ? "System" : actingUserId;
            }
            else
            {
                entity = new OnboardingChecklistTemplateItem
                {
                    Id = IDManager.GetNewId(new OnboardingChecklistTemplateItem()),
                    TenantId = tenantId,
                    CreatedBy = string.IsNullOrWhiteSpace(actingUserId) ? "System" : actingUserId
                };

                _context.OnboardingChecklistTemplateItems.Add(entity);
            }

            entity.StageType = dto.StageType;
            entity.Title = dto.Title;
            entity.Description = dto.Description;
            entity.IsMandatory = dto.IsMandatory;
            entity.SortOrder = dto.SortOrder;
            entity.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();

            return AssembleTemplateDto(entity);
        }

        public async Task<bool> DeleteTemplateItemAsync(string id, string tenantId)
        {
            var entity = await _context.OnboardingChecklistTemplateItems
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted);

            if (entity == null)
                return false;

            entity.IsDeleted = true;
            entity.ModifiedOn = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        #endregion

        #region Helpers

        private async Task<List<OnboardingChecklistItem>> BuildChecklistItemsForNewCaseAsync(string caseId, string tenantId, string createdBy)
        {
            var result = new List<OnboardingChecklistItem>();

            var activeTemplates = await _context.OnboardingChecklistTemplateItems
                .Where(x => x.TenantId == tenantId && x.IsActive && !x.IsDeleted)
                .OrderBy(x => x.StageType)
                .ThenBy(x => x.SortOrder)
                .ToListAsync();

            foreach (OnboardingStageType stage in Enum.GetValues(typeof(OnboardingStageType)))
            {
                var stageTemplates = activeTemplates.Where(x => x.StageType == stage).ToList();

                if (stageTemplates.Count > 0)
                {
                    foreach (var t in stageTemplates)
                    {
                        result.Add(new OnboardingChecklistItem
                        {
                            Id = IDManager.GetNewId(new OnboardingChecklistItem()),
                            OnboardingCaseId = caseId,
                            StageType = stage,
                            Title = t.Title,
                            Description = t.Description,
                            IsMandatory = t.IsMandatory,
                            SortOrder = t.SortOrder,
                            Status = OnboardingChecklistItemStatus.Pending,
                            TenantId = tenantId,
                            CreatedBy = createdBy
                        });
                    }
                }
                else if (DefaultChecklistItems.TryGetValue(stage, out var defaults))
                {
                    int sort = 1;
                    foreach (var (title, isMandatory) in defaults)
                    {
                        result.Add(new OnboardingChecklistItem
                        {
                            Id = IDManager.GetNewId(new OnboardingChecklistItem()),
                            OnboardingCaseId = caseId,
                            StageType = stage,
                            Title = title,
                            IsMandatory = isMandatory,
                            SortOrder = sort++,
                            Status = OnboardingChecklistItemStatus.Pending,
                            TenantId = tenantId,
                            CreatedBy = createdBy
                        });
                    }
                }
            }

            return result;
        }

        private async Task<int> NextSortOrderAsync(string caseId, OnboardingStageType stage, string tenantId)
        {
            var maxSort = await _context.OnboardingChecklistItems
                .Where(x => x.OnboardingCaseId == caseId && x.StageType == stage && x.TenantId == tenantId && !x.IsDeleted)
                .Select(x => (int?)x.SortOrder)
                .MaxAsync();

            return (maxSort ?? 0) + 1;
        }

        // If every mandatory checklist item across all 5 stages is now
        // Completed or NotApplicable, auto-completes the case - unless it is
        // already Completed/Cancelled, or has zero mandatory items at all
        // (nothing to "complete" yet, e.g. right after creation before any
        // template/defaults were mandatory).
        private async Task RecomputeCaseCompletionAsync(string caseId, string tenantId, string? actingUserId)
        {
            var onboardingCase = await _context.OnboardingCases
                .Include(x => x.ChecklistItems)
                .FirstOrDefaultAsync(x => x.Id == caseId && x.TenantId == tenantId && !x.IsDeleted);

            if (onboardingCase == null)
                return;

            if (onboardingCase.Status == OnboardingCaseStatus.Completed ||
                onboardingCase.Status == OnboardingCaseStatus.Cancelled)
                return;

            var items = (onboardingCase.ChecklistItems ?? new List<OnboardingChecklistItem>())
                .Where(x => !x.IsDeleted)
                .ToList();

            var mandatoryItems = items.Where(x => x.IsMandatory).ToList();

            bool allMandatoryDone = mandatoryItems.Count > 0 &&
                mandatoryItems.All(x => x.Status == OnboardingChecklistItemStatus.Completed
                                      || x.Status == OnboardingChecklistItemStatus.NotApplicable);

            if (allMandatoryDone)
            {
                onboardingCase.Status = OnboardingCaseStatus.Completed;
                onboardingCase.CompletedOn = DateTime.UtcNow;
                onboardingCase.ModifiedOn = DateTime.UtcNow;
                onboardingCase.ModifiedBy = string.IsNullOrWhiteSpace(actingUserId) ? "System" : actingUserId;

                await _context.SaveChangesAsync();
            }
        }

        // ApprovedBy/CompletedBy store the acting User.Id - resolve a
        // display name via the linked Employee if one exists, else fall
        // back to the account's Username. Mirrors
        // AttendanceRegularizationService.BuildApprovedByNameMapAsync.
        private async Task<Dictionary<string, string>> BuildCompletedByNameMapAsync(IEnumerable<OnboardingChecklistItem> items)
        {
            var userIds = items
                .Where(x => !string.IsNullOrEmpty(x.CompletedBy))
                .Select(x => x.CompletedBy)
                .Distinct()
                .ToList();

            if (userIds.Count == 0)
                return new Dictionary<string, string>();

            var users = await _context.Users
                .Include(u => u.Employee)
                .Where(u => userIds.Contains(u.Id))
                .ToListAsync();

            return users.ToDictionary(
                u => u.Id,
                u => u.Employee != null ? $"{u.Employee.FirstName} {u.Employee.LastName}".Trim() : u.Username);
        }

        private static int ComputeProgressPercent(IEnumerable<OnboardingChecklistItem> items)
        {
            var mandatory = items.Where(x => x.IsMandatory).ToList();

            if (mandatory.Count == 0)
                return 100;

            var done = mandatory.Count(x => x.Status == OnboardingChecklistItemStatus.Completed
                                          || x.Status == OnboardingChecklistItemStatus.NotApplicable);

            return (int)Math.Round(done * 100.0 / mandatory.Count, MidpointRounding.AwayFromZero);
        }

        private static OnboardingCaseDto AssembleDto(OnboardingCase x, Dictionary<string, string> completedByNameMap)
        {
            var items = (x.ChecklistItems ?? new List<OnboardingChecklistItem>())
                .Where(i => !i.IsDeleted)
                .OrderBy(i => i.StageType)
                .ThenBy(i => i.SortOrder)
                .ToList();

            return new OnboardingCaseDto
            {
                Id = x.Id,

                EmployeeId = x.EmployeeId,
                EmployeeName = x.Employee != null ? $"{x.Employee.FirstName} {x.Employee.LastName}".Trim() : null,
                EmployeeCode = x.Employee?.EmployeeCode,
                DepartmentName = x.Employee?.Department != null ? x.Employee.Department.Name : null,
                DesignationName = x.Employee?.Designation != null ? x.Employee.Designation.Name : null,

                CandidateId = x.CandidateId,

                StartDate = x.StartDate,
                TargetCompletionDate = x.TargetCompletionDate,

                Status = x.Status,
                StatusName = x.Status.ToString(),

                CompletedOn = x.CompletedOn,

                ProgressPercent = ComputeProgressPercent(items),

                Remarks = x.Remarks,

                CreatedBy = x.CreatedBy,
                CreatedOn = x.CreatedOn,

                ChecklistItems = items.Select(i => AssembleItemDto(i, completedByNameMap)).ToList()
            };
        }

        private static OnboardingChecklistItemDto AssembleItemDto(OnboardingChecklistItem i, Dictionary<string, string> completedByNameMap)
        {
            return new OnboardingChecklistItemDto
            {
                Id = i.Id,
                OnboardingCaseId = i.OnboardingCaseId,

                StageType = i.StageType,
                StageTypeName = i.StageType.ToString(),

                Title = i.Title,
                Description = i.Description,

                IsMandatory = i.IsMandatory,
                SortOrder = i.SortOrder,

                Status = i.Status,
                StatusName = i.Status.ToString(),

                CompletedOn = i.CompletedOn,
                CompletedBy = i.CompletedBy,
                CompletedByName = !string.IsNullOrEmpty(i.CompletedBy) && completedByNameMap.TryGetValue(i.CompletedBy, out var name)
                    ? name
                    : null,

                Remarks = i.Remarks
            };
        }

        private static OnboardingChecklistTemplateItemDto AssembleTemplateDto(OnboardingChecklistTemplateItem x)
        {
            return new OnboardingChecklistTemplateItemDto
            {
                Id = x.Id,
                StageType = x.StageType,
                StageTypeName = x.StageType.ToString(),
                Title = x.Title,
                Description = x.Description,
                IsMandatory = x.IsMandatory,
                SortOrder = x.SortOrder,
                IsActive = x.IsActive
            };
        }

        #endregion
    }
}
