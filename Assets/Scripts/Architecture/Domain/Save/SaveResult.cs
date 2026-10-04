namespace Spotlight.Domain.Save
{
    /// <summary>
    /// 表示存档或进度操作的可观察结果。
    /// </summary>
    public enum SaveResultCode
    {
        Success,
        Empty,
        NotFound,
        Invalid
    }

    /// <summary>
    /// 不携带业务数据的存档操作结果。
    /// </summary>
    public readonly struct SaveResult
    {
        public SaveResultCode Code { get; }

        public bool IsSuccess => Code == SaveResultCode.Success;

        public SaveResult(SaveResultCode code)
        {
            Code = code;
        }
    }

    /// <summary>
    /// 携带快照的读取结果；未找到或空存档时 Snapshot 为 null。
    /// </summary>
    public readonly struct SnapshotResult
    {
        public SaveResultCode Code { get; }
        public SaveSnapshot Snapshot { get; }

        public bool IsSuccess => Code == SaveResultCode.Success;

        public SnapshotResult(SaveResultCode code, SaveSnapshot snapshot)
        {
            Code = code;
            Snapshot = snapshot;
        }
    }

    /// <summary>
    /// 携带快照列表的读取结果。
    /// </summary>
    public readonly struct SnapshotListResult
    {
        public SaveResultCode Code { get; }
        public System.Collections.Generic.IReadOnlyList<SaveSnapshot> Snapshots { get; }

        public bool IsSuccess => Code == SaveResultCode.Success;

        public SnapshotListResult(
            SaveResultCode code,
            System.Collections.Generic.IReadOnlyList<SaveSnapshot> snapshots)
        {
            Code = code;
            Snapshots = snapshots;
        }
    }

    /// <summary>
    /// 表示永久进度更新是否改变了状态。
    /// </summary>
    public readonly struct ProgressResult
    {
        public bool Succeeded { get; }
        public bool Changed { get; }

        public ProgressResult(bool succeeded, bool changed)
        {
            Succeeded = succeeded;
            Changed = changed;
        }
    }
}
