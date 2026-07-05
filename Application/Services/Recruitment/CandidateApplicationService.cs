using Application.DTOs.Recruitment;
using Application.Interfaces.Recruitment;
using Domain.Entities;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static Domain.Enums.EnumExtensions;

namespace Application.Services.Recruitment
{
    public class CandidateApplicationService : ICandidateApplicationService
    {
        private readonly ApplicationDbContext _context;

        public CandidateApplicationService(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Get All

        public async Task<List<CandidateApplicationListDto>> GetAllAsync()
        {
            try
            {
            var list = await Query().ToListAsync();
            ApplyText(list);
            return list;
            }
            catch (Exception)
            {
                return new List<CandidateApplicationListDto>();
            }
        }

        private IQueryable<CandidateApplicationListDto> Query()
        {
            return _context.CandidateApplications
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderByDescending(x => x.AppliedDate)
                .Select(x => new CandidateApplicationListDto
                {
                    Id = x.Id,
                    CandidateId = x.CandidateId,
                    CandidateName = x.Candidate != null ? x.Candidate.Name : "",
                    JobOpeningId = x.JobOpeningId,
                    JobTitle = x.JobOpening != null ? x.JobOpening.Title : "",
                    AppliedDate = x.AppliedDate,
                    Status = (int)x.Status,
                    Remarks = x.Remarks,
                    InterviewCount = _context.InterviewSchedules
                        .Count(i => i.CandidateApplicationId == x.Id && !i.IsDeleted)
                });
        }

        private static void ApplyText(List<CandidateApplicationListDto> list)
        {
            foreach (var item in list)
                item.StatusText = ((CandidateStatus)item.Status).ToString();
        }

        #endregion

        #region Get By Id

        public async Task<CandidateApplicationListDto> GetByIdAsync(string id)
        {
            try
            {
            var dto = await Query().FirstOrDefaultAsync(x => x.Id == id);
            if (dto != null)
                dto.StatusText = ((CandidateStatus)dto.Status).ToString();
            return dto;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<CandidateApplicationDto> GetForEditAsync(string id)
        {
            try
            {
            var entity = await _context.CandidateApplications
                .Include(x => x.Candidate)
                .Include(x => x.JobOpening)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null)
                return null;

            return new CandidateApplicationDto
            {
                Id = entity.Id,
                CandidateId = entity.CandidateId,
                CandidateName = entity.Candidate != null ? entity.Candidate.Name : "",
                JobOpeningId = entity.JobOpeningId,
                JobTitle = entity.JobOpening != null ? entity.JobOpening.Title : "",
                AppliedDate = entity.AppliedDate,
                Status = (int)entity.Status,
                StatusText = entity.Status.ToString(),
                Remarks = entity.Remarks,
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

        public async Task<string> CreateAsync(CandidateApplicationDto dto)
        {
            try
            {
            var exists = await _context.CandidateApplications.AnyAsync(x =>
                x.CandidateId == dto.CandidateId &&
                x.JobOpeningId == dto.JobOpeningId &&
                !x.IsDeleted);

            if (exists)
                return "Candidate has already applied to this job.";

            var entity = new CandidateApplication
            {
                Id = IDManager.GetNewId(new CandidateApplication()),
                CandidateId = dto.CandidateId,
                JobOpeningId = dto.JobOpeningId,
                AppliedDate = dto.AppliedDate,
                Status = (CandidateStatus)dto.Status,
                Remarks = dto.Remarks,
                TenantId = dto.TenantId,
                CreatedBy = dto.CreatedBy
            };

            await _context.CandidateApplications.AddAsync(entity);
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

        public async Task<string> UpdateAsync(string id, CandidateApplicationDto dto)
        {
            try
            {
            var entity = await _context.CandidateApplications
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null)
                return "Application Not Found";

            entity.CandidateId = dto.CandidateId;
            entity.JobOpeningId = dto.JobOpeningId;
            entity.AppliedDate = dto.AppliedDate;
            entity.Status = (CandidateStatus)dto.Status;
            entity.Remarks = dto.Remarks;
            entity.TenantId = dto.TenantId;
            entity.ModifiedBy = dto.ModifiedBy;
            entity.ModifiedOn = dto.ModifiedOn ?? DateTime.UtcNow;

            _context.CandidateApplications.Update(entity);
            await _context.SaveChangesAsync();

            return entity.Id;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        #endregion

        #region Change Stage

        public async Task<string> ChangeStageAsync(string id, int status, string userId)
        {
            try
            {
            var entity = await _context.CandidateApplications
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null)
                return "Application Not Found";

            entity.Status = (CandidateStatus)status;
            entity.ModifiedBy = userId;
            entity.ModifiedOn = DateTime.UtcNow;

            _context.CandidateApplications.Update(entity);

            // Keep candidate profile status in sync
            var candidate = await _context.Candidates
                .FirstOrDefaultAsync(c => c.Id == entity.CandidateId && !c.IsDeleted);

            if (candidate != null)
            {
                candidate.Status = (CandidateStatus)status;
                candidate.ModifiedBy = userId;
                candidate.ModifiedOn = DateTime.UtcNow;
            }

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
            var entity = await _context.CandidateApplications
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null)
                return false;

            entity.IsDeleted = true;
            entity.ModifiedOn = DateTime.UtcNow;

            _context.CandidateApplications.Update(entity);
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
