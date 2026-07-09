namespace BiometricAgent.Models
{
    public enum PunchType
    {
        In = 1,
        Out = 2,
        BreakIn = 3,
        BreakOut = 4
    }

    /// <summary>
    /// One raw punch read off the device. Mirrors Application.DTOs.Attendances.PunchItemDto
    /// on the API side - keep the two in sync if either changes.
    /// </summary>
    public class PunchRecord
    {
        public string EmployeeCode { get; set; } = "";
        public DateTime PunchTime { get; set; }
        public PunchType PunchType { get; set; } = PunchType.In;
    }
}
