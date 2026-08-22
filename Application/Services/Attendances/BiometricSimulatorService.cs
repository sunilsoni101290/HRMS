using Application.Common.Exceptions;
using Application.DTOs.Attendances;
using Application.Interfaces.Attendances;
using Domain.Entities;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static Domain.Enums.EnumExtensions;

namespace Application.Services.Attendances
{
    /// <summary>
    /// See IBiometricSimulatorService. Every method here ends by calling the
    /// real IBiometricSyncService.IngestPunchesAsync (using the target
    /// device's own DeviceCode/DeviceKey, exactly like a real BiometricAgent
    /// would) followed by IAttendanceProcessorService.ProcessAttendanceAsync
    /// when anything new was inserted - it does not touch
    /// BiometricAttendanceLog/Attendance directly, so the simulator can never
    /// drift from the real ingest/duplicate-check/processing behaviour.
    /// </summary>
    public class BiometricSimulatorService : IBiometricSimulatorService
    {
        private readonly ApplicationDbContext _db;
        private readonly IBiometricSyncService _syncService;
        private readonly IAttendanceProcessorService _processor;
        private readonly ILogger<BiometricSimulatorService> _logger;

        public BiometricSimulatorService(
            ApplicationDbContext db,
            IBiometricSyncService syncService,
            IAttendanceProcessorService processor,
            ILogger<BiometricSimulatorService> logger)
        {
            _db = db;
            _syncService = syncService;
            _processor = processor;
            _logger = logger;
        }

        public async Task<List<SimulatorMappedEmployeeDto>> GetMappedEmployeesAsync(string tenantId)
        {
            return await _db.EmployeeBiometricMappings
                .Include(x => x.Employee)
                .Where(x => string.IsNullOrEmpty(tenantId) || x.Employee.TenantId == tenantId)
                .Select(x => new SimulatorMappedEmployeeDto
                {
                    EmployeeId = x.EmployeeId,
                    EmployeeName = x.Employee.FirstName + " " + x.Employee.LastName,
                    BiometricEmployeeCode = x.BiometricEmployeeCode,
                    IsActive = x.IsActive
                })
                .OrderBy(x => x.EmployeeName)
                .ToListAsync();
        }

        private async Task<(BiometricDevice Device, EmployeeBiometricMapping Mapping)> ResolveAsync(
            string deviceId, string employeeId, string tenantId)
        {
            var device = await _db.BiometricDevices
                .FirstOrDefaultAsync(x => x.Id == deviceId &&
                    (string.IsNullOrEmpty(tenantId) || x.TenantId == tenantId));

            if (device == null)
                throw new NotFoundException("Simulator device not found.", "DEVICE_NOT_FOUND");

            if (string.IsNullOrWhiteSpace(device.DeviceKey))
                throw new BadRequestException(
                    "This device has no Device Key configured - open it in Edit and save once to generate one before simulating punches.",
                    "DEVICE_KEY_MISSING");

            var mapping = await _db.EmployeeBiometricMappings
                .FirstOrDefaultAsync(x => x.EmployeeId == employeeId);

            if (mapping == null)
                throw new BadRequestException(
                    "This employee has no Biometric Mapping yet - create one first (Employee -> Biometric Mapping) so the simulator has a Biometric User ID to punch with.",
                    "MAPPING_NOT_FOUND");

            return (device, mapping);
        }

        private async Task<SimulatorResultDto> PushAsync(
            BiometricDevice device, List<PunchItemDto> punches, bool overrideDeviceKey = false)
        {
            var request = new PunchIngestRequestDto
            {
                DeviceCode = device.DeviceCode,
                DeviceKey = overrideDeviceKey ? "SIMULATED-WRONG-KEY" : device.DeviceKey,
                AgentCode = null,
                Punches = punches
            };

            var result = await _syncService.IngestPunchesAsync(request);

            if (result.Success && result.InsertedCount > 0)
                await _processor.ProcessAttendanceAsync();

            return new SimulatorResultDto
            {
                Success = result.Success,
                Message = result.Message,
                ReceivedCount = result.ReceivedCount,
                InsertedCount = result.InsertedCount,
                DuplicateCount = result.DuplicateCount
            };
        }

