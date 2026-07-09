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
        /// Returns punches recorded strictly after <paramref name="since"/>.
        /// Implementations should read all logs and filter, since most of
        /// these device SDKs don't support server-side "since" filtering.
        /// </summary>
        Task<List<PunchRecord>> GetNewPunchesAsync(DateTime since, CancellationToken ct);
    }
}
