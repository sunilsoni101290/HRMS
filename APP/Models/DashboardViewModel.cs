namespace APP.Models
{
    public class DashboardViewModel
    {
        public int TotalEmployees { get; set; }
        public int ActiveEmployees { get; set; }
        public int InactiveEmployees { get; set; }

        public int PresentToday { get; set; }
        public int AbsentToday { get; set; }

        public int OnLeaveToday { get; set; }

        public int NewJoiners { get; set; }
        public int ResignedEmployees { get; set; }

        public int LateArrivals { get; set; }
        public int OnLeave { get; set; }

        public int OpenPositions { get; set; }
        public int PendingApprovals { get; set; }

        public List<EmployeeViewModel> RecentJoinees { get; set; }
        public List<EmployeeViewModel> UpcomingBirthdays { get; set; }

        public List<string> Notifications { get; set; }
    }

    public class EmployeeViewModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Designation { get; set; }
    }
}
