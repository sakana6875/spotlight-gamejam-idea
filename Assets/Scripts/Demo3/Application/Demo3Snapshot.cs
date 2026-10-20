using System;

namespace Healing.Demo3.Application
{
    /// <summary>
    /// Demo3 的检查点临时状态（Memento）。
    /// 只含可稳定序列化的数据：不含 GameObject / Transform / AudioSource。
    /// 由 CheckpointManager 收集、SaveManager 写入存档；
    /// 永久进度（Demo3 是否完成等）在 ProgressManager，不在此处。
    /// </summary>
    [Serializable]
    public class Demo3Snapshot
    {
        public float Elapsed;
        public float Pressure;
        public int CrowdCount;
        public float PlayerX;
        public float PlayerY;
    }
}
