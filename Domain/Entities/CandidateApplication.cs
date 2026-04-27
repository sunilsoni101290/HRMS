using System;
using System.Collections.Generic;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    public class CandidateApplication : BaseEntity
    {
        public string CandidateId { get; set; }
        public virtual Candidate Candidate { get; set; }

        public string JobOpeningId { get; set; }
        public virtual JobOpening JobOpening { get; set; }

        public DateTime AppliedDate { get; set; }

        // Hiring Pipeline Status
        public CandidateStatus Status { get; set; }
        // Applied / Shortlisted / Interview / Offer / Hired / Rejected

        public string Remarks { get; set; }
        public override string GetSequencePrefix() => "APP";
    }
}
