using System.ComponentModel.DataAnnotations;

namespace APP.Models.DTOs
{
    // ==============================
    // Job Opening
    // ==============================

    public class JobOpeningDto
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Please enter Job Title.")]
        [MaxLength(150)]
        [Display(Name = "Job Title")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Please select Department.")]
        [Display(Name = "Department")]
        public string DepartmentId { get; set; }
        public string? DepartmentName { get; set; }

        [Required(ErrorMessage = "Please select Designation.")]
        [Display(Name = "Designation")]
        public string DesignationId { get; set; }
        public string? DesignationName { get; set; }

        [Display(Name = "Vacancies")]
        public int VacancyCount { get; set; } = 1;

        [Display(Name = "Min Salary")]
        public decimal? MinSalary { get; set; }

        [Display(Name = "Max Salary")]
        public decimal? MaxSalary { get; set; }

        [Display(Name = "Job Description")]
        public string? JobDescription { get; set; }

        [Display(Name = "Required Skills")]
        public string? RequiredSkills { get; set; }

        [Display(Name = "Status")]
        public int Status { get; set; } = 1;
        public string? StatusText { get; set; }

        [Display(Name = "Posted Date")]
        [DataType(DataType.Date)]
        public DateTime PostedDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Closing Date")]
        [DataType(DataType.Date)]
        public DateTime? ClosingDate { get; set; }

        public string? TenantId { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }
    }

    public class JobOpeningListDto
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string? DepartmentName { get; set; }
        public string? DesignationName { get; set; }
        public int VacancyCount { get; set; }
        public int Status { get; set; }
        public string? StatusText { get; set; }
        public DateTime PostedDate { get; set; }
        public DateTime? ClosingDate { get; set; }
        public int ApplicationCount { get; set; }
    }

    // ==============================
    // Candidate
    // ==============================

    public class CandidateDto
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Please enter Candidate Name.")]
        [MaxLength(150)]
        [Display(Name = "Full Name")]
        public string Name { get; set; }

        [MaxLength(150)]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [MaxLength(15)]
        [Display(Name = "Phone")]
        public string? Phone { get; set; }

        [Display(Name = "Experience (months)")]
        public int? TotalExperience { get; set; }

        [Display(Name = "Skills")]
        public string? Skills { get; set; }

        [Display(Name = "Current Company")]
        public string? CurrentCompany { get; set; }

        [Display(Name = "Current Salary")]
        public decimal? CurrentSalary { get; set; }

        [Display(Name = "Expected Salary")]
        public decimal? ExpectedSalary { get; set; }

        [Display(Name = "Resume")]
        public string? ResumeUrl { get; set; }

        [Display(Name = "Status")]
        public int Status { get; set; } = 1;
        public string? StatusText { get; set; }

        [Display(Name = "Source")]
        public string? Source { get; set; }

        public string? TenantId { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }
    }

    public class CandidateListDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public int? TotalExperience { get; set; }
        public string? Skills { get; set; }
        public string? Source { get; set; }
        public string? ResumeUrl { get; set; }
        public int Status { get; set; }
        public string? StatusText { get; set; }
        public int ApplicationCount { get; set; }
    }

    // ==============================
    // Candidate Application
    // ==============================

    public class CandidateApplicationDto
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Please select a Candidate.")]
        [Display(Name = "Candidate")]
        public string CandidateId { get; set; }
        public string? CandidateName { get; set; }

        [Required(ErrorMessage = "Please select a Job Opening.")]
        [Display(Name = "Job Opening")]
        public string JobOpeningId { get; set; }
        public string? JobTitle { get; set; }

        [Display(Name = "Applied Date")]
        [DataType(DataType.Date)]
        public DateTime AppliedDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Stage")]
        public int Status { get; set; } = 1;
        public string? StatusText { get; set; }

        [Display(Name = "Remarks")]
        public string? Remarks { get; set; }

        public string? TenantId { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }
    }

    public class CandidateApplicationListDto
    {
        public string Id { get; set; }
        public string? CandidateId { get; set; }
        public string? CandidateName { get; set; }
        public string? JobOpeningId { get; set; }
        public string? JobTitle { get; set; }
        public DateTime AppliedDate { get; set; }
        public int Status { get; set; }
        public string? StatusText { get; set; }
        public string? Remarks { get; set; }
        public int InterviewCount { get; set; }
    }

    // ==============================
    // Interview Schedule
    // ==============================

    public class InterviewScheduleDto
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Please select an Application.")]
        [Display(Name = "Application")]
        public string CandidateApplicationId { get; set; }
        public string? CandidateName { get; set; }
        public string? JobTitle { get; set; }

        [Required(ErrorMessage = "Please select the Interview Date.")]
        [Display(Name = "Interview Date")]
        public DateTime InterviewDate { get; set; } = DateTime.UtcNow;

        [Required(ErrorMessage = "Please select an Interviewer.")]
        [Display(Name = "Interviewer")]
        public string InterviewerId { get; set; }
        public string? InterviewerName { get; set; }

        [Display(Name = "Mode")]
        public int Mode { get; set; } = 1;
        public string? ModeText { get; set; }

        [Display(Name = "Status")]
        public int Status { get; set; } = 1;
        public string? StatusText { get; set; }

        [Display(Name = "Feedback")]
        public string? Feedback { get; set; }

        public string? TenantId { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }
    }

    public class InterviewScheduleListDto
    {
        public string Id { get; set; }
        public string? CandidateApplicationId { get; set; }
        public string? CandidateName { get; set; }
        public string? JobTitle { get; set; }
        public DateTime InterviewDate { get; set; }
        public string? InterviewerId { get; set; }
        public string? InterviewerName { get; set; }
        public int Mode { get; set; }
        public string? ModeText { get; set; }
        public int Status { get; set; }
        public string? StatusText { get; set; }
        public string? Feedback { get; set; }
    }

    public class RecruitmentDashboardDto
    {
        public int OpenPositions { get; set; }
        public int TotalVacancies { get; set; }
        public int TotalCandidates { get; set; }
        public int TotalApplications { get; set; }
        public int Hires { get; set; }
        public int InterviewsScheduled { get; set; }
        public int InterviewsCompleted { get; set; }

        public int AppliedCount { get; set; }
        public int ShortlistedCount { get; set; }
        public int InterviewCount { get; set; }
        public int SelectedCount { get; set; }
        public int RejectedCount { get; set; }

        public List<InterviewScheduleListDto> UpcomingInterviews { get; set; } = new();
        public List<CandidateApplicationListDto> RecentApplications { get; set; } = new();
    }
}
