using System.Text.Json;

namespace BiometricAgent.Services
{
    /// <summary>
    /// Tracks the timestamp of the last punch successfully handed to the API,
    /// persisted to disk so a service restart doesn't re-read the device's
    /// entire log history from the beginning.
    /// </summary>
    public class SyncState
    {
        private readonly string _filePath;

        public SyncState(AgentOptions options)
        {
            Directory.CreateDirectory(options.StateFolder);
            _filePath = Path.Combine(options.StateFolder, "last-sync.json");
        }

        public DateTime GetLastSyncedTime()
        {
            if (!File.Exists(_filePath))
                return DateTime.Now.AddDays(-1); // first run: only pull the last day of logs

            try
            {
                var json = File.ReadAllText(_filePath);
                var data = JsonSerializer.Deserialize<StateFile>(json);
                return data?.LastPunchTime ?? DateTime.Now.AddDays(-1);
            }
            catch
            {
                return DateTime.Now.AddDays(-1);
            }
        }

        public void SetLastSyncedTime(DateTime value)
        {
            var json = JsonSerializer.Serialize(new StateFile { LastPunchTime = value });
            File.WriteAllText(_filePath, json);
        }

        private class StateFile
        {
            public DateTime LastPunchTime { get; set; }
        }
    }
}
