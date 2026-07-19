using Application.DTOs.Leaves;
using Application.Interfaces.Leaves;
using Domain.Entities;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Leaves
{
    public class ApprovalDelegationService : IApprovalDelegationService
    {
        private readonly ApplicationDbContext _context;

        public ApprovalDelegationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ApprovalDelegationDto> CreateAsync(CreateApprovalDelegationRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.DelegatorEmployeeId) || string.IsNullOrWhiteSpace(request.DelegateEmployeeId))
                throw new InvalidOperationException("Delegator and Delegate are both required.");

            // A manager can't hand their own approval authority to
            // themselves - that's just a no-op dressed up as a delegation.
            if (string.Equals(request.DelegatorEmployeeId, request.DelegateEmployeeId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("You cannot delegate your approval authority to yourself.");

            var startDate = request.StartDate.Date;
            var endDate = request.EndDate.Date;

            if (endDate < startDate)
                throw new InvalidOperationException("End date cannot be before start date.");

            var delegateEmployee = await _context.Employees.FirstOrDefaultAsync(x => x.Id == request.DelegateEmployeeId);

            if (delegateEmployee == null)
                throw new InvalidOperationException("The selected delegate employee was not found.");

            // A manager shouldn't have two conflicting active proxies
            // covering overlapping dates - whichever leave request comes in
            // during the overlap would otherwise have two different
            // "authorized delegate" answers depending on which row happens
            // to be picked.
            bool overlaps = await _context.ApprovalDelegations.AnyAsync(x =>
                x.DelegatorEmployeeId == request.DelegatorEmployeeId &&
                x.IsActive &&
                startDate <= x.EndDate.Date &&
                endDate >= x.StartDate.Date);

            if (overlaps)
                throw new InvalidOperationException(
                    "You already have an active delegation that overlaps these dates. Revoke it first or choose different dates.");

            var entity = new ApprovalDelegation
            {
                Id = IDManager.GetNewId(new ApprovalDelegation()),
                TenantId = request.TenantId,
                DelegatorEmployeeId = request.DelegatorEmployeeId,
                DelegateEmployeeId = request.DelegateEmployeeId,
                StartDate = startDate,
                EndDate = endDate,
                Reason = request.Reason,
                IsActive = true,
                CreatedOn = DateTime.UtcNow,
                CreatedBy = request.CreatedBy
            };

            _context.ApprovalDelegations.Add(entity);

            await _context.SaveChangesAsync();

            return await MapToDtoAsync(entity);
        }

        public async Task<bool> RevokeAsync(string id, string? revokedBy)
        {
            var entity = await _context.ApprovalDelegations.FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null || !entity.IsActive)
                return false;

            entity.IsActive = false;
            entity.ModifiedOn = DateTime.UtcNow;
            entity.ModifiedBy = revokedBy;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<ApprovalDelegationDto>> GetForEmployeeAsync(string employeeId)
        {
            if (string.IsNullOrEmpty(employeeId))
                return new List<ApprovalDelegationDto>();

            var entities = await _context.ApprovalDelegations
                .Where(x => x.DelegatorEmployeeId == employeeId)
                .OrderByDescending(x => x.CreatedOn)
                .ToListAsync();

            if (entities.Count == 0)
                return new List<ApprovalDelegationDto>();

            var employeeIds = entities
                .SelectMany(x => new[] { x.DelegatorEmployeeId, x.DelegateEmployeeId })
                .Distinct()
                .ToList();

            var names = await _context.Employees
                .Where(x => employeeIds.Contains(x.Id))
                .Select(x => new { x.Id, Name = (x.FirstName + " " + x.LastName).Trim() })
                .ToDictionaryAsync(x => x.Id, x => x.Name);

            var today = DateTime.UtcNow.Date;

            return entities.Select(x => new ApprovalDelegationDto
            {
                Id = x.Id,
                DelegatorEmployeeId = x.DelegatorEmployeeId,
                DelegatorEmployeeName = names.TryGetValue(x.DelegatorEmployeeId, out var dn) ? dn : null,
                DelegateEmployeeId = x.DelegateEmployeeId,
                DelegateEmployeeName = names.TryGetValue(x.DelegateEmployeeId, out var gn) ? gn : null,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                Reason = x.Reason,
                IsActive = x.IsActive,
                IsCurrentlyInEffect = x.IsActive && x.StartDate.Date <= today && x.EndDate.Date >= today,
                CreatedOn = x.CreatedOn,
                CreatedBy = x.CreatedBy
            }).ToList();
        }

        public async Task<string?> GetActiveDelegateForAsync(string? delegatorEmployeeId, DateTime onDate)
        {
            if (string.IsNullOrEmpty(delegatorEmployeeId))
                return null;

            var date = onDate.Date;

            return await _context.ApprovalDelegations
                .Where(x =>
                    x.DelegatorEmployeeId == delegatorEmployeeId &&
                    x.IsActive &&
                    x.StartDate.Date <= date &&
                    x.EndDate.Date >= date)
                .OrderByDescending(x => x.CreatedOn)
                .Select(x => x.DelegateEmployeeId)
                .FirstOrDefaultAsync();
        }

        private async Task<ApprovalDelegationDto> MapToDtoAsync(ApprovalDelegation entity)
        {
            var names = await _context.Employees
                .Where(x => x.Id == entity.DelegatorEmployeeId || x.Id == entity.DelegateEmployeeId)
                .Select(x => new { x.Id, Name = (x.FirstName + " " + x.LastName).Trim() })
                .ToDictionaryAsync(x => x.Id, x => x.Name);

            var today = DateTime.UtcNow.Date;

            return new ApprovalDelegationDto
            {
                Id = entity.Id,
                DelegatorEmployeeId = entity.DelegatorEmployeeId,
                DelegatorEmployeeName = names.TryGetValue(entity.DelegatorEmployeeId, out var dn) ? dn : null,
                DelegateEmployeeId = entity.DelegateEmployeeId,
                DelegateEmployeeName = names.TryGetValue(entity.DelegateEmployeeId, out var gn) ? gn : null,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                Reason = entity.Reason,
                IsActive = entity.IsActive,
                IsCurrentlyInEffect = entity.IsActive && entity.StartDate.Date <= today && entity.EndDate.Date >= today,
                CreatedOn = entity.CreatedOn,
                CreatedBy = entity.CreatedBy
            };
        }
    }
}
