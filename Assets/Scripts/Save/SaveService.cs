using System.Collections.Generic;
using System.Linq;
using Spotlight.Domain.Save;

namespace Spotlight.Save
{
    /// <summary>
    /// 提供当前 GameJam 所需的内存存档和永久进度；不提供进程外持久化保证。
    /// </summary>
    public sealed class SaveService
    {
        private const int CurrentSaveVersion = 1;
        private readonly SaveContainer _container;

        public bool HasSave => _container.Snapshots.Count > 0;
        public SettingsData Settings => _container.Settings;
        public PermanentProgress PermanentProgress => _container.PermanentProgress;

        public SaveService()
        {
            _container = new SaveContainer(CurrentSaveVersion, CreateDefaultSettings());
        }

        public bool IsDemoUnlocked(string demoId)
        {
            return _container.PermanentProgress.IsDemoUnlocked(demoId);
        }

        public bool IsDemoCompleted(string demoId)
        {
            return _container.PermanentProgress.IsDemoCompleted(demoId);
        }
        public bool HasStoryFlag(string flagId)
        {
            return _container.PermanentProgress.HasStoryFlag(flagId);
        }

        public ProgressResult UnlockDemo(string demoId)
        {
            return UpdateProgress(demoId, _container.PermanentProgress.SetDemoUnlocked(demoId));
        }

        public ProgressResult CompleteDemo(string demoId)
        {
            return UpdateProgress(demoId, _container.PermanentProgress.SetDemoCompleted(demoId));
        }

        public ProgressResult SetStoryFlag(string flagId)
        {
            return UpdateProgress(flagId, _container.PermanentProgress.SetStoryFlag(flagId));
        }

        private ProgressResult UpdateProgress(string id, bool changed)
        {
            return new ProgressResult(!string.IsNullOrWhiteSpace(id), changed);
        }

        public SaveResult SaveCheckpoint(SaveSnapshot snapshot)
        {
            if (snapshot == null || snapshot.Checkpoint == null
                || string.IsNullOrWhiteSpace(snapshot.CheckpointId)
                || string.IsNullOrWhiteSpace(snapshot.Checkpoint.SceneId))
            {
                return new SaveResult(SaveResultCode.Invalid);
            }

            _container.SetSnapshot(snapshot.Clone());
            return new SaveResult(SaveResultCode.Success);
        }

        public SnapshotResult LoadLatestSnapshot()
        {
            if (_container.Snapshots.Count == 0)
            {
                return new SnapshotResult(SaveResultCode.Empty, null);
            }

            SaveSnapshot latest = _container.Snapshots.Values
                .OrderByDescending(snapshot => snapshot.SavedAtUnixSeconds)
                .ThenBy(snapshot => snapshot.CheckpointId)
                .First();
            return new SnapshotResult(SaveResultCode.Success, latest.Clone());
        }

        public SnapshotResult LoadSnapshot(string checkpointId)
        {
            if (string.IsNullOrWhiteSpace(checkpointId))
            {
                return new SnapshotResult(SaveResultCode.Invalid, null);
            }

            if (!_container.TryGetSnapshot(checkpointId, out SaveSnapshot snapshot))
            {
                return new SnapshotResult(SaveResultCode.NotFound, null);
            }

            return new SnapshotResult(SaveResultCode.Success, snapshot.Clone());
        }

        public SnapshotListResult ListSnapshots()
        {
            List<SaveSnapshot> snapshots = _container.Snapshots.Values
                .OrderByDescending(snapshot => snapshot.SavedAtUnixSeconds)
                .ThenBy(snapshot => snapshot.CheckpointId)
                .Select(snapshot => snapshot.Clone())
                .ToList();

            if (snapshots.Count == 0)
            {
                return new SnapshotListResult(SaveResultCode.Empty, snapshots);
            }

            return new SnapshotListResult(SaveResultCode.Success, snapshots);
        }

        public SaveResult SaveSettings(SettingsData settings)
        {
            if (settings == null)
            {
                return new SaveResult(SaveResultCode.Invalid);
            }

            _container.SetSettings(settings.Clone());
            return new SaveResult(SaveResultCode.Success);
        }

        public SaveResult ResetSave()
        {
            _container.ClearSnapshots();
            _container.PermanentProgress.Clear();
            _container.SetSettings(CreateDefaultSettings());
            return new SaveResult(SaveResultCode.Success);
        }

        private static SettingsData CreateDefaultSettings()
        {
            return new SettingsData(1f, 1f, 1f, 1f);
        }
    }
}
