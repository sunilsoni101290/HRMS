using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class NotificationGroupUser : BaseEntity
    {
        public string NotificationGroupId { get; set; }
        public NotificationGroup NotificationGroup { get; set; }

        public string UserId { get; set; }
        public User User { get; set; }
        public override string GetSequencePrefix() => "NGU";
    }
}
