using Application.DTOs.Recruitment;
using Application.Interfaces.Recruitment;
using Domain.Entities;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static Domain.Enums.EnumExtensions;

namespace Application.Services.Recruitment
{
    public class InterviewScheduleService : IInterviewScheduleService
    {
        private readonly ApplicationDbContext _context;

        public InterviewScheduleService(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Get All

        public async Task<List<InterviewScheduleListDto>> GetAllAsync()
        {
            try
            {
            var list = await Query().ToListAsync();
            ApplyText(list);
            return list;
            }
            catch (Exception)
            {
                return new List<InterviewScheduleListDto>();
            }
        }

        public async Task<List<InterviewScheduleListDto>> GetByApplicationAsync(string applicationId)
        {
            try
            {
            var list = await Query()
                .Where(x => x.CandidateApplicationId == applicationId)
                .ToListAsync();
            ApplyText(list);
            return list;
            }
            catch (Exception)
            {
                return new List<InterviewScheduleListDto>();
            }
        }

        private IQueryable<InterviewScheduleListDto> Query()
        {
            return _context.InterviewSchedules
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderByDescending(x => x.InterviewDate)
                .Select(x => new InterviewScheduleListDto
                {
                    Id = x.Id,
                    CandidateApplicationId = x.CandidateApplicationId,
                    CandidateName = x.CandidateApplication != null && x.CandidateApplication.Candidate != null
                        ? x.CandidateApplication.Candidate.Name : "",
                    JobTitle = x.CandidateApplication != null && x.CandidateApplication.JobOpening != null
                        ? x.CandidateApplication.JobOpening.Title : "",
                    InterviewDate = x.InterviewDate,
                    InterviewerId = x.InterviewerId,
                    InterviewerName = x.Interviewer != null
                        ? x.Interviewer.FirstName + " " + x.Interviewer.LastName : "",
                    Mode = (int)x.Mode,
                    Status = (int)x.Status,
                    Feedback = x.Feedback
                });
        }

        private static void ApplyText(List<InterviewScheduleListDto> list)
        {
            foreach (var item in list)
            {
                item.ModeText = ((InterviewScheduleMode)item.Mode).ToString();
                item.StatusText = ((InterviewScheduleStatus)item.Status).ToString();
            }
        }

        #endregion

        #region Get By Id

        public async Task<InterviewScheduleDto> GetByIdAsync(string id)
        {
            try
            {
            var entity = await _context.InterviewSchedules
                .Include(x => x.CandidateApplication).ThenInclude(a => a.Candidate)
                .Include(x => x.CandidateApplication).ThenInclude(a => a.JobOpening)
                .Include(x => x.Interviewer)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null)
                return null;

            return new InterviewScheduleDto
            {
                Id = entity.Id,
                CandidateApplicationId = entity.CandidateApplicationId,
                CandidateName = entity.CandidateApplication?.Candidate != null ? entity.CandidateApplication.Candidate.Name : "",
                JobTitle = entity.CandidateApplication?.JobOpening != null ? entity.CandidateApplication.JobOpening.Title : "",
                InterviewDate = entity.InterviewDate,
                InterviewerId = entity.InterviewerId,
                InterviewerName = entity.Interviewer != null ? entity.Interviewer.FirstName + " " + entity.Interviewer.LastName : "",
                Mode = (int)entity.Mode,
                ModeText = entity.Mode.ToString(),
                Status = (int)entity.Status,
                StatusText = entity.Status.ToString(),
                Feedback = entity.Feedback,
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

        public async Task<string> CreateAsync(InterviewScheduleDto dto)
        {
            try
            {
            var entity = new InterviewSchedule
            {
                Id = IDManager.GetNewId(new InterviewSchedule()),
                CandidateApplicationId = dto.CandidateApplicationId,
                InterviewDate = dto.InterviewDate,
                InterviewerId = dto.InterviewerId,
                Mode = (InterviewScheduleMode)dto.Mode,
                Status = (InterviewScheduleStatus)dto.Status,
                Feedback = dto.Feedback,
                TenantId = dto.TenantId,
                CreatedBy = dto.CreatedBy
            };

            await _context.InterviewSchedules.AddAsync(entity);

            // Move the application into the Interview stage
            var application = await _context.CandidateApplications
                .FirstOrDefaultAsync(a => a.Id == dto.CandidateApplicationId && !a.IsDeleted);

            if (application != null && application.Status < CandidateStatus.Interview)
            {
                application.Status = CandidateStatus.Interview;
                application.ModifiedBy = dto.CreatedBy;
                application.ModifiedOn = DateTime.UtcNow;
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

        #region Update

        public async Task<string> UpdateAsync(string id, InterviewScheduleDto dto)
        {
            try
            {
            var entity = await _context.InterviewSchedules
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null)
                return "Interview Not Found";

            entity.CandidateApplicationId = dto.CandidateApplicationId;
            entity.InterviewDate = dto.InterviewDate;
            entity.InterviewerId = dto.InterviewerId;
            entity.Mode = (InterviewScheduleMode)dto.Mode;
            entity.Status = (InterviewScheduleStatus)dto.Status;
            entity.Feedback = dto.Feedback;
            entity.TenantId = dto.TenantId;
            entity.ModifiedBy = dto.ModifiedBy;
            entity.ModifiedOn = dto.ModifiedOn ?? DateTime.UtcNow;

            _context.InterviewSchedules.Update(entity);
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

        public async Task<string> ChangeStatusAsync(string id, int status, string feedback, string userId)
        {
            try
            {
            var entity = await _context.InterviewSchedules
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null)
                return "Interview Not Found";

            entity.Status = (InterviewScheduleStatus)status;
            if (!string.IsNullOrEmpty(feedback))
                entity.Feedback = feedback;
            entity.ModifiedBy = userId;
            entity.ModifiedOn = DateTime.UtcNow;

            _context.InterviewSchedules.Update(entity);
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
            var entity = await _context.InterviewSchedules
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null)
                return false;

            entity.IsDeleted = true;
            entity.ModifiedOn = DateTime.UtcNow;

            _context.InterviewSchedules.Update(entity);
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
