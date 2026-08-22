using BiometricAgent.Models;
using Microsoft.Extensions.Logging;

namespace BiometricAgent.Drivers
{
    /// <summary>
    /// Fake driver that generates a punch every polling cycle. Use this
    /// (DriverType = "Mock" in appsettings.json) to verify the agent's
    /// offline queue, API push, and Windows Service install/logging all
    /// work end-to-end before wiring up real hardware.
    /// </summary>
    public class MockDeviceDriver : IDeviceDriver
    {
        private readonly ILogger<MockDeviceDriver> _logger;
        private readonly Random _random = new();

        public MockDeviceDriver(ILogger<MockDeviceDriver> logger)
        {
            _logger = logger;
        }

        public Task<bool> ConnectAsync(CancellationToken ct)
        {
            _logger.LogInformation("[Mock] Pretending to connect to a biometric device.");
            return Task.FromResult(true);
        }

        public Task DisconnectAsync()
        {
            _logger.LogInformation("[Mock] Pretending to disconnect.");
            return Task.CompletedTask;
        }

        public Task<string?> TryGetDeviceInfoAsync(CancellationToken ct)
        {
            _logger.LogInformation("[Mock] Pretending to read device serial number.");
            return Task.FromResult<string?>("MOCK-SERIAL-0001");
        }

        public Task<List<PunchRecord>> GetNewPunchesAsync(DateTime since, CancellationToken ct)
        {
            var punch = new PunchRecord
            {
                EmployeeCode = $"EMP{_random.Next(1, 5):000}",
                PunchTime = DateTime.Now,
                PunchType = _random.Next(0, 2) == 0 ? PunchType.In : PunchType.Out
            };

            _logger.LogInformation("[Mock] Generated test punch: {Code} {Type} {Time}",
                punch.EmployeeCode, punch.PunchType, punch.PunchTime);

            return Task.FromResult(new List<PunchRecord> { punch });
        }

        public void Dispose() { }
    }
}
