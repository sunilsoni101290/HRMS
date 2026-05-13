using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Employee
{
    public class DashboardDto
    {
        public int TotalEmployees { get; set; }
        public int ActiveEmployees { get; set; }
        public int InactiveEmployees { get; set; }

        public int PresentToday { get; set; }
        public int AbsentToday { get; set; }

        public int OnLeaveToday { get; set; }

        public List<EmployeeMiniDto> RecentJoinees { get; set; }
        public List<EmployeeMiniDto> UpcomingBirthdays { get; set; }

        public List<string> Notifications { get; set; }
    }

    public class EmployeeMiniDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Designation { get; set; }
    }
}
