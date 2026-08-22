using System.Reflection;
using BiometricAgent.Models;
using Microsoft.Extensions.Logging;

namespace BiometricAgent.Drivers
{
    /// <summary>
    /// Talks to an eSSL (or ZKTeco-derived, since most eSSL hardware ships
    /// the same underlying SDK under a different ProgID) attendance device
    /// over its vendor COM SDK.
    ///
    /// IMPORTANT - this must be adjusted per the actual SDK on the client's
    /// machine before going live:
    ///   1. Install the vendor's SDK (e.g. eSSL "eTimeTrackLite" bundles it,
    ///      or the standalone SDK CD/download that ships with the device).
    ///   2. Register the COM DLL: run `regsvr32 zkemkeeper.dll` (or whatever
    ///      the vendor's DLL is called) from an elevated x86 command prompt.
    ///   3. Confirm the ProgID in appsettings.json ("Agent:Device:ProgId")
    ///      matches what got registered - check HKEY_CLASSES_ROOT if unsure.
    ///   4. Confirm the method names below (Connect_Net, ReadGeneralLogData,
    ///      SSR_GetGeneralLogData, ...) match the SDK's documentation -
    ///      these are the standard ZK-family names, but some eSSL firmware
    ///      builds rename or reorder parameters.
    ///   5. Confirm the InOutMode -> PunchType mapping against how the
    ///      device is actually configured (In/Out/Break buttons vary by model).
    ///
    /// This uses late-bound COM (reflection) instead of a compiled COM
    /// interop reference, so the project builds without the vendor DLL
    /// present. It only works when actually run on Windows with the SDK
    /// registered.
    /// </summary>
    public class EsslDeviceDriver : IDeviceDriver
    {
        private readonly DeviceOptions _options;
        private readonly ILogger<EsslDeviceDriver> _logger;
        private object? _device;
        private Type? _deviceType;

        private const int MachineNumber = 1;

        public EsslDeviceDriver(DeviceOptions options, ILogger<EsslDeviceDriver> logger)
        {
            _options = options;
            _logger = logger;
        }

        public Task<bool> ConnectAsync(CancellationToken ct)
        {
            if (!OperatingSystem.IsWindows())
            {
                _logger.LogError("EsslDeviceDriver requires Windows (COM interop). Use the Mock driver for cross-platform testing.");
                return Task.FromResult(false);
            }

            try
            {
                _deviceType = Type.GetTypeFromProgID(_options.ProgId, throwOnError: false);

                if (_deviceType == null)
                {
                    _logger.LogError(
                        "COM ProgID '{ProgId}' is not registered on this machine. " +
                        "Install the vendor SDK and run regsvr32 on its DLL, then verify the ProgID.",
                        _options.ProgId);
                    return Task.FromResult(false);
                }

                _device = Activator.CreateInstance(_deviceType);

                if (_options.CommKey != 0)
                {
                    Invoke("SetCommPassword", _options.CommKey);
                }

                var connected = (bool)Invoke("Connect_Net", _options.IPAddress, _options.Port)!;

                if (!connected)
                {
                    _logger.LogWarning(
                        "Could not connect to biometric device at {Ip}:{Port}. " +
                        "Check network reachability, IP/port, and that no other " +
                        "application (e.g. the vendor's own attendance software) " +
                        "is holding the connection open.",
                        _options.IPAddress, _options.Port);
                }

                return Task.FromResult(connected);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to biometric device via COM SDK.");
                return Task.FromResult(false);
            }
        }

