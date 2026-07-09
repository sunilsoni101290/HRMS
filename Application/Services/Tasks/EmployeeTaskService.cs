using Application.DTOs.Tasks;
using Application.Interfaces.Tasks;
using Domain.Entities;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Tasks
{
    public class EmployeeTaskService : IEmployeeTaskService
    {
        private readonly ApplicationDbContext _context;

        public EmployeeTaskService(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Get All

        public async Task<List<EmployeeTaskListDto>> GetAllAsync()
        {
            try
            {
            return await _context.EmployeeTasks
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderByDescending(x => x.CreatedOn)
                .Select(x => new EmployeeTaskListDto
                {
                    Id = x.Id,
                    Title = x.Title,
                    EmployeeId = x.EmployeeId,
                    EmployeeName = x.Employee != null ? x.Employee.FirstName + " " + x.Employee.LastName : "",
                    DueDate = x.DueDate,
                    Status = x.Status,
                    Priority = x.Priority,
                    Remarks = x.Remarks
                })
                .ToListAsync();
            }
            catch (Exception)
            {
                return new List<EmployeeTaskListDto>();
            }
        }

        #endregion

        #region Get By Id

        public async Task<EmployeeTaskDto> GetByIdAsync(string id)
        {
            try
            {
            var entity = await _context.EmployeeTasks
                .Include(x => x.Employee)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null)
                return null;

            return new EmployeeTaskDto
            {
                Id = entity.Id,
                Title = entity.Title,
                Description = entity.Description,
                EmployeeId = entity.EmployeeId,
                EmployeeName = entity.Employee != null ? entity.Employee.FirstName + " " + entity.Employee.LastName : "",
                DueDate = entity.DueDate,
                Status = entity.Status,
                Priority = entity.Priority,
                AssignedBy = entity.AssignedBy,
                CompletedDate = entity.CompletedDate,
                Remarks = entity.Remarks,
                CompanyId = entity.CompanyId,
                BranchId = entity.BranchId,
                TenantId = entity.TenantId,
                CreatedBy = entity.CreatedBy,
                ModifiedBy = entity.ModifiedBy,
                ModifiedOn = entity.ModifiedOn
            };
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion

        #region Create

        public async Task<string> CreateAsync(EmployeeTaskDto dto)
        {
            try
            {
                var entity = new EmployeeTask
                {
                    Id = IDManager.GetNewId(new EmployeeTask()),
                    Title = dto.Title,
                    Description = dto.Description,
                    EmployeeId = dto.EmployeeId,
                    DueDate = dto.DueDate,
                    Status = string.IsNullOrEmpty(dto.Status) ? "Pending" : dto.Status,
                    Priority = string.IsNullOrEmpty(dto.Priority) ? "Medium" : dto.Priority,
                    AssignedBy = dto.AssignedBy ?? dto.CreatedBy,
                    CompanyId = dto.CompanyId,
                    BranchId = dto.BranchId,
                    TenantId = dto.TenantId,
                    CreatedBy = dto.CreatedBy
                };

            await _context.EmployeeTasks.AddAsync(entity);
            await _context.SaveChangesAsync();

            return entity.Id;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        #endregion

        #region Update

        public async Task<string> UpdateAsync(string id, EmployeeTaskDto dto)
        {
            try
            {
            var entity = await _context.EmployeeTasks
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null)
                return "Task Not Found";

            entity.Title = dto.Title;
            entity.Description = dto.Description;
            entity.EmployeeId = dto.EmployeeId;
            entity.DueDate = dto.DueDate;
            entity.Status = dto.Status;
            entity.Priority = dto.Priority;
            entity.Remarks = dto.Remarks;
            entity.CompanyId = dto.CompanyId;
            entity.BranchId = dto.BranchId;
            entity.TenantId = dto.TenantId;
            entity.ModifiedBy = dto.ModifiedBy;
            entity.ModifiedOn = dto.ModifiedOn ?? DateTime.UtcNow;

            if (dto.Status == "Completed" && entity.CompletedDate == null)
                entity.CompletedDate = DateTime.UtcNow;

            _context.EmployeeTasks.Update(entity);
            await _context.SaveChangesAsync();

            return entity.Id;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        #endregion

        #region Change Status

        public async Task<string> ChangeStatusAsync(string id, string status, string userId, string? remarks = null)
        {
            try
            {
            var entity = await _context.EmployeeTasks
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null)
                return "Task Not Found";

            entity.Status = status;
            entity.ModifiedBy = userId;
            entity.ModifiedOn = DateTime.UtcNow;

            if (remarks != null)
                entity.Remarks = remarks;

            if (status == "Completed")
                entity.CompletedDate = DateTime.UtcNow;

            _context.EmployeeTasks.Update(entity);
            await _context.SaveChangesAsync();

            return entity.Id;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        #endregion

        #region Delete (Soft)

        public async Task<bool> DeleteAsync(string id)
        {
            try
            {
            var entity = await _context.EmployeeTasks
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null)
                return false;

            entity.IsDeleted = true;
            entity.ModifiedOn = DateTime.UtcNow;

            _context.EmployeeTasks.Update(entity);
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
