using Application.DTOs.EmployeeLifecycle;
using Application.Interfaces.EmployeeLifecycle;
using Domain.Entities;
using Domain.Helper;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Domain.Enums.EnumExtensions;

namespace Application.Services.EmployeeLifecycle
{
    // Phase 2 of the "Probation & Confirmation" module - Performance
    // Improvement Plan (PIP). See Domain/Entities/PipRecord.cs.
    //
    // CREATION (CreateAsync) is an automatic system/HR hand-off from an
    // Approved ProbationConfirmation row with Recommendation ==
    // PlaceOnPIP - it requires Create permission on
    // AppFeatureConstants.PIP but has NO maker-checker gate.
    //
    // The maker-checker (segregation-of-duties) gate - identical
    // invariant to ProbationConfirmationService (actingUserId !=
    // MakerId, no override, checked BEFORE any permission check) -
    // applies instead to the FINAL OUTCOME RESOLUTION:
    // ProposeOutcomeAsync (Maker proposes Successful/Unsuccessful) then
    // ApproveOutcomeAsync/RejectOutcomeAsync (a DIFFERENT Checker
    // resolves it).
    public class PipService : IPipService
    {
        private readonly ApplicationDbContext _context;

        public PipService(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Create

        public async Task<PipRecordDto> CreateAsync(CreatePipRecordDto dto, string tenantId, string actingUserId)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            // HR authorization - Create permission on PIP. No maker-
            // checker gate on this action (see class remarks above).
            if (!await HasPermissionAsync(actingUserId, Actions.Create))
                throw new UnauthorizedAccessException("You are not authorized to create a PIP record.");

            var probationConfirmation = await _context.ProbationConfirmations
                .FirstOrDefaultAsync(x => x.Id == dto.ProbationConfirmationId && x.TenantId == tenantId);

            if (probationConfirmation == null)
                throw new Exception("Probation Confirmation record not found.");

            if (probationConfirmation.Status != ProbationConfirmationStatus.Approved)
                throw new Exception("A PIP can only be created from an Approved Probation Confirmation record.");

            if (probationConfirmation.Recommendation != ProbationRecommendation.PlaceOnPIP)
                throw new Exception("A PIP can only be created from a Probation Confirmation recommending Place on PIP.");

            // One PIP per triggering confirmation - prevent duplicates.
            var alreadyExists = await _context.PipRecords
                .AnyAsync(x => x.ProbationConfirmationId == dto.ProbationConfirmationId && x.TenantId == tenantId);

            if (alreadyExists)
                throw new Exception("A PIP record has already been created for this Probation Confirmation.");

            // Defense in depth - don't trust the caller's EmployeeId
            // blindly even though this is HR-only; it must match the
            // triggering Probation Confirmation's own EmployeeId.
            if (!string.Equals(dto.EmployeeId, probationConfirmation.EmployeeId, StringComparison.Ordinal))
                throw new Exception("The Employee does not match the Employee on the referenced Probation Confirmation record.");

            if (dto.EndDate <= dto.StartDate)
                throw new Exception("End Date must be after Start Date.");

            var entity = new PipRecord
            {
                Id = IDManager.GetNewId(new PipRecord()),

                EmployeeId = dto.EmployeeId,
                ProbationConfirmationId = dto.ProbationConfirmationId,

                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Goals = dto.Goals,

                FinalOutcome = PipFinalOutcome.InProgress,

                TenantId = tenantId,
                CreatedOn = DateTime.UtcNow,
                CreatedBy = actingUserId
            };

            _context.PipRecords.Add(entity);
            await _context.SaveChangesAsync();

            return await GetByIdInternalAsync(entity.Id, tenantId);
        }

        #endregion

        #region Read

        private async Task<PipRecord> GetEntityByIdAsync(string id, string tenantId)
        {
            var entity = await _context.PipRecords
                .Include(x => x.Employee).ThenInclude(e => e.Department)
                .Include(x => x.Employee).ThenInclude(e => e.Designation)
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);

            if (entity == null)
                throw new Exception("PIP record not found.");

            return entity;
        }

        // Internal, trusted re-fetch used immediately after this service
        // itself already performed and authorized a mutation - no
        // additional authorization check here.
        private async Task<PipRecordDto> GetByIdInternalAsync(string id, string tenantId)
        {
            var entity = await GetEntityByIdAsync(id, tenantId);
            return await AssembleDtoAsync(entity);
        }

        // HR-internal view: anyone holding View permission on PIP may view
        // any record tenant-wide - same permissive model as
        // ProbationConfirmationService.GetByIdAsync.
        public async Task<PipRecordDto> GetByIdAsync(string id, string tenantId, string actingUserId)
        {
            if (!await HasPermissionAsync(actingUserId, Actions.View))
                throw new UnauthorizedAccessException("You are not authorized to view PIP records.");

            var entity = await GetEntityByIdAsync(id, tenantId);
            return await AssembleDtoAsync(entity);
        }

