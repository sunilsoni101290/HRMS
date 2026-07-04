using Application.DTOs.Recruitment;
using Application.Interfaces.Recruitment;
using Domain.Entities;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static Domain.Enums.EnumExtensions;

namespace Application.Services.Recruitment
{
    public class JobOpeningService : IJobOpeningService
    {
        private readonly ApplicationDbContext _context;

        public JobOpeningService(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Get All

        public async Task<List<JobOpeningListDto>> GetAllAsync()
        {
            try
            {
            var list = await _context.JobOpenings
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderByDescending(x => x.PostedDate)
                .Select(x => new JobOpeningListDto
                {
                    Id = x.Id,
                    Title = x.Title,
                    DepartmentName = x.Department != null ? x.Department.Name : "",
                    DesignationName = x.Designation != null ? x.Designation.Name : "",
                    VacancyCount = x.VacancyCount,
                    Status = (int)x.Status,
                    PostedDate = x.PostedDate,
                    ClosingDate = x.ClosingDate,
                    ApplicationCount = _context.CandidateApplications
                        .Count(a => a.JobOpeningId == x.Id && !a.IsDeleted)
                })
                .ToListAsync();

            foreach (var item in list)
                item.StatusText = ((JobStatus)item.Status).ToString();

            return list;
            }
            catch (Exception)
            {
                return new List<JobOpeningListDto>();
            }
        }

        #endregion

        #region Get By Id

        public async Task<JobOpeningDto> GetByIdAsync(string id)
        {
            try
            {
            var entity = await _context.JobOpenings
                .Include(x => x.Department)
                .Include(x => x.Designation)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null)
                return null;

            return new JobOpeningDto
            {
                Id = entity.Id,
                Title = entity.Title,
                DepartmentId = entity.DepartmentId,
                DepartmentName = entity.Department != null ? entity.Department.Name : "",
                DesignationId = entity.DesignationId,
                DesignationName = entity.Designation != null ? entity.Designation.Name : "",
                VacancyCount = entity.VacancyCount,
                MinSalary = entity.MinSalary,
                MaxSalary = entity.MaxSalary,
                JobDescription = entity.JobDescription,
                RequiredSkills = entity.RequiredSkills,
                Status = (int)entity.Status,
                StatusText = entity.Status.ToString(),
                PostedDate = entity.PostedDate,
                ClosingDate = entity.ClosingDate,
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

        public async Task<string> CreateAsync(JobOpeningDto dto)
        {
            try
            {
            var entity = new JobOpening
            {
                Id = IDManager.GetNewId(new JobOpening()),
                Title = dto.Title,
                DepartmentId = dto.DepartmentId,
                DesignationId = dto.DesignationId,
                VacancyCount = dto.VacancyCount,
                MinSalary = dto.MinSalary,
                MaxSalary = dto.MaxSalary,
                JobDescription = dto.JobDescription,
                RequiredSkills = dto.RequiredSkills,
                Status = (JobStatus)dto.Status,
                PostedDate = dto.PostedDate,
                ClosingDate = dto.ClosingDate,
                TenantId = dto.TenantId,
                CreatedBy = dto.CreatedBy
            };

            await _context.JobOpenings.AddAsync(entity);
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

        public async Task<string> UpdateAsync(string id, JobOpeningDto dto)
        {
            try
            {
            var entity = await _context.JobOpenings
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null)
                return "Job Opening Not Found";

            entity.Title = dto.Title;
            entity.DepartmentId = dto.DepartmentId;
            entity.DesignationId = dto.DesignationId;
            entity.VacancyCount = dto.VacancyCount;
            entity.MinSalary = dto.MinSalary;
            entity.MaxSalary = dto.MaxSalary;
            entity.JobDescription = dto.JobDescription;
            entity.RequiredSkills = dto.RequiredSkills;
            entity.Status = (JobStatus)dto.Status;
            entity.PostedDate = dto.PostedDate;
            entity.ClosingDate = dto.ClosingDate;
            entity.TenantId = dto.TenantId;
            entity.ModifiedBy = dto.ModifiedBy;
            entity.ModifiedOn = dto.ModifiedOn ?? DateTime.UtcNow;

            _context.JobOpenings.Update(entity);
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
            var entity = await _context.JobOpenings
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null)
                return false;

            entity.IsDeleted = true;
            entity.ModifiedOn = DateTime.UtcNow;

            _context.JobOpenings.Update(entity);
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
