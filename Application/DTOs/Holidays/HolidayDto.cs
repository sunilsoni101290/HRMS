using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Holidays
{
    public class HolidayDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public DateTime Date { get; set; }
        public string TenantId { get; set; }
    }

    public class CreateHolidayDto
    {
        public string Name { get; set; }
        public DateTime Date { get; set; }
        public string TenantId { get; set; }
    }

    public class WeekOffDto
    {
        public string Id { get; set; }
        public DayOfWeek Day { get; set; }
        public string TenantId { get; set; }
    }

    public class CreateWeekOffDto
    {
        public DayOfWeek Day { get; set; }
        public string TenantId { get; set; }
    }
}
