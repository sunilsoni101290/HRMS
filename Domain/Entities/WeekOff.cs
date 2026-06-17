using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class WeekOff : BaseEntity
    {
        public DayOfWeek Day { get; set; }
    }
}
