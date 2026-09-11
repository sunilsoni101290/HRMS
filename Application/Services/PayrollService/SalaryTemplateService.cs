using Application.DTOs.Payroll;
using Application.Interfaces.Payroll;
using Domain.Entities;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static Domain.Enums.EnumExtensions;

namespace Application.Services.PayrollService
{
    // Reusable salary "master" CRUD + bulk employee assignment. See
    // Domain/Entities/SalaryTemplate.cs for how this relates to
    // SalaryStructure (the actual per-employee assignment record, unchanged
    // by this class except for the new nullable SourceTemplateId link it
    // stamps when AssignAsync creates one).
    public class SalaryTemplateService : ISalaryTemplateService
    {
        private readonly ApplicationDbContext _context;

        public SalaryTemplateService(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Get All

        public async Task<List<SalaryTemplateListDto>> GetAllAsync()
        {
            try
            {
                var templates = await _context.SalaryTemplates
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted)
                    .OrderBy(x => x.Name)
                    .Select(x => new
                    {
                        x.Id,
                        x.Name,
                        x.Description,
                        x.EffectiveFrom,
                        x.IsActive,
                        Lines = x.Details.Select(d => new
                        {
                            d.Amount,
                            Type = (int)d.SalaryComponent.ComponentType
                        }).ToList()
                    })
                    .ToListAsync();

                // Distinct EMPLOYEE count (not row count) sourced from each
                // template - a template applied to the same employee twice
                // (e.g. a later raise using the same template) counts that
                // employee once here.
                var employeeCounts = await _context.SalaryStructures
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted && x.SourceTemplateId != null)
                    .Select(x => new { x.SourceTemplateId, x.EmployeeId })
                    .Distinct()
                    .GroupBy(x => x.SourceTemplateId)
                    .Select(g => new { TemplateId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.TemplateId!, x => x.Count);

                return templates.Select(x => new SalaryTemplateListDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    EffectiveFrom = x.EffectiveFrom,
                    IsActive = x.IsActive,
                    ComponentCount = x.Lines.Count,
                    GrossSalary = x.Lines.Where(l => l.Type == (int)SalaryComponentType.Earning).Sum(l => l.Amount),
                    EmployeeCount = employeeCounts.TryGetValue(x.Id, out var c) ? c : 0
                }).ToList();
            }
            catch (Exception)
            {
                return new List<SalaryTemplateListDto>();
            }
        }

        #endregion

        #region Get By Id

        public async Task<SalaryTemplateDto> GetByIdAsync(string id)
        {
            try
            {
                var entity = await _context.SalaryTemplates
                    .AsNoTracking()
                    .Include(x => x.Details)
                        .ThenInclude(d => d.SalaryComponent)
                    .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

                if (entity == null)
                    return null;

                var dto = new SalaryTemplateDto
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    Description = entity.Description,
                    EffectiveFrom = entity.EffectiveFrom,
                    IsActive = entity.IsActive,
                    TenantId = entity.TenantId,
                    CreatedBy = entity.CreatedBy,
                    ModifiedBy = entity.ModifiedBy,
                    ModifiedOn = entity.ModifiedOn,
                    Details = entity.Details.Select(d => new SalaryTemplateLineDto
                    {
                        Id = d.Id,
                        SalaryComponentId = d.SalaryComponentId,
                        SalaryComponentName = d.SalaryComponent != null ? d.SalaryComponent.Name : "",
                        ComponentType = d.SalaryComponent != null ? (int)d.SalaryComponent.ComponentType : 0,
                        Amount = d.Amount,
                        CalculationType = d.CalculationType
                    }).ToList()
                };

                dto.TotalEarnings = dto.Details.Where(l => l.ComponentType == (int)SalaryComponentType.Earning).Sum(l => l.Amount);
                dto.TotalDeductions = dto.Details.Where(l => l.ComponentType == (int)SalaryComponentType.Deduction).Sum(l => l.Amount);
                dto.GrossSalary = dto.TotalEarnings;

                dto.EmployeeCount = await _context.SalaryStructures
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted && x.SourceTemplateId == id)
                    .Select(x => x.EmployeeId)
                    .Distinct()
                    .CountAsync();

                return dto;
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion

        #region Validation helpers

