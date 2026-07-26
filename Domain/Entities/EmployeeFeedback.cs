using System;
using System.ComponentModel.DataAnnotations;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    // Employee Feedback - the FOURTH phase of the "Probation & Confirmation"
    // (Employee Lifecycle) module (see Domain/Entities/ProbationConfirmation.cs
    // Phase 1, Domain/Entities/PipRecord.cs Phase 2,
    // Domain/Entities/EmployeeTransfer.cs Phase 3). UNLIKE the preceding
    // three phases, this feature has NO maker-checker workflow - Feedback is
    // general ongoing performance feedback usable any time for any employee,
    // not tied to probation/approval workflows. Plain CRUD, see
    // EmployeeFeedbackService for authorization rules:
    //   - Create: the acting user must be the target Employee's current
    //     ReportingManagerId, OR hold Create permission on
    //     AppFeatureConstants.EMPLOYEE_FEEDBACK (HR/Admin).
    //   - Update/Delete: only the original author (GivenByUserId ==
    //     actingUserId) or someone holding Edit/Delete permission (HR/Admin).
    //   - View: the subject Employee themself (only if IsVisibleToEmployee),
    //     the author, the subject's current Reporting Manager, or anyone
    //     holding View permission (HR/Admin).
    public class EmployeeFeedback : BaseEntity
    {
        // The employee the feedback is ABOUT.
        [Required]
        public string EmployeeId { get; set; }
        public virtual Employee Employee { get; set; }

        // The acting User.Id (NOT EmployeeId) of whoever gave the feedback -
        // same "identity is about who's logged in" reasoning as MakerId in
        // the other three Phase entities in this module.
        [Required]
        public string GivenByUserId { get; set; }

        public DateTime FeedbackDate { get; set; }

        public FeedbackCategory Category { get; set; } = FeedbackCategory.General;

        // Optional 1-5 rating scale.
        public int? Rating { get; set; }

        public string? Strengths { get; set; }

        public string? AreasOfImprovement { get; set; }

        public string? Comments { get; set; }

        // Whether the subject Employee can see this feedback in their own
        // self-service view. When false, only the author, the subject's
        // Reporting Manager, or HR/Admin (View permission) can see it - the
        // subject themself cannot, even though it is feedback about them.
        public bool IsVisibleToEmployee { get; set; } = true;

        public override string GetSequencePrefix() => "FDB";
    }
}
