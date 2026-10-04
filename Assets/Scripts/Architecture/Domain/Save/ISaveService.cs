using System.Collections.Generic;

namespace Spotlight.Domain.Save
{
    /// <summary>
    /// 提供检查点、快照列表、设置和存档容器生命周期操作。
    /// </summary>
    public interface ISaveService
    {
        bool HasSave { get; }
        SettingsData Settings { get; }
        SaveResult SaveCheckpoint(SaveSnapshot snapshot);
        SnapshotResult LoadLatestSnapshot();
        SnapshotResult LoadSnapshot(string checkpointId);
        SnapshotListResult ListSnapshots();
        SaveResult SaveSettings(SettingsData settings);
        SaveResult ResetSave();
    }

    /// <summary>
    /// 提供跨检查点保留的 Demo 和剧情进度操作。
    /// </summary>
    public interface IProgressService
    {
        bool IsDemoUnlocked(string demoId);
        bool IsDemoCompleted(string demoId);
        bool HasStoryFlag(string flagId);
        ProgressResult UnlockDemo(string demoId);
        ProgressResult CompleteDemo(string demoId);
        ProgressResult SetStoryFlag(string flagId);
    }
}
