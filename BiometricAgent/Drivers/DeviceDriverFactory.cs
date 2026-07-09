using Microsoft.Extensions.Logging;

namespace BiometricAgent.Drivers
{
    public static class DeviceDriverFactory
    {
        public static IDeviceDriver Create(DeviceOptions options, ILoggerFactory loggerFactory)
        {
            return options.DriverType.Trim().ToLowerInvariant() switch
            {
                "mock" => new MockDeviceDriver(loggerFactory.CreateLogger<MockDeviceDriver>()),
                "essl" => new EsslDeviceDriver(options, loggerFactory.CreateLogger<EsslDeviceDriver>()),
                _ => throw new NotSupportedException(
                    $"Unknown Agent:Device:DriverType '{options.DriverType}'. Use 'Essl' or 'Mock'.")
            };
        }
    }
}
