using System;
using System.Collections.Generic;
using static Domain.Enums.EnumExtensions;

namespace Application.DTOs.Attendances
{
    /// <summary>
    /// DTOs for the development-only Biometric Simulator page. Every request
    /// here is translated into a real PunchIngestRequestDto and pushed
    /// through IBiometricSyncService.IngestPunchesAsync - the exact same
    /// pipeline the on-site BiometricAgent uses - so the simulator never
    /// becomes a second, parallel attendance system. See
    /// BiometricSimulatorService.
    /// </summary>
    public class SimulatePunchRequestDto
    {
        public string EmployeeId { get; set; }
        public string DeviceId { get; set; }
        public DateTime PunchDateTime { get; set; }
        public PunchType PunchType { get; set; }
        public string? VerifyMode { get; set; }

        /// <summary>When true, the same punch is sent twice in a row to exercise duplicate protection (Scenario 5).</summary>
        public bool SendTwice { get; set; }
    }

    public class GenerateFullDayRequestDto
    {
        public string EmployeeId { get; set; }
        public string DeviceId { get; set; }
        public DateTime Date { get; set; }
        public string InTime { get; set; } = "09:00";
        public string? BreakOutTime { get; set; } = "13:00";
        public string? BreakInTime { get; set; } = "14:00";
        public string OutTime { get; set; } = "18:00";
        public string? VerifyMode { get; set; }
    }

    public class RunScenarioRequestDto
    {
        public string EmployeeId { get; set; }
        public string DeviceId { get; set; }
        public DateTime Date { get; set; }

        /// <summary>
        /// One of: Normal, Late, MultiplePunches, MissingOut, Duplicate,
        /// NightShift, Overtime, Holiday, WeeklyOff, Leave. Holiday/WeeklyOff/
        /// Leave just punch a normal day on the given Date - it's the real
        /// Holiday/WeekOff/Leave modules (via the attendance engine) that are
        /// expected to reclassify it, not the simulator.
        /// </summary>
        public string ScenarioCode { get; set; }
    }

    public class ClearTestDataRequestDto
    {
        public string DeviceId { get; set; }
        public string? EmployeeId { get; set; }
    }

    public class SimulatorResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int ReceivedCount { get; set; }
        public int InsertedCount { get; set; }
        public int DuplicateCount { get; set; }
        public string? Stage { get; set; }
    }

    public class SimulatorMappedEmployeeDto
    {
        public string EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string BiometricEmployeeCode { get; set; }
        public bool IsActive { get; set; }
    }
}