        private async Task<string?> ValidateAsync(SalaryTemplateDto dto, string? excludeId)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return "Structure Name is required.";

            var nameExists = await _context.SalaryTemplates
                .AnyAsync(x => !x.IsDeleted
                    && x.Id != excludeId
                    && x.Name.ToLower() == dto.Name.Trim().ToLower());

            if (nameExists)
                return $"A salary structure named '{dto.Name.Trim()}' already exists. Please choose a different name.";

            var lines = (dto.Details ?? new List<SalaryTemplateLineDto>())
                .Where(l => !string.IsNullOrEmpty(l.SalaryComponentId))
                .ToList();

            if (!lines.Any())
                return "Please add at least one salary component.";

            if (lines.Any(l => l.Amount < 0))
                return "Amount cannot be negative.";

            var duplicateComponents = lines
                .GroupBy(l => l.SalaryComponentId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateComponents.Any())
                return "Each salary component may only appear once in a structure.";

            return null;
        }

        #endregion

        #region Create

        public async Task<string> CreateAsync(SalaryTemplateDto dto)
        {
            var error = await ValidateAsync(dto, excludeId: null);
            if (error != null)
                return $"ERROR:{error}";

            try
            {
                var template = new SalaryTemplate
                {
                    Id = IDManager.GetNewId(new SalaryTemplate()),
                    Name = dto.Name.Trim(),
                    Description = dto.Description,
                    EffectiveFrom = dto.EffectiveFrom,
                    IsActive = dto.IsActive,
                    TenantId = dto.TenantId,
                    CreatedBy = dto.CreatedBy
                };

                await _context.SalaryTemplates.AddAsync(template);

                foreach (var line in dto.Details.Where(l => !string.IsNullOrEmpty(l.SalaryComponentId)))
                {
                    await _context.SalaryTemplateDetails.AddAsync(new SalaryTemplateDetail
                    {
                        Id = IDManager.GetNewId(new SalaryTemplateDetail()),
                        SalaryTemplateId = template.Id,
                        SalaryComponentId = line.SalaryComponentId,
                        Amount = line.Amount,
                        CalculationType = line.CalculationType <= 0 ? 1 : line.CalculationType,
                        TenantId = dto.TenantId,
                        CreatedBy = dto.CreatedBy
                    });
                }

                await _context.SaveChangesAsync();
                return template.Id;
            }
            catch (Exception ex)
            {
                return $"ERROR:{ex.Message}";
            }
        }

        #endregion

        #region Update

        public async Task<string> UpdateAsync(string id, SalaryTemplateDto dto)
        {
            var error = await ValidateAsync(dto, excludeId: id);
            if (error != null)
                return $"ERROR:{error}";

            try
            {
                var template = await _context.SalaryTemplates
                    .Include(x => x.Details)
                    .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

                if (template == null)
                    return "ERROR:Salary Structure not found.";

                template.Name = dto.Name.Trim();
                template.Description = dto.Description;
                template.EffectiveFrom = dto.EffectiveFrom;
                template.IsActive = dto.IsActive;
                template.ModifiedBy = dto.ModifiedBy;
                template.ModifiedOn = dto.ModifiedOn ?? DateTime.UtcNow;

                // Replace detail lines - this only changes the TEMPLATE.
                // Employees already applied from it keep their existing
                // SalaryStructure/SalaryDetail rows untouched (see
                // SalaryStructure.SourceTemplateId's remarks) - a template
                // edit never retroactively changes anyone's pay.
                _context.SalaryTemplateDetails.RemoveRange(template.Details);

                foreach (var line in dto.Details.Where(l => !string.IsNullOrEmpty(l.SalaryComponentId)))
                {
                    await _context.SalaryTemplateDetails.AddAsync(new SalaryTemplateDetail
                    {
                        Id = IDManager.GetNewId(new SalaryTemplateDetail()),
                        SalaryTemplateId = template.Id,
                        SalaryComponentId = line.SalaryComponentId,
                        Amount = line.Amount,
                        CalculationType = line.CalculationType <= 0 ? 1 : line.CalculationType,
                        TenantId = dto.TenantId,
                        CreatedBy = dto.ModifiedBy ?? dto.CreatedBy
                    });
                }

                await _context.SaveChangesAsync();
                return template.Id;
            }
            catch (Exception ex)
            {
                return $"ERROR:{ex.Message}";
            }
        }

        #endregion

        #region Delete (soft - blocked while any employee is currently applied from it)

        // Returns "" on success, or a user-facing message when the delete
        // is blocked/failed. A template that has ever been applied to an
        // employee is never hard/soft-deleted while that link still exists
        // - the caller is told to Deactivate instead, which stops it being
        // offered on the Assign screen going forward without touching any
        // existing SalaryStructure row or its SourceTemplateId (data
        // integrity - see requirement to never orphan/misattribute
        // existing payroll history).
        public async Task<string> DeleteAsync(string id)
        {
            try
            {
                var template = await _context.SalaryTemplates
                    .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

                if (template == null)
                    return "Salary Structure not found.";

                var appliedCount = await _context.SalaryStructures
                    .Where(x => !x.IsDeleted && x.SourceTemplateId == id)
                    .Select(x => x.EmployeeId)
                    .Distinct()
                    .CountAsync();

                if (appliedCount > 0)
                    return $"This salary structure is applied to {appliedCount} employee(s) and cannot be deleted. Deactivate it instead so it can no longer be applied to new employees.";

                template.IsDeleted = true;
                template.ModifiedOn = DateTime.UtcNow;

                _context.SalaryTemplates.Update(template);
                await _context.SaveChangesAsync();

                return "";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        #endregion

        #region Assign (bulk apply to employees)

        // Applies the template to every requested employee as of
        // EffectiveFrom, each becoming its own new SalaryStructure +
        // SalaryDetails row (never edits an existing assignment in place -
        // that would silently rewrite history/payroll already generated
        // from it). Per-employee outcomes:
        //  - Skipped: employee already has an assignment dated exactly
        //    EffectiveFrom (the "overlapping effective salary assignment"
        //    rule) - creating a second one for the same date would make
        //    Payroll generation's "latest EffectiveFrom <= period" pick
        //    ambiguous between two same-dated rows, so it's refused with a
        //    clear reason rather than silently duplicating.
        //  - Applied: a brand-new dated assignment is created, copying the
        //    template's current component/amount lines. A different
        //    EffectiveFrom than the employee's existing assignment(s) is
        //    always allowed by design - that's exactly how a raise/change
        //    effective from a future date is meant to work, and existing
        //    Payroll rows are never touched.
        // All the actual inserts happen inside one transaction/SaveChanges
        // call, so a mid-batch failure (e.g. a DB error) rolls back every
        // insert from this call rather than leaving a partially-applied
        // batch; employees skipped for the business reason above are never
        // part of that write to begin with.
        public async Task<SalaryTemplateAssignResultDto> AssignAsync(SalaryTemplateAssignDto dto)
        {
            var result = new SalaryTemplateAssignResultDto();

            var template = await _context.SalaryTemplates
                .Include(x => x.Details)
                .FirstOrDefaultAsync(x => x.Id == dto.SalaryTemplateId && !x.IsDeleted);

            if (template == null)
            {
                result.Results.Add(new SalaryTemplateAssignResultLineDto
                {
                    Success = false,
                    Message = "Salary Structure not found."
                });
                return result;
            }

            if (!template.IsActive)
            {
                result.Results.Add(new SalaryTemplateAssignResultLineDto
                {
                    Success = false,
                    Message = $"'{template.Name}' is Inactive and cannot be applied to new employees. Activate it first."
                });
                return result;
            }

            if (template.Details == null || !template.Details.Any())
            {
                result.Results.Add(new SalaryTemplateAssignResultLineDto
                {
                    Success = false,
                    Message = $"'{template.Name}' has no salary components and cannot be applied."
                });
                return result;
            }

            var employeeIds = (dto.EmployeeIds ?? new List<string>())
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct()
                .ToList();

            result.TotalSelected = employeeIds.Count;

            if (!employeeIds.Any())
                return result;

            var effectiveDate = dto.EffectiveFrom.Date;

            var employees = await _context.Employees
                .AsNoTracking()
                .Where(x => employeeIds.Contains(x.Id) && !x.IsDeleted)
                .Select(x => new { x.Id, Name = x.FirstName + " " + x.LastName })
                .ToDictionaryAsync(x => x.Id, x => x.Name);

            // Employees who already have an assignment dated exactly
            // effectiveDate - looked up once for the whole batch rather
            // than per-employee, since that's just as correct and far
            // cheaper than N round-trips.
            var alreadyAssignedIds = await _context.SalaryStructures
                .Where(x => !x.IsDeleted
                    && employeeIds.Contains(x.EmployeeId)
                    && x.EffectiveFrom.Date == effectiveDate)
                .Select(x => x.EmployeeId)
                .Distinct()
                .ToListAsync();

            var alreadyAssignedSet = alreadyAssignedIds.ToHashSet();

            // Fix: EnableRetryOnFailure (API/Program.cs) means EF Core
            // requires every manually-started transaction to run inside
            // the execution strategy's own retry wrapper - a bare
            // BeginTransactionAsync() (as this used to be) throws "The
            // configured execution strategy 'SqlServerRetryingExecutionStrategy'
            // does not support user-initiated transactions" the moment this
            // runs. Same fix/pattern already used in SequenceService.GetNextERPIdAsync.
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                // Reset per-attempt state: if a transient failure causes the
                // strategy to retry this whole delegate, a previous (failed)
                // attempt may already have appended entries below - without
                // this reset a retried attempt would duplicate them.
                result.Results.Clear();
                result.AppliedCount = 0;
                result.SkippedCount = 0;

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                foreach (var employeeId in employeeIds)
                {
                    var employeeName = employees.TryGetValue(employeeId, out var n) ? n : employeeId;

                    if (!employees.ContainsKey(employeeId))
                    {
                        result.Results.Add(new SalaryTemplateAssignResultLineDto
                        {
                            EmployeeId = employeeId,
                            EmployeeName = employeeName,
                            Success = false,
                            Message = "Employee not found - skipped."
                        });
                        result.SkippedCount++;
                        continue;
                    }

                    if (alreadyAssignedSet.Contains(employeeId))
                    {
                        result.Results.Add(new SalaryTemplateAssignResultLineDto
                        {
                            EmployeeId = employeeId,
                            EmployeeName = employeeName,
                            Success = false,
                            Message = $"Already has a salary assignment effective {effectiveDate:dd MMM yyyy} - skipped."
                        });
                        result.SkippedCount++;
                        continue;
                    }

                    var structure = new SalaryStructure
                    {
                        Id = IDManager.GetNewId(new SalaryStructure()),
                        EmployeeId = employeeId,
                        EffectiveFrom = effectiveDate,
                        SourceTemplateId = template.Id,
                        TenantId = dto.TenantId,
                        CreatedBy = dto.CreatedBy
                    };

                    await _context.SalaryStructures.AddAsync(structure);

                    foreach (var line in template.Details)
                    {
                        await _context.SalaryDetails.AddAsync(new SalaryDetail
                        {
                            Id = IDManager.GetNewId(new SalaryDetail()),
                            SalaryStructureId = structure.Id,
                            SalaryComponentId = line.SalaryComponentId,
                            Amount = line.Amount,
                            TenantId = dto.TenantId,
                            CreatedBy = dto.CreatedBy
                        });
                    }

                    result.Results.Add(new SalaryTemplateAssignResultLineDto
                    {
                        EmployeeId = employeeId,
                        EmployeeName = employeeName,
                        Success = true,
                        Message = $"Applied '{template.Name}' effective {effectiveDate:dd MMM yyyy}."
                    });
                    result.AppliedCount++;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                result.Results.Clear();
                result.AppliedCount = 0;
                result.SkippedCount = 0;
                result.Results.Add(new SalaryTemplateAssignResultLineDto
                {
                    Success = false,
                    Message = $"Nothing was applied - an error occurred and the whole batch was rolled back: {ex.Message}"
                });
            }
            });

            return result;
        }

        #endregion

        #region Toggle Active

        public async Task<bool> ToggleActiveAsync(string id)
        {
            try
            {
                var template = await _context.SalaryTemplates
                    .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

                if (template == null)
                    return false;

                template.IsActive = !template.IsActive;
                template.ModifiedOn = DateTime.UtcNow;

                _context.SalaryTemplates.Update(template);
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
