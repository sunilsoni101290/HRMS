using System.Text.Json;
using Microsoft.Extensions.Options;

namespace BiometricAgent.Services
{
    /// <summary>
    /// Tracks, per device, the timestamp of the last punch successfully
    /// handed to the API, persisted to disk so a service restart doesn't
    /// re-read a device's entire log history from the beginning. Keyed by
    /// DeviceCode so one agent can track multiple assigned devices
    /// independently (one state file per device).
    /// </summary>
    public class SyncState
    {
        private readonly string _folder;
        private readonly object _lock = new();

        public SyncState(IOptions<AgentOptions> options)
        {
            _folder = options.Value.StateFolder;
            Directory.CreateDirectory(_folder);
        }

        public DateTime GetLastSyncedTime(string deviceCode)
        {
            var path = FilePath(deviceCode);

            if (!File.Exists(path))
                return DateTime.Now.AddDays(-1); // first run for this device: only pull the last day of logs

            try
            {
                lock (_lock)
                {
                    var json = File.ReadAllText(path);
                    var data = JsonSerializer.Deserialize<StateFile>(json);
                    return data?.LastPunchTime ?? DateTime.Now.AddDays(-1);
                }
            }
            catch
            {
                return DateTime.Now.AddDays(-1);
            }
        }

        public void SetLastSyncedTime(string deviceCode, DateTime value)
        {
            var json = JsonSerializer.Serialize(new StateFile { LastPunchTime = value });

            lock (_lock)
            {
                File.WriteAllText(FilePath(deviceCode), json);
            }
        }

        private string FilePath(string deviceCode) =>
            Path.Combine(_folder, $"last-sync-{SanitizeForFileName(deviceCode)}.json");

        private static string SanitizeForFileName(string value)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                value = value.Replace(c, '_');
            return value;
        }

        private class StateFile
        {
            public DateTime LastPunchTime { get; set; }
        }
    }
}
