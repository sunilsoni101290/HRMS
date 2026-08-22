using BiometricAgent.Models;

namespace BiometricAgent.Drivers
{
    /// <summary>
    /// Abstraction over "however we talk to the physical device". Different
    /// brands (eSSL, ZKTeco, Realtime, Mantra...) expose different SDKs, so
    /// swapping the driver is the only thing that should change if the client
    /// has different hardware - Worker/ApiClient/OfflineQueue stay the same.
    /// </summary>
    public interface IDeviceDriver : IDisposable
    {
        Task<bool> ConnectAsync(CancellationToken ct);

        Task DisconnectAsync();

        /// <summary>
        /// Best-effort lightweight, non-destructive device probe (serial
        /// number / firmware / product code - whatever the SDK exposes
        /// cheaply) used ONLY to strengthen a Test Connection result beyond
        /// "the socket/SDK handshake opened". Caller must already be
        /// connected (call after a successful ConnectAsync, before
        /// DisconnectAsync). Must NOT read/modify attendance logs or device
        /// configuration.
        ///
        /// Returns null if the probe isn't supported by this driver/firmware
        /// or fails for any reason - callers must treat that as "no extra
        /// info available", NOT as a connection failure, since ConnectAsync
        /// succeeding already proved real SDK-level device communication.
        /// </summary>
        Task<string?> TryGetDeviceInfoAsync(CancellationToken ct);

        /// <summary>
        /// Returns punches recorded strictly after <paramref name="since"/>.
        /// Implementations should read all logs and filter, since most of
        /// these device SDKs don't support server-side "since" filtering.
        /// </summary>
        Task<List<PunchRecord>> GetNewPunchesAsync(DateTime since, CancellationToken ct);
    }
}
