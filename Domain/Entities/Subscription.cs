using System;
using System.Collections.Generic;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    public class Subscription : BaseEntity
    {
        public string TenantId { get; set; }
        public string TenantTypeId { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public decimal AmountPaid { get; set; }

        public PaymentStatus PaymentStatus { get; set; }
        public override string GetSequencePrefix() => "SC";
    }
}
