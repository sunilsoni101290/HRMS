using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class NotificationGroupAccess : BaseEntity
    {
        public string NotificationGroupId { get; set; }
        public NotificationGroup NotificationGroup { get; set; }

        public string AppFeatureId { get; set; } // LEAVE_APPLY, PAYROLL_RUN
        public override string GetSequencePrefix() => "NGA";
    }
}