        public async Task<SimulatorResultDto> SimulatePunchAsync(SimulatePunchRequestDto request, string tenantId)
        {
            var (device, mapping) = await ResolveAsync(request.DeviceId, request.EmployeeId, tenantId);

            var item = new PunchItemDto
            {
                EmployeeCode = mapping.BiometricEmployeeCode,
                PunchTime = request.PunchDateTime,
                PunchType = request.PunchType,
                VerifyMode = request.VerifyMode
            };

            var punches = request.SendTwice
                ? new List<PunchItemDto> { item, item }
                : new List<PunchItemDto> { item };

            return await PushAsync(device, punches);
        }

        public async Task<SimulatorResultDto> GenerateFullDayAsync(GenerateFullDayRequestDto request, string tenantId)
        {
            var (device, mapping) = await ResolveAsync(request.DeviceId, request.EmployeeId, tenantId);

            var punches = new List<PunchItemDto>();

            void Add(string? time, PunchType type)
            {
                if (string.IsNullOrWhiteSpace(time))
                    return;

                if (!TimeSpan.TryParse(time, out var t))
                    throw new BadRequestException($"'{time}' is not a valid time (expected HH:mm).", "INVALID_TIME");

                punches.Add(new PunchItemDto
                {
                    EmployeeCode = mapping.BiometricEmployeeCode,
                    PunchTime = request.Date.Date.Add(t),
                    PunchType = type,
                    VerifyMode = request.VerifyMode
                });
            }

            Add(request.InTime, PunchType.In);
            Add(request.BreakOutTime, PunchType.BreakOut);
            Add(request.BreakInTime, PunchType.BreakIn);
            Add(request.OutTime, PunchType.Out);

            return await PushAsync(device, punches);
        }

        public async Task<SimulatorResultDto> RunScenarioAsync(RunScenarioRequestDto request, string tenantId)
        {
            var (device, mapping) = await ResolveAsync(request.DeviceId, request.EmployeeId, tenantId);

            var date = request.Date.Date;
            var punches = new List<PunchItemDto>();

            PunchItemDto P(DateTime dt, PunchType type) => new PunchItemDto
            {
                EmployeeCode = mapping.BiometricEmployeeCode,
                PunchTime = dt,
                PunchType = type
            };

            switch (request.ScenarioCode)
            {
                case "Normal":
                case "Holiday":
                case "WeeklyOff":
                case "Leave":
                    // Same "normal" punch pair - it's the Holiday/WeekOff/Leave
                    // modules (via the attendance engine) that are expected to
                    // reclassify the day, not the simulator.
                    punches.Add(P(date.AddHours(9), PunchType.In));
                    punches.Add(P(date.AddHours(18), PunchType.Out));
                    break;

                case "Late":
                    punches.Add(P(date.AddHours(9).AddMinutes(30), PunchType.In));
                    punches.Add(P(date.AddHours(18), PunchType.Out));
                    break;

                case "MultiplePunches":
                    punches.Add(P(date.AddHours(9), PunchType.In));
                    punches.Add(P(date.AddHours(13), PunchType.BreakOut));
                    punches.Add(P(date.AddHours(14), PunchType.BreakIn));
                    punches.Add(P(date.AddHours(18), PunchType.Out));
                    break;

                case "MissingOut":
                    punches.Add(P(date.AddHours(9), PunchType.In));
                    break;

                case "Duplicate":
                    var p = P(date.AddHours(9), PunchType.In);
                    punches.Add(p);
                    punches.Add(p);
                    break;

                case "NightShift":
                    punches.Add(P(date.AddHours(22), PunchType.In));
                    punches.Add(P(date.AddDays(1).AddHours(6), PunchType.Out));
                    break;

                case "Overtime":
                    punches.Add(P(date.AddHours(9), PunchType.In));
                    punches.Add(P(date.AddHours(20), PunchType.Out));
                    break;

                default:
                    throw new BadRequestException($"Unknown scenario '{request.ScenarioCode}'.", "UNKNOWN_SCENARIO");
            }

            return await PushAsync(device, punches);
        }

