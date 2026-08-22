using System.ComponentModel.DataAnnotations;

namespace APP.Models.DTOs
{
    /// <summary>Mirrors Application.DTOs.Attendances.BiometricSimulatorDto - see that file for the pipeline explanation.</summary>
    public class SimulatePunchRequestDto
    {
        [Required]
        public string EmployeeId { get; set; } = string.Empty;

        [Required]
        public string DeviceId { get; set; } = string.Empty;

        [Required]
        public DateTime PunchDateTime { get; set; } = DateTime.Now;

        [Required]
        public int PunchType { get; set; } = 1;

        public string? VerifyMode { get; set; }

        public bool SendTwice { get; set; }
    }

    public class GenerateFullDayRequestDto
    {
        [Required]
        public string EmployeeId { get; set; } = string.Empty;

        [Required]
        public string DeviceId { get; set; } = string.Empty;

        [Required]
        public DateTime Date { get; set; } = DateTime.Today;

        public string InTime { get; set; } = "09:00";
        public string? BreakOutTime { get; set; } = "13:00";
        public string? BreakInTime { get; set; } = "14:00";
        public string OutTime { get; set; } = "18:00";
        public string? VerifyMode { get; set; }
    }

    public class RunScenarioRequestDto
    {
        [Required]
        public string EmployeeId { get; set; } = string.Empty;

        [Required]
        public string DeviceId { get; set; } = string.Empty;

        [Required]
        public DateTime Date { get; set; } = DateTime.Today;

        [Required]
        public string ScenarioCode { get; set; } = "Normal";
    }

    public class ClearTestDataRequestDto
    {
        [Required]
        public string DeviceId { get; set; } = string.Empty;

        public string? EmployeeId { get; set; }
    }

    public class SimulatorResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ReceivedCount { get; set; }
        public int InsertedCount { get; set; }
        public int DuplicateCount { get; set; }
    }

    public class SimulatorMappedEmployeeDto
    {
        public string EmployeeId { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string BiometricEmployeeCode { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
