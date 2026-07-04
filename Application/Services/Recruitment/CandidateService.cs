using Application.DTOs.Recruitment;
using Application.Interfaces.Recruitment;
using Domain.Entities;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static Domain.Enums.EnumExtensions;

namespace Application.Services.Recruitment
{
    public class CandidateService : ICandidateService
    {
        private readonly ApplicationDbContext _context;

        public CandidateService(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Get All

        public async Task<List<CandidateListDto>> GetAllAsync()
        {
            try
            {
            var list = await _context.Candidates
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderByDescending(x => x.CreatedOn)
                .Select(x => new CandidateListDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Email = x.Email,
                    Phone = x.Phone,
                    TotalExperience = x.TotalExperience,
                    Skills = x.Skills,
                    Source = x.Source,
                    ResumeUrl = x.ResumeUrl,
                    Status = (int)x.Status,
                    ApplicationCount = _context.CandidateApplications
                        .Count(a => a.CandidateId == x.Id && !a.IsDeleted)
                })
                .ToListAsync();

            foreach (var item in list)
                item.StatusText = ((CandidateStatus)item.Status).ToString();

            return list;
            }
            catch (Exception)
            {
                return new List<CandidateListDto>();
            }
        }

        #endregion

        #region Get By Id

        public async Task<CandidateDto> GetByIdAsync(string id)
        {
            try
            {
            var entity = await _context.Candidates
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null)
                return null;

            return new CandidateDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Email = entity.Email,
                Phone = entity.Phone,
                TotalExperience = entity.TotalExperience,
                Skills = entity.Skills,
                CurrentCompany = entity.CurrentCompany,
                CurrentSalary = entity.CurrentSalary,
                ExpectedSalary = entity.ExpectedSalary,
                ResumeUrl = entity.ResumeUrl,
                Status = (int)entity.Status,
                StatusText = entity.Status.ToString(),
                Source = entity.Source,
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

        public async Task<string> CreateAsync(CandidateDto dto)
        {
            try
            {
            var entity = new Candidate
            {
                Id = IDManager.GetNewId(new Candidate()),
                Name = dto.Name,
                Email = dto.Email,
                Phone = dto.Phone,
                TotalExperience = dto.TotalExperience,
                Skills = dto.Skills,
                CurrentCompany = dto.CurrentCompany,
                CurrentSalary = dto.CurrentSalary,
                ExpectedSalary = dto.ExpectedSalary,
                ResumeUrl = dto.ResumeUrl,
                Status = (CandidateStatus)dto.Status,
                Source = dto.Source,
                TenantId = dto.TenantId,
                CreatedBy = dto.CreatedBy
            };

            await _context.Candidates.AddAsync(entity);
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

        public async Task<string> UpdateAsync(string id, CandidateDto dto)
        {
            try
            {
            var entity = await _context.Candidates
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null)
                return "Candidate Not Found";

            entity.Name = dto.Name;
            entity.Email = dto.Email;
            entity.Phone = dto.Phone;
            entity.TotalExperience = dto.TotalExperience;
            entity.Skills = dto.Skills;
            entity.CurrentCompany = dto.CurrentCompany;
            entity.CurrentSalary = dto.CurrentSalary;
            entity.ExpectedSalary = dto.ExpectedSalary;
            if (!string.IsNullOrEmpty(dto.ResumeUrl))
                entity.ResumeUrl = dto.ResumeUrl;
            entity.Status = (CandidateStatus)dto.Status;
            entity.Source = dto.Source;
            entity.TenantId = dto.TenantId;
            entity.ModifiedBy = dto.ModifiedBy;
            entity.ModifiedOn = dto.ModifiedOn ?? DateTime.UtcNow;

            _context.Candidates.Update(entity);
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
            var entity = await _context.Candidates
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null)
                return false;

            entity.IsDeleted = true;
            entity.ModifiedOn = DateTime.UtcNow;

            _context.Candidates.Update(entity);
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