        public async Task<SimulatorResultDto> SimulateApiFailureAsync(string deviceId, string tenantId)
        {
            var device = await _db.BiometricDevices
                .FirstOrDefaultAsync(x => x.Id == deviceId &&
                    (string.IsNullOrEmpty(tenantId) || x.TenantId == tenantId));

            if (device == null)
                throw new NotFoundException("Simulator device not found.", "DEVICE_NOT_FOUND");

            // Deliberately wrong DeviceKey - exercises the real auth-rejection
            // path in IngestPunchesAsync rather than faking a response.
            return await PushAsync(device, new List<PunchItemDto>
            {
                new PunchItemDto
                {
                    EmployeeCode = "SIMULATED",
                    PunchTime = DateTime.Now,
                    PunchType = PunchType.In
                }
            }, overrideDeviceKey: true);
        }

        public async Task<SimulatorResultDto> SimulateDeviceOfflineAsync(string deviceId, string tenantId)
        {
            var device = await _db.BiometricDevices
                .FirstOrDefaultAsync(x => x.Id == deviceId &&
                    (string.IsNullOrEmpty(tenantId) || x.TenantId == tenantId));

            if (device == null)
                throw new NotFoundException("Simulator device not found.", "DEVICE_NOT_FOUND");

            var wasActive = device.IsActive;
            device.IsActive = false;

            try
            {
                await _db.SaveChangesAsync();

                // Real rejection path: IngestPunchesAsync refuses inactive devices.
                return await PushAsync(device, new List<PunchItemDto>
                {
                    new PunchItemDto
                    {
                        EmployeeCode = "SIMULATED",
                        PunchTime = DateTime.Now,
                        PunchType = PunchType.In
                    }
                });
            }
            finally
            {
                device.IsActive = wasActive;
                await _db.SaveChangesAsync();
            }
        }

        public async Task<int> ClearTestDataAsync(ClearTestDataRequestDto request, string tenantId)
        {
            var device = await _db.BiometricDevices
                .FirstOrDefaultAsync(x => x.Id == request.DeviceId &&
                    (string.IsNullOrEmpty(tenantId) || x.TenantId == tenantId));

            if (device == null)
                throw new NotFoundException("Simulator device not found.", "DEVICE_NOT_FOUND");

            var rawQuery = _db.BiometricAttendanceLogs
                .Where(x => x.DeviceId == device.Id);

            if (!string.IsNullOrEmpty(request.EmployeeId))
            {
                var code = await _db.EmployeeBiometricMappings
                    .Where(x => x.EmployeeId == request.EmployeeId)
                    .Select(x => x.BiometricEmployeeCode)
                    .FirstOrDefaultAsync();

                if (!string.IsNullOrEmpty(code))
                    rawQuery = rawQuery.Where(x => x.EmployeeCode == code);
            }

            var rawRows = await rawQuery.ToListAsync();

            // Also remove the Attendance/AttendanceLog rows this test data
            // produced, so re-running a scenario starts clean - but only ones
            // clearly attributable to this device (IsBiometricAttendance +
            // SourceDeviceId), never hand-entered/regularized attendance.
            var attendanceLogs = await _db.AttendanceLogs
                .Where(x => x.DeviceId == device.Id)
                .ToListAsync();

            var attendances = await _db.Attendances
                .Where(x => x.IsBiometricAttendance && x.SourceDeviceId == device.Id)
                .ToListAsync();

            _db.AttendanceLogs.RemoveRange(attendanceLogs);
            _db.Attendances.RemoveRange(attendances);
            _db.BiometricAttendanceLogs.RemoveRange(rawRows);

            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Simulator cleared test data for device {DeviceCode}: {RawCount} raw log(s), {AttCount} attendance day(s).",
                device.DeviceCode, rawRows.Count, attendances.Count);

            return rawRows.Count;
        }
    }
}
