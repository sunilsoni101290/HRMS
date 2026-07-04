using Application.DTOs.Recruitment;
using Application.Interfaces.Recruitment;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using static Domain.Enums.EnumExtensions;

namespace Application.Services.Recruitment
{
    public class RecruitmentDashboardService : IRecruitmentDashboardService
    {
        private readonly ApplicationDbContext _context;

        public RecruitmentDashboardService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<RecruitmentDashboardDto> GetDashboardAsync()
        {
            try
            {
            var dto = new RecruitmentDashboardDto();

            var openJobs = await _context.JobOpenings
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.Status == JobStatus.Open)
                .Select(x => x.VacancyCount)
                .ToListAsync();

            dto.OpenPositions = openJobs.Count;
            dto.TotalVacancies = openJobs.Sum();

            dto.TotalCandidates = await _context.Candidates.CountAsync(x => !x.IsDeleted);

            var appStatuses = await _context.CandidateApplications
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .Select(x => (int)x.Status)
                .ToListAsync();

            dto.TotalApplications = appStatuses.Count;
            dto.AppliedCount = appStatuses.Count(s => s == (int)CandidateStatus.Applied);
            dto.ShortlistedCount = appStatuses.Count(s => s == (int)CandidateStatus.Shortlisted);
            dto.InterviewCount = appStatuses.Count(s => s == (int)CandidateStatus.Interview);
            dto.SelectedCount = appStatuses.Count(s => s == (int)CandidateStatus.Selected);
            dto.RejectedCount = appStatuses.Count(s => s == (int)CandidateStatus.Rejected);
            dto.Hires = dto.SelectedCount;

            var interviewStatuses = await _context.InterviewSchedules
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .Select(x => (int)x.Status)
                .ToListAsync();

            dto.InterviewsScheduled = interviewStatuses.Count(s => s == (int)InterviewScheduleStatus.Scheduled);
            dto.InterviewsCompleted = interviewStatuses.Count(s => s == (int)InterviewScheduleStatus.Completed);

            var now = DateTime.UtcNow.Date;

            dto.UpcomingInterviews = await _context.InterviewSchedules
                .AsNoTracking()
                .Where(x => !x.IsDeleted
                         && x.Status == InterviewScheduleStatus.Scheduled
                         && x.InterviewDate >= now)
                .OrderBy(x => x.InterviewDate)
                .Take(5)
                .Select(x => new InterviewScheduleListDto
                {
                    Id = x.Id,
                    CandidateName = x.CandidateApplication != null && x.CandidateApplication.Candidate != null
                        ? x.CandidateApplication.Candidate.Name : "",
                    JobTitle = x.CandidateApplication != null && x.CandidateApplication.JobOpening != null
                        ? x.CandidateApplication.JobOpening.Title : "",
                    InterviewDate = x.InterviewDate,
                    InterviewerName = x.Interviewer != null
                        ? x.Interviewer.FirstName + " " + x.Interviewer.LastName : "",
                    Mode = (int)x.Mode,
                    Status = (int)x.Status
                })
                .ToListAsync();

            dto.RecentApplications = await _context.CandidateApplications
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderByDescending(x => x.AppliedDate)
                .Take(5)
                .Select(x => new CandidateApplicationListDto
                {
                    Id = x.Id,
                    CandidateName = x.Candidate != null ? x.Candidate.Name : "",
                    JobTitle = x.JobOpening != null ? x.JobOpening.Title : "",
                    AppliedDate = x.AppliedDate,
                    Status = (int)x.Status
                })
                .ToListAsync();

            foreach (var i in dto.UpcomingInterviews)
            {
                i.ModeText = ((InterviewScheduleMode)i.Mode).ToString();
                i.StatusText = ((InterviewScheduleStatus)i.Status).ToString();
            }
            foreach (var a in dto.RecentApplications)
                a.StatusText = ((CandidateStatus)a.Status).ToString();

            return dto;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
