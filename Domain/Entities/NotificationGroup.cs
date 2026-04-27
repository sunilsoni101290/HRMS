using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class NotificationGroup : BaseEntity
    {
        public string Name { get; set; } // HR, Admin, Manager

        public string TenantId { get; set; }

        public ICollection<NotificationGroupUser> Users { get; set; }
        public override string GetSequencePrefix() => "NG";
    }
}
