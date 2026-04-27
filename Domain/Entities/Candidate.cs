using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    public class Candidate : BaseEntity
    {
        [Required, MaxLength(150)]
        public string Name { get; set; }

        [MaxLength(150)]
        public string Email { get; set; }

        [MaxLength(15)]
        public string Phone { get; set; }

        // Profile
        public int? TotalExperience { get; set; } // in months
        public string Skills { get; set; }

        public string CurrentCompany { get; set; }
        public decimal? CurrentSalary { get; set; }
        public decimal? ExpectedSalary { get; set; }

        // Resume
        public string ResumeUrl { get; set; }

        // Status
        public CandidateStatus Status { get; set; }
        // Applied / Shortlisted / Interview / Selected / Rejected

        // Source
        public string Source { get; set; } // LinkedIn, Referral, Portal

        public override string GetSequencePrefix() => "CAND";
        // Navigation
        public ICollection<CandidateApplication> Applications { get; set; }
    }
}