        // "Active PIPs" HR tracking view - FinalOutcome == InProgress only.
        // No permission gate, mirroring
        // ProbationConfirmationService.GetDueForReviewAsync's equivalent
        // dashboard-style listing.
        public async Task<List<PipRecordDto>> GetActivePipsAsync(string tenantId, string? departmentId, string? search, string actingUserId)
        {
            if (!await HasPermissionAsync(actingUserId, Actions.View))
                throw new UnauthorizedAccessException("You are not authorized to view PIP records.");

            var query = _context.PipRecords
                .Include(x => x.Employee).ThenInclude(e => e.Department)
                .Include(x => x.Employee).ThenInclude(e => e.Designation)
                .Where(x => x.TenantId == tenantId && x.FinalOutcome == PipFinalOutcome.InProgress)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(departmentId))
                query = query.Where(x => x.Employee.DepartmentId == departmentId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(x =>
                    (x.Employee.FirstName + " " + x.Employee.LastName).Contains(term) ||
                    x.Employee.EmployeeCode.Contains(term));
            }

            var entities = await query.OrderBy(x => x.EndDate).ToListAsync();

            return await AssembleDtoListAsync(entities);
        }

        public async Task<List<PipRecordDto>> GetAllAsync(string tenantId, string? finalOutcome, string? departmentId, string? search, string actingUserId)
        {
            if (!await HasPermissionAsync(actingUserId, Actions.View))
                throw new UnauthorizedAccessException("You are not authorized to view PIP records.");

            var query = _context.PipRecords
                .Include(x => x.Employee).ThenInclude(e => e.Department)
                .Include(x => x.Employee).ThenInclude(e => e.Designation)
                .Where(x => x.TenantId == tenantId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(finalOutcome) && Enum.TryParse<PipFinalOutcome>(finalOutcome, true, out var outcomeEnum))
                query = query.Where(x => x.FinalOutcome == outcomeEnum);

            if (!string.IsNullOrWhiteSpace(departmentId))
                query = query.Where(x => x.Employee.DepartmentId == departmentId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(x =>
                    (x.Employee.FirstName + " " + x.Employee.LastName).Contains(term) ||
                    x.Employee.EmployeeCode.Contains(term));
            }

            var entities = await query.OrderByDescending(x => x.CreatedOn).ToListAsync();

            return await AssembleDtoListAsync(entities);
        }

        #endregion

        #region Maker-Checker Workflow (Final Outcome Resolution)

        // Maker action - proposes the final outcome. Requires Create
        // permission on PIP, mirroring
        // ProbationConfirmationService.CreateAsync's Maker gate (the
        // analogous "maker proposes" step here, since PIP creation itself
        // has no maker-checker gate).
        public async Task<PipRecordDto> ProposeOutcomeAsync(string id, ProposePipOutcomeDto dto, string tenantId, string actingUserId)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            if (!await HasPermissionAsync(actingUserId, Actions.Create))
                throw new UnauthorizedAccessException("You are not authorized to propose a PIP outcome.");

            var record = await _context.PipRecords
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);

            if (record == null)
                throw new Exception("PIP record not found.");

            // No double-propose: a live proposal already exists only when
            // a Maker has previously staged one AND it is still
            // PendingChecker (a freshly-created record has MakerId ==
            // null even though Status defaults to PendingChecker, so the
            // very first proposal is not blocked here; after a Reject,
            // Status flips to Rejected, allowing the Maker to propose
            // again).
            if (record.MakerId != null && record.Status == PipOutcomeStatus.PendingChecker)
                throw new Exception("This PIP record already has an outcome proposal pending checker action.");

            if (record.FinalOutcome != PipFinalOutcome.InProgress)
                throw new Exception("This PIP record's final outcome has already been resolved.");

            if (!Enum.IsDefined(typeof(PipFinalOutcome), dto.FinalOutcome))
                throw new Exception("Invalid final outcome.");

            var proposedOutcome = (PipFinalOutcome)dto.FinalOutcome;

            if (proposedOutcome != PipFinalOutcome.Successful && proposedOutcome != PipFinalOutcome.Unsuccessful)
                throw new Exception("The proposed final outcome must be Successful or Unsuccessful.");

            record.ProposedFinalOutcome = proposedOutcome;

            record.MakerId = actingUserId;
            record.MakerActionOn = DateTime.UtcNow;
            record.MakerRemarks = string.IsNullOrWhiteSpace(dto.MakerRemarks) ? null : dto.MakerRemarks.Trim();

            record.Status = PipOutcomeStatus.PendingChecker;

            record.ModifiedOn = DateTime.UtcNow;
            record.ModifiedBy = actingUserId;

            await _context.SaveChangesAsync();

            return await GetByIdInternalAsync(record.Id, tenantId);
        }

        public async Task<PipRecordDto> ApproveOutcomeAsync(string id, CheckerActionDto dto, string actingUserId, string tenantId)
        {
            var record = await _context.PipRecords
                .Include(x => x.Employee)
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);

            if (record == null)
                throw new Exception("PIP record not found.");

            // ---- THE CORE MAKER-CHECKER INVARIANT ----
            // Checked BEFORE any permission check, with NO override (not
            // even for HR/Admin) - identical to
            // ProbationConfirmationService.EnsureCheckerIsNotMaker.
            EnsureCheckerIsNotMaker(record, actingUserId);

            if (!await HasPermissionAsync(actingUserId, Actions.Approve))
                throw new UnauthorizedAccessException("You are not authorized to approve PIP records.");

            if (record.Status != PipOutcomeStatus.PendingChecker)
                throw new Exception("Only records pending checker action can be approved.");

            if (!record.ProposedFinalOutcome.HasValue)
                throw new Exception("No proposed outcome to approve.");

            record.FinalOutcome = record.ProposedFinalOutcome.Value;
            record.Status = PipOutcomeStatus.Approved;
            record.CheckerId = actingUserId;
            record.CheckerActionOn = DateTime.UtcNow;
            record.CheckerRemarks = string.IsNullOrWhiteSpace(dto?.CheckerRemarks) ? null : dto.CheckerRemarks.Trim();

            record.ModifiedOn = DateTime.UtcNow;
            record.ModifiedBy = actingUserId;

            var employee = record.Employee
                ?? await _context.Employees.FirstOrDefaultAsync(x => x.Id == record.EmployeeId);

            if (employee == null)
                throw new Exception("Employee not found.");

            // Apply the resolved outcome to the Employee record - mirrors
            // ProbationConfirmationService.ApproveAsync's Confirm/
            // Terminate side-effects exactly.
            switch (record.FinalOutcome)
            {
                case PipFinalOutcome.Successful:
                    employee.ConfirmationDate = DateTime.UtcNow.Date;

                    // Only auto-flip EmploymentType FROM Probation
                    // specifically - never assume every confirmation
                    // implies "Permanent" for a non-probationary
                    // EmploymentType.
                    if (employee.EmploymentType == EmploymentType.Probation)
                        employee.EmploymentType = EmploymentType.Permanent;
                    break;

                case PipFinalOutcome.Unsuccessful:
                    employee.RelievingDate = DateTime.UtcNow.Date;
                    break;
            }

            employee.ModifiedOn = DateTime.UtcNow;
            employee.ModifiedBy = actingUserId;

            await _context.SaveChangesAsync();

            return await GetByIdInternalAsync(record.Id, tenantId);
        }

        public async Task<PipRecordDto> RejectOutcomeAsync(string id, CheckerActionDto dto, string actingUserId, string tenantId)
        {
            var record = await _context.PipRecords
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);

            if (record == null)
                throw new Exception("PIP record not found.");

            // ---- THE CORE MAKER-CHECKER INVARIANT ----
            // Same ordering as ApproveOutcomeAsync: identity check first,
            // no override.
            EnsureCheckerIsNotMaker(record, actingUserId);

            if (!await HasPermissionAsync(actingUserId, Actions.Approve))
                throw new UnauthorizedAccessException("You are not authorized to act on PIP records.");

            if (record.Status != PipOutcomeStatus.PendingChecker)
                throw new Exception("Only records pending checker action can be rejected.");

            if (string.IsNullOrWhiteSpace(dto?.CheckerRemarks))
                throw new Exception("Checker remarks are required to reject a PIP outcome proposal.");

            record.Status = PipOutcomeStatus.Rejected;
            record.CheckerId = actingUserId;
            record.CheckerActionOn = DateTime.UtcNow;
            record.CheckerRemarks = dto.CheckerRemarks.Trim();

            // FinalOutcome stays InProgress; clear the staged proposal so
            // the Maker can propose again later (e.g. after further
            // review) via ProposeOutcomeAsync.
            record.ProposedFinalOutcome = null;

            record.ModifiedOn = DateTime.UtcNow;
            record.ModifiedBy = actingUserId;

            await _context.SaveChangesAsync();

            return await GetByIdInternalAsync(record.Id, tenantId);
        }

        // THE CORE MAKER-CHECKER INVARIANT: the acting user's own resolved
        // identity (their User.Id, i.e. actingUserId) must differ from
        // record.MakerId. No override for this - not even HR/Admin can
        // check their own maker action.
        private static void EnsureCheckerIsNotMaker(PipRecord record, string actingUserId)
        {
            if (!string.IsNullOrEmpty(actingUserId) && actingUserId == record.MakerId)
                throw new UnauthorizedAccessException(
                    "The checker must be a different person than the maker - you cannot approve your own submission.");
        }

        #endregion

        #region Authorization Helpers

        // Does the acting user hold, through any Role assigned to them, an
        // allowed RolePermission for the given action on the PIP feature -
        // same join shape as ProbationConfirmationService.HasPermissionAsync,
        // scoped to AppFeatureConstants.PIP.
        private async Task<bool> HasPermissionAsync(string? actingUserId, string action)
        {
            if (string.IsNullOrEmpty(actingUserId))
                return false;

            return await (
                from ur in _context.UserRoles
                join rp in _context.RolePermissions.Where(x => x.IsAllowed) on ur.RoleId equals rp.RoleId
                join p in _context.Permissions.Where(x =>
                        x.FeatureId == AppFeatureConstants.PIP && x.Action == action)
                    on rp.PermissionId equals p.Id
                where ur.UserId == actingUserId
                select p.Id
            ).AnyAsync();
        }

        #endregion

        #region DTO Assembly

        private static PipRecordDto AssembleDto(PipRecord x)
        {
            return new PipRecordDto
            {
                Id = x.Id,

                EmployeeId = x.EmployeeId,
                EmployeeName = x.Employee != null ? $"{x.Employee.FirstName} {x.Employee.LastName}".Trim() : null,
                EmployeeCode = x.Employee?.EmployeeCode,
                DepartmentName = x.Employee?.Department?.Name,
                DesignationName = x.Employee?.Designation?.Name,

                ProbationConfirmationId = x.ProbationConfirmationId,

                StartDate = x.StartDate,
                EndDate = x.EndDate,
                Goals = x.Goals,

                MidReviewDate = x.MidReviewDate,
                MidReviewNotes = x.MidReviewNotes,

                FinalOutcome = (int)x.FinalOutcome,
                FinalOutcomeName = x.FinalOutcome.ToString(),

                MakerId = x.MakerId,
                MakerActionOn = x.MakerActionOn,
                MakerRemarks = x.MakerRemarks,

                Status = (int)x.Status,
                StatusName = x.Status.ToString(),

                CheckerId = x.CheckerId,
                CheckerActionOn = x.CheckerActionOn,
                CheckerRemarks = x.CheckerRemarks,

                TenantId = x.TenantId,
                CreatedOn = x.CreatedOn,
                CreatedBy = x.CreatedBy
            };
        }

        // MakerId/CheckerId store the acting User.Id - resolve a display
        // name via the linked Employee if one exists, else fall back to
        // the account's Username. Mirrors
        // ProbationConfirmationService.BuildUserNameMapAsync.
        private async Task<Dictionary<string, string>> BuildUserNameMapAsync(IEnumerable<string?> userIds)
        {
            var ids = userIds
                .Where(x => !string.IsNullOrEmpty(x))
                .Select(x => x!)
                .Distinct()
                .ToList();

            if (ids.Count == 0)
                return new Dictionary<string, string>();

            var users = await _context.Users
                .Include(u => u.Employee)
                .Where(u => ids.Contains(u.Id))
                .ToListAsync();

            return users.ToDictionary(
                u => u.Id,
                u => u.Employee != null ? $"{u.Employee.FirstName} {u.Employee.LastName}".Trim() : u.Username);
        }

        private async Task<PipRecordDto> AssembleDtoAsync(PipRecord x)
        {
            var dto = AssembleDto(x);

            var nameMap = await BuildUserNameMapAsync(new[] { x.MakerId, x.CheckerId });

            if (!string.IsNullOrEmpty(x.MakerId))
                dto.MakerName = nameMap.TryGetValue(x.MakerId, out var makerName) ? makerName : null;

            if (!string.IsNullOrEmpty(x.CheckerId))
                dto.CheckerName = nameMap.TryGetValue(x.CheckerId, out var checkerName) ? checkerName : null;

            return dto;
        }

        private async Task<List<PipRecordDto>> AssembleDtoListAsync(List<PipRecord> entities)
        {
            var userIds = entities.SelectMany(x => new[] { x.MakerId, x.CheckerId });
            var nameMap = await BuildUserNameMapAsync(userIds);

            var list = new List<PipRecordDto>();

            foreach (var x in entities)
            {
                var dto = AssembleDto(x);

                if (!string.IsNullOrEmpty(x.MakerId) && nameMap.TryGetValue(x.MakerId, out var makerName))
                    dto.MakerName = makerName;

                if (!string.IsNullOrEmpty(x.CheckerId) && nameMap.TryGetValue(x.CheckerId, out var checkerName))
                    dto.CheckerName = checkerName;

                list.Add(dto);
            }

            return list;
        }

        #endregion
    }
}