        public Task DisconnectAsync()
        {
            try
            {
                if (_device != null)
                    Invoke("Disconnect");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error while disconnecting from device.");
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Strong verification beyond "Connect_Net returned true": asks the
        /// device for its serial number, a real protocol round-trip that
        /// proves the device is actually responding, not just that a TCP
        /// socket opened. Standard ZK-family signature:
        /// bool GetSerialNumber(int MachineNumber, out string SerialNumber).
        /// Late-bound COM handles the `out string` via the array element
        /// being written back in place, same as SSR_GetGeneralLogData above.
        /// </summary>
        public Task<string?> TryGetDeviceInfoAsync(CancellationToken ct)
        {
            if (_device == null || _deviceType == null)
                return Task.FromResult<string?>(null);

            try
            {
                var args = new object[] { MachineNumber, "" };

                var ok = (bool)_deviceType.InvokeMember(
                    "GetSerialNumber",
                    BindingFlags.InvokeMethod,
                    null,
                    _device,
                    args)!;

                if (!ok)
                    return Task.FromResult<string?>(null);

                var serial = Convert.ToString(args[1]);

                return Task.FromResult<string?>(string.IsNullOrWhiteSpace(serial) ? null : $"Serial: {serial}");
            }
            catch (Exception ex)
            {
                // Best-effort only - some firmware/SDK builds don't expose
                // this call. Never treat a probe failure as a connection
                // failure; ConnectAsync already proved SDK-level
                // communication with the device.
                _logger.LogDebug(ex, "Device info probe (GetSerialNumber) unavailable or failed - continuing without it.");
                return Task.FromResult<string?>(null);
            }
        }

        public Task<List<PunchRecord>> GetNewPunchesAsync(DateTime since, CancellationToken ct)
        {
            var results = new List<PunchRecord>();

            if (_device == null)
                return Task.FromResult(results);

            try
            {
                // Loads all attendance log records from the device into an
                // internal read buffer that SSR_GetGeneralLogData then pages through.
                var loaded = (bool)Invoke("ReadGeneralLogData", MachineNumber)!;

                if (!loaded)
                {
                    _logger.LogWarning("Device returned no log data (ReadGeneralLogData failed).");
                    return Task.FromResult(results);
                }

                while (true)
                {
                    ct.ThrowIfCancellationRequested();

                    // Standard ZK-family signature:
                    // bool SSR_GetGeneralLogData(int machineNumber, out string enrollNumber,
                    //   out int verifyMode, out int inOutMode, out int year, out int month,
                    //   out int day, out int hour, out int minute, out int second, ref int workCode)
                    var args = new object[]
                    {
                        MachineNumber, "", 0, 0, 0, 0, 0, 0, 0, 0, 0
                    };

                    var more = (bool)_deviceType!.InvokeMember(
                        "SSR_GetGeneralLogData",
                        BindingFlags.InvokeMethod,
                        null,
                        _device,
                        args)!;

                    if (!more)
                        break;

                    var enrollNumber = Convert.ToString(args[1]) ?? "";
                    var inOutMode = Convert.ToInt32(args[3]);
                    var year = Convert.ToInt32(args[4]);
                    var month = Convert.ToInt32(args[5]);
                    var day = Convert.ToInt32(args[6]);
                    var hour = Convert.ToInt32(args[7]);
                    var minute = Convert.ToInt32(args[8]);
                    var second = Convert.ToInt32(args[9]);

                    DateTime punchTime;
                    try
                    {
                        punchTime = new DateTime(year, month, day, hour, minute, second);
                    }
                    catch
                    {
                        continue; // malformed record from the device buffer, skip it
                    }

                    if (punchTime <= since)
                        continue;

                    results.Add(new PunchRecord
                    {
                        EmployeeCode = enrollNumber,
                        PunchTime = punchTime,
                        PunchType = MapInOutMode(inOutMode)
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed while reading punches from device.");
            }

            return Task.FromResult(results);
        }

        /// <summary>
        /// Default ZK-family convention: 0=Check-In, 1=Check-Out, 2=Break-Out,
        /// 3=Break-In, 4/5=Overtime In/Out (treated as In/Out here).
        /// VERIFY against the client's actual device configuration - some
        /// deployments only ever use 0/1 and let the ERP's shift rules decide
        /// in/out, in which case simplify this mapping accordingly.
        /// </summary>
        private static PunchType MapInOutMode(int inOutMode) => inOutMode switch
        {
            0 => PunchType.In,
            1 => PunchType.Out,
            2 => PunchType.BreakOut,
            3 => PunchType.BreakIn,
            4 => PunchType.In,
            5 => PunchType.Out,
            _ => PunchType.In
        };

        private object? Invoke(string method, params object[] args)
        {
            return _deviceType!.InvokeMember(
                method,
                BindingFlags.InvokeMethod,
                null,
                _device,
                args);
        }

        public void Dispose()
        {
            if (_device != null && OperatingSystem.IsWindows())
            {
                try
                {
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(_device);
                }
                catch
                {
                    // best effort
                }
            }
        }
    }
}
