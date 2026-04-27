using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    public class JobOpening : BaseEntity
    {
        [Required, MaxLength(150)]
        public string Title { get; set; }

        public string DepartmentId { get; set; }
        public virtual Department Department { get; set; }

        public string DesignationId { get; set; }
        public virtual Designation Designation { get; set; }

        public int VacancyCount { get; set; }

        // Salary Range
        public decimal? MinSalary { get; set; }
        public decimal? MaxSalary { get; set; }

        // Job Details
        public string JobDescription { get; set; }
        public string RequiredSkills { get; set; }

        // Status
        public JobStatus Status { get; set; }
        // Open / Closed / OnHold

        // Dates
        public DateTime PostedDate { get; set; }
        public DateTime? ClosingDate { get; set; }

        // Navigation
        public ICollection<CandidateApplication> Applications { get; set; }
        public override string GetSequencePrefix() => "JBO";
    }
}
