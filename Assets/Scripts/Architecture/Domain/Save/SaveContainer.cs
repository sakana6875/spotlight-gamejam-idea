using System.Collections.Generic;

namespace Spotlight.Domain.Save
{
    /// <summary>
    /// 聚合永久进度、设置和检查点快照；不包含 Unity 对象或文件系统状态。
    /// </summary>
    public sealed class SaveContainer
    {
        private readonly Dictionary<string, SaveSnapshot> _snapshots;

        public int SaveVersion { get; }
        public PermanentProgress PermanentProgress { get; }
        public SettingsData Settings { get; private set; }
        public IReadOnlyDictionary<string, SaveSnapshot> Snapshots => _snapshots;

        public SaveContainer(int saveVersion, SettingsData settings)
        {
            SaveVersion = saveVersion;
            PermanentProgress = new PermanentProgress();
            Settings = settings;
            _snapshots = new Dictionary<string, SaveSnapshot>();
        }

        public void SetSettings(SettingsData settings)
        {
            Settings = settings;
        }

        public void SetSnapshot(SaveSnapshot snapshot)
        {
            _snapshots[snapshot.CheckpointId] = snapshot;
        }

        public bool TryGetSnapshot(string checkpointId, out SaveSnapshot snapshot)
        {
            return _snapshots.TryGetValue(checkpointId, out snapshot);
        }

        public void ClearSnapshots()
        {
            _snapshots.Clear();
        }
    }
}
